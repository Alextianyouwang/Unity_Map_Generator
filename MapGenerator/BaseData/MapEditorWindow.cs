#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class MapEditorWindow : EditorWindow
{
    string filePath = "Assets/MapGenerator/";
    MapGenBase newMapGen;
    

    MapGenBase loadedMapGen;

    SerializedObject newMapGenSO;
    SerializedObject loadedMapGenSO;


    MapGenBase previousLoadedMapGen;
    MapGenBase displayMapGen;

    GameObject newMapParent;


    Material newMapMaterial;

    string mapName;


    bool hasGenerated = false;
    bool autoUpdate = true;

    Vector2 scrollPos;
   
    bool hasLoaded = false;
    bool hasUnloaded = false;
    
    int horizontalChunkNumber = 3;
    int previousHC;
    int verticalChunkNumber = 3;
    int previousVC;

    float chunkSize = 100f;
    float previousChunkSize;
    int levelOfDetail = 3;
    int previousLod;

    int seed = 0;
    Vector2 noiseOffset = new Vector2(1000, 1000);
    float noiseScale = 50f;
    float persistence = 0.3f;
    float lacunarity = 3f;
    int octaves = 3;
    float noiseHeight = 20f;

    [MenuItem("Tools/Map Generator")]

    static void Init() 
    {
        MapEditorWindow window = GetWindow<MapEditorWindow>("Map Generator");
        window.minSize = new Vector2(400f, 800f);
        window.Show();
        
    }

    private void OnEnable()
    {
        Initialize();
    }

    void Initialize() 
    {
        newMapGen = CreateInstance<MapGenBase>();
        newMapGenSO = new SerializedObject(newMapGen);
    }

    private void ChooseMapToLoad(out MapGenBase targetMapGenBase, out SerializedObject targetMapGenSO) 
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Map Asset Source", EditorStyles.boldLabel);
        //"displayMapGen" fills the exposed field, it is equals to "newMapGen" by default
        displayMapGen = EditorGUILayout.ObjectField(
            loadedMapGen == null ? "Create From Base" : "Modify From Exist",
            displayMapGen == null ? newMapGen : displayMapGen,
            typeof(MapGenBase), true) as MapGenBase;
        EditorGUILayout.Space(10);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(5));
         //If "loadedMapGen" is not assigned to "newMapGen", it will be ready to assigned externally.
        if (loadedMapGen != newMapGen)
        {
            loadedMapGen = displayMapGen == newMapGen ? null : displayMapGen;
        }

        if (loadedMapGen == null)
        {
            //Will be called once when manually set "loadedMapGen" to null;
            //"newMapGen" will takeover and its value will reset.
            if (!hasUnloaded)
            {
                hasUnloaded = true;
                hasLoaded = false;

                InitValue();

                //newMapGen.Clear();
                hasGenerated = false;
            }

            targetMapGenBase = newMapGen;
            targetMapGenSO = newMapGenSO;
        }
        else
        {
            //Will be called when "loadedMapGen" has changed
            if (previousLoadedMapGen != loadedMapGen)
            {
                hasLoaded = false;
            }
            // Will be called when "loadedMapGen" has been assigned reference or changed
            //Its value will be loaded.
            if (!hasLoaded)
            {
                hasUnloaded = false;
                hasLoaded = true;

                RestoreValue(loadedMapGen);
                loadedMapGenSO = new SerializedObject(loadedMapGen);

                //loadedMapGen.Clear();
                hasGenerated = false;
            }

            targetMapGenSO = loadedMapGenSO;
            targetMapGenBase = loadedMapGen;

        }
    }

    private void EditorContent(MapGenBase target,SerializedObject targetSO) 
    {
        EditorGUILayout.LabelField("Map Attributes", EditorStyles.boldLabel);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        newMapParent = (GameObject)EditorGUILayout.ObjectField("Map Parent Object", newMapParent, typeof(GameObject), true);
        newMapMaterial = (Material)EditorGUILayout.ObjectField("Map Material", newMapMaterial, typeof(Material), true);
        horizontalChunkNumber = EditorGUILayout.IntSlider("H Chunk Number", horizontalChunkNumber, 1, 5);
        verticalChunkNumber = EditorGUILayout.IntSlider("V Chunk Number", verticalChunkNumber, 1, 5);
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Mesh Chunk Attributes", EditorStyles.boldLabel);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        chunkSize = EditorGUILayout.FloatField("Chunk Size", chunkSize);
        levelOfDetail = EditorGUILayout.IntSlider("LOD", levelOfDetail, 0, 6);
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Pseudo Random Noise Attributes", EditorStyles.boldLabel);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        seed = EditorGUILayout.IntField("Seed", seed);
        noiseOffset = EditorGUILayout.Vector2Field("Offset", noiseOffset);
        noiseScale = EditorGUILayout.Slider("Scale", noiseScale, 1f, 200f);
        persistence = EditorGUILayout.Slider("Persistence", persistence, 0f, 1f);
        lacunarity = EditorGUILayout.Slider("Lacunarity", lacunarity, 0f, 4f);
        octaves = EditorGUILayout.IntSlider("Octaves", octaves, 1, 6);
        noiseHeight = EditorGUILayout.Slider("Height Multiplier", noiseHeight, -200f, 200f);
        SerializedProperty animationCurve = targetSO.FindProperty("heightCurve");
        EditorGUILayout.PropertyField(animationCurve, false);
        targetSO.ApplyModifiedProperties();
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Additional Heightmap Attributes", EditorStyles.boldLabel);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(500), GUILayout.MinHeight(200), GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        SerializedProperty heightMaps = targetSO.FindProperty("heightMapAttribute");
        EditorGUILayout.PropertyField(heightMaps, true);
        targetSO.ApplyModifiedProperties();

        EditorGUILayout.EndScrollView();
        GUILayout.FlexibleSpace();

        target.tileWidth = horizontalChunkNumber;
        target.tileHeight = verticalChunkNumber;
        target.mapParent = newMapParent;
        target.meshMaterial = newMapMaterial;

        target.size = chunkSize;
        target.lod = levelOfDetail;

        target.seed = seed;
        target.noiseOffset = noiseOffset;
        target.noiseScale = noiseScale;
        target.persistence = persistence;
        target.lacunarity = lacunarity;
        target.octaves = octaves;
        target.noiseHeight = noiseHeight;
    }

    private void UpdateValue(MapGenBase targetMapGenBase) 
    {

        if (autoUpdate && hasGenerated)

        {
            if (previousVC != verticalChunkNumber ||
               previousHC != horizontalChunkNumber)
            {
                targetMapGenBase.Clear();
                targetMapGenBase.Initialize();
                targetMapGenBase.InitTiles();
            }
            else if (previousChunkSize != chunkSize)
            {
                targetMapGenBase.UpdateTilePosition();
                targetMapGenBase.UpdateMeshVertices();
            }
            else if (previousLod != levelOfDetail)
            {
                targetMapGenBase.UpdateMesh();
                targetMapGenBase.UpdateTileName();
            }
            else if (GUI.changed)
            {
                targetMapGenBase.UpdateMeshVertices();
            }
        }
    }

    void InitValue() 
    {
        newMapParent = null;
        newMapMaterial = null;
        horizontalChunkNumber = 3;
        verticalChunkNumber = 3;
        chunkSize = 100f;
        levelOfDetail = 0;
        seed = 0;
        noiseOffset = new Vector2(1000, 1000);
        noiseScale = 50f;
        persistence = 0.3f;
        lacunarity = 3f;
        octaves = 3;
        noiseHeight = 20f;
    }
    void RestoreValue(MapGenBase target) 
    {
        horizontalChunkNumber = target.tileWidth;
        verticalChunkNumber = target.tileHeight;
        newMapParent = target.mapParent;
        newMapMaterial = target.meshMaterial;

        chunkSize = target.size;
        levelOfDetail = target.lod;

        seed = target.seed;
        noiseOffset = target.noiseOffset;
        noiseScale = target.noiseScale;
        persistence = target.persistence;
        lacunarity = target.lacunarity;
        octaves = target.octaves;
        noiseHeight = target.noiseHeight;
    }

    private void OnGUI()
    {
        if (EditorApplication.isPlaying)
        {
            Close();
        }
        if (loadedMapGen != null) 
        {
            previousLoadedMapGen = loadedMapGen;
        }
        previousHC = horizontalChunkNumber;
        previousVC = verticalChunkNumber;
        previousChunkSize = chunkSize;
        previousLod = levelOfDetail;

        MapGenBase targetMapGenBase;
        SerializedObject targetMapGenSO;

        ChooseMapToLoad(out targetMapGenBase, out targetMapGenSO);
        EditorContent(targetMapGenBase, targetMapGenSO);
        UpdateValue(targetMapGenBase);

        if (targetMapGenBase.mapParent != null &&
             targetMapGenBase.meshMaterial != null &&
             !hasGenerated) 
        {
            if (GUILayout.Button("Generate") &&
              !hasGenerated)
            {
                if (targetMapGenBase.mapParent != null &&
                    targetMapGenBase.meshMaterial != null)
                {
                    hasGenerated = true;
                    targetMapGenBase.Clear();
                    targetMapGenBase.Initialize();
                    targetMapGenBase.InitTiles();
                }
            }
        }

        if (hasGenerated)
        {
            autoUpdate = GUILayout.Toggle(autoUpdate, "AutoUpdate");
            if (!hasLoaded)
            {
                mapName = EditorGUILayout.TextField("Name", mapName == null ? "NewMapAsset" : mapName);
                if (GUILayout.Button("Create Template"))
                {
                    string finalPath = filePath + "MapObjects/";
                    if (!Directory.Exists(finalPath))
                    {
                        Directory.CreateDirectory(finalPath);
                    }
                    string finalName = AddFileIndex(finalPath, mapName, ".asset");
                    //Create Map Asset from "newMapGen", push its reference to "loadedMapGen" then initialize "newMapGen"
                    AssetDatabase.CreateAsset(newMapGen, finalPath + finalName + ".asset");
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = newMapGen;
                    loadedMapGen = newMapGen;
                    loadedMapGen.name = finalName;
                    Initialize();

                }
            }
            else 
            {
                if (GUILayout.Button("Create Prefab"))
                {
                    //string finalPath = AddFileIndex(filePath + mapName + "/", mapName, "") + "/";
                    string finalName = loadedMapGen == null ? mapName : loadedMapGen.name;
                    string folderPath = filePath + "MapPrefabs/";
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                    string finalPath = folderPath + finalName;
                    if (!Directory.Exists(finalPath))
                    {
                        Directory.CreateDirectory(finalPath);
                    }
                    GameObject prefab = PrefabUtility.SaveAsPrefabAsset(targetMapGenBase.mapParent, finalPath + "/" + finalName + ".prefab");
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = prefab;
                    int index = 0;
                    for (int y = 0; y < targetMapGenBase.tileHeight; y++)
                    {
                        for (int x = 0; x < targetMapGenBase.tileWidth; x++)
                        {
                            Mesh chunkMesh = targetMapGenBase.infoArray[x, y].mesh;
                            if (!AssetDatabase.Contains(chunkMesh))
                            {
                                AssetDatabase.CreateAsset(chunkMesh, finalPath + "/" + "(" + x.ToString() + "," + y.ToString() + ")" + ".mesh");
                            }
                            prefab.transform.GetChild(index).GetComponent<MeshFilter>().mesh = chunkMesh;
                            prefab.transform.GetChild(index).GetComponent<MeshCollider>().sharedMesh = chunkMesh;
                            prefab.transform.GetChild(index).GetComponent<MeshRenderer>().material = targetMapGenBase.meshMaterial;
                            index += 1;

                        }
                    }
                    targetMapGenBase.Clear();
                    targetMapGenBase.Initialize();
                    targetMapGenBase.InitTiles();
                }
            }
            
            if (GUILayout.Button("Clear"))
            {
                hasGenerated = false;
                targetMapGenBase.Clear();
            }

        }

    }

    private string AddFileIndex(string filePath, string originalName, string type)
    {
        string[] allFiles =  Directory.GetFiles(filePath);
        

        int index = 0;
        foreach (string fileName in allFiles)
        {
            string prefix = fileName.Replace(filePath, "");
            if ((prefix.Replace(type, "") == originalName || prefix.Replace(type, "").Contains(originalName + "(")) && !prefix.Replace(type, "").EndsWith(".meta"))
            {
                index += 1;

            }
        }
        string finalName = index == 0 ? originalName : originalName + "(" + index.ToString() + ")";
        return finalName;
    }
}
#endif