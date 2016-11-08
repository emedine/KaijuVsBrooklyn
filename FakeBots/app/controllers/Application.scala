package controllers

import actors.{ClientConnection, FakeBots, RegionManagerClient}
import akka.actor.{ActorSystem, Props}
import akka.stream.Materializer
import com.google.inject.Inject
import play.api.libs.json.JsValue
import play.api.libs.streams.ActorFlow
import play.api.mvc._

class Application @Inject()(implicit system: ActorSystem, materializer: Materializer) extends Controller {

  val regionManagerClient = system.actorOf(Props[RegionManagerClient], "regionManagerClient")

  def index = Action {
    println("\n\n\nyas kween\n\n\n")
    Ok(views.html.index("Your new application is ready."))
  }


  def stream(email: String) = WebSocket.accept[String, String] { request =>
    ActorFlow.actorRef(out => ClientConnection.props(out, regionManagerClient))
  }

  def streamJSON = WebSocket.accept[JsValue, JsValue] { request =>
    ActorFlow.actorRef(out => ClientConnection.props(out, regionManagerClient))
  }

  def bot_locations = WebSocket.accept[String, String] { request =>
    ActorFlow.actorRef(out => FakeBots.props(out, system.scheduler))
  }
}
