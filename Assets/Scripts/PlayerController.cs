using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : AbstractEntity
{
    [SerializeField]
    private CameraFollow _cameraFollow;

    [SerializeField] private AudioSource _footstepSoundEmitter;

    public float MoveSpeed = 5;

    [HideInInspector]
    public bool UserMoveEnabled;

    private Vector3 _lastPos;
    private float _traveledDistance = 0;

    private float _footstepTimer;
    private const float _footstepTimerMax = 0.6f;


    // Start is called before the first frame update
    protected override void Start() {
        base.Start();

        _lastPos = transform.position;
        _traveledDistance = 0;
        _footstepTimer = _footstepTimerMax;

        Game.Manager.Player = this;
    }

    // Update is called once per frame
    private void Update() {
        Move();

        //Debug
        //bool val = Game.Input.Player.Activate.ReadValue<float>() > 0;
        //if (val && last != val) {
        //    Game.Manager.CurrentRoom.ToggleLight();
        //}
        //last = val;
    }

    //debug
    //private bool last = false;

    protected override IEnumerator StartAlive() {
        UserMoveEnabled = true;

        yield return null;
    }


    private void Move() {
        if (!UserMoveEnabled)
            return;


        Vector2 v = Game.Input.Player.Move.ReadValue<Vector2>();

        if (v.x != 0 || v.y != 0) {
            if(_footstepTimer >= _footstepTimerMax) {
                _footstepSoundEmitter.PlayOneShot(_footstepSoundEmitter.clip);
                _footstepTimer = 0;
            }

            _footstepTimer += _deltaTime;

            // Get the forward and right directions based on the current camera view
            Vector3 camForward = Camera.main.transform.forward;
            camForward.y = 0;
            Vector3 camRight = new Vector3(-camForward.z, 0, camForward.x);

            // Construct the direction the player is moving in
            Vector3 moveVec = (camForward * v.y + camRight * -v.x).normalized;

            // Rotate the player model to face the new direction with a really fast interpolation
            Quaternion newDir = Quaternion.LookRotation(moveVec, -Vector3.Cross(camForward, camRight));
            MainModel.rotation = Quaternion.Lerp(MainModel.rotation, newDir, Time.deltaTime * 20);

            // Apply movement speed
            moveVec *= MoveSpeed;

            // Create the vector the player moves at; halves the speed of the player's movement when focusing
            //moveVec *= 2.5f * (Game.Input.Player.Sprint.ReadValue<Single>() == 1 ? 3 : 2);

            // Set the velocity vector of the rigidbody, leaving the y component alone
            MainRB.velocity = new Vector3(moveVec.x, MainRB.velocity.y, moveVec.z);
        }

        // Increment distance traveled
        Vector3 distanceVector = transform.position - _lastPos;
        _traveledDistance += distanceVector.magnitude * GameManager.DistanceMultiplier;
        _lastPos = transform.position;

        if(_traveledDistance != 0) {
            Game.Manager.UpdateLighting(_traveledDistance);
        }
    }
}
