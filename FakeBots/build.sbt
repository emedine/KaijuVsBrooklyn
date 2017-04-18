name := "playYasKween"

version := "1.4"

lazy val `play_yas_kween` = (project in file(".")).enablePlugins(PlayScala)

scalaVersion := "2.11.7"

libraryDependencies ++= Seq(
  jdbc ,
  cache ,
  ws   ,
  specs2 % Test,
  "de.grundid.opendatalab" % "geojson-jackson" % "1.1"
)

// setting a maintainer which is used for all packaging types
maintainer := "Yas Kween"

// exposing the play ports
dockerExposedPorts in Docker := Seq(9000, 9443)

unmanagedResourceDirectories in Test <+=  baseDirectory ( _ /"target/web/public/test" )

resolvers += "scalaz-bintray" at "https://dl.bintray.com/scalaz/releases"
