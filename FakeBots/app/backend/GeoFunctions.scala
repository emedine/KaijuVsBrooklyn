package backend


import backend.geomodels._

import scala.collection.JavaConversions._

/**
  * Geo functions.
  */
class GeoFunctions(val settings: SettingsImpl) {
  /**
    * Get the region for the given point.
    *
    * @param point The point.
    * @return The id of the region at the given zoom depth.
    */
  def regionForPoint(point: LatLng): RegionId = regionForPoint(point, settings.MaxZoomDepth)

  /**
    * Get the region for the given point.
    *
    * @param point     The point.
    * @param zoomDepth The zoom depth.
    * @return The id of the region at the given zoom depth.
    */
  def regionForPoint(point: LatLng, zoomDepth: Int): RegionId = {
    assert(zoomDepth <= settings.MaxZoomDepth)
    val axisSteps: Long = 1l << zoomDepth
    val xStep: Double = 360d / axisSteps
    val x: Int = Math.floor((point.getLng + 180) / xStep).toInt
    val yStep: Double = 180d / axisSteps
    val y: Int = Math.floor((point.getLat + 90) / yStep).toInt
    RegionId(zoomDepth, x, y)
  }

  /**
    * Get the regions for the given bounding box.
    *
    * @param bbox The bounding box.
    * @return The regions
    */
  def regionsForBoundingBox(bbox: BoundingBox): Set[RegionId] = regionsAtZoomLevel(bbox, settings.MaxZoomDepth)

  private def regionsAtZoomLevel(bbox: BoundingBox, zoomLevel: Int): Set[RegionId] = if (zoomLevel == 0) Set(RegionId(0, 0, 0))
  else {
    val axisSteps: Int = 1 << zoomLevel
    // First, we get the regions that the bounds are in
    val southWestRegion: RegionId = regionForPoint(bbox.getSouthWest, zoomLevel)
    val northEastRegion: RegionId = regionForPoint(bbox.getNorthEast, zoomLevel)
    // Now calculate the width of regions we need, we need to add 1 for it to be inclusive of both end regions
    val xLength: Int = northEastRegion.x - southWestRegion.x + 1
    val yLength: Int = northEastRegion.y - southWestRegion.y + 1
    // Check if the number of regions is in our bounds
    val numRegions: Int = xLength * yLength
    if (numRegions <= 0) Set(RegionId(0, 0, 0))
    else if (settings.MaxSubscriptionRegions >= numRegions) {
      (0 until numRegions).map { i =>
        val y: Int = i / xLength
        val x: Int = i % xLength
        // We need to mod positive the x value, because it's possible that the bounding box started or ended
        // from less than -180 or greater than 180 W/E.
        RegionId(zoomLevel, modPositive(southWestRegion.x + x, axisSteps), southWestRegion.y + y)
      }.toSet
    }
    else regionsAtZoomLevel(bbox, zoomLevel - 1)
  }

  /**
    * Get the bounding box for the given region.
    */
  def boundingBoxForRegion(regionId: RegionId): BoundingBox = {
    val axisSteps: Long = 1l << regionId.zoomLevel
    val yStep: Double = 180d / axisSteps
    val xStep: Double = 360d / axisSteps
    val latRegion: Double = regionId.y * yStep - 90
    val lngRegion: Double = regionId.x * xStep - 180
    new BoundingBox(new LatLng(latRegion, lngRegion), new LatLng(latRegion + yStep, lngRegion + xStep))
  }

  def summaryRegionForRegion(regionId: RegionId): Option[RegionId] =
    if (regionId.zoomLevel == 0) None
    else Some(RegionId(regionId.zoomLevel - 1, regionId.x >>> 1, regionId.y >>> 1))

  /**
    * Cluster the given points into n2 boxes
    *
    * @param id     The id of the region
    * @param bbox   The bounding box within which to cluster
    * @param points The points to cluster
    * @return The clustered points
    */

  def cluster(id: String, bbox: BoundingBox, points: List[PointOfInterest]): List[PointOfInterest] = {
    if (points.size > settings.ClusterThreshold)
      groupNBoxes(bbox, settings.ClusterDimension, points)
        .filter(_._2.nonEmpty)
        .map(cluster => {
          if (cluster._2.size == 1) cluster._2.head
          else {

            val result = cluster._2.toStream.map(point => {
              val count = point match {
                case c: Cluster => c.getCount
                case _ => 1
              }
              // Normalise to a 0-360 based version of longitude
              val normalisedWest = modPositive(point.position.getLng + 180, 360)
              (point.position.getLat * count, normalisedWest * count, count)
            }).fold((0d, 0d, 0))((a, b) => (a._1 + b._1, a._2 + b._2, a._3 + b._3))

            // Compute averages
            val count = result._3
            Cluster(
              s"$id-${cluster._1}",
              System.currentTimeMillis(),
              new LatLng(result._1 / count, (result._2 / count) - 180),
              count)
          }
        }).toList
    else points
  }

  /**
    * Group the positions into n2 boxes
    *
    * @param bbox      The bounding box
    * @param positions The positions to group
    * @return The grouped positions
    */
  private def groupNBoxes(bbox: BoundingBox, n: Int, positions: List[PointOfInterest]): Stream[(Int, List[PointOfInterest])] = {
    val boxes = new Array[(Int, List[PointOfInterest])](n * n)
    positions.toStream.foreach(pos => {
      val segment =
        latitudeSegment(n, bbox.getSouthWest.getLat, bbox.getNorthEast.getLat, pos.position.getLat) * n +
          longitudeSegment(n, bbox.getSouthWest.getLng, bbox.getNorthEast.getLng, pos.position.getLng)
      boxes(segment)._2.add(pos)
    })
    boxes.toStream
  }

  /**
    * Find the segment that the point lies in in the given south/north range
    *
    * @return A number from 0 to n - 1
    */
  def latitudeSegment(n: Int, south: Double, north: Double, point: Double): Int = {
    // Normalise so that the southern most point is 0
    val range = north - south
    val normalisedPoint = point - south
    val segment = Math.floor(normalisedPoint * (n / range)).toInt

    // If the point was never in the given range default to 0.
    if (segment >= n || segment < 0) 0 else segment
  }

  /**
    * Find the segment that the point lies in in the given west/east range
    *
    * @return A number from 0 to n - 1
    */
  def longitudeSegment(n: Int, west: Double, east: Double, point: Double): Int = {
    // Normalise so that the western most point is 0, taking into account the 180 cut over
    val range = modPositive(east - west, 360)
    val normalisedPoint = modPositive(point - west, 360)
    val segment = Math.floor(normalisedPoint * (n / range)).toInt

    // The point was never in the given range.  Default to 0.
    if (segment >= n || segment < 0) 0 else segment
  }

  /**
    * Modulo function that always returns a positive number
    */
  def modPositive(x: Double, y: Int): Double = {
    val mod = x % y
    if (mod > 0) mod else mod + y
  }

  /**
    * Modulo function that always returns a positive number
    */
  def modPositive(x: Int, y: Int): Int = {
    val mod: Int = x % y
    if (mod > 0) mod else mod + y
  }

  def distanceBetweenPoints(pointA: LatLng, pointB: LatLng): Double = {
    // Setup the inputs to the formula
    val R = 6371009d // average radius of the earth in metres
    val dLat = Math.toRadians(pointB.getLat - pointA.getLat)
    val dLng = Math.toRadians(pointB.getLng - pointA.getLng)
    val latA = Math.toRadians(pointA.getLat)
    val latB = Math.toRadians(pointB.getLat)
    // The actual haversine formula. a and c are well known value names in the formula.
    val a = Math.sin(dLat / 2) * Math.sin(dLat / 2) + Math.sin(dLng / 2) * Math.sin(dLng / 2) * Math.cos(latA) * Math.cos(latB)
    val c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a))
    val distance = R * c
    distance
  }
}
