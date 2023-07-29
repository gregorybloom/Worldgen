using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameControlRelay : MonoBehaviour
{
    public enum ControlMode { Editor, Debug, Testing, Normal };
    public ControlMode currentMode;


    // Start is called before the first frame update
    void Start()
    {
#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            currentMode = ControlMode.Editor;
        }
        else
        {
            currentMode = ControlMode.Testing;
        }
#else
        currentMode = ControlMode.Normal;
#endif


    }

    // Update is called once per frame
    void Update()
    {
    }
}
