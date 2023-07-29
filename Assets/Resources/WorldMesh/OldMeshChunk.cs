using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class OldMeshChunk : MonoBehaviour
{
    public Vector3 blockScale = new Vector3(1.0f, 1.0f, 1.0f);
    public Vector3 center = new Vector3(1.0f, 1.0f, 1.0f);
    public Dictionary<string, ShortBlockData> blockList = new Dictionary<string, ShortBlockData>();

    public string spriteDataName = "blockSprites";
    public string saveDataName = "";

    public GameObject blockPrefab;
    public Material blockMaterial;

    public bool filterEdges = false;
    public bool rebuildNotCombine = false;

    [System.Serializable]
    public class SaveBlockMeshData : SavableData, ISaveDataMessageHooks
    {
        public int count;
        public int[] typeslist;
        public int[,] coordslist;
        public int[,,] sideslist;

        public SaveBlockMeshData()
        {
            count = 1;
            typeslist = new int[1] { -1 };
            coordslist = new int[1, 3] { { 0, 0, 0 } };
            sideslist = new int[1, 6, 2];
        }
        public SaveBlockMeshData(OldMeshChunk blockMesh)
        {
            count = blockMesh.blockList.Count;
            typeslist = new int[count];
            sideslist = new int[count, 6, 2];
            coordslist = new int[count, 3];
            int i = 0;
            foreach (KeyValuePair<string, ShortBlockData> blocks in blockMesh.blockList)
            {
                ShortBlockData block = blocks.Value;
                for (int j = 0; j < block.sides.coords.Length; j++)
                {
                    sideslist[i, j, 0] = block.sides.coords[j].x;
                    sideslist[i, j, 1] = block.sides.coords[j].y;
                }

                typeslist[i] = block.type;
                coordslist[i, 0] = block.coords.x;
                coordslist[i, 1] = block.coords.y;
                coordslist[i, 2] = block.coords.z;
                i++;
            }
        }
        public void LoadFrom(SaveBlockMeshData savedData)
        {
            Debug.Log("LOADING IN");
            count = savedData.count;
            typeslist = savedData.typeslist;
            sideslist = savedData.sideslist;
            coordslist = savedData.coordslist;

            for (int i = 0; i < savedData.sideslist.GetLength(0); i++)
            {
                for (int j = 0; j < savedData.sideslist.GetLength(1); j++)
                {
                    //                    Debug.Log(i + "," + j + " == " + savedData.sideslist[i, j,0] + "," + savedData.sideslist[i, j,1]);
                }
            }
        }

//        [field: NonSerialized]
        public void SaveData(BinaryFormatter bf, FileStream stream, SavableData target)
        {
            bf.Serialize(stream, target);
        }

//        [field: NonSerialized]
        public void LoadData(BinaryFormatter bf, FileStream stream, SavableData target)
        {
            SaveBlockMeshData data = bf.Deserialize(stream) as SaveBlockMeshData;
            if (target is SaveBlockMeshData)
            {
                ((SaveBlockMeshData)target).LoadFrom(data);
            }
        }
    }

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


    public void SaveThisData(SavableData target, BinaryFormatter bf, FileStream stream)
    {
        if (target is SaveBlockMeshData)
        {
            ((SaveBlockMeshData)target).SaveData(bf, stream, target);
        }
    }
    public void LoadThisData(SavableData target, BinaryFormatter bf, FileStream stream)
    {
        if (target is SaveBlockMeshData)
        {
            ((SaveBlockMeshData)target).LoadData(bf, stream, target);
        }
    }
    public void FillFromData(SavableData target)
    {
        if (target is SaveBlockMeshData)
        {
            SaveBlockMeshData serialized = (SaveBlockMeshData)target;

            ExecuteEvents.Execute<IBuildBlockMessageHooks>(gameObject, null, (x, y) => x.FillFromDataBegin());
            blockList.Clear();

            for (int i = 0; i < serialized.count; i++)
            {
                Vector2Int[] blockSides = new Vector2Int[serialized.sideslist.GetLength(1)];
                for (int j = 0; j < serialized.sideslist.GetLength(1); j++)
                {
                    blockSides[j].x = serialized.sideslist[i, j, 0];
                    blockSides[j].y = serialized.sideslist[i, j, 1];
                }

                ShortBlockData block = new ShortBlockData(serialized.typeslist[i], blockSides, serialized.coordslist[i, 0], serialized.coordslist[i, 1], serialized.coordslist[i, 2]);
                string label = getBlockCoordLabel(new Vector3Int(serialized.coordslist[i, 0], serialized.coordslist[i, 1], serialized.coordslist[i, 2]));
                blockList.Add(label, block);
            }

            ExecuteEvents.Execute<IBuildBlockMessageHooks>(gameObject, null, (x, y) => x.FillFromDataFinished());
        }
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

        if (!blockPrefab) blockPrefab = Resources.Load("WorldMesh/OldMeshBlock") as GameObject;

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
        OldMeshBlock meshScript = block.GetComponent<OldMeshBlock>();
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
                meshScript.SetSideTiles(coords);
            }
        }



        ExecuteEvents.Execute<IBuildBlockMessageHooks>(gameObject, null, (x, y) => x.InstantiatedInitializedBlock(block));

        return block;
    }
    private void prepareBlock(GameObject block, Vector3 blockCoord)
    {
        var classScript = block.GetComponent<OldMeshBlock>();
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
                classScript.SetSideVisibility(sides);
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
            _blockList[i].SetActive(false);
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
        if (saveDataName == "") saveDataName = "Scene1_" + gameObject.name;

        DefineBlockPrefab();

        center = new Vector3(transform.position.x, transform.position.y, transform.position.z);
    }

    private void Start()
    {
        if(blockList.Count == 0)
        {
            addBlockAt(new Vector3Int(0, 0, 0));
        }
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








public interface ISaveManagerMessageHooks : IEventSystemHandler
{
    // functions that can be called via the messaging system
    void SaveThisData(SavableData target, BinaryFormatter bf, FileStream stream);
    void LoadThisData(SavableData target, BinaryFormatter bf, FileStream stream);
}
public interface ISaveDataMessageHooks : IEventSystemHandler
{
    // functions that can be called via the messaging system
    void SaveData(BinaryFormatter bf, FileStream stream);
}

[System.Serializable]
public class SavableData : IEventSystemHandler, ISaveDataMessageHooks
{
    public void SaveData(BinaryFormatter bf, FileStream stream)
    {
        Debug.Log("NOTHING");
    }
    public void LoadData(BinaryFormatter bf, FileStream stream)
    {
        Debug.Log("MORE NOTHING");
    }

}

