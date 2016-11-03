/*     INFINITY CODE 2013-2015      */
/*   http://www.infinity-code.com   */

public enum RealWorldTerrainDownloadType
{
    data,
    file,
    www
}

public enum RealWorldTerrainGenerateBuildingPhase
{
    house,
    wall
}

public enum RealWorldTerrainBuildRCollider
{
    none,
    simple,
    complex
}

public enum RealWorldTerrainBuildRRenderMode
{
    full,
    lowDetail,
    box
}

public enum RealWorldTerrainGenerateType
{
    full,
    terrain,
    texture,
    additional
}

public enum RealWorldTerrainMaxElevation
{
    autoDetect,
    realWorldValue
}

public enum RealWorldTerrainElevationProvider
{
    SRTM,
    BingMaps
}

public enum RealWorldTerrainOSMBuildingType
{
    house,
    wall
}

public enum RealWorldTerrainOSMRoadType
{
    motorway,
    trunk,
    primary,
    secondary,
    tertiary,
    unclassified,
    residential,
    service,
    motorway_link,
    trunk_link,
    primary_link,
    secondary_link,
    tertiary_link,
    living_street,
    pedestrian,
    track,
    bus_guideway,
    raceway,
    road,
    footway,
    cycleway,
    bridleway,
    steps,
    path
}

public enum RealWorldTerrainOSMRoofType
{
    dome,
    flat
}

public enum RealWorldTerrainPhase
{
    downloading,
    finish,
    generateHeightmaps,
    generateOSMBuildings,
    generateOSMGrass,
    generateOSMRivers,
    generateOSMRoads,
    generateOSMTrees,
    generateTerrains,
    generateTextures,
    idle,
    loadHeightmaps,
    unzipHeightmaps
}

public enum RealWorldTerrainResultType
{
    terrain,
    mesh,
#if T4M
    T4M,
#endif
#if TERRAVOL
    terraVol,
#endif
}

public enum RealWorldTerrainTextureProvider
{
    arcGIS,
    google,
    mapQuest,
    nokia,
    virtualEarth,
    openStreetMap,
    custom = 999
}

public enum RealWorldTerrainTextureType
{
    satellite,
    terrain,
    relief
}

public enum RealWorldTerrainUpdateType
{
    all,
    alpha,
    beta,
    releaseCandidate,
    stable
}