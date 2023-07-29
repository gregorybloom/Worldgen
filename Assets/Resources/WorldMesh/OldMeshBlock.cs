using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OldMeshBlock : MonoBehaviour
{
    public GameObject masterMesh;

    public Material blockMaterial;
    public string spriteDataName = "blockSprites";
    SpriteDatabase.SpriteData spriteData;

    public Vector3 blockSize = new Vector3(1f, 1f, 1f);

    bool[] sideVisibility = { true, true, true, true, true, true };
    Vector2[,] sideUVs = new Vector2[6, 6];
    int[,] sideTileCoords = new int[6, 2];

    List<Vector2> uvs = new List<Vector2>();
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();


    float UVclipping = 0.0007f; // amount of textures to clip, as a fraction of a 1.0f side


    public interface IBlockMessageHooks : IEventSystemHandler
    {
        // functions that can be called via the messaging system
        void InitializedBlock(GameObject block);
    }



    public void InitializeBlock(GameObject obj, string spritename, Material material, OldMeshChunk.ShortBlockData data = null)
    {
        return;
        masterMesh = gameObject;
        spriteDataName = spriteDataName;
        blockMaterial = blockMaterial;
        Init();

        if (data != null)
        {
            int[,] coords = new int[6, 2];
            for (int i = 0; i < data.sides.coords.Length; i++)
            {
                coords[i, 0] = data.sides.coords[i].x;
                coords[i, 1] = data.sides.coords[i].y;
            }
            SetSideTiles(coords);
        }

    }


    public void SetSideVisibility(bool[] visible)
    {
        for (int j = 0; j < sideVisibility.Length; j++) sideVisibility[j] = visible[j];
    }
    public void SetSideTiles(int[,] coords)
    {
        for (int j = 0; j < sideTileCoords.GetLength(0); j++)
        {
            for (int k = 0; k < sideTileCoords.GetLength(1); k++)
            {
                sideTileCoords[j, k] = coords[j, k];
            }
        }
        FillDefaultUVdata();
    }
    private void FillDefaultUVdata()
    {
        SpriteDatabase SPRITE_DATABASE = EngineManager.Instance.SpriteDatabase;
        if (SPRITE_DATABASE == null) return;
        SpriteDatabase.SpriteData spriteData = SPRITE_DATABASE.dictSpriteData[spriteDataName];
        if (spriteData == null) return;

        float tilePercX = 1f / spriteData.tileCountX;
        float tilePercY = 1f / spriteData.tileCountY;

        for (int j = 0; j < VoxelData.voxelTriangleUVs.GetLength(0); j++)
        {
            float tileX = sideTileCoords[j, 0];    // tile position
            float tileY = sideTileCoords[j, 1];    // tile position

            float umin = tilePercX * tileX;
            float umax = tilePercX * (tileX + 1);

            float vmin = tilePercY * tileY;
            float vmax = tilePercY * (tileY + 1);

            for (int a = 0; a < VoxelData.voxelTriangleUVs.GetLength(1); a++)
            {
                float ux = umin;
                float vx = vmin;

                if (VoxelData.voxelTriangleUVs[j, a, 0] == 1) ux = umax;
                if (VoxelData.voxelTriangleUVs[j, a, 1] == 1) vx = vmax;


                float clipu = blockSize.x;
                float clipv = blockSize.y;
                if (VoxelData.voxelNormals[j].x != 0.0f) clipu = blockSize.y;
                if (VoxelData.voxelNormals[j].y != 0.0f) clipv = blockSize.z;

                // slightly trimming for seams
                if (VoxelData.voxelTriangleUVs[j, a, 0] == 0) ux += UVclipping * clipu;
                if (VoxelData.voxelTriangleUVs[j, a, 0] == 1) ux -= UVclipping * clipu;
                if (VoxelData.voxelTriangleUVs[j, a, 1] == 0) vx += UVclipping * clipv;
                if (VoxelData.voxelTriangleUVs[j, a, 1] == 1) vx -= UVclipping * clipv;


                sideUVs[j, a] = new Vector2(ux, vx);
            }
        }
    }

    /********************************/
    /*  BLOCK PREFAB MANIPULATIONS  */
    private void DefineBlockPrefab()
    {
        if (!blockMaterial) blockMaterial = Resources.Load("WorldMesh/BlockTestMaterial") as Material;
        GetComponent<Renderer>().sharedMaterial = blockMaterial;

        SpriteDatabase SPRITE_DATABASE = EngineManager.Instance.SpriteDatabase;
        if (SPRITE_DATABASE == null) return;
        spriteData = SPRITE_DATABASE.dictSpriteData[spriteDataName];
        if (spriteData == null) return;

        uvs.Clear();
        GetComponent<Renderer>().sharedMaterial.mainTexture.filterMode = FilterMode.Point;
        GetComponent<Renderer>().sharedMaterial.mainTexture.wrapMode = TextureWrapMode.Clamp;
    }

    private void FillCubePoints()
    {
        uvs.Clear();

        for (int j = 0; j < sideUVs.GetLength(0); j++)
        {
            if (!sideVisibility[j]) continue;
            for (int a = 0; a < sideUVs.GetLength(1); a++)
            {
                uvs.Add(new Vector2(sideUVs[j, a].x, sideUVs[j, a].y));
            }
        }

        vertices.Clear();
        triangles.Clear();
        int vertexIndex = 0;

        // where j is the cube sides
        for (int j = 0; j < VoxelData.voxelTris.GetLength(0); j++)
        {
            if (!sideVisibility[j]) continue;
            // where i is the triangle vertices
            for (int i = 0; i < VoxelData.voxelTris.GetLength(1); i++)
            {
                int triangleIndex = VoxelData.voxelTris[j, i];
                vertices.Add(Vector3.Scale(blockSize, VoxelData.voxelVerts[triangleIndex] - (Vector3.one / 2.0f)));
                triangles.Add(vertexIndex);

                vertexIndex++;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }
    public void Init()
    {
        uvs.Clear();

        DefineBlockPrefab();


        if (masterMesh)
        {
            ExecuteEvents.Execute<IBlockMessageHooks>(masterMesh, null, (x, y) => x.InitializedBlock(gameObject));
        }
        else
        {
            SetSideTiles(new int[,] { { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 } });
        }


        DrawMesh();
    }
    public void DrawMesh()
    {
        FillCubePoints();

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
    }
}



public static class VoxelData
{
    public static readonly Vector3[] voxelVerts = new Vector3[8]
    {
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 1.0f)
    };
    public static readonly int[,] voxelTris = new int[6, 6] {
        {0,3,1,1,3,2}, // Back Face
        {5,6,4,4,6,7}, // Front Face
        {3,7,2,2,7,6}, // Top Face
        {1,5,0,0,5,4}, // Bottom Face
        {4,7,0,0,7,3}, // Left Face
        {1,2,5,5,2,6} // Right Face
    };
    //  If camera is facing Z-
    public static readonly string[] voxelNames = new string[6] {
        "Back",
        "Front",
        "Top",
        "Bottom",
        "Right",
        "Left"
    };
    public static readonly Vector3[] voxelNormals = new Vector3[6] {
        new Vector3(0.0f, 0.0f, -1.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, -1.0f, 0.0f),
        new Vector3(-1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f)
    };
    public static readonly int[,,] voxelTriangleUVs = new int[6, 6, 2]
    {
        // front face
        { {0,0},{0,1},{1,0},{1,0},{0,1},{1,1} },
        // back face
        { {0,0},{0,1},{1,0},{1,0},{0,1},{1,1} },
        // top face
        { {0,0},{0,1},{1,0},{1,0},{0,1},{1,1} },
        // bottom face
        { {0,0},{0,1},{1,0},{1,0},{0,1},{1,1} },
        // right face
        { {0,0},{0,1},{1,0},{1,0},{0,1},{1,1} },
        // left face
        { {0,0},{0,1},{1,0},{1,0},{0,1},{1,1} }
    };

    /*
    //  UVs for Cube
    public static readonly int[,] voxelCubeUVs = new int[24, 2]
    {
        // Front Face
        {0,0},
        {1,0},
        {0,1},
        {1,1},
        // Top Face First (4 & 5)
        {0,1},
        {1,1},
        // Back Face First (6 & 7)
//        {0,1},
//        {1,1},
        {1,0},
        {0,0},
        // Top Face Second (8 & 9)
        {0,0},
        {1,0},
        // Back Face Second (10 & 11)
//        {0,0},
//        {1,0},
        {1,1},
        {0,1},
        // Bottom Face
        {0,0},
        {0,1},
        {1,1},
        {1,0},
        // Left Face
        {0,0},
        {0,1},
        {1,1},
        {1,0},
        // Right Face
        {0,0},
        {0,1},
        {1,1},
        {1,0}
    };
    /**/

}
