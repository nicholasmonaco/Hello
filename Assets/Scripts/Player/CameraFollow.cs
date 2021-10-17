using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField]
    private Transform _cameraParent;

    [HideInInspector]
    public bool FollowingPlayer = true;
    public float LookSpeed = 1;
    private Vector2 _rotation;

    [SerializeField] private float _vertLookLimit_min = 70; // Min angle (in degrees) when moving the camera vertically
    [SerializeField] private float _vertLookLimit_max = 34; // Max angle (in degrees) when moving the camera vertically


    // Start is called before the first frame update
    void Start() {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked; //move these to a game manager thing later

        _rotation.y = transform.eulerAngles.y;
    }

    // Update is called once per frame
    void Update() {
        if (FollowingPlayer)
            Follow();
    }


    void Follow() {
        if (!Game.Manager.Player.CanMove) return;

        Vector2 camMovement = Game.Input.Player.Look.ReadValue<Vector2>() * LookSpeed;

        _rotation.y += camMovement.x;
        _rotation.x += -camMovement.y;
        _rotation.x = Mathf.Clamp(_rotation.x, -_vertLookLimit_min, _vertLookLimit_max);

        _cameraParent.localRotation = Quaternion.Lerp(_cameraParent.localRotation, Quaternion.Euler(_rotation.x, _rotation.y, 0), Time.deltaTime * 25);
    }
}
