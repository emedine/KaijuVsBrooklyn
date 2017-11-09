package controllers

import actors._
import akka.actor.{ActorRef, ActorSystem, Props}
import akka.stream.Materializer
import akka.stream.scaladsl.Flow
import com.google.inject.Inject
import play.api.libs.json.JsValue
import play.api.libs.streams.ActorFlow
import play.api.mvc._

class Application @Inject()(implicit system: ActorSystem, materializer: Materializer) extends Controller {

  val regionManagerClient: ActorRef = system.actorOf(Props[RegionManagerClient], "regionManagerClient")

  val userManager: ActorRef = system.actorOf(Props[UserManager], "userManager")

  val botManager: ActorRef = system.actorOf(BotManager.props(system.scheduler), "botManager")

  def index = Action {
    println("\n\n\nyas kween\n\n\n")
    Ok(views.html.index("Your new application is ready."))
  }

  def bot_locations: WebSocket = WebSocket.accept[String, String] { _ =>
    ActorFlow.actorRef(out => FakeBots.props(out, system.scheduler))
  }

  def locations: WebSocket = WebSocket.accept[String, String] { _ =>
    ActorFlow.actorRef(out => ClientConnection.props(out, system.scheduler, isBotsList = true, userManager, botManager))
  }

  def players: WebSocket = WebSocket.accept[String, String] { _ =>
    ActorFlow.actorRef(out => ClientConnection.props(out, system.scheduler, isBotsList = false, userManager, botManager))
  }
}
