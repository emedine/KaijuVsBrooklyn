package backend.models

import akka.routing.ConsistentHashingRouter.ConsistentHashable
import backend.geomodels.{RegionId, RegionPoints, UserPosition}

trait RegionManagerProtocol {
  def regionId: RegionId

  def consistentHashKey = regionId.name
}

case class UpdateUserPositionWithRegion(regionId: RegionId, userPosition: UserPosition) extends RegionManagerProtocol with ConsistentHashable

case class UpdateRegionPoints(regionId: RegionId, regionPoints: RegionPoints) extends RegionManagerProtocol with ConsistentHashable
