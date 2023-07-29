using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.MaterialProperty;

public class MouseBuildMeshControls : MonoBehaviour
{
    MeshChunk meshScript;

    public GameObject selector;
    public GameObject marker;

    public int dropType = 1;
    public bool[] clickedState = new bool[2];

    public GameObject markerPrefab;


    public void InitializedBlock(GameObject block)
    {
        MeshBlock blockMeshScript = block.GetComponent<MeshBlock>();
        blockMeshScript.SetSurfaceTiles(MeshBlock.defineSides(dropType));
    }
    public void DefineBlockPrefabEnd(GameObject blockPrefab)
    {
        if (selector) selector.transform.localScale = blockPrefab.transform.localScale;
    }
    public void InstantiatedBlock(GameObject block)
    {
    }
    public void InstantiatedInitializedBlock(GameObject block)
    {
    }
    public void FillFromDataBegin()
    {
    }

    public void FillFromDataFinished()
    {
    }


    public void RebuildBegin()
    {
        selector.SetActive(false);
        marker.SetActive(false);
    }





    // Start is called before the first frame update
    void Start()
    {
        meshScript = GetComponent<MeshChunk>();

        if(markerPrefab == null) markerPrefab = Resources.Load("WorldMesh/UIMarkingSphere") as GameObject;


        clickedState[0] = false;
        clickedState[1] = false;

    }



    // Update is called once per frame
    void Update()
    {
        Camera cam = null;

        GameObject[] foundList = GameObject.FindGameObjectsWithTag("CameraSwitchboard");
        foreach(GameObject obj in foundList)
        {
            CameraSwitchboard cameraSwitchboard = obj.GetComponent<CameraSwitchboard>();
            if(cameraSwitchboard != null)
            {
                cam = cameraSwitchboard.getCurrentCamera();
                break;
            }
        }
        if (cam == null) return;


        // In-game camera and editing
        if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftControl))
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!clickedState[0] && Physics.Raycast(ray, out hit, 1000.0f))
            {
                if (hit.collider.tag == "BuildBlockMesh")
                {
                    Vector3 blockPos = getNewBlockPosition(hit, true);
                    Vector3Int blockCoord = meshScript.getBlockCoordAtPosition(blockPos);
                    meshScript.addBlockAt(blockCoord);
                }
            }
            clickedState[0] = true;
        }
        else if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftControl))
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!clickedState[1] && Physics.Raycast(ray, out hit, 1000.0f))
            {
                if (hit.collider.tag == "BuildBlockMesh" && hit.transform == transform)
                {
                    if (meshScript.blockList.Count > 1)
                    {
                        Vector3 blockPos = getClickedBlockPosition(hit, true);
                        Vector3Int blockCoord = meshScript.getBlockCoordAtPosition(blockPos);
                        meshScript.removeBlockAt(blockCoord);
                    }
                }
            }
            clickedState[1] = true;
        }
        else
        {
            // Reset click states and markers after click
            clickedState[0] = false;
            clickedState[1] = false;
            if (selector != null) selector.SetActive(false);
            if (marker != null) marker.SetActive(false);

            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Debug.Log("A");
            if (Physics.Raycast(ray, out hit, 1000.0f))
            {
                Debug.Log("B");
                if (hit.collider.tag == "BuildBlockMesh" && hit.transform == gameObject.transform)
                {
                    Debug.Log("C");
                    Vector3 blockPos = getNewBlockPosition(hit, false);


                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        selector.SetActive(true);
                        selector.transform.position = blockPos;
                        selector.transform.localScale = meshScript.blockScale;

                    }

                    marker.SetActive(true);
                    marker.transform.position = getSurfaceCenter(hit, false);
                }
            }
        }

        int typecheck = dropType;
        if (Input.GetKeyDown(KeyCode.Keypad1) && Input.GetKey(KeyCode.LeftControl)) dropType = 1;
        if (Input.GetKeyDown(KeyCode.Keypad2) && Input.GetKey(KeyCode.LeftControl)) dropType = 2;
        if (typecheck != dropType) Debug.Log("new droptype: " + dropType);

        //        if (Input.GetKeyDown(KeyCode.Alpha3) && Input.GetKey(KeyCode.LeftControl)) dropType = 3;
        //        if (Input.GetKeyDown(KeyCode.Alpha4) && Input.GetKey(KeyCode.LeftControl)) dropType = 4;
        //        if (Input.GetKeyDown(KeyCode.Alpha5) && Input.GetKey(KeyCode.LeftControl)) dropType = 5;
    }


    public Vector3 getSurfaceCenter(RaycastHit hit, bool click)
    {
        Vector3 blockScale = meshScript.blockScale;

        Vector3 blockShift = new Vector3();
        blockShift.x = transform.position.x - Mathf.Round(transform.position.x);
        blockShift.y = transform.position.y - Mathf.Round(transform.position.y);
        blockShift.z = transform.position.z - Mathf.Round(transform.position.z);

        Vector3 blockPos = new Vector3(hit.point.x, hit.point.y, hit.point.z);
        Vector3 fsckyou = blockPos - blockShift + (blockScale / 2.0f);

        // if coordinate is for normal, mesh 'hit' depth is used (subtract shift to neutralize it later).
        //      Otherwise, clip the coordinates so hovering within a side 'finds' that side (and not elsewhere in the grid)
        if ((hit.normal.x == 0.0)) blockPos.x = Mathf.Floor(fsckyou.x / blockScale.x) * blockScale.x;
        else blockPos.x -= blockShift.x;
        if ((hit.normal.y == 0.0)) blockPos.y = Mathf.Floor(fsckyou.y / blockScale.y) * blockScale.y;
        else blockPos.y -= blockShift.y;
        if ((hit.normal.z == 0.0)) blockPos.z = Mathf.Floor(fsckyou.z / blockScale.z) * blockScale.z;
        else blockPos.z -= blockShift.z;

        //  Adjust the final result by the off-grid shift
        blockPos = blockPos + blockShift;

        return blockPos;
    }
    public Vector3 getClickedBlockPosition(RaycastHit hit, bool click)
    {
        Vector3 blockScale = meshScript.blockScale;
        Vector3 blockPos = getSurfaceCenter(hit, click) - (Vector3.Scale(blockScale, hit.normal) / 2.0f);
        return blockPos;
    }
    public Vector3 getNewBlockPosition(RaycastHit hit, bool click)
    {
        Vector3 blockScale = meshScript.blockScale;
        Vector3 blockPos = getSurfaceCenter(hit, click) + (Vector3.Scale(blockScale, hit.normal) / 2.0f);
        return blockPos;
    }
}
