using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OldMeshBlock;
using UnityEngine.EventSystems;
using static SpriteDatabase;

public class MeshBlock : MonoBehaviour
{
    static float UVclipping = 0.0007f; // amount of textures to clip, as a fraction of a 1.0f side

    static int surfaceCount = 6;

    public string spriteDataName = "blockSprites";
    SpriteDatabase.SpriteData spriteData;


    public Vector3 blockSize = new Vector3(1f, 1f, 1f);

    public Material blockMaterial = null;
    public GameObject masterMesh = null;


    [SerializeField]
    bool[] surfaceVisibility = new bool[surfaceCount];
    [SerializeField]
    Vector2[,] surfaceUVs = new Vector2[surfaceCount, surfaceCount];
    [SerializeField]
    int[,] surfaceTileCoords = new int[surfaceCount, 2];


    [SerializeField]
    List<Vector2> uvs = new List<Vector2>();
    [SerializeField]
    List<Vector3> vertices = new List<Vector3>();
    [SerializeField]
    List<int> triangles = new List<int>();




    public static int[,] defineSides(int type)
    {
        if (type == 1)
        {
            int[,] sides = new int[,] {
                { 4, 11 }, { 3, 11 },   // back, front
                { 2, 11 }, { 2, 15 },   // top, bottom
                { 0, 11 }, { 3, 15 }    // right, left
            };
            return sides;
        }
        if (type == 2)
        {
            int[,] sides = new int[,] {
                { 4, 11 }, { 4, 11 },   // back, front
                { 2, 11 }, { 2, 15 },   // top, bottom
                { 4, 11 }, { 4, 15 }    // right, left
            };
            return sides;
        }
        return null;
    }
    public void SetSurfaceVisibility(bool[] visible)
    {
        for (int j = 0; j < visible.Length; j++) surfaceVisibility[j] = visible[j];
    }
    public void SetSurfaceTiles(int[,] coords)
    {
        for (int j = 0; j < coords.GetLength(0); j++)
        {
            for (int k = 0; k < coords.GetLength(1); k++)
            {
                surfaceTileCoords[j, k] = coords[j, k];
            }
        }
        FillVoxelSurfaces();
    }



    private void FillMeshArrays()
    {
        uvs.Clear();
        vertices.Clear();
        triangles.Clear();

        for (int j = 0; j < surfaceUVs.GetLength(0); j++)
        {
            if (!surfaceVisibility[j]) continue;
            for (int a = 0; a < surfaceUVs.GetLength(1); a++)
            {
                uvs.Add(new Vector2(surfaceUVs[j, a].x, surfaceUVs[j, a].y));
            }
        }


        int vertexIndex = 0;

        // where j is the cube sides
        for (int j = 0; j < VoxelData.voxelTris.GetLength(0); j++)
        {
            if (!surfaceVisibility[j]) continue;
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
    private void FillVoxelSurfaces()
    {
        SpriteDatabase SPRITE_DATABASE = EngineManager.Instance.SpriteDatabase;
        if (SPRITE_DATABASE == null) return;
        SpriteDatabase.SpriteData spriteData = SPRITE_DATABASE.dictSpriteData[spriteDataName];
        if (spriteData == null) return;

        Debug.Log("FILL VALUE");
        // select new tile coord?

        float tilePercX = 1f / spriteData.tileCountX;
        float tilePercY = 1f / spriteData.tileCountY;

        for (int j = 0; j < VoxelData.voxelTriangleUVs.GetLength(0); j++)
        {
            float tileX = surfaceTileCoords[j, 0];    // tile position
            float tileY = surfaceTileCoords[j, 1];    // tile position

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

                surfaceUVs[j, a] = new Vector2(ux, vx);
            }
        }
    }


    public void BuildFromBlocklist()
    {
        Debug.Log("Build From Blocklist");
        DrawMesh();
    }
    private void DefineBlockPrefab()
    {
        if (blockMaterial == null)
        {
            blockMaterial = Resources.Load("WorldMesh/BlockTestMaterial") as Material;
        }
        GetComponent<Renderer>().sharedMaterial = blockMaterial;

        SpriteDatabase SPRITE_DATABASE = EngineManager.Instance.SpriteDatabase;
        if (SPRITE_DATABASE == null) return;
        spriteData = SPRITE_DATABASE.dictSpriteData[spriteDataName];
        if (spriteData == null) return;

        uvs.Clear();
        GetComponent<Renderer>().sharedMaterial.mainTexture.filterMode = FilterMode.Point;
        GetComponent<Renderer>().sharedMaterial.mainTexture.wrapMode = TextureWrapMode.Clamp;
    }


    // ***************************************************************************************************

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
            Debug.Log("MASTER FOUND");
            //            ExecuteEvents.Execute<IBlockMessageHooks>(masterMesh, null, (x, y) => x.InitializedBlock(gameObject));
        }
        else
        {
            SetSurfaceTiles(new int[,] { { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 } });
        }


        DrawMesh();
    }
    public void DrawMesh()
    {
        FillMeshArrays();

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
    }












}
