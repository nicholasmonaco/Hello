using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GameManager : MonoBehaviour
{
    //Note: these color values just set all 3 components of rgb as the value / 10f
    public static int MaxAmbientLight = 20; //in hsv
    public static int MinAmbientLight = 6; // in hsv
    private static Color MaxAmbientLightColor = new Color(0.17f, 0.17f, 0.17f, 1);
    private static Color MinAmbientLightColor = new Color(0.05f, 0.05f, 0.05f, 1);

    public static float MaxDistance = 500; //idk
    public static float DistanceMultiplier = 0;

    public static int MapSize = 10;



    public Color LightOffColor;

    public Material BaseDoorwayMaterial;
    public Material InvisibleDoorwayMaterial;

    [SerializeField] private Volume PostProcessingVolume;
    private FilmGrain _filmGrainEffect;

    public AudioSource GlobalAudioSource;
    [SerializeField] private Transform _roomHolder;
    [SerializeField] private Transform _entityHolder;
    [SerializeField] private MeshRenderer _staticRenderer;
    public FadeToBlack Fader;

    [HideInInspector] public Dictionary<Vector2Int, RoomController> Rooms;
    [HideInInspector] public RoomController CurrentRoom;
    public Vector2Int CurrentGridPos => CurrentRoom.GridPos;
    public Vector2Int LastGridPos = Vector2Int.zero;

    public int TraveledRooms = 0;

    public bool CanSeeMonster = false;
    [HideInInspector] public bool MonsterSpawned = false;

    public bool CanDie = false;


    [HideInInspector] public PlayerController Player;
    public Transform PlayerTransform => Player.transform;
    public Vector3 PlayerPosition => Player.transform.position;

    #region Prefabs
    [SerializeField] private GameObject MonsterDistance_Prefab;
    #endregion



    private void Awake() {
        GlobalAudioSource.volume = Game.VolumeScale;

        Rooms = new Dictionary<Vector2Int, RoomController>(MapSize * MapSize);
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
        if (!CanSeeMonster && currentDistance >= MaxDistance * 3 / 5f) {
            CanSeeMonster = true;
        }else if (!CanDie && currentDistance >= MaxDistance) {
            CanDie = true;
        }

        RenderSettings.ambientLight = Color.Lerp(MaxAmbientLightColor, MinAmbientLightColor, currentDistance / MaxDistance);
        _filmGrainEffect.intensity.value = Mathf.Clamp01(currentDistance / MaxDistance);
    }



    [HideInInspector] public bool DistanceEnemySummoned = false;

    public void SummonDistanceEnemy() {
        if (!CanSeeMonster || MonsterSpawned || !SideWalkMonsterSpawned) return;

        // chance of trying at all
        float rng = Random.Range(0, 100f);
        if (rng > 12f) return;

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
                CurrentRoom.GetDoorInDirection(dir).SetDoorway(true);

                //count-1 should be spawnpoint's room
                //get all values in between those using gridpositions
                for(int i = 1; i <= roomDistCount; i++) {
                    Vector2Int pos = CurrentRoom.GridPos + dir * i;
                    roomsOnPath.Add(Rooms[pos]);
                    Rooms[pos].KeepOpenDoors(dir);
                    Rooms[pos].GetDoorInDirection(dir).SetDoorway(true);
                    Rooms[pos].GetDoorInDirection(dir*-1).SetDoorway(true);
                }

                roomsOnPath[0].GetDoorInDirection(dir * -1).KeepOpen = false;

                roomsOnPath[roomsOnPath.Count - 1].GetDoorInDirection(dir).KeepOpen = false;
                roomsOnPath[roomsOnPath.Count - 1].GetDoorInDirection(dir).SetDoorway(false);

                enemy.Init(roomsOnPath, ScareType.WalkTowards);
                
                MonsterSpawned = true;
                DistanceEnemySummoned = true;

                DistanceMultiplier = 1;
                break;
            } else {
                continue;
            }
        }

    }

    private bool SideWalkMonsterSpawned = false;

    public void SummonWalkwayEnemy() {
        if (SideWalkMonsterSpawned || MonsterSpawned) return;

        // chance of trying at all
        float rng = Random.Range(0, 100f);
        if (rng > 70f) return;

        // Raycast out direction forward from the player
        int roomDistCount = 2;
        float minSpawnDist = 14 * roomDistCount; //14 is the size of a room in the xz plane

        Vector2Int dir = CurrentGridPos - LastGridPos;

        // get direction player is looking and check if we can see that direction
        Vector3 playerLookDir = PlayerTransform.forward;
        playerLookDir.y = 0;
        playerLookDir.Normalize();
        Vector3 abs = new Vector3(Mathf.Abs(playerLookDir.x), 0, Mathf.Abs(playerLookDir.z));
        Vector2Int lookDir = abs.x > abs.z ? new Vector2Int((int)Mathf.Sign(playerLookDir.x), 0) : new Vector2Int(0, (int)Mathf.Sign(playerLookDir.z));
        lookDir *= -1;

        if (lookDir != dir) {
            //Debug.Log($"LookDir {lookDir} doesn't match spawnedDir {dir}");
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

            List<RoomController> roomsOnPath = new List<RoomController>(1 + roomDistCount);

            //0 should be rayOrigin's room
            roomsOnPath.Add(CurrentRoom);
            CurrentRoom.KeepOpenDoors(dir);
            CurrentRoom.GetDoorInDirection(dir).SetDoorway(true);

            //count-1 should be spawnpoint's room
            //get all values in between those using gridpositions
            for (int i = 1; i <= roomDistCount; i++) {
                Vector2Int pos = CurrentRoom.GridPos + dir * i;
                roomsOnPath.Add(Rooms[pos]);
                Rooms[pos].KeepOpenDoors(dir);
                Rooms[pos].GetDoorInDirection(dir).SetDoorway(true);
                Rooms[pos].GetDoorInDirection(dir * -1).SetDoorway(true);
            }

            roomsOnPath[0].GetDoorInDirection(dir * -1).KeepOpen = false;

            roomsOnPath[roomsOnPath.Count - 1].GetDoorInDirection(dir).KeepOpen = false;
            roomsOnPath[roomsOnPath.Count - 1].GetDoorInDirection(dir).SetDoorway(false);

            enemy.Init(roomsOnPath, ScareType.SideWalk);

            MonsterSpawned = true;
            SideWalkMonsterSpawned = true;
            DistanceMultiplier = 0.3f;
        }
    }

    public void DelayCanDie() {
        StartCoroutine(EnableDeathPossibility());
    }

    private IEnumerator EnableDeathPossibility() {
        yield return new WaitForSeconds(15);
        CanDie = true;
    }


    private bool Dead = false;

    public void TryDie() {
        if (!CanDie || Dead || !DistanceEnemySummoned) return;

        bool rng = Random.Range(0, 100f) <= 18f; //18% chance to die
        if (!rng) return;

        RoomController lastRoom = Rooms[LastGridPos];

        Vector3 spawnPoint = lastRoom.transform.position + (lastRoom.transform.position + PlayerTransform.position)/2f;
        spawnPoint.y = 0;

        Vector3 ff = (PlayerTransform.position - spawnPoint + new Vector3(0,-4,0)).normalized;
        Quaternion facing = Quaternion.LookRotation(ff, Vector3.up);

        Enemy_Distance enemy = Instantiate(MonsterDistance_Prefab, spawnPoint + new Vector3(0, -0.5f,0), facing, _entityHolder).GetComponent<Enemy_Distance>();
        List<RoomController> roomssss = new List<RoomController>() {
            lastRoom,
            CurrentRoom
        };
        enemy.Init(roomssss, ScareType.Die);
        Dead = true;

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
        RoomController[] rooms = _roomHolder.GetComponentsInChildren<RoomController>();

        for (int i = 0; i < rooms.Length; i++) {
            if (rooms[i] != null && rooms[i].GridPos != Vector2Int.zero) {
                rooms[i].SetAllDoorways_Random();
            }
        }
    }

}
