using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitchboard : MonoBehaviour
{
    public Camera currentCam = null;

    [SerializeField]
    public Camera[] cameraList;

    [SerializeField]
    public int listPos = 0;



    // Start is called before the first frame update
    void Start()
    {
        listPos = 0;
        updateListCameras();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            IncrementCamera();
        }
    }

    void IncrementCamera()
    {
        listPos += 1;
        if (listPos >= cameraList.Length)
        {
            listPos = 0;
        }
        updateListCameras();
    }
    void updateListCameras()
    {
        // If no tilted camera fit the camera number, activate a normal camera for that number
        for (int i = 0; i < cameraList.Length; i++)
        {
            if (i == listPos) activateCamera(cameraList[i]);
            else deactivateCamera(cameraList[i]);
        }
    }

    public Camera getCurrentCamera()
    {
        for (int i = 0; i < cameraList.Length; i++)
        {
            if (cameraList[i].enabled) return cameraList[i];
        }
        return null;
    }

    // *** CAMERA SETUP ***
    public void activateCamera(Camera cam)
    {
        cam.enabled = true;
        setCameraDefaults(cam);
        currentCam = cam;

//        CTAA_PC script = cam.GetComponent<CTAA_PC>();
//        if (script) script.enabled = false;
    }
    public void deactivateCamera(Camera cam)
    {
        cam.enabled = false;
        setCameraDefaults(cam);

//        CTAA_PC script = cam.GetComponent<CTAA_PC>();
//        if (script) script.enabled = false;
    }
    void setCameraDefaults(Camera cam)
    {
        AudioListener audioListener = cam.GetComponent<AudioListener>();
        audioListener.enabled = cam.enabled;
    }


}
