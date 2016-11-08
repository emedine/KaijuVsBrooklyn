package backend.geomodels
/**
  * The points of interest for a given regionId.
  */
case class RegionPoints(regionId: RegionId, points: java.util.List[PointOfInterest])
