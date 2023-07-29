using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class MeshChunk : MonoBehaviour
{
    public Vector3 blockScale = new Vector3(1.0f, 1.0f, 1.0f);
    public Vector3 center = new Vector3(1.0f, 1.0f, 1.0f);
    public Dictionary<string, ShortBlockData> blockList = new Dictionary<string, ShortBlockData>();

    public string spriteDataName = "blockSprites";

    public GameObject blockPrefab;
    public Material blockMaterial;

    public bool filterEdges = false;
    public bool rebuildNotCombine = false;


    public interface IBuildNewBlockMessageHooks : IEventSystemHandler
    {
        void InitializeBlock(GameObject obj, string spritename, Material material, ShortBlockData data = null);
    }
    public interface IBuildBlockMessageHooks : IEventSystemHandler
    {
        // functions that can be called via the messaging system
        void InstantiatedBlock(GameObject block);
        void InstantiatedInitializedBlock(GameObject block);
        void RebuildBegin();
        void AddBlockEnd();
        void RemoveBlockEnd();
        void DefineBlockPrefabEnd(GameObject blockPrefab);
        void DefineBlockData(Vector3Int coord, string label, string act, ShortBlockData data);
        void FillFromDataBegin();
        void FillFromDataFinished();
    }



    public void BuildFromBlocklist()
    {
        Rebuild();
    }

    /********************************/
    /*  BLOCK PREFAB MANIPULATIONS  */
    private void DefineBlockPrefab()
    {
        if (!blockMaterial) blockMaterial = Resources.Load("WorldMesh/BlockTestMaterial") as Material;
        GetComponent<Renderer>().sharedMaterial = blockMaterial;

        if (!blockPrefab) blockPrefab = Resources.Load("GameModel/GameWorld/GameVoxels/SingleVoxel/ManuallyWrittenMesh") as GameObject;

        ExecuteEvents.Execute<IBuildBlockMessageHooks>(gameObject, null, (x, y) => x.DefineBlockPrefabEnd(blockPrefab));
    }
    private GameObject InstantiateBlock(Vector3 blockPos, Vector3 blockCoord, ShortBlockData data = null)
    {
        GameObject block = (GameObject)Instantiate(blockPrefab, blockPos, Quaternion.identity);
        block.transform.localScale = blockScale;

        block.GetComponent<Renderer>().sharedMaterial = blockMaterial;
        block.GetComponent<Renderer>().sharedMaterial.mainTexture.filterMode = FilterMode.Point;
        block.GetComponent<Renderer>().sharedMaterial.mainTexture.wrapMode = TextureWrapMode.Clamp;

        ExecuteEvents.Execute<IBuildBlockMessageHooks>(gameObject, null, (x, y) => x.InstantiatedBlock(block));

        //        ExecuteEvents.Execute<IBuildNewBlockMessageHooks>(block, null, (x, y) => x.InitializeBlock(gameObject,spriteDataName,blockMaterial,data));
        MeshBlock meshScript = block.GetComponent<MeshBlock>();
        if (meshScript)
        {
            meshScript.masterMesh = gameObject;
            meshScript.spriteDataName = spriteDataName;
            meshScript.blockMaterial = blockMaterial;
            meshScript.Init();

            if (data != null)
            {
                int[,] coords = new int[6, 2];
                for (int i = 0; i < data.sides.coords.Length; i++)
                {
                    coords[i, 0] = data.sides.coords[i].x;
                    coords[i, 1] = data.sides.coords[i].y;
                }
                meshScript.SetSurfaceTiles(coords);
            }
        }



        ExecuteEvents.Execute<IBuildBlockMessageHooks>(gameObject, null, (x, y) => x.InstantiatedInitializedBlock(block));

        return block;
    }
    private void prepareBlock(GameObject block, Vector3 blockCoord)
    {
        var classScript = block.GetComponent<MeshBlock>();
        if (classScript)
        {
            if (filterEdges)
            {
                bool[] sides = new bool[6];
                for (int a = 0; a < 6; a++)
                {
                    string label = getBlockCoordLabel(blockCoord + VoxelData.voxelNormals[a]);
                    sides[a] = !blockList.ContainsKey(label);
                }
                classScript.SetSurfaceVisibility(sides);
            }
            classScript.DrawMesh();
        }
    }

    /************************/
    /*  MESH MANIPULATIONS  */
    public void Combine(GameObject block)
    {
        GameObject[] _blockList = { block };
        Combine(_blockList, true);
    }
    private void Combine(GameObject[] _blockList, bool newCollider = false)
    {
        List<MeshFilter> meshFilters = new List<MeshFilter>();

        Vector2[] oldMeshUVs = null;
        if (GetComponent<MeshFilter>())
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                oldMeshUVs = GetComponent<MeshFilter>().sharedMesh.uv;
            }
            else
            {
                oldMeshUVs = GetComponent<MeshFilter>().mesh.uv;
            }
#endif
            meshFilters.Add(GetComponent<MeshFilter>());
        }


        int i = 0;
        for (i = 0; i < _blockList.Length; i++)
        {
            meshFilters.Add(_blockList[i].GetComponent<MeshFilter>());
        }

        CombineInstance[] combine = new CombineInstance[meshFilters.Count];
        if (newCollider)
        {
            MeshCollider[] meshcolliders = this.gameObject.GetComponents<MeshCollider>();
            for (i = 0; i < meshcolliders.Length; i++) Destroy(meshcolliders[i]);
        }

        Vector3 curPosition = transform.localPosition;
        //        transform.localPosition = Vector3.zero;

        i = 0;
        while (i < meshFilters.Count)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);
            i++;
        }

        Mesh targetMesh = null;
#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            targetMesh = transform.GetComponent<MeshFilter>().sharedMesh;
        }
        else
        {
            targetMesh = transform.GetComponent<MeshFilter>().mesh;
        }
#endif

        targetMesh = new Mesh();
        targetMesh.CombineMeshes(combine, true);


        // make new UV array
        int UVtotals = oldMeshUVs.Length;


#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            for (int n = 0; n < _blockList.Length; n++) UVtotals += _blockList[n].GetComponent<MeshFilter>().sharedMesh.uv.Length;
        }
        else
        {
            for (int n = 0; n < _blockList.Length; n++) UVtotals += _blockList[n].GetComponent<MeshFilter>().mesh.uv.Length;
        }
#endif

        Vector2[] newMeshUVs = new Vector2[UVtotals];
        // copy over all UVs
        for (i = 0; i < oldMeshUVs.Length; i++) newMeshUVs[i] += oldMeshUVs[i];
        i = oldMeshUVs.Length;
        for (int n = 0; n < _blockList.Length; n++)
        {
            Mesh sumMeshes = null;
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                sumMeshes = _blockList[n].GetComponent<MeshFilter>().sharedMesh;
            }
            else
            {
                sumMeshes = _blockList[n].GetComponent<MeshFilter>().mesh;
            }
#endif

            var uvlist = sumMeshes.uv;
            for (int m = 0; m < uvlist.Length; m++)
            {
                newMeshUVs[i] += sumMeshes.uv[m];
                i++;
            }
        }

        targetMesh.uv = newMeshUVs;

        targetMesh.RecalculateBounds();
        targetMesh.RecalculateNormals();
        MeshUtility.Optimize(targetMesh);

#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            transform.GetComponent<MeshFilter>().sharedMesh = targetMesh;
        }
        else
        {
            transform.GetComponent<MeshFilter>().mesh = targetMesh;
        }
#endif

        if (newCollider) this.gameObject.AddComponent<MeshCollider>();
        transform.localPosition = curPosition;
        transform.gameObject.SetActive(true);

        for (i = 0; i < _blockList.Length; i++)
        {
            _blockList[i].active = false;
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) DestroyImmediate(_blockList[i]);
            else Destroy(_blockList[i]);
#endif
        }
    }
    private void Rebuild()
    {
        ExecuteEvents.Execute<IBuildBlockMessageHooks>(gameObject, null, (x, y) => x.RebuildBegin());

        MeshCollider[] meshcolliders = this.gameObject.GetComponents<MeshCollider>();

        Mesh targetMesh = null;
#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            for (int i = 0; i < meshcolliders.Length; i++) DestroyImmediate(meshcolliders[i]);
            targetMesh = transform.GetComponent<MeshFilter>().sharedMesh;
        }
        else
        {
            for (int i = 0; i < meshcolliders.Length; i++) Destroy(meshcolliders[i]);
            targetMesh = transform.GetComponent<MeshFilter>().mesh;
        }
#endif

        if (targetMesh) targetMesh.Clear();

        targetMesh = new Mesh();
        targetMesh.RecalculateBounds();
        targetMesh.RecalculateNormals();
        MeshUtility.Optimize(targetMesh);

#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            transform.GetComponent<MeshFilter>().sharedMesh = targetMesh;
        }
        else
        {
            transform.GetComponent<MeshFilter>().mesh = targetMesh;
        }
#endif

        List<GameObject> rebuildBlocks = new List<GameObject>();
        foreach (KeyValuePair<string, ShortBlockData> stats in blockList)
        {
            int[] nums = System.Array.ConvertAll(stats.Key.Split(','), int.Parse);
            Vector3Int coord = new Vector3Int(nums[0], nums[1], nums[2]);
            Vector3 blockLocalPos = getBlockPositionAtCoord(coord, false);

            GameObject block = InstantiateBlock(blockLocalPos, coord, stats.Value);
            block.transform.parent = gameObject.transform;
            prepareBlock(block, coord);
            rebuildBlocks.Add(block);
        }

        Combine(rebuildBlocks.ToArray(), false);

        meshcolliders = this.gameObject.GetComponents<MeshCollider>();
#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            for (int i = 0; i < meshcolliders.Length; i++) DestroyImmediate(meshcolliders[i]);
        }
        else
        {
            for (int i = 0; i < meshcolliders.Length; i++) Destroy(meshcolliders[i]);
        }
#endif

        this.gameObject.AddComponent<MeshCollider>();
    }


    /*  MANIPULATE BLOCKLIST  */
    private string getBlockCoordLabel(Vector3 coord)
    {
        string label = (int)Mathf.Round(coord.x) + "," + (int)Mathf.Round(coord.y) + "," + (int)Mathf.Round(coord.z);
        return label;
    }
    private void editBlocklist(Vector3Int coord, string act)
    {
        string label = getBlockCoordLabel(coord);
        ShortBlockData data = new ShortBlockData();
        ExecuteEvents.Execute<IBuildBlockMessageHooks>(gameObject, null, (x, y) => x.DefineBlockData(coord, label, act, data));

        if (act == "add") blockList.Add(label, data);
        if (act == "remove") blockList.Remove(label);
        if (act == "edit") blockList[label] = data;
    }

    /*************************************/
    /*  MANIPULATE BLOCKS AND POSITIONS  */
    public Vector3Int getBlockCoordAtPosition(Vector3 pos, bool abs = true)
    {
        Vector3Int coordinate = new Vector3Int(0, 0, 0);
        Vector3 partly = pos;
        if (abs) partly -= center;
        coordinate.x = (int)Mathf.Round(partly.x / blockScale.x);
        coordinate.y = (int)Mathf.Round(partly.y / blockScale.y);
        coordinate.z = (int)Mathf.Round(partly.z / blockScale.z);
        return coordinate;
    }
    public Vector3 getBlockPositionAtCoord(Vector3Int coord, bool abs = true)
    {
        Vector3 position = new Vector3(0, 0, 0);
        position.x = ((float)coord.x) * blockScale.x;
        position.y = ((float)coord.y) * blockScale.y;
        position.z = ((float)coord.z) * blockScale.z;
        if (abs) position += center;
        return position;
    }
    public bool addBlockAt(Vector3Int blockCoord)
    {
        string label = getBlockCoordLabel(blockCoord);
        if (blockList.ContainsKey(label)) return false;

        Vector3 blockLocalPos = getBlockPositionAtCoord(blockCoord, false);
        editBlocklist(blockCoord, "add");

        if (rebuildNotCombine)
        {
            Rebuild();
        }
        else
        {
            GameObject block = InstantiateBlock(blockLocalPos, blockCoord);
            block.transform.parent = this.transform;
            prepareBlock(block, blockCoord);
            Combine(block);
        }
        ExecuteEvents.Execute<IBuildBlockMessageHooks>(gameObject, null, (x, y) => x.AddBlockEnd());

        return true;
    }
    public bool removeBlockAt(Vector3Int blockCoord)
    {
        string label = getBlockCoordLabel(blockCoord);
        if (!blockList.ContainsKey(label)) return false;

        editBlocklist(blockCoord, "remove");

        Rebuild();

        ExecuteEvents.Execute<IBuildBlockMessageHooks>(gameObject, null, (x, y) => x.RemoveBlockEnd());
        return true;
    }
    public bool editBlockAt(Vector3Int blockCoord)
    {
        string label = getBlockCoordLabel(blockCoord);
        if (!blockList.ContainsKey(label)) return false;

        editBlocklist(blockCoord, "edit");

        Rebuild();

        return true;
    }

    /*********************/
    /*  ACTOR FUNCTIONS  */

    // Start is called before the first frame update
    void Awake()
    {
        DefineBlockPrefab();

        center = new Vector3(transform.position.x, transform.position.y, transform.position.z);
    }

    // Update is called once per frame
    void Update()
    {

    }



    public class ShortBlockData
    {
        public struct BlockCoordSides
        {
            public Vector2Int[] coords;
            public BlockCoordSides(BlockCoordSides blocksides)
            {
                coords = new Vector2Int[6];
                if (blocksides.coords.Length == 6)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        coords[i].x = blocksides.coords[i].x;
                        coords[i].y = blocksides.coords[i].y;
                    }
                }
            }
            public BlockCoordSides(Vector2Int[] list)
            {
                coords = new Vector2Int[6];
                if (list.Length == 6)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        coords[i].x = list[i].x;
                        coords[i].y = list[i].y;
                    }
                }
            }
        }
        /**/

        public int type;
        public Vector3Int coords;
        public BlockCoordSides sides;
        public ShortBlockData() { }
        public ShortBlockData(int _type, Vector2Int[] _sides, int coordx, int coordy, int coordz)
        {
            setValues(_type, _sides, coordx, coordy, coordz);
        }
        public void setValues(int _type, Vector2Int[] _sides, int coordx, int coordy, int coordz)
        {
            type = _type;
            if (_sides != null)
            {
                if (_sides.Length == 6)
                {
                    sides = new BlockCoordSides(_sides);
                }
                else
                {
                    throw BlockException();
                }
            }
            coords = new Vector3Int(coordx, coordy, coordz);
        }

        private Exception BlockException()
        {
            throw new NotImplementedException();
        }
    }
}
