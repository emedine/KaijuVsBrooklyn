package models.backend

/**
  * A region id.
  *
  * The zoomLevel indicates how deep this region is zoomed, a zoom level of 8 means that there are 2 ^^ 8 steps on the
  * axis of this zoomLevel, meaning the zoomLevel contains a total of 2 ^^ 16 regions.
  *
  * The x value starts at 0 at -180 West, and goes to 2 ^^ zoomLevel at 180 East.  The y value starts at 0 at -90 South,
  * and goes to 2 ^^ zoomLevel at 90 North.
  */
case class RegionId(zoomLevel: Int, x: Int, y: Int) {
  def name = "region-" + zoomLevel + "-" + x + "-" + y

  override def toString: String = name

  override def equals(o: Any): Boolean = {
    if (this == o) return true
    if (o == null || (getClass ne o.getClass)) return false
    val regionId: RegionId = o.asInstanceOf[RegionId]
    if (x != regionId.x) return false
    if (y != regionId.y) return false
    if (zoomLevel != regionId.zoomLevel) return false
    true
  }

  override def hashCode: Int = {
    var result: Int = zoomLevel
    result = 31 * result + x
    result = 31 * result + y
    result
  }
}