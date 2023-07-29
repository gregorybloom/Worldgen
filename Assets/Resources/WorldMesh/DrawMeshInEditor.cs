using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

[ExecuteInEditMode]
public class DrawMeshInEditor : MonoBehaviour
{
#if UNITY_EDITOR

    // Start is called before the first frame update
    void Start()
    {
        MeshBlock meshScript = GetComponent<MeshBlock>();
        Renderer renderer = GetComponent<Renderer>();
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            // editor code here!
            var tempMaterial = new Material(meshScript.blockMaterial);
            renderer.sharedMaterial = tempMaterial;
        }
        else
        {
            if (meshScript != null)
            {
                renderer.sharedMaterial = meshScript.blockMaterial;
            }
        }

    }

    public void SetMeshActor()
    {
        MeshBlock meshScript = this.GetComponent<MeshBlock>();

        Debug.Log("SET MESH ACTOR");
        meshScript.BuildFromBlocklist();

        /*
        EditorManager UNITYEDITOR_MANAGEMENT = EditorManager;
        FZWorldEditorSaveManager WORLDEDITOR_SAVE = UNITYEDITOR_MANAGEMENT.WorldEditorSaveManager;

        MeshBlock.SaveBlockMeshData serialized = new MeshBlock.SaveBlockMeshData();
        string filename = meshScript.saveDataName + ".blocks";
        FZWorldEditorSaveManager.ReadFromFile(filename, gameObject, serialized);

        meshScript.FillFromData(serialized);
        meshScript.BuildFromBlocklist();
        /**/
    }
    public void DrawMeshActor()
    {
        Debug.Log("DRAW MESH ACTOR");
    }
    public void ClearDrawMeshActor()
    {
        MeshBlock meshScript = this.GetComponent<MeshBlock>();

        transform.GetComponent<MeshFilter>().sharedMesh.Clear();
        transform.GetComponent<MeshFilter>().sharedMesh = new Mesh();
        transform.GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();
        transform.GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();
        MeshUtility.Optimize(transform.GetComponent<MeshFilter>().sharedMesh);

        MeshCollider[] meshcolliders = this.gameObject.GetComponents<MeshCollider>();

        for (int i = 0; i < meshcolliders.Length; i++) DestroyImmediate(meshcolliders[i]);

        this.gameObject.AddComponent<MeshCollider>();
    }
#endif
}
