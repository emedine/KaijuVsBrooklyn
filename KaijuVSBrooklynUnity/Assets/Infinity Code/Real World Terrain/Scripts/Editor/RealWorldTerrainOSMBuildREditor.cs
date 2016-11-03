/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RealWorldTerrainBuildR))]
public class RealWorldTerrainOSMBuildREditor : Editor
{
    public static List<string> alreadyCreated;

    private static RealWorldTerrainPrefs prefs
    {
        get { return RealWorldTerrain.prefs; }
    }

#if BUILDR
    private static void LoadConstraints(BuildrGenerateConstraints constraints)
    {
        XmlNodeList xmlData = null;

        if (RealWorldTerrainBuildRPresets.buildrGeneratorStyles == null || RealWorldTerrainBuildRPresets.buildrGeneratorStyles.Length <= prefs.customBuildRGeneratorStyle) return;

        string dataFilePath = "Assets/Buildr/XML/" + RealWorldTerrainBuildRPresets.buildrGeneratorStyles[prefs.customBuildRGeneratorStyle];

        if (File.Exists(dataFilePath))
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(dataFilePath);
            xmlData = xml.SelectNodes("data");
            if (xmlData != null && xmlData.Count > 0 && xmlData[0]["datatype"].FirstChild.Value != "ProGen") return;
        }

        if (xmlData == null) return;

        XmlNode node = (xmlData[0]["constraint"]);

        constraints.constrainType = node["constrainType"].FirstChild.Value == "True";
        constraints.type = (BuildrGenerateConstraints.buildingTypes)Enum.Parse(typeof(BuildrGenerateConstraints.buildingTypes), node["type"].FirstChild.Value);
        constraints.useSeed = node["useSeed"].FirstChild.Value == "True";
        constraints.seed = int.Parse(node["seed"].FirstChild.Value);
        constraints.minimumFloorHeight = float.Parse(node["minimumFloorHeight"].FirstChild.Value);
        constraints.maximumFloorHeight = float.Parse(node["maximumFloorHeight"].FirstChild.Value);
        constraints.constrainHeight = node["constrainHeight"].FirstChild.Value == "True";
        constraints.minimumHeight = float.Parse(node["minimumHeight"].FirstChild.Value);
        constraints.maximumHeight = float.Parse(node["maximumHeight"].FirstChild.Value);
        constraints.constrainFloorNumber = node["constrainFloorNumber"].FirstChild.Value == "True";
        constraints.floorNumber = int.Parse(node["floorNumber"].FirstChild.Value);
        constraints.constrainPlanByArea = node["constrainPlanByArea"].FirstChild.Value == "True";
        constraints.area.x = float.Parse(node["areax"].FirstChild.Value);
        constraints.area.y = float.Parse(node["areay"].FirstChild.Value);
        constraints.area.width = float.Parse(node["areawidth"].FirstChild.Value);
        constraints.area.height = float.Parse(node["areaheight"].FirstChild.Value);
        constraints.constrainPlanByPlan = node["constrainPlanByPlan"].FirstChild.Value == "True";
        
        constraints.constrainDesign = node["constrainDesign"].FirstChild.Value == "True";
        constraints.texturePackXML = node["texturePackXML"].FirstChild.Value;

        constraints.openingMinimumWidth = float.Parse(node["openingMinimumWidth"].FirstChild.Value);
        constraints.openingMaximumWidth = float.Parse(node["openingMaximumWidth"].FirstChild.Value);
        constraints.openingMinimumHeight = float.Parse(node["openingMinimumHeight"].FirstChild.Value);
        constraints.openingMaximumHeight = float.Parse(node["openingMaximumHeight"].FirstChild.Value);
        constraints.minimumBayMinimumWidth = float.Parse(node["minimumBayMinimumWidth"].FirstChild.Value);
        constraints.minimumBayMaximumWidth = float.Parse(node["minimumBayMaximumWidth"].FirstChild.Value);
        constraints.openingMinimumDepth = float.Parse(node["openingMinimumDepth"].FirstChild.Value);
        constraints.openingMaximumDepth = float.Parse(node["openingMaximumDepth"].FirstChild.Value);
        constraints.facadeMinimumDepth = float.Parse(node["facadeMinimumDepth"].FirstChild.Value);
        constraints.facadeMaximumDepth = float.Parse(node["facadeMaximumDepth"].FirstChild.Value);

        constraints.minimumRoofHeight = float.Parse(node["minimumRoofHeight"].FirstChild.Value);
        constraints.maximumRoofHeight = float.Parse(node["maximumRoofHeight"].FirstChild.Value);
        constraints.minimumRoofDepth = float.Parse(node["minimumRoofDepth"].FirstChild.Value);
        constraints.maximumRoofDepth = float.Parse(node["maximumRoofDepth"].FirstChild.Value);
        constraints.minimumRoofFloorDepth = float.Parse(node["minimumRoofFloorDepth"].FirstChild.Value);
        constraints.maximumRoofFloorDepth = float.Parse(node["maximumRoofFloorDepth"].FirstChild.Value);
        constraints.roofStyleFlat = node["roofStyleFlat"].FirstChild.Value == "True";
        constraints.roofStyleMansard = node["roofStyleMansard"].FirstChild.Value == "True";
        constraints.roofStyleBarrel = node["roofStyleBarrel"].FirstChild.Value == "True";
        constraints.roofStyleGabled = node["roofStyleGabled"].FirstChild.Value == "True";
        constraints.roofStyleHipped = node["roofStyleHipped"].FirstChild.Value == "True";
        constraints.roofStyleLeanto = node["roofStyleLeanto"].FirstChild.Value == "True";
        constraints.roofStyleSteepled = node["roofStyleSteepled"].FirstChild.Value == "True";
        constraints.roofStyleSawtooth = node["roofStyleSawtooth"].FirstChild.Value == "True";
        constraints.allowParapet = node["allowParapet"].FirstChild.Value == "True";
        constraints.parapetChance = float.Parse(node["parapetChance"].FirstChild.Value);
        constraints.allowDormers = node["allowDormers"].FirstChild.Value == "True";
        constraints.dormerChance = float.Parse(node["dormerChance"].FirstChild.Value);
        constraints.dormerMinimumWidth = float.Parse(node["dormerMinimumWidth"].FirstChild.Value);
        constraints.dormerMaximumWidth = float.Parse(node["dormerMaximumWidth"].FirstChild.Value);
        constraints.dormerMinimumHeight = float.Parse(node["dormerMinimumHeight"].FirstChild.Value);
        constraints.dormerMaximumHeight = float.Parse(node["dormerMaximumHeight"].FirstChild.Value);
        constraints.dormerMinimumRoofHeight = float.Parse(node["dormerMinimumRoofHeight"].FirstChild.Value);
        constraints.dormerMaximumRoofHeight = float.Parse(node["dormerMaximumRoofHeight"].FirstChild.Value);
        constraints.dormerMinimumSpacing = float.Parse(node["dormerMinimumSpacing"].FirstChild.Value);
        constraints.dormerMaximumSpacing = float.Parse(node["dormerMaximumSpacing"].FirstChild.Value);


        constraints.parapetMinimumDesignWidth = float.Parse(node["parapetMinimumDesignWidth"].FirstChild.Value);
        constraints.parapetMaximumDesignWidth = float.Parse(node["parapetMaximumDesignWidth"].FirstChild.Value);
        constraints.parapetMinimumHeight = float.Parse(node["parapetMinimumHeight"].FirstChild.Value);
        constraints.parapetMaximumHeight = float.Parse(node["parapetMaximumHeight"].FirstChild.Value);
        constraints.parapetMinimumFrontDepth = float.Parse(node["parapetMinimumFrontDepth"].FirstChild.Value);
        constraints.parapetMaximumFrontDepth = float.Parse(node["parapetMaximumFrontDepth"].FirstChild.Value);
        constraints.parapetMinimumBackDepth = float.Parse(node["parapetMinimumBackDepth"].FirstChild.Value);
        constraints.parapetMaximumBackDepth = float.Parse(node["parapetMaximumBackDepth"].FirstChild.Value);

        constraints.rowStyled = node["rowStyled"].FirstChild.Value == "True";
        constraints.columnStyled = node["columnStyled"].FirstChild.Value == "True";
        constraints.externalAirConUnits = node["externalAirConUnits"].FirstChild.Value == "True";
        constraints.splitLevel = node["splitLevel"].FirstChild.Value == "True";
        constraints.taperedLevels = node["taperedLevels"].FirstChild.Value == "True";
        constraints.singleLevel = node["singleLevel"].FirstChild.Value == "True";
        constraints.atticDesign = node["atticDesign"].FirstChild.Value == "True";
        constraints.shopGroundFloor = node["shopGroundFloor"].FirstChild.Value == "True";
    }
#endif

    private static void CreateBuilding(RealWorldTerrainContainer globalContainer, RealWorldTerrainOSMWay way)
    {
#if BUILDR
        List<Vector3> points = RealWorldTerrainOSM.GetGlobalPointsFromWay(way, RealWorldTerrainOSMBuildingEditor.nodes);

        if (points.Count < 3) return;

        if (points.First() == points.Last())
        {
            points.Remove(points.Last());
            if (points.Count < 3) return;
        }

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 p = RealWorldTerrainEditorUtils.GlobalToLocalWithElevation(points[i], globalContainer.scale);
            p.y = RealWorldTerrainEditorUtils.NormalizeElevation(globalContainer, p.y);
            points[i] = p;
        }

        for (int i = 0; i < points.Count; i++)
        {
            int prev = i - 1;
            if (prev < 0) prev = points.Count - 1;

            int next = i + 1;
            if (next >= points.Count) next = 0;

            if ((points[prev] - points[i]).magnitude < 1f)
            {
                points.RemoveAt(i);
                i--;
                continue;
            }

            if ((points[next] - points[i]).magnitude < 1f)
            {
                points.RemoveAt(next);
                continue;
            }

            float a1 = RealWorldTerrainUtils.Angle2D(points[prev], points[i]);
            float a2 = RealWorldTerrainUtils.Angle2D(points[i], points[next]);

            if (Mathf.Abs(a1 - a2) < 5)
            {
                points.RemoveAt(i);
                i--;
            }
        }

        if (points.Count < 3) return;

        Vector3 centerPoint = Vector3.zero;
        centerPoint = points.Aggregate(centerPoint, (current, point) => current + point) / points.Count;
        centerPoint.y = points.Min(p => p.y);

        int southIndex = -1;
        float southZ = float.MaxValue;

        for (int i = 0; i < points.Count; i++)
        {
            points[i] -= centerPoint;

            if (points[i].z < southZ)
            {
                southZ = points[i].z;
                southIndex = i;
            }
        }

        int prevIndex = southIndex - 1;
        if (prevIndex < 0) prevIndex = points.Count - 1;

        int nextIndex = southIndex + 1;
        if (nextIndex >= points.Count) nextIndex = 0;

        float angle1 = RealWorldTerrainUtils.Angle2D(points[southIndex], points[nextIndex]);
        float angle2 = RealWorldTerrainUtils.Angle2D(points[southIndex], points[prevIndex]);

        if (angle1 < angle2) points.Reverse();

        GameObject house = RealWorldTerrainUtils.CreateGameObject(RealWorldTerrainOSMBuildingEditor.houseContainer, way.id);
        house.AddComponent<RealWorldTerrainMeta>().GetFromOSM(way);
        house.AddComponent<RealWorldTerrainBuildR>();
        BuildrPlan plan = CreateInstance<BuildrPlan>();

        plan.AddVolume(points.Select(p => new Vector2z(p.x, p.z)).ToArray());
        BuildrEditMode editMode = house.AddComponent<BuildrEditMode>();
        editMode.StartBuilding();
        BuildrData data = editMode.data;
        data.generateCollider = (BuildrData.ColliderGenerationModes)prefs.buildRCollider;
        BuildrGenerateConstraints constraints = data.generatorConstraints;
        LoadConstraints(constraints);

        if (RealWorldTerrainBuildRPresets.buildrGeneratorTexturePacks != null &&
            RealWorldTerrainBuildRPresets.buildrGeneratorTexturePacks.Length >
            prefs.customBuildRGeneratorTexturePack)
        {
            constraints.texturePackXML = "Assets/Buildr/XML/" + RealWorldTerrainBuildRPresets.buildrGeneratorTexturePacks[prefs.customBuildRGeneratorTexturePack];
        }

        constraints.plan = plan;
        constraints.constrainPlanByPlan = true;
        bool useAllRoofs = points.Count == 4;
        constraints.roofStyleBarrel = useAllRoofs;
        constraints.roofStyleGabled = useAllRoofs;
        constraints.roofStyleHipped = useAllRoofs;
        constraints.roofStyleLeanto = useAllRoofs;
        constraints.roofStyleSawtooth = useAllRoofs;

        constraints.roofStyleMansard = useAllRoofs;
        constraints.roofStyleSteepled = useAllRoofs;

        constraints.roofStyleFlat = true;

        BuildrBuildingGenerator.Generate(data);

        int numberOfFloors = data.plan.volumes[0].numberOfFloors = way.HasTagKey("building:levels")
            ? int.Parse(way.GetTagValue("building:levels"))
            : prefs.buildingLevelLimits.Random();

        if (numberOfFloors > 1) numberOfFloors -= 1;

        for (int i = 0; i < data.plan.numberOfFacades; i++)
        {
            BuildrVolumeStylesUnit[] units = data.plan.volumes[0].styles.GetContentsByFacade(i);
            data.plan.volumes[0].styles.ModifyFloors(units[0].entryID, numberOfFloors);
        }

        RealWorldTerrainBuildRPresetsItem preset = null;
        if (prefs.customBuildRPresets != null && prefs.customBuildRPresets.Length > 0)
            preset = prefs.customBuildRPresets[UnityEngine.Random.Range(0, prefs.customBuildRPresets.Length)];

        if (preset != null)
        {
            if (!String.IsNullOrEmpty(preset.facade)) BuildrXMLImporter.ImportFacades(preset.facade, data);
            if (!String.IsNullOrEmpty(preset.roof)) BuildrXMLImporter.ImportRoofs(preset.roof, data);
            if (!String.IsNullOrEmpty(preset.texture)) BuildrXMLImporter.ImportTextures(preset.texture, data);
        }

        try
        {
            editMode.UpdateRender((BuildrEditMode.renderModes)prefs.buildRRenderMode);
        }
        catch{}
        editMode.stage = BuildrEditMode.stages.facades;

        house.transform.position = centerPoint + RealWorldTerrainOSMBuildingEditor.houseContainer.transform.position;
#endif
    }

    public static void Generate(RealWorldTerrainContainer globalContainer)
    {
        if (!RealWorldTerrainOSMBuildingEditor.loaded)
        {
            RealWorldTerrainOSMBuildingEditor.Load();

            if (RealWorldTerrainOSMBuildingEditor.ways.Count == 0)
            {
                RealWorldTerrain.phaseComplete = true;
                return;
            }

            if (RealWorldTerrain.generateTarget is RealWorldTerrainItem)
            {
                RealWorldTerrainItem item = RealWorldTerrain.generateTarget as RealWorldTerrainItem;
                RealWorldTerrainOSMBuildingEditor.baseContainer = RealWorldTerrainUtils.CreateGameObject(globalContainer, "Buildings " + item.x + "x" + (item.container.terrainCount.y - item.y - 1));
                RealWorldTerrainOSMBuildingEditor.baseContainer.transform.position = item.transform.position;
            }
            else RealWorldTerrainOSMBuildingEditor.baseContainer = RealWorldTerrainUtils.CreateGameObject(globalContainer, "Buildings");

            RealWorldTerrainOSMBuildingEditor.houseContainer = RealWorldTerrainUtils.CreateGameObject(RealWorldTerrainOSMBuildingEditor.baseContainer, "Houses");
            globalContainer.generatedBuildings = true;

            if (RealWorldTerrainOSMBuildingEditor.ways.Count == 0)
            {
                RealWorldTerrain.phaseComplete = true;
                return;
            }

            alreadyCreated = new List<string>();
        }

        //long startTime = DateTime.Now.Ticks;

        EditorUtility.DisplayProgressBar("Generate Buildings", "", 0);

        for (int i = RealWorldTerrain.phaseIndex; i < RealWorldTerrainOSMBuildingEditor.ways.Count; i++)
        {
            RealWorldTerrainOSMWay way = RealWorldTerrainOSMBuildingEditor.ways[i];
            if (alreadyCreated.Contains(way.id)) continue;
            if (way.GetTagValue("building") == "bridge") continue;
            string layer = way.GetTagValue("layer");
            if (!String.IsNullOrEmpty(layer) && Int32.Parse(layer) < 0) continue;

            CreateBuilding(globalContainer, way);
            alreadyCreated.Add(way.id);

            EditorUtility.DisplayProgressBar("Generate Buildings", "", (i + 1) / (float)RealWorldTerrainOSMBuildingEditor.ways.Count);

            /*if (new TimeSpan(DateTime.Now.Ticks - startTime).TotalSeconds > 1f)
            {
                RealWorldTerrain.phaseIndex = i + 1;
                RealWorldTerrain.phaseProgress = RealWorldTerrain.phaseIndex / (float)RealWorldTerrainOSMBuildingEditor.ways.Count;
                return;
            }*/
        }

        EditorUtility.ClearProgressBar();

        alreadyCreated = null;
        RealWorldTerrain.phaseComplete = true;
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Invert normals"))
        {
#if BUILDR
            RealWorldTerrainBuildR t = (RealWorldTerrainBuildR) target;
            BuildrEditMode editMode = t.GetComponent<BuildrEditMode>();
            editMode.data.plan.volumes[0].points.Reverse();
            editMode.UpdateRender(BuildrEditMode.renderModes.full);
            editMode.stage = BuildrEditMode.stages.facades;
#endif
        }
    }
}