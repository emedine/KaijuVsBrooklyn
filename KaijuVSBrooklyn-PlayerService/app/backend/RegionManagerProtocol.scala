package backend

import akka.routing.ConsistentHashingRouter.ConsistentHashable
import models.backend.RegionId
import models.backend.RegionPoints
import models.backend.UserPosition

trait RegionManagerProtocol {
  def regionId: RegionId

  def consistentHashKey = regionId.getName
}

case class UpdateUserPositionWithRegion(regionId: RegionId, userPosition: UserPosition) extends RegionManagerProtocol with ConsistentHashable

case class UpdateRegionPoints(regionId: RegionId, regionPoints: RegionPoints) extends RegionManagerProtocol with ConsistentHashable