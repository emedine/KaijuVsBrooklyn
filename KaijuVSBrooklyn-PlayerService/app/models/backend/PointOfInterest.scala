package models.backend

/**
  * A point of interest, either a user position or a cluster of positions
  */

sealed trait PointOfInterest {
  def id: String

  def timestamp: Long

  def position: LatLng

}

case class UserPosition(id: String, timestamp: Long, position: LatLng) extends PointOfInterest

case class Cluster(id: String, timestamp: Long, position: LatLng, count: Int) extends PointOfInterest {
  def getCount = count
}
