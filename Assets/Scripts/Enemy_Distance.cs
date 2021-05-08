using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Enemy_Distance : MonoBehaviour
{
    //if it's been seen for more than x seconds, play sfx and disappearing effect + light flickers
    //if not seen for that long, and current room changes + not being seen, despawn and set distance enemy flag back

    private static readonly float MaxSeenTime = 0.45f; //subject to change
    private static readonly float MovementSpeed = 4.5f; //subject to change
    
    private float _seenTime = 0;
    private bool _scareTriggered = false;

    [SerializeField] private AudioClip _scareSFX;
    [SerializeField] private AudioClip _walkScareSFX;
    [SerializeField] private AudioClip _dieScareSFX;

    [SerializeField] private Renderer _renderer;
    [SerializeField] private Animator _animator;
    [SerializeField] private Collider _collider;
    [SerializeField] private Rigidbody _rigidbody;

    [SerializeField] private Transform _facePoint;
    [SerializeField] private Transform _shadow;

    private Vector3 _originRoomPos; // The position of the room that was used as a base when it spawned
    private Vector3 _spawnRoomPos; // The initial position of the monster
    private List<RoomController> _roomsInPath; // The rooms from the origin room to the spawning room
    private RoomController _originRoom;
    private RoomController _spawnRoom;

    private Renderer[] _allRenderers;
    private Animator[] _allAnimators;



    private void Awake() {
        _allRenderers = GetComponentsInChildren<Renderer>();
        _allAnimators = GetComponentsInChildren<Animator>();
    }


    public void Init(List<RoomController> roomsInPath, ScareType scareType) {
        _roomsInPath = new List<RoomController>(roomsInPath);
        _originRoom = roomsInPath[0];
        _spawnRoom = roomsInPath[roomsInPath.Count - 1];

        _originRoomPos = _originRoom.transform.position + new Vector3(0, 4, 0);
        _spawnRoomPos = _spawnRoom.transform.position + new Vector3(0, 4, 0);

        switch (scareType) {
            case ScareType.SideWalk:
                //repositoin logic
                Vector3 towardsOrigin = _originRoomPos - _spawnRoomPos;
                towardsOrigin = new Vector3(towardsOrigin.z, 0, -towardsOrigin.x).normalized;

                transform.position -= towardsOrigin * 8; //7 is half room size + 1 for extra
                transform.rotation = Quaternion.LookRotation(towardsOrigin, Vector3.up);

                //update logic
                UpdateAction = SideWalkUpdate;
                break;
            case ScareType.WalkTowards:
                UpdateAction = WalkTowardsUpdate;
                break;
            case ScareType.Die:
                UpdateAction = () => {
                    _scareTriggered = true;
                    _collider.enabled = false;
                    _rigidbody.useGravity = false;
                    _shadow.gameObject.SetActive(false);

                    StartCoroutine(Kill());
                };
                break;
        }
    }


    public System.Action UpdateAction = () => { };

    private void Update() {
        if (_scareTriggered) return;

        UpdateAction();
    }

    public void WalkTowardsUpdate() {
        if (_renderer.isVisible) { //do we want it to have both renderers be used for this or not?
            _seenTime += Time.deltaTime;

            if (_seenTime >= MaxSeenTime) {
                _scareTriggered = true;
                StartCoroutine(Scare());
            }
        } else if (Game.Manager.CurrentGridPos != _originRoom.GridPos) {
            if (!_renderer.isVisible) {
                //despawn immediately
                Destroy(this.gameObject);
            }
        }
    }

    public void SideWalkUpdate() {
        _scareTriggered = true;

        _collider.enabled = false;
        _rigidbody.useGravity = false;

        StartCoroutine(WalkScare());
    }




    private bool AnyRendererVisible {
        get {
            foreach(Renderer r in _allRenderers) {
                if (r.isVisible) return true;
            }
            return false;
        }
    }

    private IEnumerator Scare() {
        const float scareDuration = 4f;
        float scareCheck = scareDuration * 0.55f;

        //play sfx
        Game.Manager.GlobalAudioSource.PlayOneShot(_scareSFX, 0.4f);

        //burst static + whatever else
        IEnumerator burst = Game.Manager.BurstStatic(0.3f, scareCheck * 0.4f, scareCheck * 0.6f + 0.02f);
        StartCoroutine(burst);

        //turn off light in room it's in
        _spawnRoom.ToggleLight(false);

        //slow player walking movement speed
        //todo

        // Loop logic

        float timer = scareDuration;
        bool animStarted = false;

        Vector3 towardsOrigin = _originRoomPos - _spawnRoomPos;
        towardsOrigin.y = 0;
        towardsOrigin.Normalize();

        while(timer > 0) {
            yield return null;

            timer -= Time.fixedDeltaTime;

            //drag camera towards looking here
            //todo

            //flicker lights in rooms towards it
            foreach (RoomController room in _roomsInPath) {
                if(Random.Range(0, 10f) <= 1.5f) room.ToggleLight();
            }

            // Check to walk
            if (timer < scareCheck) {
                if (!animStarted) {
                    //play walking animation
                    animStarted = true;
                    float flux = 1f / MovementSpeed; //the ratio between physical movement speed and animation speed //subject to change

                    foreach (Animator a in _allAnimators) {
                        a.SetInteger("State", (int)EnemyDistanceAnimState.Walking);
                        a.speed *= MovementSpeed * flux;
                    }

                    StopCoroutine(burst);
                }

                // move towards the origin room slowly
                transform.position += towardsOrigin * MovementSpeed * Time.fixedDeltaTime;
                Game.Manager.SetStatic(Mathf.Clamp01(1 - (timer / scareDuration)));
            }
        }


        // turn off all lights on path and superfuzz static
        foreach(RoomController room in _roomsInPath) {
            room.ToggleLight(false);
        }

        //superfuzz static
        Game.Manager.SetStatic(Mathf.Clamp01(1 - (timer / scareDuration)));

        // wait a bit
        yield return new WaitForSeconds(0.3f);

        // disable the renderer
        foreach(Renderer r in _allRenderers) {
            r.enabled = false;
        }

        //turn lights back on
        foreach (RoomController room in _roomsInPath) {
            room.ToggleLight(true);
        }

        // cool off static
        Game.Manager.StartCoroutine(Game.Manager.CoolOffStatic(1.2f));

        //restore player walking movement speed
        //todo

        // allow player death 
        //Game.Manager.DelayCanDie();

        // destroy this
        Destroy(this.gameObject);
    }

    public IEnumerator WalkScare() {
        const float scareDuration = 4f;

        //play sfx
        Game.Manager.GlobalAudioSource.PlayOneShot(_walkScareSFX, 0.4f);

        //burst static + whatever else
        IEnumerator burst = Game.Manager.BurstStatic(0.25f, scareDuration * 0.5f, scareDuration * 0.5f);
        StartCoroutine(burst);

        //turn off light in room it's in
        _spawnRoom.ToggleLight(false);

        // loop logic
        //play walking animation
        float flux = 1f / MovementSpeed * 1.2f; //the ratio between physical movement speed and animation speed //subject to change

        foreach (Animator a in _allAnimators) {
            a.SetInteger("State", (int)EnemyDistanceAnimState.Walking);
            a.speed *= MovementSpeed * flux;
        }

        //StopCoroutine(burst);

        float timer = scareDuration;

        Vector3 towardsOrigin = _originRoomPos - _spawnRoomPos;
        towardsOrigin = new Vector3(towardsOrigin.z, 0, -towardsOrigin.x).normalized;

        //transform.position -= towardsOrigin * 18; //14 is room size + 4 for extra
        //transform.rotation = Quaternion.LookRotation(towardsOrigin, Vector3.up);

        while (timer > 0) {
            yield return null;

            timer -= Time.fixedDeltaTime;

            //drag camera towards looking here
            //todo

            //flicker lights in room
            if (Random.Range(0, 10f) <= 1.5f) _spawnRoom.ToggleLight();

            // walk
            transform.position += towardsOrigin * MovementSpeed * Time.fixedDeltaTime;
        }

        // turn light back on
        _spawnRoom.ToggleLight(true);

        // cool off static
        StopCoroutine(burst);
        Game.Manager.StartCoroutine(Game.Manager.CoolOffStatic(1.2f));

        // destroy this
        Destroy(this.gameObject);
    }


    private IEnumerator Kill() {
        //stop player mouse movement
        Game.Manager.Player.UserMoveEnabled = false;
        Game.Manager.Player.CanMove = false;

        //play sound effect
        Game.Manager.GlobalAudioSource.PlayOneShot(_dieScareSFX, 0.4f);

        //rptate to face face
        const float rotateTimer = 1.5f;
        float timer = rotateTimer;
        Quaternion origCamRot = Camera.main.transform.rotation;

        Vector3 dir = (_facePoint.position + new Vector3(0, 4, 0) - Camera.main.transform.position).normalized;
        Quaternion facing = Quaternion.LookRotation(dir);

        while(timer > 0) {
            timer -= Time.deltaTime;
            Camera.main.transform.rotation = Quaternion.Lerp(facing, origCamRot, timer / rotateTimer);
            Game.Manager.SetStatic((1 - timer / rotateTimer) / 3f);
            yield return null;
        }

        Camera.main.transform.rotation = facing;
        yield return new WaitForEndOfFrame();

        //fuzz
        Game.Manager.StartCoroutine(Game.Manager.FadeUpStatic(1));

        //fade to black
        yield return StartCoroutine(Game.Manager.Fader.Fade_C(1.5f));

        //wait
        yield return new WaitForSeconds(1.5f);

        //go to main menu
        SceneManager.LoadScene(0);
    }


    private void OnDestroy() {
        if (!_scareTriggered) {
            Game.Manager.CanSeeMonster = true;
        }

        Vector2Int dir = _spawnRoom.GridPos - _originRoom.GridPos;
        dir = new Vector2Int((int)Mathf.Abs(Mathf.Sign(dir.x)), (int)Mathf.Abs(Mathf.Sign(dir.y)));

        foreach(RoomController room in _roomsInPath) {
            room.KeepOpenDoors(dir, false);
        }

        Game.Manager.MonsterSpawned = false;
    }
}

internal enum EnemyDistanceAnimState {
    Idle = 0,
    Walking = 1
}

public enum ScareType {
    SideWalk,
    WalkTowards,
    Die
}
