using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquareRoomController : RoomController {
    #region Static Variables
    private static List<Vector2Int> dirs = new List<Vector2Int>(2) {
        new Vector2Int(1, 0),
        new Vector2Int(0, 1)
    };
    #endregion

    #region Variables
    [SerializeField] private LampController _lampController;

    public Vector2Int GridPos => GridPositions[0];

    public DoorwayController LeftDoor;
    public DoorwayController RightDoor;
    public DoorwayController UpDoor;
    public DoorwayController DownDoor;

    public List<SquareRoomCorner> Corners;
    #endregion




    public override void OnDoorToggleWhenNotCurrentRoom() {
        foreach(SquareRoomCorner corner in Corners) {
            if (corner.IsVisible || !DoorOpenAdjacentToCorner(corner.CornerDirection)) continue;

            bool visible = Random.Range(0, 100f) > 50f;
            if (visible == corner.IsOn) continue;

            if (visible) {
                corner.CornerRenderer.material = Game.Manager.BaseDoorwayMaterial;
            } else {
                corner.CornerRenderer.material = Game.Manager.InvisibleDoorwayMaterial;
            }

            corner.CornerCollider.enabled = visible;
            corner.IsOn = visible;
        }
    }

    private bool DoorOpenAdjacentToCorner(Vector2Int cornerDir) {
        DoorwayController door = GetDoorInDirection(new Vector2Int(cornerDir.x, 0));
        if (door != null && door.Open) return true;

        door = GetDoorInDirection(new Vector2Int(0, cornerDir.y));
        if (door != null && door.Open) return true;

        return false;
    }




    public RoomController GetRoomInDirection(Vector2Int direction) {
        Vector2Int pos = GridPos + direction;
        if(pos.x < -GameManager.MapSize || pos.x > GameManager.MapSize ||
           pos.y < -GameManager.MapSize || pos.y > GameManager.MapSize) {
            return null;
        }

        RoomController room = Game.Manager.Rooms(pos);
        return room != null ? room : null;
    }

    public DoorwayController GetPairedDoorway(Vector2Int direction) {
        Vector2Int inverse = direction * -1;

        if (!(GetRoomInDirection(direction) is SquareRoomController room)) return null;

        if(inverse == new Vector2Int(-1, 0)) { return room.LeftDoor; }
        else if (inverse == new Vector2Int(1, 0)) { return room.RightDoor; }
        else if (inverse == new Vector2Int(0, -1)) { return room.DownDoor; }
        else if (inverse == new Vector2Int(0, 1)) { return room.UpDoor; }

        return null;
    }

    public override DoorwayController GetDoorInDirection(Vector2Int dir) {
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


    private SquareRoomController[] GetAdjacentRooms(Vector2Int excludeDirection) {
        List<SquareRoomController> rooms = new List<SquareRoomController>(4);

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

            RoomController room = Game.Manager.Rooms(pos);
            if(room != null && room is SquareRoomController squareRoom) {
                rooms.Add(squareRoom);
            }
        }

        return rooms.ToArray();
    }



    public override void UpdateRoom() {
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
                RoomController foundRoom = GetRoomInDirection(dir * n);
                if (!(foundRoom is SquareRoomController otherRoom)) continue;

                float otherDist = otherRoom.PlayerDistance;
                if (otherDist < curDist) {
                    // Game.Manager.LastGridPos = Game.Manager.CurrentGridPos;
                    Game.Manager.CurrentRoom = otherRoom;

                    Game.Manager.SummonWalkwayEnemy();
                    Game.Manager.SummonDistanceEnemy();
                    Game.Manager.SummonDistanceWatcherEnemy();
                    Game.Manager.TryDie();

                    // So when this switches, we want to get the rooms adjacent to this one (in the directions we didnt come from),
                    //   and open the doors randomly on the rooms adjacent to those.
                    HashSet<Vector2Int> evaluatedRooms = new HashSet<Vector2Int>();

                    SquareRoomController[] adjacentRooms = otherRoom.GetAdjacentRooms(dir * n * -1);
                    foreach(SquareRoomController subRoom in adjacentRooms) {
                        evaluatedRooms.Add(subRoom.GridPos);

                        Vector2Int dirBetweenRooms = subRoom.GridPos - otherRoom.GridPos;
                        SquareRoomController[] subRooms = subRoom.GetAdjacentRooms(dirBetweenRooms);

                        foreach(SquareRoomController subsubRoom in subRooms) {
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

    public override void KeepOpenDoors(bool x, bool y, bool keepOpen = true) {
        List<Vector2Int> dirs = new List<Vector2Int>(2);
        if (x) { dirs.Add(new Vector2Int(1, 0)); }
        if (y) { dirs.Add(new Vector2Int(0, 1)); }

        foreach(Vector2Int rawDir in dirs) {
            for(int i = -1; i <= 1; i += 2) {
                Vector2Int dir = rawDir * i;

                GetDoorInDirection(dir)?.SetKeepOpen(keepOpen);
            }
        }
    }

    public override void KeepOpenDoors(Vector2Int rawAxis, bool keepOpen = true) {
        KeepOpenDoors(rawAxis.x != 0, rawAxis.y != 0, keepOpen);
    }


    public override void ToggleLight() {
        _lampController.ToggleLight();
    }

    public override void ToggleLight(bool on) {
        _lampController.On = on;
    }

    public override void ToggleLight(bool on, float duration) {
        // if (_lampController.On = on) return;

        _lampController.On = on;
        // StartCoroutine(ToggleLightDur(on, duration));
    }

    [System.Obsolete("This doesn't work, use normal ToggleLight instead.")]
    private IEnumerator ToggleLightDur(bool on, float duration) {
        float oldIntensity = on ? 0 : LampController.MaxIntensity;
        float newIntensity = on ? LampController.MaxIntensity : 0;

        float timer = duration;
        while(timer > 0) {
            yield return null;
            timer -= Time.deltaTime;

            _lampController.Intensity = Mathf.SmoothStep(newIntensity, oldIntensity, timer / duration);
        }

        yield return new WaitForEndOfFrame();

        _lampController.Intensity = newIntensity;
    }



    public override DoorwayController GetRandomOpenDoor() {
        List<DoorwayController> doors = new List<DoorwayController>(4);

        if (LeftDoor != null && LeftDoor.Open) doors.Add(LeftDoor);
        if (RightDoor != null && RightDoor.Open) doors.Add(RightDoor);
        if (UpDoor != null && UpDoor.Open) doors.Add(UpDoor);
        if (DownDoor != null && DownDoor.Open) doors.Add(DownDoor);

        if (doors.Count == 0) return null;

        return doors[Random.Range(0, doors.Count)];
    }

    public override DoorwayController GetRandomUnseenDoor() {
        List<DoorwayController> doors = new List<DoorwayController>(4);

        if (LeftDoor != null && !LeftDoor.IsVisible) doors.Add(LeftDoor);
        if (RightDoor != null && !RightDoor.IsVisible) doors.Add(RightDoor);
        if (UpDoor != null && !UpDoor.IsVisible) doors.Add(UpDoor);
        if (DownDoor != null && !DownDoor.IsVisible) doors.Add(DownDoor);

        if (doors.Count == 0) return null;

        return doors[Random.Range(0, doors.Count)];
    }
}
