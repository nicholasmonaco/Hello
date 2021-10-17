using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomController : MonoBehaviour {
    #region Static Values
    public static Vector2 GridTileSize = new Vector2(14, 14);

    private static Vector2Int _lastCheckedGridPosNearestPlayer = Vector2Int.zero;

    public static Vector2Int GridPosNearestPlayer {
        get {
            Vector2 playerPosFlat = new Vector2(Game.Manager.PlayerPosition.x, Game.Manager.PlayerPosition.z);
            Vector2 unroundedNearest = playerPosFlat / GridTileSize;

            Vector2Int result = new Vector2Int(Mathf.RoundToInt(unroundedNearest.x), Mathf.RoundToInt(unroundedNearest.y));

            if(_lastCheckedGridPosNearestPlayer != result) {
                Game.Manager.LastGridPos = _lastCheckedGridPosNearestPlayer;
                _lastCheckedGridPosNearestPlayer = result;
            }

            return result;
        }
    }


    public static int NextRoomId = 0;
    #endregion

    #region Variables
    private bool _initialized = false;

    [HideInInspector] public int RoomId;
    [HideInInspector] public List<Vector2Int> GridPositions;

    [HideInInspector] public Vector2Int? GenerationPointGridPos;

    [HideInInspector] public List<DoorwayController> Doors;
    

    protected float PlayerDistance => Vector3.Distance(Game.Manager.PlayerPosition, transform.position); //this will have to change
    #endregion



    public void InitRoom(Vector2Int initialGridSpawnpoint) {
        _initialized = true;

        GenerationPointGridPos = initialGridSpawnpoint;

        RoomId = NextRoomId++;

        GridPositions = new List<Vector2Int>();

        // we need to determine what tiles the room takes up after its initial spawn position
        Vector2Int[] positions = SolveGridPositions(initialGridSpawnpoint);
        foreach(Vector2Int gridPos in positions) {
            GridPositions.Add(gridPos);
        }

        Game.Manager.SetRoom(this);

        DoorwayController[] doorways = GetComponentsInChildren<DoorwayController>();
        foreach (DoorwayController doorway in doorways) {
            doorway.GridPos = FindNearestGridPosToDoorWorldPos(doorway.transform.position, doorway.FacingDirection);
        }
    }


    public virtual void Start() {
        if (!_initialized) InitRoom(GenerationPointGridPos ?? GuessGridPosByTransformWorldPos());

        if (Game.Manager.CurrentRoom == null && GridPositions.Contains(Vector2Int.zero)) Game.Manager.CurrentRoom = this;
    }

    
    public virtual void UpdateRoom() {

    }


    protected virtual Vector2Int[] SolveGridPositions(Vector2Int initialGridPosition) {
        return new Vector2Int[1] { initialGridPosition };
    }


    public virtual void OnDoorToggleWhenNotCurrentRoom() {
        
    }



    #region Retrieval Methods

    public virtual DoorwayController GetDoorInDirection(Vector2Int dir) {
        List<DoorwayController> doorsInDir = new List<DoorwayController>();

        foreach(DoorwayController door in Doors) {
            if(door.FacingDirection == dir) {
                doorsInDir.Add(door);
            }
        }

        if (doorsInDir.Count == 0) return null;

        return doorsInDir[Random.Range(0, doorsInDir.Count)];
    }

    public virtual DoorwayController GetDoorInDirection(Vector2Int dir, Vector2Int centerGridPos) {
        List<DoorwayController> doorsInDir = new List<DoorwayController>();

        foreach (DoorwayController door in Doors) {
            if (door.GridPos == centerGridPos && door.FacingDirection == dir) {
                return door;
            }
        }

        return null;
    }

    public Vector2Int FindNearestGridPosToDoorWorldPos(Vector3 doorWorldPos, Vector2Int doorFacingDir) {
        Vector2 approxRoomWorldPos = new Vector2(doorWorldPos.x - doorFacingDir.x * GridTileSize.x / 2f,
                                                 doorWorldPos.z - doorFacingDir.y * GridTileSize.y / 2f);

        Vector2 approxGridPos = new Vector2(approxRoomWorldPos.x / GridTileSize.x,
                                            approxRoomWorldPos.y / GridTileSize.y);

        approxGridPos += new Vector2(GridTileSize.x - (approxGridPos.x % GridTileSize.x), 
                                     GridTileSize.y - (approxGridPos.y % GridTileSize.y));

        return new Vector2Int(Mathf.RoundToInt(approxGridPos.x),
                              Mathf.RoundToInt(approxGridPos.y));
    }

    protected Vector2Int GuessGridPosByTransformWorldPos() {
        return new Vector2Int(Mathf.RoundToInt(transform.position.x / GridTileSize.x),
                              Mathf.RoundToInt(transform.position.z / GridTileSize.y));
    }

    #endregion


    #region Door Methods

    public virtual void KeepOpenDoors(bool x, bool y, Vector2Int centerGridPos, bool keepOpen = true) {
        foreach(DoorwayController door in Doors) {
            if ((x && door.FacingDirection.x != 0 && door.GridPos.y == centerGridPos.y) ||
                (y && door.FacingDirection.y != 0 && door.GridPos.x == centerGridPos.x)) { 
            
                door.SetKeepOpen(keepOpen);
            }
        }
    }

    public virtual void KeepOpenDoors(Vector2Int rawAxis, Vector2Int centerGridPos, bool keepOpen = true) {
        KeepOpenDoors(rawAxis.x != 0, rawAxis.y != 0, centerGridPos, keepOpen);
    }


    public virtual void KeepOpenDoors(bool x, bool y, bool keepOpen = true) {
        foreach (DoorwayController door in Doors) {
            if ((x && door.FacingDirection.x != 0) ||
                (y && door.FacingDirection.y != 0)) {

                door.SetKeepOpen(keepOpen);
            }
        }
    }

    public virtual void KeepOpenDoors(Vector2Int rawAxis, bool keepOpen = true) {
        KeepOpenDoors(rawAxis.x != 0, rawAxis.y != 0, keepOpen);
    }


    public virtual DoorwayController GetRandomOpenDoor() {
        List<DoorwayController> doors = new List<DoorwayController>();

        foreach(DoorwayController door in Doors) {
            if (door.Open) doors.Add(door);
        }

        if (doors.Count == 0) return null;

        return doors[Random.Range(0, doors.Count)];
    }

    public virtual DoorwayController GetRandomUnseenDoor() {
        List<DoorwayController> doors = new List<DoorwayController>(4);

        foreach(DoorwayController door in Doors) {
            if (!door.IsVisible) doors.Add(door);
        }

        if (doors.Count == 0) return null;

        return doors[Random.Range(0, doors.Count)];
    }

    #endregion


    #region Light Methods

    public virtual void ToggleLight() {
        // _lampController.ToggleLight();
    }

    public virtual void ToggleLight(bool on) {
        // _lampController.On = on;
    }

    public virtual void ToggleLight(bool on, float duration) {
        // _lampController.On = on;
    }

    #endregion

}
