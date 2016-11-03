using UnityEngine;
using System.Collections;
// using LitJson;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;

public class KaijuMain : MonoBehaviour {

    public float speed = 1.5f;

    public bool hasTarget = true;
    private Vector3 targPos;
    private Vector3 camPos;
    private GameObject gObj;
    private GameObject CamRig;

    private float mapWidth;
    private float mapHeight;

    /// position markers
    public GameObject playerMarker;
    public GameObject posBounds;
    private Vector3 mapBounds;

    // JSON objects

    public string title;
    public string id;
    public ArrayList latArray; // array of latitude
    public ArrayList LongArray; // array of longitude
   //// List<string> PNameArray = new ArrayList<string>();  // array of player names

    TargetProfile TarD = new TargetProfile();

    // Use this for initialization
    void Start () {
        /// get target map bounds-- this is the basis for our latlong to unity positioning system
        mapBounds = posBounds.GetComponent<Renderer>().bounds.size;
        // mapBounds = Vector3.Scale(transform.localScale, posBounds.GetComponent<MeshFilter>().mesh.bounds.size);
        mapWidth = mapBounds.x;
        mapHeight = mapBounds.z;
        Debug.Log("MapSIZE  " + mapWidth + " " + mapHeight);


        /// initialize targets from .txt file
        addTargets();

        /// set json check for character locations. Figure out update method
        /// InvokeRepeating ("ParseJson", 0, 5);

        ParseJson();

      
}

    // Update is called once per frame
    void Update () {
        //transform.position = Input.mousePosition;
        // Debug.Log("tp:" + transform.position);
        // Debug.Log("mp:" + Input.mousePosition);
        CamRig = GameObject.Find("OVRCameraRig");
        
        if (hasTarget == false) {
            gObj = GameObject.Find("TargetPosMarker");
            if (gObj) {
                Vector3 tmpPos = Vector3.Lerp(transform.position, gObj.transform.position, speed / 23);
                /// CamRig.transform.position = Vector3.Lerp(transform.position, gObj.transform.position, speed / 150);

                CamRig.transform.position = new Vector3(tmpPos.x, 10, tmpPos.z);
            }
        }

        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)) {
            /// Debug.Log("Primary Trigger HIT");
            hasTarget = false;
            /// update position of target
            var gObj = GameObject.Find("TargetPosMarker");
            if (gObj) {
                targPos = new Vector3(gObj.transform.position.x, 10 , gObj.transform.position.z);
                CamRig = GameObject.Find("OVRCameraRig");
                // CamRig.transform.position = gObj.transform.position;
                // CamRig.transform.Translate(0, 10, 0); /// make sure height doesn't change 
                // Debug.Log("New x: " + CamRig.transform.position.x + "New Y: " + CamRig.transform.position.y + "New Z: " + CamRig.transform.position.z + " targ X: " + targPos.x + " targY: " + targPos.y + " targZ: " + targPos.z);
                // Debug.Log("New x: " + this.gameObject.transform.position.x + "New Y: " + this.gameObject.transform.position.x + " targ X: " + newPos.x + " targY: " + newPos.y);
            }
            else
            {
                Debug.Log("NO MARKER FOUND");

            }
        }
    }




    ///////////// ADD PREMADE TARGET OBJECTS BY LAT AND LONG /////////////////////////////////////////

    void ParseJson()
    {
        /// Debug.Log(JsonConvert.SerializeObject(TarD));
        string json = File.ReadAllText(@"X:\WORK\KAYFABE_VR\KaijuVSBrooklyn\Assets\Resources\playerdata.json");
        var jo = JObject.Parse(json);

        // var theLat = jo["positions"]["features"][0]["geometry"]["coordinates"].ToString(); //this sort of works
        var tLength = jo["positions"]["features"].ToObject<ArrayList>().Count;

        float theLat = 0f;
        float theLong = 0f;
        for (int i = 0; i < tLength; i++)
        {
            theLat = jo["positions"]["features"][i]["geometry"]["coordinates"][0].Value<float>();
            theLong = jo["positions"]["features"][i]["geometry"]["coordinates"][1].Value<float>();
            /// add name to array
            var tName = jo["positions"]["features"][i]["id"].Value<string>();
            /// PNameArray.Add
           ///  Debug.Log("name: " + PNameArray.Add(tName));
            /// update positions
            latLongToXZ(theLat, theLong, tName);
            Debug.Log("Length: " + tLength + " == theLat: " + theLat + " theLong: " + theLong);
        }

        /*
        TarD.tarLat = 3.2456f;
        TarD.tarLong = 5.2456f;
        TarD.tName = "Eric";
        
        TargetData CurTarData = JsonConvert.DeserializeObject<TargetData>(json);
        Debug.Log("Cur Target Data: " + CurTarData.ToString());
        */
    }

    void addTargets()
    {
        //// get the coordinates from a .txt file
        // string text = System.IO.File.ReadAllText(@"X:\WORK\KAYFABE_VR\KaijuVSBrooklyn\Assets\Resources\positioningdata.txt");
        string[] tLat = System.IO.File.ReadAllLines(@"X:\WORK\KAYFABE_VR\KaijuVSBrooklyn\Assets\Resources\positioningdataLat.txt");
        string[] tLong = System.IO.File.ReadAllLines(@"X:\WORK\KAYFABE_VR\KaijuVSBrooklyn\Assets\Resources\positioningdataLong.txt");
        
        for(int i = 0; i<tLat.Length; i++){
            /// we're not doing it this way anymore
           ///  latLongToXZ(float.Parse(tLat[i]), float.Parse(tLong[i]), PNameArray[i].ToString());
            
        }

        


    }
    ///////////// CONVERT LAT AND LONG INTO x and z data  /////////////////////////////////////////




    void latLongToXZ(float tLat, float tLong, string tName)
    {

        /// READS LONGITUDE AND LAT
        RealWorldTerrainContainer rwtc = GameObject.Find("RealWorld Terrain").GetComponent<RealWorldTerrainContainer>();
        // Debug.Log("TopLeft:"+rwtc.topLeft);
        // Debug.Log("BotRight:" + rwtc.bottomRight);

        float rX = (rwtc.topLeft.x - tLong) / (rwtc.topLeft.x - rwtc.bottomRight.x);
        float rY = (rwtc.topLeft.y - tLat) / (rwtc.topLeft.y - rwtc.bottomRight.y);

        // Debug.Log("Orig coords: (" + tLat + ", " + tLong + ") " + "Ratio coords: (" + rX + ", " + rY + ")");

        Vector3 newPos = new Vector3(rX * mapWidth, 0f, mapHeight- (rY * mapHeight));

        // no worky?
        /*
        float screenX = ((tLong + 180) * (mapWidth / 360));
        float screenZ = (((tLat * -1) + 90) * (mapHeight / 180));
        */
        //float screenX = mapWidth * (180 + tLat) / 360;
        //float screenZ = mapHeight * (90 - tLong) / 180;
        //Vector3 newPos= new Vector3(screenX, 0, screenZ);
        // GameObject ego = GameObject.Instantiate(playerMarker, m_eyeball.position, m_eyeball.rotation) as GameObject;

        GameObject cube = GameObject.Instantiate(playerMarker);
        cube.AddComponent<Rigidbody>();
        cube.transform.position = newPos;

        /*
        var data = cube.GetComponent<TargetProfile>();
        data.TargetName = tName;
        */

        //// Debug.Log("SPAWNING PLAYERS at " + screenX + ", " + screenZ + " from the LAT: " + tLat + ", and LONG: " + tLong);
        /// REALLY COMPLEX ALGORYTHM

        ////http://stackoverflow.com/questions/18838915/convert-lat-lon-to-pixel-coordinate
        // This map would show Germany:
        /*
        $south = deg2rad(47.2);
        $north = deg2rad(55.2);
        $west = deg2rad(5.8);
        $east = deg2rad(15.2);
        */
        // This also controls the aspect ratio of the projection
        /*
        $width = 1000;
        $height = 1500;
        */
        // Formula for mercator projection y coordinate:
        /// function mercY($lat) { return log(tan($lat / 2 + M_PI / 4)); }

        // Some constants to relate chosen area to screen coordinates
        /*
        $ymin = mercY($south);
        $ymax = mercY($north);
        $xFactor = $width / ($east - $west);
        $yFactor = $height / ($ymax - $ymin);

        function mapProject($lat, $lon) { // both in radians, use deg2rad if neccessary
                        global $xFactor, $yFactor, $west, $ymax;
              $x = $lon;
              $y = mercY($lat);
              $x = ($x - $west)*$xFactor;
              $y = ($ymax - $y)*$yFactor; // y points south
                        return array($x, $y);
        }

    */



    }

    void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.name == "TargetPosMarker")
        {
            /// Destroy(col.gameObject);
            print("TARGET REACHED " + other.gameObject.name);
            hasTarget = true;

            /// GameObject ego = GameObject.Instantiate(m_explosion, col.gameObject.transform.position, col.gameObject.transform.rotation) as GameObject;
            ///Destroy(col.gameObject.transform.parent.gameObject);
            ///Debug.Log("YOU HIT SOMETHING");
            /// Object.Destroy(this.gameObject);
        }
        else
        {

            Debug.Log("pass thru" + other.gameObject.name);
        }

    }

}


class TargetProfile {

    public float tarLat;
    public float tarLong;
    public string tName;

    public TargetProfile()
    {


    }

    public string TargetName { get; internal set; }


    /// get the json file
    /// 

    /// parse it
    public override string ToString()
    {
        return tName+": "+tarLat + ", " + tarLong;
    }

}
