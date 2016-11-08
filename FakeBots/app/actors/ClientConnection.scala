package actors

import akka.actor.Actor.Receive
import akka.actor.{Actor, ActorRef, Props}
import com.fasterxml.jackson.databind.JsonNode
import play.api.libs.json.JsValue

/*
package actors

/**
  * Created by lerenzo on 11/3/16.
  */
package actors

import akka.actor._
import actors.PositionSubscriberProtocol.PositionSubscriberUpdate
import com.fasterxml.jackson.databind.JsonNode
import models.backend._
import actors.ClientConnectionProtocol._
import org.geojson.Feature
import org.geojson.FeatureCollection
import org.geojson.Point
import play.libs.Json
import java.util.stream.Collectors

import backend.geomodels.{BoundingBox, LatLng, UserPosition}

/**
  * Represents a client connection
  */
object ClientConnection {
  /**
    * @param email               The email address of the client
    * @param upstream            The upstream actor to send to
    * @param regionManagerClient The region manager client to send updates to
    */
  def props(email: String, upstream: ActorRef, regionManagerClient: ActorRef): Props = return Props.create(classOf[ClientConnection], () -> new ClientConnection(email, upstream, regionManagerClient))
}

class ClientConnection private(val email: String, val upstream: ActorRef, val regionManagerClient: ActorRef) extends UntypedActor {
  this.subscriber = getContext.actorOf(PositionSubscriber.props(self), "positionSubscriber")
  final private var subscriber: ActorRef = null

  @throws[Exception]
  def onReceive(msg: Any) {
    if (msg.isInstanceOf[JsonNode]) {
      val event: ClientConnectionProtocol.ClientEvent = Json.fromJson(msg.asInstanceOf[JsonNode], classOf[ClientConnectionProtocol.ClientEvent])
      if (event.isInstanceOf[ClientConnectionProtocol.UserMoved]) {
        val userMoved: ClientConnectionProtocol.UserMoved = event.asInstanceOf[ClientConnectionProtocol.UserMoved]
        regionManagerClient.tell(new UserPosition(email, System.currentTimeMillis, LatLng.fromLngLatAlt(userMoved.getPosition.getCoordinates)), self)
      }
      else if (event.isInstanceOf[ClientConnectionProtocol.ViewingArea]) {
        val viewingArea: ClientConnectionProtocol.ViewingArea = event.asInstanceOf[ClientConnectionProtocol.ViewingArea]
        subscriber.tell(BoundingBox.fromBbox(viewingArea.getArea.getBbox), self)
      }
    }
    else if (msg.isInstanceOf[PositionSubscriberProtocol.PositionSubscriberUpdate]) {
      val update: PositionSubscriberProtocol.PositionSubscriberUpdate = msg.asInstanceOf[PositionSubscriberProtocol.PositionSubscriberUpdate]
      val collection: FeatureCollection = new FeatureCollection
      collection.setFeatures(update.getUpdates.stream.map(pos -> {
        Feature feature = new Feature();
        Point point = new Point();

        point.setCoordinates(pos.position().toLngLatAlt());
        feature.setGeometry(point);

        feature.setId(pos.id());
        feature.setProperty("timestamp", pos.timestamp());
        if (pos instanceof Cluster) feature.setProperty("count", ((Cluster) pos).count())

        return feature;
      }).collect(Collectors.toList))
      update.getArea.ifPresent(bbox -> collection.setBbox(bbox.toBbox()))
      upstream.tell(Json.toJson(new ClientConnectionProtocol.UserPositions(collection)), self)
    }
  }
}
*/


class ClientConnection(out: ActorRef, regionManagerClient: ActorRef) extends Actor {
  println("just made a clientConnection yas!")

  override def receive: Receive = {
    case json: JsValue =>
      println("yaaaaas just received a json node")
      out ! s"just got the json message: ${json.toString}"

    case s: String =>
      println("just got a string message" + s)
      out ! s"just got the string message: $s"

    case n: Int =>
      println("just got a number")
      out ! s"just got the number $n"
  }
}


object ClientConnection {
  def props(out: ActorRef, regionManagerClient: ActorRef): Props = Props(new ClientConnection(out, regionManagerClient))
}
