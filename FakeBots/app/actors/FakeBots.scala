package actors

import java.time.ZonedDateTime
import java.util.UUID

import actors.FakeBots.JSONConstants
import akka.actor.{Actor, ActorRef, Props, Scheduler}
import org.geojson.Point
import play.api.libs.json.JsValue

import scala.util.parsing.json.{JSON, JSONArray, JSONObject, JSONType}
import play.libs.Json

import scala.collection.immutable.IndexedSeq
import scala.collection.mutable
import scala.concurrent.duration._
import scala.util.Random
import scala.concurrent.ExecutionContext.Implicits.global

class FakeBots(out: ActorRef, scheduler: Scheduler) extends Actor {
  println("just made a clientConnection yas!")

//  upper left: lat: 36.0358, long: -113.6438
//  lower right: lat: 35.568, long: -113.067

  val nyc: bbox = bbox(-122.3959, -122.4165, 37.78048, 37.78917)

  val bots: IndexedSeq[Bot] = (0 until 20).map(Bot(_, generatePointInNyc, generatePointInNyc))

  var users: mutable.HashMap[String, User] = new mutable.HashMap[String, User]()

  var currentFrame = 0

  val maxFrame = 60

  val scale = 1f

  def isLastFrame = currentFrame == maxFrame

  val cancellable =
    scheduler.schedule(
      0.seconds,
      1.second,
      new Runnable() {
        override def run(): Unit = {
          if(isLastFrame) {
            bots.foreach(_.updatePoints())
            currentFrame = 0
          } else{
//            moveBots(bots, scale, currentFrame, maxFrame)
            currentFrame = currentFrame + 1
            out ! createLocationJSON(mapBots(currentFrame, maxFrame) ++ mapUsers)
          }
        }
      })

  def addUser(name: Option[String], location: Option[Point]): User = {
    val id = UUID.randomUUID.toString
    val newUser = User(id, name.getOrElse("No Name"), location)
    users += id -> newUser
    newUser
  }

  def generatePointInNyc: Point = {
    val seed = Random.nextFloat/(Random.nextFloat*100)
//    val long = nyc.left * (1 + seed)
    val long = Random.nextFloat * (nyc.right-nyc.left) + nyc.left
    val lat = Random.nextFloat * (nyc.top-nyc.bottom) + nyc.bottom
//    val lat = nyc.bottom * (1 + seed)

    new Point(long, lat)
  }

  def mapUsers: Seq[JSONUser] = users.values.map(u => {
    val location = u.location.map(_.getCoordinates)
    JSONUser(u.id, u.name, lat = location.map(_.getLatitude).getOrElse(0.0), lng = location.map(_.getLongitude).getOrElse(0.0), isBot = false)
  }).toSeq

  def mapBots(curr: Int, max: Int): Seq[JSONUser] = {
    val delta = (max.toFloat - curr.toFloat) / max.toFloat
    bots map { bot => {

        val latDelta = bot.current.getCoordinates.getLatitude + (bot.target.getCoordinates.getLatitude - bot.current.getCoordinates.getLatitude) * delta
        val longDelta = bot.current.getCoordinates.getLongitude + (bot.target.getCoordinates.getLongitude - bot.current.getCoordinates.getLongitude) * delta

        JSONUser(id = bot.id.toString, name = JSONConstants.BotName, lat = latDelta, lng = longDelta, isBot = true)
      }
    }
  }


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
              |            ${nyc.left},
              |            ${nyc.top},
              |            ${nyc.right},
              |            ${nyc.bottom}
              |        ],
              |        "features": [
              |            ${points.mkString(", ")}
              |        ]
              |    }
              |}
      """.stripMargin
  }



  override def receive: Receive = {

    case s: String =>
      JSON.parseRaw(s) collect {
        case json: JSONType => json match { case ob: JSONObject => ob.obj.get("event") match {
              case Some(JSONConstants.CreatePlayer) =>
                val name = ob.obj.get("name").map { case s:String => s }
                val user = addUser(name, getLatLngFromJson(ob.obj.get("location")))

                out ! raw""" {"event": ${JSONConstants.PlayerAdded}, id: ${user.id}" """
              case Some(JSONConstants.UpdatePlayer) =>
                updatePlayer(ob.obj)
                "jowiefj" //TODO get geolocation and use that for the location of the user
            }
        }
      }
  }

  def updatePlayer(json: Map[String, Any]) = {
    val id = json("id").toString
    val name = json.get("name").map { case s:String => s }
    val location = getLatLngFromJson(json.get("location"))

    users.get(id).foreach(u => {
      u.name = name.getOrElse("No Name")
      u.location = location
    })
  }


  def getLatLngFromJson(location: Option[Any]): Option[Point] =
    location collect { case points: JSONArray => new Point(points.list.head.asInstanceOf[Double],points.list(1).asInstanceOf[Double])}

  case class bbox(left: Double, right: Double, top: Double, bottom: Double)

  case class User(id: String, var name: String, var location: Option[Point])

  case class Bot(id: Int, var current: Point, var target: Point) {
    def updatePoints() = {
      this.current = this.target
      this.target = generatePointInNyc
    }
  }

  case class JSONUser(id: String, name: String, lat: Double, lng: Double, isBot: Boolean)
}

object FakeBots {
  def props(out: ActorRef, scheduler: Scheduler): Props = Props(new FakeBots(out, scheduler))

  object JSONConstants {
    val CreatePlayer = "createPlayer"
    val UpdatePlayer = "updatePlayer"
    val PlayerAdded = "playerAdded"
    val BotName = "Bot"
  }
}



