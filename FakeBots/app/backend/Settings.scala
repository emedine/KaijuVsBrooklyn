package backend

import akka.actor.{AbstractExtensionId, ExtendedActorSystem, ExtensionIdProvider}

object Settings {
  val SettingsProvider: Settings = new Settings
}

class Settings extends AbstractExtensionId[SettingsImpl] with ExtensionIdProvider {
  def lookup: Settings = Settings.SettingsProvider

  def createExtension(system: ExtendedActorSystem): SettingsImpl = new SettingsImpl(system.settings.config)
}
