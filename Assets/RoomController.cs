using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomController : MonoBehaviour
{
    #region Static Variables
    private static List<Vector2Int> dirs = new List<Vector2Int>(2) {
        new Vector2Int(1, 0),
        new Vector2Int(0, 1)
    };
    #endregion

    #region Variables
    [SerializeField] private LampController _lampController;

    public Vector2Int GridPos = Vector2Int.zero;

    public DoorwayController LeftDoor;
    public DoorwayController RightDoor;
    public DoorwayController UpDoor;
    public DoorwayController DownDoor;

    private float PlayerDistance => Vector3.Distance(Game.Manager.PlayerPosition, transform.position);
    #endregion


    private void Start() {
        Game.Manager.Rooms[GridPos] = this;

        DoorwayController[] doorways = GetComponentsInChildren<DoorwayController>();
        foreach(DoorwayController doorway in doorways) {
            doorway.GridPos = GridPos;
        }

        if(GridPos == Vector2Int.zero) Game.Manager.CurrentRoom = this;
    }

    public RoomController GetRoomInDirection(Vector2Int direction) {
        Vector2Int pos = GridPos + direction;
        if(pos.x < -GameManager.MapSize || pos.x > GameManager.MapSize ||
           pos.y < -GameManager.MapSize || pos.y > GameManager.MapSize) {
            return null;
        }

        RoomController room;
        if (Game.Manager.Rooms.TryGetValue(pos, out room)) return room;
        return null;
    }

    public DoorwayController GetPairedDoorway(Vector2Int direction) {
        Vector2Int inverse = direction * -1;

        RoomController room = GetRoomInDirection(direction);
        if (room == null) return null;

        if(inverse == new Vector2Int(-1, 0)) { return room.LeftDoor; }
        else if (inverse == new Vector2Int(1, 0)) { return room.RightDoor; }
        else if (inverse == new Vector2Int(0, -1)) { return room.DownDoor; }
        else if (inverse == new Vector2Int(0, 1)) { return room.UpDoor; }

        return null;
    }

    public DoorwayController GetDoorInDirection(Vector2Int dir) {
        Vector2Int pos = GridPos + dir;
        if (pos.x < -GameManager.MapSize || pos.x > GameManager.MapSize ||
            pos.y < -GameManager.MapSize || pos.y > GameManager.MapSize) {
            return null;
        }

        if (dir == new Vector2Int(-1, 0)) { return LeftDoor; }
        else if (dir == new Vector2Int(1, 0)) { return RightDoor; }
        else if (dir == new Vector2Int(0, -1)) { return DownDoor; }
        else if (dir == new Vector2Int(0, 1)) { return UpDoor; }

        return null;
    }

    public void RandomToggleDoors(Vector2Int keepDirection, int recursiveCount = 1) {
        // So we don't mess with the keepDirection + need at least 1 to be open

        int openCount = Random.Range(1, 4); // Random between 1 and 3
        int opened = 0;

        List<Vector2Int> options = new List<Vector2Int>(4) {
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(0, 1)
        };

        options.Remove(keepDirection);


        //Close all doors first
        for(int i = 0; i < options.Count; i++) {
            Vector2Int openDir = options[i];
            DoorwayController door = GetDoorInDirection(openDir);
            if (door != null) {
                door.SetDoorway(false);
                // Do we need to close the doors of each direction here? idk
            }
        }

        // Open the determined amount of doors randomly
        for(int i = 0; i < openCount; i++) {
            int index = Random.Range(0, options.Count);
            Vector2Int openDir = options[index];

            DoorwayController door = GetDoorInDirection(openDir);
            if(door != null) {
                opened++;
                door.SetDoorway(true);
                if(recursiveCount > 0) GetPairedDoorway(openDir).Room.RandomToggleDoors(openDir * -1, recursiveCount - 1);
            }

            options.RemoveAt(index);
        }

        // Make sure one is always open, even in corner cases
        while (opened == 0) {
            int index = Random.Range(0, options.Count);
            Vector2Int openDir = options[index];

            DoorwayController door = GetDoorInDirection(openDir);
            if (door != null) {
                opened++;
                door.SetDoorway(true);
                if(recursiveCount > 0) GetPairedDoorway(openDir).Room.RandomToggleDoors(openDir * -1, recursiveCount - 1);
            }

            options.RemoveAt(index);
        }
    }


    public void SetAllDoorways(bool open, Material openMat, Material closeMat) {
        List<Vector2Int> options = new List<Vector2Int>(4) {
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(0, 1)
        };

        for (int i = 0; i < options.Count; i++) {
            Vector2Int openDir = options[i];
            DoorwayController door = GetDoorInDirection(openDir);
            if (door != null) {
                door.SetDoorway_Single(open, openMat, closeMat);
            }
        }
    }

    public void SetAllDoorways_Random() {
        List<Vector2Int> options = new List<Vector2Int>(4) {
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(0, 1)
        };

        for (int i = 0; i < options.Count; i++) {
            Vector2Int openDir = options[i];
            DoorwayController door = GetDoorInDirection(openDir);
            if (door != null) {
                bool open = Random.Range(0, 2) == 0;
                door.SetDoorway(open);
            }
        }
    }


    private RoomController[] GetAdjacentRooms(Vector2Int excludeDirection) {
        List<RoomController> rooms = new List<RoomController>(4);

        List<Vector2Int> options = new List<Vector2Int>(4) {
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(0, 1)
        };

        if (options.Contains(excludeDirection)) options.Remove(excludeDirection);

        foreach(Vector2Int dir in options) {
            Vector2Int pos = GridPos + dir;
            if (pos.x < -GameManager.MapSize || pos.x > GameManager.MapSize ||
                pos.y < -GameManager.MapSize || pos.y > GameManager.MapSize) {
                continue;
            }

            rooms.Add(Game.Manager.Rooms[pos]);
        }

        return rooms.ToArray();
    }



    public void UpdateRoom() {
        if (!CheckRoomCoords()) return;

        LeftDoor.UpdateDoorway();
        RightDoor.UpdateDoorway();
        UpDoor.UpdateDoorway();
        DownDoor.UpdateDoorway();

        //CheckRoomCoords();
    }


    private bool CheckRoomCoords() {
        float curDist = PlayerDistance;

        foreach(Vector2Int dir in dirs) {
            for (int n = -1; n <= 1; n += 2) {
                RoomController otherRoom = GetRoomInDirection(dir * n);
                if (otherRoom == null) continue;

                float otherDist = otherRoom.PlayerDistance;
                if (otherDist < curDist) {
                    Game.Manager.LastGridPos = Game.Manager.CurrentGridPos;
                    Game.Manager.CurrentRoom = otherRoom;

                    Game.Manager.SummonWalkwayEnemy();
                    Game.Manager.SummonDistanceEnemy();
                    Game.Manager.TryDie();

                    // So when this switches, we want to get the rooms adjacent to this one (in the directions we didnt come from),
                    //   and open the doors randomly on the rooms adjacent to those.
                    HashSet<Vector2Int> evaluatedRooms = new HashSet<Vector2Int>();

                    RoomController[] adjacentRooms = otherRoom.GetAdjacentRooms(dir * n * -1);
                    foreach(RoomController subRoom in adjacentRooms) {
                        evaluatedRooms.Add(subRoom.GridPos);

                        Vector2Int dirBetweenRooms = subRoom.GridPos - otherRoom.GridPos;
                        RoomController[] subRooms = subRoom.GetAdjacentRooms(dirBetweenRooms);

                        foreach(RoomController subsubRoom in subRooms) {
                            if(subsubRoom != otherRoom && !evaluatedRooms.Contains(subsubRoom.GridPos)) {
                                evaluatedRooms.Add(subsubRoom.GridPos);
                                Vector2Int dirfrom = subsubRoom.GridPos - subRoom.GridPos;
                                //dirfrom *= -1;
                                subsubRoom.RandomToggleDoors(dirfrom);
                            }
                        }
                    }

                    Game.Manager.TraveledRooms++;
                    return false;
                }
            }
        }

        return true;
    }

    public void KeepOpenDoors(bool x, bool y, bool keepOpen = true) {
        List<Vector2Int> dirs = new List<Vector2Int>(2);
        if (x) { dirs.Add(new Vector2Int(1, 0)); }
        if (y) { dirs.Add(new Vector2Int(0, 1)); }

        foreach(Vector2Int rawDir in dirs) {
            for(int i = -1; i <= 1; i += 2) {
                Vector2Int dir = rawDir * i;

                GetDoorInDirection(dir).KeepOpen = keepOpen;
            }
        }
    }

    public void KeepOpenDoors(Vector2Int rawAxis, bool keepOpen = true) {
        for (int i=-1;i<=1;i+=2) {
            Vector2Int axis = rawAxis * i;
            DoorwayController door = GetDoorInDirection(axis);
            if (door != null) {
                door.KeepOpen = keepOpen;
                //Debug.Log($"Door from {GridPos} in direction {axis} was set to stay open.");
            } else {
                //Debug.Log($"Door from {GridPos} in direction {axis} was null.");
            }
        }
    }


    public void ToggleLight() {
        _lampController.ToggleLight();
    }

    public void ToggleLight(bool on) {
        _lampController.On = on;
    }
}
