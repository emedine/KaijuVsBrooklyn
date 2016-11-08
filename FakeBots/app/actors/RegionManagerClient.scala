package actors

/**
  * Created by lerenzo on 11/3/16.
  */

import akka.actor.Actor.Receive
import akka.actor.{Actor, ActorRef, Props, UntypedActor}
import akka.routing.FromConfig
import backend.geomodels.UserPosition
import backend.models.UpdateUserPositionWithRegion
import backend.{Settings, SettingsImpl}

/**
  * A client for the region manager, handles routing of position updates to the
  * regionManager on the right backend node.
  */
//TODO: 10/28/16 Change this to Scala
//TODO: 10/28/16 Understand and move to a separate application folder
object RegionManagerClient {
  def props: Props = Props[RegionManagerClient]
}

class RegionManagerClient extends Actor {
  //final private val regionManagerRouter: ActorRef = getContext.actorOf(Props.empty.withRouter(FromConfig.getInstance), "router")
  //final private val settings: appSettings.SettingsImpl = appSettings.Settings.SettingsProvider.get(getContext.system)

  val regionManagerRouter: ActorRef = context.actorOf(Props.empty.withRouter(FromConfig.getInstance), "router")
  val settings: SettingsImpl = Settings.SettingsProvider.get(context.system)

  /*@throws[Exception]
  def onReceive(msg: Any) {
    if (msg.isInstanceOf[UserPosition]) {
      val pos: UserPosition = msg.asInstanceOf[UserPosition]
      val regionId: RegionId = settings.appSettings.GeoFunctions.regionForPoint(pos.position)
      regionManagerRouter.tell(new UpdateUserPositionWithRegion(regionId, pos), self)
    }
  }*/

  override def receive: Receive = {
    case "yas" => sender ! "yas"
    case UserPosition(id, timestamp, position) => {
      val regionId = settings.GeoFunctions.regionForPoint(position)
      regionManagerRouter ! UpdateUserPositionWithRegion(regionId, UserPosition(id, timestamp, position))
    }
  }
}
