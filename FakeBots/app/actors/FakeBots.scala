package actors

import akka.actor.{Actor, ActorRef, Props, Scheduler}
import org.geojson.Point
import play.api.libs.json.JsValue
import play.libs.Json

import scala.concurrent.duration._
import scala.util.Random
import scala.concurrent.ExecutionContext.Implicits.global

class FakeBots(out: ActorRef, scheduler: Scheduler) extends Actor {
  println("just made a clientConnection yas!")

  val nyc = bbox(-74.2669, -73.6283, 40.8699, 40.5696)

  var targetPoints = (0 until 100) map { _: Int => generatePointInNyc }

  var bots = (0 until 100) map { _ => Bot(generatePointInNyc, generatePointInNyc) }

  val Tick = "tick"

  var currentFrame = 0

  val maxFrames = 60

  def delta = (maxFrames - currentFrame) / maxFrames

  val cancellable =
    scheduler.schedule(
      0.milliseconds,
      500.milliseconds,
      new Runnable() {
        override def run(): Unit = {
          currentFrame = currentFrame match {
            case num if num == maxFrames =>
              updatePoints()
              0
            case _ =>
              moveBots()
              currentFrame + 1
          }
          println("\n\n yas from the bots\n\n")
        }
      })

  def generatePointInNyc: Point = {
    val seed = Random.nextFloat
    val lat = nyc.left * (1 + seed)
    val long = nyc.bottom * (1 + seed)

    new Point(lat, long)
  }


  def updatePoints() = {
    bots = bots.map { bot => Bot(bot.target, generatePointInNyc) }
  }

  def moveBots() = {

    val points = bots map { bot => {

      val latDelta = bot.target.getCoordinates.getLatitude - (bot.target.getCoordinates.getLatitude - bot.current.getCoordinates.getLatitude) * delta
      val longDelta = bot.target.getCoordinates.getLongitude - (bot.target.getCoordinates.getLongitude - bot.current.getCoordinates.getLongitude) * delta

      raw"""
         |{
         |    "type": "Feature",
         |    "properties": {
         |        "timestamp": 1478120027631
         |    },
         |    "geometry": {
         |        "type": "Point",
         |        "coordinates": [
         |            $latDelta,
         |            $longDelta
         |        ]
         |    },
         |    "id": "${bot.hashCode()}"
         |}
        """.stripMargin
      }
    }

    val json = raw"""
       |{
       |    "event": "user-positions",
       |    "positions": {
       |        "type": "FeatureCollection",
       |        "bbox": [
       |            -74.2669,
       |            40.8699,
       |            -73.6283,
       |            40.5696
       |        ],
       |        "features": [
       |            ${points.mkString(", ")}
       |        ]
       |    }
       |}
        """.stripMargin

    out ! json
  }

  override def receive: Receive = {
    case json: JsValue =>
      println("yaaaaas just received a json node")
    //out ! s"just got the json message: ${json.toString}"

    case s: String =>
      println("just got a string message" + s)
      out ! s"just got the string message: $s"

    case "tick" =>
      println("yas")
  }

  case class bbox(left: Double, right: Double, top: Double, bottom: Double)

  case class Bot(current: Point, target: Point)

}

object FakeBots {
  def props(out: ActorRef, scheduler: Scheduler): Props = Props(new FakeBots(out, scheduler))
}



