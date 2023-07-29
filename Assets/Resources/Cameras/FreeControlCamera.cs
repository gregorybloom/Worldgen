using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FreeControlCamera : MonoBehaviour
{
    [SerializeField]
    public bool takeoverControls = false;
    public bool takeoverOn = false;

    [SerializeField]
    private float PixelsPerUnit = 16f;
    [SerializeField]
    private float flatSpeed = 1f;

    private bool isMoving = false;
    private float moveHorizontal = 0f;
    private float moveVertical = 0f;
    private float moveForward = 0f;
    private float rotateSideways = 0f;

    [SerializeField]
    private float _rotateSensitivity = 0.2f;

    private Vector3 _mouseReference;
    private Vector3 _mouseOffset;
    private Vector3 _rotation;
    private bool _isRotating = false;

    // Rotational Movement
    [SerializeField]
    private float rotateMovementSpeed = 4.0f;

    Vector3 _rotatedmove;

    private void Start()
    {
        _rotation = new Vector3(90f, 0f, 0f);
        takeoverOn = takeoverControls;
    }

    private void Update()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null || cam.enabled == false) return;


        moveHorizontal = 0;
        moveVertical = 0;
        moveForward = 0;
        rotateSideways = 0;
        _rotatedmove = Vector3.zero;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            takeoverOn = false;
        }
        else
        {
            takeoverOn = true;
        }


        UpdateControlRelay();

        if (!gameObject.GetComponent<Camera>().enabled) return;



        if (Input.GetMouseButton(1))
        {
            if (Input.GetKey(KeyCode.W)) _rotatedmove.z += 1;
            if (Input.GetKey(KeyCode.S)) _rotatedmove.z += -1;
            if (Input.GetKey(KeyCode.D)) _rotatedmove.x += 1;
            if (Input.GetKey(KeyCode.A)) _rotatedmove.x += -1;
            if (Input.GetKey(KeyCode.E)) _rotatedmove.y += 1;
            if (Input.GetKey(KeyCode.Q)) _rotatedmove.y += -1;
            if (Input.GetKey(KeyCode.Z)) rotateSideways -= 1;
            if (Input.GetKey(KeyCode.X)) rotateSideways += 1;
            _rotatedmove = Quaternion.Euler(_rotation.x, _rotation.y, _rotation.z) * _rotatedmove;
        }

        isMoving = (Mathf.Abs(moveHorizontal) + Mathf.Abs(moveVertical)) > 0;
        if (!isMoving) isMoving = (Mathf.Abs(_rotatedmove.x) + Mathf.Abs(_rotatedmove.y) + Mathf.Abs(_rotatedmove.z)) > 0;

        if (Input.GetMouseButton(1))
        {
            if (!_isRotating)
            {
                // store mouse
                _mouseReference = Input.mousePosition;
            }
            // rotating flag
            _isRotating = true;
        }
        else
        {
            _isRotating = false;
        }

        if (_isRotating)
        {
            // offset
            _mouseOffset = (Input.mousePosition - _mouseReference);
            // apply rotation
            _rotation.x += -(_mouseOffset.y) * _rotateSensitivity;
            _rotation.y += (_mouseOffset.x) * _rotateSensitivity;
            _rotation.z = 0;

            if (_rotation.x > 360) _rotation.x -= 360;
            if (_rotation.x < 0) _rotation.x += 360;
            if (_rotation.y > 360) _rotation.y -= 360;
            if (_rotation.y < 0) _rotation.y += 360;
            if (_rotation.z > 360) _rotation.z -= 360;
            if (_rotation.z < 0) _rotation.z += 360;
            // rotate
            transform.rotation = Quaternion.Euler(_rotation.x, _rotation.y, _rotation.z);

            // store mouse
            _mouseReference = Input.mousePosition;  /**/
        }

    }


    void FixedUpdate()
    {
        if (isMoving)
        {
            if ((Mathf.Abs(moveHorizontal) + Mathf.Abs(moveVertical)) > 0)
            {
                Vector3 tmp = transform.position + new Vector3(moveHorizontal, 0, moveVertical).normalized * flatSpeed * Time.deltaTime;

                // Round if walking on a different axis (to not affect movement)
                if (Mathf.Abs(moveHorizontal) == 0) tmp.x = Mathf.Round(tmp.x * PixelsPerUnit) / PixelsPerUnit;
                if (Mathf.Abs(moveVertical) == 0) tmp.z = Mathf.Round(tmp.z * PixelsPerUnit) / PixelsPerUnit;

                transform.position = tmp;
            }
            else if ((Mathf.Abs(_rotatedmove.x) + Mathf.Abs(_rotatedmove.y) + Mathf.Abs(_rotatedmove.z)) > 0)
            {
                Vector3 tmp = transform.position + _rotatedmove * rotateMovementSpeed * Time.deltaTime;
                transform.position = tmp;
            }
        }
        if (rotateSideways != 0)
        {
            Vector3 point = transform.forward;
            transform.Rotate(point, rotateSideways * 15 * Time.deltaTime);
        }
    }


    void UpdateControlRelay()
    {
        return;
/*        FZGameControlRelay CONTROL_RELAY = FZApplicationManagement.GameControl.GameControlRelay;

        if (CONTROL_RELAY != null)
        {
            bool isCameraActive = CONTROL_RELAY.isControlActorActive(gameObject.name);
            if (gameObject.GetComponent<Camera>().enabled)
            {
                if (takeoverControls && !takeoverOn)
                {
                    if (isCameraActive)
                    {
                        CONTROL_RELAY.deactivateAllControlActors();
                        string[] names = new string[] { "gameCharacter" };
                        CONTROL_RELAY.activateControlActors(names);
                    }
                    return;
                }
                if (takeoverControls && takeoverOn && !isCameraActive)
                {
                    CONTROL_RELAY.deactivateAllControlActors();
                    string[] names = new string[] { gameObject.name };
                    CONTROL_RELAY.activateControlActors(names);
                }
            }
            else if (!isCameraActive && takeoverControls)
            {
                return;
            }
        }
/**/
    }
}
