package backend

import akka.actor._
import akka.routing.FromConfig
import models.backend.{RegionId, RegionPoints}

/**
  * Handles instantiating region and summary region actors when data arrives for them, if they don't already exist.
  * It also routes the `RegionPoints` from child `Region` or `SummaryRegion` to the node
  * responsible for the target region.
  */
class RegionManager extends Actor {
  val regionManagerRouter: ActorRef = context.actorOf(Props.empty.withRouter(FromConfig.getInstance), "router")


  //TODO change settings impl to scala
  val settings: SettingsImpl = Settings.SettingsProvider.get(context.system)

  def receive = {
    case UpdateUserPositionWithRegion(regionId, userPosition) =>
      getRegionActor(regionId, Props(classOf[Region], regionId)) ! userPosition

    case UpdateRegionPoints(regionId:RegionId, regionPoints: RegionPoints) =>
      getRegionActor(regionId, Props(classOf[SummaryRegion], regionId)) ! regionPoints

    //TODO change Geofunctions to scala
    case RegionPoints(regionId, points: RegionPoints) => {
      val summaryRegionId = settings.GeoFunctions.summaryRegionForRegion(points.regionId).asInstanceOf[Option[RegionId]]

        if(summaryRegionId.nonEmpty) regionManagerRouter ! UpdateRegionPoints(summaryRegionId.get, points)
    }
  }

  //TODO: Change Region ID to scala
  def getRegionActor(regionId: RegionId, props: Props): ActorRef = {
    val maybeChild = context.child(regionId.name)

    if(maybeChild.isDefined) maybeChild.get else context.actorOf(props)
  }
}