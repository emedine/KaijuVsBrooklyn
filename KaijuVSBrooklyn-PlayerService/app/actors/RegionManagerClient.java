package actors;

import akka.actor.ActorRef;
import akka.actor.Props;
import akka.actor.UntypedActor;
import akka.routing.FromConfig;
import backend.Settings;
import backend.SettingsImpl;
import backend.UpdateUserPosition;
import backend.UpdateUserPositionWithRegion;
import models.backend.RegionId;
import models.backend.UserPosition;

/**
 * A client for the region manager, handles routing of position updates to the
 * regionManager on the right backend node.
 */


//TODO: 10/28/16 Change this to Scala
//TODO: 10/28/16 Understand and move to a separate application folder
public class RegionManagerClient extends UntypedActor {
  public static Props props() {
      return Props.create(RegionManagerClient.class, RegionManagerClient::new);
  }

    private final ActorRef regionManagerRouter =
            getContext().actorOf(Props.empty().withRouter(FromConfig.getInstance()), "router");
    private final SettingsImpl settings = Settings.SettingsProvider.get(getContext().system());

    public void onReceive(Object msg) throws Exception {
        if (msg instanceof UserPosition) {
            UserPosition pos = (UserPosition) msg;
            RegionId regionId = settings.GeoFunctions.regionForPoint(pos.position());
            regionManagerRouter.tell(new UpdateUserPositionWithRegion(regionId, pos), self());
        }
    }
}