package actors

import java.time.ZonedDateTime

import akka.actor.{Actor, ActorRef, Props, Scheduler}
import org.geojson.Point
import play.api.libs.json.JsValue
import play.libs.Json

import scala.concurrent.duration._
import scala.util.Random
import scala.concurrent.ExecutionContext.Implicits.global

class FakeBots(out: ActorRef, scheduler: Scheduler) extends Actor {
  println("just made a clientConnection yas!")

//  upper left: lat: 36.0358, long: -113.6438
//  lower right: lat: 35.568, long: -113.067

  val nyc = bbox(-122.3959, -122.4165, 37.78048, 37.78917)

  val bots = (0 until 100).map(Bot(_, generatePointInNyc, generatePointInNyc))

  val Tick = "tick"

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
            moveBots(bots, scale, currentFrame, maxFrame)
            currentFrame = currentFrame + 1
          }
        }
      })

  def generatePointInNyc: Point = {
    val seed = Random.nextFloat/(Random.nextFloat*100)
//    val long = nyc.left * (1 + seed)
    val long = Random.nextFloat * (nyc.right-nyc.left) + nyc.left
    val lat = Random.nextFloat * (nyc.top-nyc.bottom) + nyc.bottom
//    val lat = nyc.bottom * (1 + seed)

    new Point(long, lat)
  }

  def moveBots(fakeBots: Seq[Bot], scale: Float, curr: Int, max: Int) = {
    val delta = (max.toFloat - curr.toFloat) / max.toFloat
    val points = fakeBots map { bot => {

      val latDelta = bot.current.getCoordinates.getLatitude + (bot.target.getCoordinates.getLatitude - bot.current.getCoordinates.getLatitude) * delta
      val longDelta = bot.current.getCoordinates.getLongitude + (bot.target.getCoordinates.getLongitude - bot.current.getCoordinates.getLongitude) * delta

      raw"""
         |{
         |    "type": "Feature",
         |    "properties": {
         |        "timestamp": "${ZonedDateTime.now.toString}",
         |        "isBot": true
         |    },
         |    "geometry": {
         |        "type": "Point",
         |        "coordinates": [
         |            $latDelta,
         |            $longDelta
         |        ]
         |    },
         |    "id": "Bot ${bot.id}"
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

    out ! json
  }

  override def receive: Receive = {
    case json: JsValue =>
      println("yaaaaas just received a json node")

    case s: String =>
      println("just got a string message" + s)

    case "tick" =>
      println("yas")
  }

  case class bbox(left: Double, right: Double, top: Double, bottom: Double)

  case class Bot(id: Int, var current: Point, var target: Point) {
    def updatePoints() = {
      this.current = this.target
      this.target = generatePointInNyc
    }
  }
}

object FakeBots {
  def props(out: ActorRef, scheduler: Scheduler): Props = Props(new FakeBots(out, scheduler))
}



