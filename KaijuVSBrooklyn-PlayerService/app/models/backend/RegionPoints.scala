package models.backend

import com.google.common.collect.ImmutableList
import java.util.Collection
import scala.collection.JavaConversions._

/**
  * The points of interest for a given regionId.
  */
case class RegionPoints(regionId: RegionId, points: java.util.List[PointOfInterest])