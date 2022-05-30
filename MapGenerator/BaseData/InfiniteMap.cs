using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class InfiniteMap : MonoBehaviour
{
    public Dictionary<Vector2, Tile> tileBook = new Dictionary<Vector2, Tile>();
    public Transform player;
    static Vector3 playerPos;
    float chunkSize; 
    int chunkInView;

    static int maxViewDist = 200;
    public MapGenBase mapGen;

    private void OnEnable()
    {
       
    }
    private void OnDisable()
    {
        //replacementObject?.SetActive(true);
    }

    void Start()
    {
        
        chunkSize = mapGen.size;
        chunkInView = Mathf.RoundToInt(maxViewDist / chunkSize);
        UpdateTile();
        //replacementObject?.SetActive(false);
    }

    void Update()
    {
        playerPos = player.position;
        UpdateTile();
    }

    void UpdateTile() 
    {
        foreach (KeyValuePair<Vector2, Tile> pair in tileBook) 
        {
            pair.Value.UpdateTile();
        }
        int tileCoordX = Mathf.RoundToInt(player.position.x / chunkSize);
        int tileCoordY = Mathf.RoundToInt(player.position.z / chunkSize);

        for (int y = -chunkInView; y <= chunkInView; y++) 
        {
            for (int x = -chunkInView; x <= chunkInView; x++) 
            {
                Vector2 currentTileCoord = new Vector2(tileCoordX + x, tileCoordY + y);
                if (!tileBook.ContainsKey(currentTileCoord))
                { 
                    Tile newTile = new Tile(currentTileCoord, chunkSize, transform,mapGen);
                    tileBook.Add(currentTileCoord, newTile);
 
                }
            }
        }
    }

    public class Tile
    {
        public Vector2 tileCoord;
        public float size;
        public bool activate;
        public GameObject currentTile;

        

        public Tile(Vector2 _tileCoord, float _size,Transform _parent, MapGenBase mapGen) 
        {
            
            tileCoord = _tileCoord;
            size = _size;
           
            Vector3 position = new Vector3(tileCoord.x * size, 0, tileCoord.y * size);

            currentTile = new GameObject();
            MeshFilter mf = currentTile.AddComponent<MeshFilter>();
            MeshRenderer mr = currentTile.AddComponent<MeshRenderer>();
            MeshCollider mc = currentTile.AddComponent<MeshCollider>();
            MapGenBase.MeshInfo newMeshInfo = new MapGenBase.MeshInfo(mapGen.verticesRes);
            Mesh targetMesh =  mapGen.FeedMesh((int)tileCoord.x, (int)tileCoord.y,newMeshInfo) .CreateMesh();

            mf.mesh=targetMesh;
            mc.sharedMesh = targetMesh;
            mr.material = mapGen.meshMaterial;
            currentTile.name = "X = " + tileCoord.x.ToString() + " " + "Y = " + tileCoord.y.ToString();
            currentTile.transform.position = position;
            currentTile.transform.parent = _parent;           
            Activate(false);
        }

        public void UpdateTile() 
        {
            float distToPlayer = Vector3.Distance(playerPos, currentTile.transform.position);
            activate = distToPlayer < maxViewDist;
            Activate(activate);
        }

        public void Activate(bool activate) 
        {
            currentTile.SetActive(activate);
        }
    }
}
