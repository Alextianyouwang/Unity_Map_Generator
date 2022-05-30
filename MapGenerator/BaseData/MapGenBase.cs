using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
[Serializable]
public class MapGenBase :ScriptableObject
{
    [HideInInspector]
    public GameObject mapParent;
    public Material meshMaterial;
    [HideInInspector]
    public int tileWidth;
    [HideInInspector]
    public int tileHeight;

    public float size;
    public int lod;

    public int seed;
    public float noiseScale ;
    public float persistence;
    public float lacunarity;
    public int octaves;
    public float noiseHeight;
    public Vector2 noiseOffset;
    public AnimationCurve heightCurve = AnimationCurve.Linear (-1f,-1f,1f,1f);

    public HeightMapAttribute[] heightMapAttribute = new HeightMapAttribute[0];
    [Serializable]
    public class HeightMapAttribute
    {
        [SerializeField]
        public Texture2D heightMap;
        [SerializeField]
        public Vector2 offset;
        [SerializeField]
        [Range(0.1f, 5f)]
        public float sampleScale = 1f;
        [SerializeField]
        [Range(0, 200f)]
        public float height = 10f;
        [SerializeField]
        [Range(0, 1f)]
        public float blend = 0f;
    }

    public GameObject[,] mapTiles;
    public MeshInfo[,] infoArray;

    private static int noiseSampleRes = 241;
    private static float increment;
    private static Vector3 centeringOffset;
    private int simplificationIncrement;
    public int verticesRes;
    
    public void Initialize() 
    {
        simplificationIncrement = lod == 0 ? 1 : lod * 2;
        verticesRes = (noiseSampleRes - 1) / simplificationIncrement +1;
        infoArray = new MeshInfo[tileWidth, tileHeight];
        for (int y = 0; y < tileHeight; y++)
        {
            for (int x = 0; x < tileWidth; x++)
            {
                MeshInfo newMeshInfo = new MeshInfo(verticesRes);
                infoArray[x, y] = FeedMesh(x, y, newMeshInfo);
            }
        }
    }
  
    public void InitTiles() 
    {
        mapTiles = new GameObject[tileWidth, tileHeight];
        for (int y = 0; y < tileHeight; y++)
        {
            for (int x = 0; x < tileWidth; x++)
            {

                GameObject mapTile = new GameObject();
                mapTile.name = verticesRes.ToString() + "^" + " (" + x.ToString() + "," + y.ToString() + ")";
                mapTile.transform.position = new Vector3(x * size, 0, y * size);
                Mesh targetMesh = infoArray[x, y].CreateMesh();
                mapTile.AddComponent<MeshFilter>().sharedMesh = targetMesh;
                mapTile.AddComponent<MeshRenderer>().material = meshMaterial;
                mapTile.AddComponent<MeshCollider>().sharedMesh = targetMesh;
                mapTile.transform.parent = mapParent.transform;
                mapTiles[x, y] = mapTile;
            }
        }
    }

    public void UpdateTilePosition()
    {
        for (int y = 0; y < tileHeight; y++)
        {
            for (int x = 0; x < tileWidth; x++)
            {
                mapTiles[x, y].transform.position = new Vector3(x * size, 0, y * size);
            }
        }
    }
    public void UpdateTileName() 
    {
        for (int y = 0; y < tileHeight; y++)
        {
            for (int x = 0; x < tileWidth; x++)
            {
                mapTiles[x, y].transform.name = verticesRes.ToString() + "^" + " Index: (" + x.ToString() + "," + y.ToString() +")";
            }
        }
    }


    public void UpdateMeshVertices() 
    {
        for (int y = 0; y < tileHeight; y++)
        {
            for (int x = 0; x < tileWidth; x++)
            {

                infoArray[x, y] = FeedMeshVertices(x, y, infoArray[x, y]);
                infoArray[x, y].UpdateVertices();
            }
        }
    }

    public void UpdateMesh() 
    {
        for (int y = 0; y < tileHeight; y++)
        {
            for (int x = 0; x < tileWidth; x++)
            {
                simplificationIncrement = lod == 0 ? 1 : lod * 2;
                verticesRes = (noiseSampleRes - 1) / simplificationIncrement + 1;
                infoArray[x, y].ResetTriangleIndex();
                infoArray[x, y].UpdateNumRows(verticesRes);                
                infoArray[x, y] = FeedMesh(x,y,infoArray[x,y]);
                infoArray[x, y].UpdateMesh();
            }
        }
    }

    public MeshInfo FeedMeshVertices(float indexX, float indexY, MeshInfo meshInfo)
    {
        simplificationIncrement = lod == 0 ? 1 : lod * 2;
        verticesRes = (noiseSampleRes - 1) / simplificationIncrement + 1;
        increment = size / (verticesRes -1);
        centeringOffset = new Vector3(-size / 2, 0, -size / 2);
        
        float[,] finalHeightValue = GetHeightValues(indexX, indexY);
        int vertIndex = 0;

        for (int y = 0; y < verticesRes ; y ++)
        {
            for (int x = 0; x < verticesRes; x++)
            {
                Vector3 relativePos = new Vector3(x, 0, y) * increment+ centeringOffset;
                relativePos.y = finalHeightValue[x * simplificationIncrement,y * simplificationIncrement];
                meshInfo.verticies[vertIndex] = relativePos;
                vertIndex += 1;
            }
        }

        return meshInfo;
    }

    public MeshInfo FeedMesh(float indexX, float indexY, MeshInfo meshInfo) 
    {
        MeshInfo finalMesh = FeedMeshVertices(indexX, indexY, meshInfo);
     

        int vertIndex = 0;
        for (int y = 0; y < verticesRes; y++)
        {
            for (int x = 0; x < verticesRes; x++)
            {
                finalMesh.uvs[vertIndex] = new Vector2(x / (float)verticesRes, y / (float)verticesRes);
                if (y < verticesRes - 1 && x < verticesRes - 1)
                {
                    finalMesh.AddTriangles(vertIndex, vertIndex + verticesRes, vertIndex + 1);
                    finalMesh.AddTriangles(vertIndex + 1, vertIndex + verticesRes, vertIndex + verticesRes + 1);
                }

                vertIndex += 1;
            }
        }
        return finalMesh;
        
    }
    float[,] GetHeightValues(float runtimeOffsetIndexX, float runtimeOffsetIndexY) 
    {
        runtimeOffsetIndexX *= noiseSampleRes-1;
        runtimeOffsetIndexY *= noiseSampleRes-1;

        float[,] cumulativeHeight = new float[noiseSampleRes, noiseSampleRes];
        float[,] noiseHeightMap = HeightMaps.PerlinMap(
            noiseSampleRes, noiseScale, 
            noiseOffset.x + runtimeOffsetIndexX, noiseOffset.y + runtimeOffsetIndexY,
            persistence,lacunarity,octaves,seed
            );
        for (int y = 0; y < noiseSampleRes; y++)
        {
            for (int x = 0; x < noiseSampleRes; x++)
            {
                
                cumulativeHeight[x, y] += noiseHeightMap[x, y];
                cumulativeHeight[x, y] = heightCurve.Evaluate(cumulativeHeight[x, y]);
                cumulativeHeight[x, y] *= noiseHeight;
            }
        }
        if (heightMapAttribute.Length != 0) 
        {
            for (int i = 0; i < heightMapAttribute.Length; i++)
            {
                if (heightMapAttribute[i].heightMap != null) 
                {
                    float[,] textureHeightValue =
                                HeightMaps.TextureHeightMap(
                                    heightMapAttribute[i].heightMap,
                                    noiseSampleRes,
                                    runtimeOffsetIndexX - heightMapAttribute[i].offset.x * size,
                                    runtimeOffsetIndexY - heightMapAttribute[i].offset.y * size,
                                    heightMapAttribute[i].sampleScale
                                    
                                    );
                    if (textureHeightValue != null) 
                    {
                        for (int y = 0; y < noiseSampleRes; y++)
                        {
                            for (int x = 0; x < noiseSampleRes; x++)
                            {
                                float targetValue = cumulativeHeight[x, y] + textureHeightValue[x, y] * heightMapAttribute[i].height;
                                float blendValue =
                                    textureHeightValue[x, y] != -1 ?
                                    Mathf.Lerp(textureHeightValue[x, y] * heightMapAttribute[i].height, targetValue, heightMapAttribute[i].blend) :
                                    cumulativeHeight[x, y];
                                if (blendValue > cumulativeHeight[x, y])
                                {
                                    cumulativeHeight[x, y] = blendValue;
                                }
                                
                            }
                        }
                    }
                }
            }
        }
        
        return cumulativeHeight;
    }
    public void Clear()
    {
        if (mapParent != null && mapParent.transform.childCount != 0) 
        {
            GameObject[] childObjects = new GameObject[mapParent.transform.childCount];
            for (int i = 0; i < mapParent.transform.childCount; i++)
            {
                childObjects[i] = mapParent.transform.GetChild(i).gameObject;
            }
            foreach (GameObject g in childObjects)
            {
                DestroyImmediate(g);
            }
        }
       
    }


    public class MeshInfo 
    {
        public Vector3[] verticies;
        public int[] triangles;
        public Vector2[] uvs;

        int triangleIndex = 0;

        public Mesh mesh;

        public MeshInfo(int numRows) 
        {
            verticies = new Vector3[numRows * numRows];
            triangles = new int[(numRows - 1) * (numRows - 1) * 6];
            uvs = new Vector2[numRows * numRows];
        }

        public void AddTriangles(int a, int b, int c) 
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }

        public void ResetTriangleIndex() 
        {
            triangleIndex = 0;
        }
       
        public Mesh CreateMesh() 
        {
     
            mesh = new Mesh();
            UpdateMesh();
            return mesh;
        }
        public void UpdateVertices() 
        {
            mesh.vertices = verticies;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }

        public void UpdateMesh()
        {
            mesh.Clear();
            mesh.vertices = verticies;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }
        public void UpdateNumRows(int numRows) 
        {
            verticies = new Vector3[numRows * numRows];
            triangles = new int[(numRows - 1) * (numRows - 1) * 6];
            uvs = new Vector2[numRows * numRows];
        }
    }
}
