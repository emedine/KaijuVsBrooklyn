package actors

import akka.actor._
import backend._
import backend.UpdateUserPosition
import akka.routing.FromConfig
import models.backend.RegionId

/**
  * A client for the region manager, handles routing of position updates to the
  * regionManager on the right backend node.
  */

/*
class RegionManagerClient extends Actor {
  def receive = {
    case UserPosition()
  }
}*/
