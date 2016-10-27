package backend;

import com.fasterxml.jackson.annotation.*;
import models.backend.LatLng;

/*
public abstract class UserMetaDataProtocol {
    public static class GetUser {
        private final String id;

        public GetUser(String id) {
            this.id = id;
        }

        public String getId() {
            return id;
        }
    }

    public static class User {
        private final String id;
        private final double distance;

        @JsonCreator
        public User(@JsonProperty("id") String id, @JsonProperty("distance") double distance) {
            this.id = id;
            this.distance = distance;
        }

        public String getId() {
            return id;
        }

        public double getDistance() {
            return distance;
        }
    }

    public static class UpdateUserPosition {
        private final String id;
        private final LatLng position;

        public UpdateUserPosition(String id, LatLng position) {
            this.id = id;
            this.position = position;
        }

        public String getId() {
            return id;
        }

        public LatLng getPosition() {
            return position;
        }
    }
}*/
