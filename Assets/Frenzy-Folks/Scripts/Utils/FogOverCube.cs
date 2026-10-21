using UnityEngine;
using BKPureNature;

public class FogOverCube : MonoBehaviour
{
    [SerializeField] private Renderer target;   // the 10x30 cube's renderer
    [SerializeField] private PureVolumetricFog fog;

    [ContextMenu("Build fog terrain")]
    private void Build()
    {
        Bounds b = target.bounds; // axis-aligned only, see notes

        var data = new TerrainData();
        data.heightmapResolution = 33;                       // smallest valid size
        data.size = new Vector3(b.size.x, 2f, b.size.z);     // flat, all heights = 0
        
        if(target.transform.Find("FogTerrain")?.gameObject != null)
            DestroyImmediate(target.transform.Find("FogTerrain")?.gameObject); // remove old terrain if exists

        GameObject go = Terrain.CreateTerrainGameObject(data);
        go.name = "FogTerrain";
        go.transform.position = new Vector3(b.min.x, b.max.y, b.min.z); // origin = top face corner
        go.transform.parent = target.gameObject.transform; // parent to cube

        DestroyImmediate(go.GetComponent<TerrainCollider>());          // no physics on this plane
        go.GetComponent<Terrain>().drawHeightmap = false;     // invisible, only its heightmap is used

        fog.SetTerrain(go.GetComponent<Terrain>());
    }
}