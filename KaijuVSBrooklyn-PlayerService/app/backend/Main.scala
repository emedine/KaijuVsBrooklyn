package backend

import java.net.URL

import actors.RegionManagerClient
import akka.actor.{ActorSystem, Props}
import akka.cluster.Cluster

import scala.collection.JavaConversions._
import scala.collection.mutable


/**
  * Main class for starting a backend node.
  * A backend node can have two roles: "backend-region" and/or "backend-summary".
  * The lowest level regions run on nodes with role "backend-region".
  * Summary level regions run on nodes with role "backend-summary".
  * <p>
  * The roles can be specfied on the sbt command line as:
  * {{{
  * sbt -Dakka.remote.netty.tcp.port=0 -Dakka.cluster.roles.1=backend-region -Dakka.cluster.roles.2=backend-summary "run-main backend.Main"
  * }}}
  * <p>
  * If the node has role "frontend" it starts the simulation bots.
  */

class Main extends App {
  val system = ActorSystem.create("t3hBotz")

  if(Cluster.get(system).getSelfRoles.contains("backend")) system.actorOf(Props[RegionManager], "regionManager")

  if(Settings.SettingsProvider.get(system).BotsEnabled && Cluster.get(system).getSelfRoles.contains("frontend")) {
    val regionManagerClient = system.actorOf(Props[RegionManagerClient], "regionManagerClient")
    var id = 1
    var url = getBotResourceId(id)
    var urls = mutable.MutableList()
    urls
    while(url.nonEmpty) {
      urls :+ url
      id += 1
      url = getBotResourceId(id)
    }

    system.actorOf(BotManager.props(regionManagerClient, urls))
  }

  def getBotResourceId(id: Int): Option[URL] = {
    Option(classOf[Main].getClassLoader.getResource(s"bots/$id.json"))
  }
}