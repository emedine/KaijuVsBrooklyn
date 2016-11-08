package backend



import java.util.concurrent.TimeUnit

import akka.actor.Extension
import com.typesafe.config.Config

import scala.concurrent.duration.{Duration, FiniteDuration}

class SettingsImpl(val config: Config) extends Extension {
  val MaxZoomDepth = config.getInt("reactiveMaps.maxZoomDepth")
  val MaxSubscriptionRegions = config.getInt("reactiveMaps.maxSubscriptionRegions")
  val ClusterThreshold = config.getInt("reactiveMaps.clusterThreshold")
  val ClusterDimension = config.getInt("reactiveMaps.clusterDimension")
  val SummaryInterval = Duration.apply(config.getDuration("reactiveMaps.summaryInterval", TimeUnit.MILLISECONDS), TimeUnit.MILLISECONDS)
  val ExpiryInterval = Duration.apply(config.getDuration("reactiveMaps.expiryInterval", TimeUnit.MILLISECONDS), TimeUnit.MILLISECONDS)
  val SubscriberBatchInterval = Duration.apply(config.getDuration("reactiveMaps.subscriberBatchInterval", TimeUnit.MILLISECONDS), TimeUnit.MILLISECONDS)
  val GeoFunctions = new GeoFunctions(this)
  val BotsEnabled = config.getBoolean("reactiveMaps.bots.enabled")
  val TotalNumberOfBots = config.getInt("reactiveMaps.bots.totalNumberOfBots")
  /**
    * The maximum zoom depth for regions.  The concrete regions will sit at this depth, summary regions will sit above
    * that.
    */
  /*final var MaxZoomDepth: Int = 0
  /**
    * The maximum number of regions that can be subscribed to.
    *
    * This is enforced automatically by selecting the deepest zoom depth for a given bounding box that is covered by
    * this number of regions or less.
    */
  final var MaxSubscriptionRegions: Int = 0
  /**
    * The number of points that need to be in a region/summary region before it decides to cluster them.
    */
  final var ClusterThreshold: Int = 0
  /**
    * The dimension depth at which to cluster.
    *
    * A region will be clustered into the square of this number boxes.
    */
  final var ClusterDimension: Int = 0
  /**
    * The interval at which each region should generate and send its summaries.
    */
  final var SummaryInterval: FiniteDuration = _
  /**
    * The interval after which user positions and cluster data should expire.
    */
  final var ExpiryInterval: FiniteDuration = _
  /**
    * The interval at which subscribers should batch their points to send to clients.
    */
  final var SubscriberBatchInterval: FiniteDuration = _
  /**
    * Geospatial functions.
    */
  final var GeoFunctions: GeoFunctions = _
  /**
    * Whether this node should run the bots it knows about.
    */
  final var BotsEnabled: Boolean = false
  /**
    * How many bots to create in total
    */
  final var TotalNumberOfBots: Int = 0*/
}
