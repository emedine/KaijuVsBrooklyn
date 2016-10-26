package backend

import com.fasterxml.jackson.annotation.{JsonCreator, JsonProperty}
import models.backend.LatLng

trait UserMetaDataProtocol {
  def id: String
}

@JsonCreator
case class User(@JsonProperty id: String, @JsonProperty distance: Double) extends UserMetaDataProtocol

case class UpdateUserPosition(id: String, position: LatLng) extends UserMetaDataProtocol