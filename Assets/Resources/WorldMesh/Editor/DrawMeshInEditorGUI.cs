using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DrawMeshInEditor))]
public class DrawMeshInEditorGUI : Editor
{
    public override void OnInspectorGUI()
    {
        //        DrawDefaultInspector();

        DrawMeshInEditor myTarget = (DrawMeshInEditor)target;

        // Writable properties, but they don't appear to be saved on restart?
        EditorGUILayout.LabelField("Name", myTarget.gameObject.name);

        if (GUILayout.Button("Draw This"))
        {
            myTarget.SetMeshActor();
            myTarget.DrawMeshActor();
        }
        if (GUILayout.Button("Draw Clear"))
        {
            myTarget.ClearDrawMeshActor();
        }
    }

}
