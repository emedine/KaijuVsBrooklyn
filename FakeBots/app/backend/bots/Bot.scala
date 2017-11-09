package backend.bots

import akka.actor.Actor
import akka.actor.Actor.Receive

/**
  * Created by lerenzo on 11/5/16.
  */
class Bot extends Actor {
  override def receive: Receive = {
    case "yas" => println("bot got a message")
  }
}

object Bot {

}