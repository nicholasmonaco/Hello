using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GameManager : MonoBehaviour {
    #region Static Values

    //Note: these color values just set all 3 components of rgb as the value / 10f
    public static int MaxAmbientLight = 20; //in hsv
    public static int MinAmbientLight = 6; // in hsv
    private static Color MaxAmbientLightColor = new Color(0.23f, 0.23f, 0.23f, 1);
    private static Color MinAmbientLightColor = new Color(0.05f, 0.05f, 0.05f, 1);

    public static float MaxDistance = 20; //1000; //idk
    public static float DistanceMultiplier = 0;

    public static int MapSize = 10;

    #endregion


    #region Reference Variables

    public SoundManager SoundManager;

    public Color LightOffColor;

    public Material BaseDoorwayMaterial;
    public Material InvisibleDoorwayMaterial;

    [SerializeField] private Volume PostProcessingVolume;
    private FilmGrain _filmGrainEffect;

    public AudioSource GlobalAudioSource;
    [SerializeField] private Transform _roomHolder;
    [SerializeField] private Transform _entityHolder;
    [SerializeField] private MeshRenderer _staticRenderer;
    [SerializeField] private Transform _periphScarePoint;
    public FadeToBlack Fader;

    #endregion


    #region Gameplay Variables

    [HideInInspector] public PlayerController Player;
    public Transform PlayerTransform => Player.transform;
    public Vector3 PlayerPosition => Player.transform.position;


    // [HideInInspector] public Dictionary<Vector2Int, RoomController> Rooms;
    [HideInInspector] public Dictionary<Vector2Int, int> RoomGrid;
    [HideInInspector] public Dictionary<int, RoomController> RoomIndex;

    public RoomController Rooms(Vector2Int pos) {
        int roomIndex;
        if(RoomGrid.TryGetValue(pos, out roomIndex)) {
            RoomController room;
            if(RoomIndex.TryGetValue(roomIndex, out room)) {
                return room;
            }
        }

        return null;
    }
    public void SetRoom(RoomController room) {
        RoomIndex.Add(room.RoomId, room);
        foreach(Vector2Int gridPos in room.GridPositions) {
            RoomGrid.Add(gridPos, room.RoomId);
        }
    }

    public void RemoveRoom(RoomController room) {
        RoomIndex.Remove(room.RoomId);
        foreach (Vector2Int gridPos in room.GridPositions) {
            RoomGrid.Remove(gridPos);
        }
    }

    [HideInInspector] public RoomController CurrentRoom;
    public Vector2Int CurrentGridPos => RoomController.GridPosNearestPlayer;
    public Vector2Int LastGridPos = Vector2Int.zero;

    public int TraveledRooms = 0;

    public bool CanSeeWalkwayMonster => TraveledRooms > 10;
    public bool CanSeeMonster = false;

    [HideInInspector] public bool MonsterSpawned = false;

    [HideInInspector] public bool SeenWalkwayScare = false;
    [HideInInspector] public bool SeenForwardScare = false;


    public bool CanDie = false;
    private bool Dead = false;
    public bool DistanceEnemyActivated = false;

    #endregion


    #region Prefabs
    [SerializeField] private GameObject MonsterDistance_Prefab;
    [SerializeField] private GameObject ScarePeriph_Prefab;
    #endregion





    private void Awake() {
        GlobalAudioSource.volume = Game.VolumeScale;

        // Rooms = new Dictionary<Vector2Int, RoomController>(MapSize * MapSize);
        RoomGrid = new Dictionary<Vector2Int, int>();
        RoomIndex = new Dictionary<int, RoomController>();

        SetStatic(0);

        Game.Manager = this;

        //RandomizeAllDoorways();
    }

    private void Start() {
        // I don't know if this has to be in start or if it could be in awake, but whatever
        PostProcessingVolume.profile.TryGet(out _filmGrainEffect);
    }


    private void Update() {
        CurrentRoom.UpdateRoom();
    }


    public void UpdateLighting(float currentDistance) {
        if (!CanSeeMonster && currentDistance >= MaxDistance * 0.2f) {
            CanSeeMonster = true;
        }else if (!CanDie && currentDistance >= MaxDistance) {
            CanDie = true;
        }


        float distFrac = Mathf.Clamp01(currentDistance / MaxDistance);
        RenderSettings.ambientLight = Color.Lerp(MaxAmbientLightColor, MinAmbientLightColor, distFrac);
        _filmGrainEffect.intensity.value = distFrac;
        SoundManager.SetProgressStaticVolume(distFrac);
    }


    private bool _periphScareEnabled = true;

    public void TrySummonPeriphScare() {
        if (!_periphScareEnabled || !SeenWalkwayScare) return;

        bool periphRng = Random.Range(0, 100f) <= 0.08f;
        if (!periphRng) return;

        Instantiate(ScarePeriph_Prefab, _periphScarePoint.position, _periphScarePoint.rotation, _periphScarePoint.parent);
        _periphScareEnabled = false;

        StartCoroutine(ReEnablePeriphScare(25));
    }

    private IEnumerator ReEnablePeriphScare(float time) {
        yield return new WaitForSeconds(time);

        _periphScareEnabled = true;
    }


    [HideInInspector] public bool DistanceEnemySummoned = false;

    public void SummonDistanceEnemy() {
        if (!CanSeeMonster || MonsterSpawned || !SeenWalkwayScare) return;

        // chance of trying at all
        float rng = Random.Range(0, 100f);
        float rngTest = !SeenForwardScare ? 90 : 14;
        if (rng > rngTest) return;

        // Raycast out 4 directions from the player
        int roomDistCount = 2;
        float minSpawnDist = 14 * roomDistCount; //14 is the size of a room in the xz plane
        
        List<Vector2Int> dirs = new List<Vector2Int>{
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(0, 1)
        };

        Vector2Int dirToNewRoom = CurrentGridPos - LastGridPos;
        dirs.Remove(dirToNewRoom);

        foreach(Vector2Int dir in dirs) {
            Vector3 rayOrigin = CurrentRoom.transform.position + new Vector3(0, 2, 0);
            Vector3 rayDirection = new Vector3(dir.x, 0, dir.y);
            Ray ray = new Ray(rayOrigin, rayDirection);

            // raycast and then make sure it is still visible
            if (!Physics.Raycast(ray, out _, minSpawnDist)){
                //spawn enemy and make it face rayorigin
                //Vector3 rayOriginZeroY = new Vector3(rayOrigin.x, 0, rayOrigin.z);
                Quaternion facingOrigin = Quaternion.LookRotation(-rayDirection.normalized, Vector3.up);
                Vector3 spawnPoint = rayOrigin + (rayDirection * minSpawnDist);
                spawnPoint.y = 0;

                //spawn the enemy_distance
                Enemy_Distance enemy = Instantiate(MonsterDistance_Prefab, spawnPoint, facingOrigin, _entityHolder).GetComponent<Enemy_Distance>();

                List<RoomController> roomsOnPath = new List<RoomController>(1 + roomDistCount);

                //0 should be rayOrigin's room
                roomsOnPath.Add(CurrentRoom);
                CurrentRoom.KeepOpenDoors(dir);
                CurrentRoom.GetDoorInDirection(dir)?.SetDoorway(true);

                //count-1 should be spawnpoint's room
                //get all values in between those using gridpositions

                RoomController lastRoom = CurrentRoom;
                Vector2Int pos = CurrentGridPos;
                Vector2Int initialPos = pos;

                for(int i = 1; i <= roomDistCount; i++) {
                    pos = CurrentGridPos + dir * i;
                    if (!RoomGrid.ContainsKey(pos) || RoomGrid[pos] == lastRoom.RoomId) continue;

                    RoomController foundRoom = Rooms(pos);

                    roomsOnPath.Add(foundRoom);
                    foundRoom.KeepOpenDoors(dir);
                    foundRoom.GetDoorInDirection(dir)?.SetDoorway(true);
                    foundRoom.GetDoorInDirection(dir*-1)?.SetDoorway(true);
                }

                roomsOnPath[0].GetDoorInDirection(dir * -1)?.SetKeepOpen(false);

                roomsOnPath[roomsOnPath.Count - 1].GetDoorInDirection(dir)?.SetKeepOpen(false);
                roomsOnPath[roomsOnPath.Count - 1].GetDoorInDirection(dir)?.SetDoorway(false);

                enemy.Init(roomsOnPath, initialPos, pos, ScareType.WalkTowards);
                
                MonsterSpawned = true;
                DistanceEnemySummoned = true;

                DistanceMultiplier = 1;
                break;
            } else {
                continue;
            }
        }

    }


    public void SummonWalkwayEnemy() {
        if (!CanSeeWalkwayMonster || DistanceEnemyActivated || MonsterSpawned) return;

        // Debug.Log("trying to spawn walkway enemy...");

        // chance of trying at all
        float rng = Random.Range(0, 100f);
        if (!SeenWalkwayScare) rng = 0;

        if (rng > 30f) {
            // Debug.Log($"RNG roll failed for walkway enemy: {rng}");
            return;
        }

        // Raycast out direction forward from the player
        int roomDistCount = 2;
        float minSpawnDist = 14 * roomDistCount; //14 is the size of a room in the xz plane

        Vector2Int dir = CurrentGridPos - LastGridPos;

        // get direction player is looking and check if we can see that direction
        Vector3 playerLookDir = Camera.main.transform.forward;
        playerLookDir.y = 0;
        playerLookDir.Normalize();
        Vector3 abs = new Vector3(Mathf.Abs(playerLookDir.x), 0, Mathf.Abs(playerLookDir.z));
        Vector2Int lookDir = abs.x > abs.z ? new Vector2Int((int)Mathf.Sign(playerLookDir.x), 0) : new Vector2Int(0, (int)Mathf.Sign(playerLookDir.z));
        // lookDir *= -1;

        if (lookDir != dir) {
            // Debug.Log($"LookDir {lookDir} doesn't match spawnedDir {dir}");
            return;
        }

        Vector3 rayOrigin = CurrentRoom.transform.position + new Vector3(0, 2, 0);
        Vector3 rayDirection = new Vector3(dir.x, 0, dir.y);
        Ray ray = new Ray(rayOrigin, rayDirection);

        // raycast and then make sure it is still visible
        if (!Physics.Raycast(ray, out _, minSpawnDist)) {
            //spawn enemy and make it face rayorigin
            //Vector3 rayOriginZeroY = new Vector3(rayOrigin.x, 0, rayOrigin.z);
            Quaternion facingOrigin = Quaternion.LookRotation(-rayDirection.normalized, Vector3.up);
            Vector3 spawnPoint = rayOrigin + (rayDirection * minSpawnDist);
            spawnPoint.y = 0;

            //spawn the enemy_distance
            Enemy_Distance enemy = Instantiate(MonsterDistance_Prefab, spawnPoint, facingOrigin, _entityHolder).GetComponent<Enemy_Distance>();

            // Debug.Log("enemy successfully spawned");

            List<RoomController> roomsOnPath = new List<RoomController>(1 + roomDistCount);

            //0 should be rayOrigin's room
            roomsOnPath.Add(CurrentRoom);
            CurrentRoom.KeepOpenDoors(dir);
            CurrentRoom.GetDoorInDirection(dir)?.SetDoorway(true);

            //count-1 should be spawnpoint's room
            //get all values in between those using gridpositions

            RoomController lastRoom = CurrentRoom;
            Vector2Int pos = CurrentGridPos;
            Vector2Int initialPos = pos;

            for (int i = 1; i <= roomDistCount; i++) {
                pos = CurrentGridPos + dir * i;

                RoomController foundRoom = Rooms(pos);

                roomsOnPath.Add(foundRoom);
                foundRoom.KeepOpenDoors(dir);
                foundRoom.GetDoorInDirection(dir)?.SetDoorway(true);
                foundRoom.GetDoorInDirection(dir * -1)?.SetDoorway(true);
            }

            roomsOnPath[0].GetDoorInDirection(dir * -1)?.SetKeepOpen(false);

            roomsOnPath[roomsOnPath.Count - 1].GetDoorInDirection(dir)?.SetKeepOpen(false);
            roomsOnPath[roomsOnPath.Count - 1].GetDoorInDirection(dir)?.SetDoorway(false);

            enemy.Init(roomsOnPath, initialPos, pos, ScareType.SideWalk);

            MonsterSpawned = true;
            DistanceMultiplier = 0.3f;
        }
        //else {
        //    Debug.Log("walkway enemy raycast test failed");
        //}
    }


    public void SummonDistanceWatcherEnemy() {
        if (!SeenWalkwayScare || MonsterSpawned) return;

        // chance of trying at all
        float rng = Random.Range(0, 100f);
        if (rng > 15f) return;

        // Raycast out 4 directions from the player
        int roomDistCount = 5;
        float minSpawnDist = 14 * roomDistCount; //14 is the size of a room in the xz plane

        List<Vector2Int> dirs = new List<Vector2Int>{
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(0, 1)
        };

        Vector2Int dir;

        // get a door that isnt visible
        DoorwayController dirDoor = CurrentRoom.GetRandomUnseenDoor();
        if (dirDoor != null) {
            dir = dirDoor.FacingDirection;
        } else {
            return;
        }


        Vector2Int dirToNewRoom = CurrentGridPos - LastGridPos;
        dirs.Remove(dirToNewRoom);

        Vector3 rayOrigin = CurrentRoom.transform.position + new Vector3(0, 2, 0);
        Vector3 rayDirection = new Vector3(dir.x, 0, dir.y);


        //spawn enemy and make it face rayorigin
        Quaternion facingOrigin = Quaternion.LookRotation(-rayDirection.normalized, Vector3.up);
        Vector3 spawnPoint = rayOrigin + (rayDirection * minSpawnDist);
        spawnPoint.y = 0;

        //spawn the enemy_distance
        Enemy_Distance enemy = Instantiate(MonsterDistance_Prefab, spawnPoint, facingOrigin, _entityHolder).GetComponent<Enemy_Distance>();

        List<RoomController> roomsOnPath = new List<RoomController>(1 + roomDistCount);

        //0 should be rayOrigin's room
        roomsOnPath.Add(CurrentRoom);
        CurrentRoom.KeepOpenDoors(dir);
        CurrentRoom.GetDoorInDirection(dir)?.SetDoorway(false);

        //count-1 should be spawnpoint's room
        //get all values in between those using gridpositions
        RoomController lastRoom = CurrentRoom;
        Vector2Int pos = CurrentGridPos;
        Vector2Int initialPos = pos;

        for (int i = 1; i <= roomDistCount; i++) {
            pos = CurrentGridPos + dir * i;
            if (!RoomGrid.ContainsKey(pos) || RoomGrid[pos] == lastRoom.RoomId) continue;

            RoomController foundRoom = Rooms(pos);
            roomsOnPath.Add(foundRoom);
            foundRoom.ToggleLight(false, 0.3f);
            foundRoom.KeepOpenDoors(dir);
            foundRoom.GetDoorInDirection(dir)?.SetDoorway(false);
            foundRoom.GetDoorInDirection(dir * -1)?.SetDoorway(false);
        }

        roomsOnPath[0].GetDoorInDirection(dir * -1)?.SetKeepOpen(false);

        roomsOnPath[roomsOnPath.Count - 1].GetDoorInDirection(dir)?.SetKeepOpen(false);
        roomsOnPath[roomsOnPath.Count - 1].GetDoorInDirection(dir)?.SetDoorway(true);

        enemy.Init(roomsOnPath, initialPos, pos, ScareType.Distance);

        MonsterSpawned = true;
    }



    public void ResetAllHeldDoors() {
        List<Vector2Int> dirs = new List<Vector2Int>{
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(0, 1)
        };

        foreach (SquareRoomController room in RoomIndex.Values) {
            foreach(Vector2Int dir in dirs) {
                room.KeepOpenDoors(dir);
            }
        }
    }



    public void DelayCanDie() {
        StartCoroutine(EnableDeathPossibility());
    }

    private IEnumerator EnableDeathPossibility() {
        yield return new WaitForSeconds(15);
        CanDie = true;
    }


    public void DelayMonsterAllowance(float time) {
        StartCoroutine(EnableMonsterSpawn(time));
    }

    private IEnumerator EnableMonsterSpawn(float time) {
        yield return new WaitForSeconds(time);
        MonsterSpawned = false;
    }



    public void TryDie() {
        // Dying is disabled. This way of dying sucks.
        /*
         
        if (!CanDie || Dead) return;

        bool rng = Random.Range(0, 100f) <= 18f; //18% chance to die
        if (!rng) return;

        SquareRoomController lastRoom = Rooms[LastGridPos];

        Vector3 spawnPoint = lastRoom.transform.position + (lastRoom.transform.position + PlayerTransform.position)/2f;
        spawnPoint.y = 0;

        Vector3 ff = (PlayerTransform.position - spawnPoint + new Vector3(0,-4,0)).normalized;
        Quaternion facing = Quaternion.LookRotation(ff, Vector3.up);

        Enemy_Distance enemy = Instantiate(MonsterDistance_Prefab, spawnPoint + new Vector3(0, -0.5f,0), facing, _entityHolder).GetComponent<Enemy_Distance>();
        List<SquareRoomController> roomssss = new List<SquareRoomController>() {
            lastRoom,
            CurrentRoom
        };
        enemy.Init(roomssss, ScareType.Die);
        Dead = true;

        */

        //if the player enters a room, the enemy spawns in the last room they were in
        //if they leave the current room without seeing the enemy, then it despawns
        //if they turn around and see it, they die <- this isnt implemented yet
    }



    public void SetStatic(float amount) {
        _staticRenderer.material.SetFloat("_Alpha", amount);
    }

    private float GetCurrentStatic() {
        return _staticRenderer.material.GetFloat("_Alpha");
    }


    public IEnumerator BurstStatic(float maxAmount, float windUpTime, float windDownTime) {
        float timer = windUpTime;
        while(timer > 0) {
            timer -= Time.deltaTime;
            SetStatic((1-timer / windUpTime) * maxAmount);
            yield return null;
        }

        SetStatic(maxAmount);
        yield return new WaitForEndOfFrame();

        timer = windDownTime;
        while (timer > 0) {
            timer -= Time.deltaTime;
            SetStatic(timer / windDownTime * maxAmount);
            yield return null;
        }

        SetStatic(0);
    }

    public IEnumerator CoolOffStatic(float duration) {
        float orig = GetCurrentStatic();
        float timer = duration;
        while (timer > 0) {
            timer -= Time.deltaTime;
            SetStatic(timer / duration * orig);
            yield return null;
        }

        SetStatic(0);
    }

    public IEnumerator FadeUpStatic(float duration) {
        float orig = GetCurrentStatic();
        float timer = duration;
        while (timer > 0) {
            timer -= Time.deltaTime;
            SetStatic(1- (timer / duration * orig));
            yield return null;
        }

        SetStatic(1);
    }





    public void RandomizeAllDoorways() {
        SquareRoomController[] rooms = _roomHolder.GetComponentsInChildren<SquareRoomController>();

        for (int i = 0; i < rooms.Length; i++) {
            if (rooms[i] != null && rooms[i].GridPos != Vector2Int.zero) {
                rooms[i].SetAllDoorways_Random();
            }
        }
    }

}
