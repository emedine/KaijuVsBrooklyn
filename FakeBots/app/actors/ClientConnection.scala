package actors

import java.time.ZonedDateTime
import java.util.concurrent.Executor

import actors.BotManager.{GetBots, RefreshBots}
import actors.ClientConnection.JSONUser
import actors.UserManager.{AddUser, GetUsers, UpdateUser, User}
import akka.actor.{Actor, ActorRef, Cancellable, Props, Scheduler}
import akka.pattern.ask
import org.geojson.Point
import ClientConnection._
import akka.actor.Actor.Receive
import akka.util.Timeout

import scala.concurrent._
import scala.concurrent.duration._
import scala.util.parsing.json.{JSON, JSONArray, JSONObject, JSONType}
import scala.concurrent.ExecutionContext.Implicits.global
import scala.util.Success


class ClientConnection(out: ActorRef, scheduler: Scheduler, isBotsList: Boolean, userManager: ActorRef, botManager: ActorRef) extends Actor {


  override def receive: Receive = {

    case s: String =>
      JSON.parseRaw(s) collect {
        case json: JSONType => json match { case ob: JSONObject => ob.obj.get("event") match {

          //Player Events
          case Some(JSONConstants.CreatePlayer) =>
            implicit val timeout: Timeout = 10.second
            val data = ob.obj.get("data").collect{ case o: JSONObject => o }
            val name: Option[String] = data.flatMap(_.obj.get("name").map { case s:String => s })
            val user = userManager ? AddUser(name)
            for(u <- user.mapTo[User]){
              out !
                raw"""
            |        {
            |           "event": "${JSONConstants.PlayerAdded}",
            |           "data": {
            |             "id": "${u.id}"
            |        }} """.stripMargin
            }

          //TODO get geolocation and use that for the location of the user
          case Some(JSONConstants.UpdatePlayer) =>
            val data: Option[Map[String, Any]] = ob.obj.get("data").collect{ case o: JSONObject => o.obj }
            val id = data.flatMap(_.get("id")).getOrElse("No Id").toString
            val name = data.flatMap(_.get("name")).map(_.toString)
            val location =  data.flatMap(_.get("location"))
            userManager ! UpdateUser(id, name, getLatLngFromJson(location))

          case Some(JSONConstants.MonsterMoved) => println("monster moved data", ob.obj.toString)

          case Some(JSONConstants.Attack) => println("just got an attack", ob.obj.toString)

          case _ => println("something else in the json constants")
        }}}
    case _ => println("something else")
  }



  val cancellable: Cancellable =
    scheduler.schedule(
      0.seconds,
      1.second,
      new Runnable() {
        override def run(): Unit = {
          if(isBotsList) getBotAndUserData onSuccess {
              case s: Seq[JSONUser] => out ! createLocationJSON(s)
            }
      }})



  def createLocationJSON(users: Seq[JSONUser]): String = {
    val points = users.map(u => {
      raw"""
         |{
         |    "id": "${u.id}",
         |    "type": "Feature",
         |    "properties": {
         |        "timestamp": "${ZonedDateTime.now.toString}",
         |        "isBot": ${u.isBot},
         |        "name": "${u.name}"
         |    },
         |    "geometry": {
         |        "type": "Point",
         |        "coordinates": [
         |            ${u.lat},
         |            ${u.lng}
         |        ]
         |    }
         |}
        """.stripMargin
    })

    raw"""
       |{
       |    "event": "user-positions",
       |    "positions": {
       |        "type": "FeatureCollection",
       |        "bbox": [
       |            ${NYCBBOX.left},
       |            ${NYCBBOX.top},
       |            ${NYCBBOX.right},
       |            ${NYCBBOX.bottom}
       |        ],
       |        "features": [
       |            ${points.mkString(", ")}
       |        ]
       |    }
       |}
      """.stripMargin
  }

  def getLatLngFromJson(location: Option[Any]): Option[Point] =
    location.collect {
      case points: JSONArray => new Point(points.list.head.asInstanceOf[Double], points.list(1).asInstanceOf[Double])
    }


  private def getBotAndUserData: Future[Seq[JSONUser]] = {
    implicit val timeout: Timeout = 10.second
    val bots = botManager ? GetBots
    val users = userManager ? GetUsers

    for {
      botList <- bots.mapTo[Seq[JSONUser]]
      userList <- users.mapTo[Seq[JSONUser]]
    } yield botList ++ userList
  }
}


object ClientConnection {
  def props(out: ActorRef, scheduler: Scheduler, isBotsList: Boolean, userManager: ActorRef, botManager: ActorRef): Props = Props(new ClientConnection(out, scheduler, isBotsList, userManager, botManager))

  case class JSONUser(id: String, name: String, lat: Double, lng: Double, isBot: Boolean)

  case class bbox(left: Double, right: Double, top: Double, bottom: Double)

  val NYCBBOX: bbox = bbox(-122.3959, -122.4165, 37.78048, 37.78917)

  object JSONConstants {
    val CreatePlayer = "createPlayer"
    val UpdatePlayer = "updatePlayer"
    val PlayerAdded = "playerAdded"
    val BotName = "Bot"
    val MonsterMoved = "monsterMoved"
    val Attack = "attack"
  }

  object Errors {
    val NoIdError = """ {"type": "error", "data": {"message": "No User Id Received"}} """
  }
}
