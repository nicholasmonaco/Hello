using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorwayController : MonoBehaviour
{
    private bool _lastRendered = false;
    [SerializeField] private Renderer _doorwayRenderer;
    [SerializeField] private Collider _doorwayCollider;
    [SerializeField] public SquareRoomController Room;

    private bool _enabled = true;

    public Vector2Int FacingDirection;
    [HideInInspector] public Vector2Int GridPos;
    public Vector2Int PairedDoorGridPos => GridPos + FacingDirection;

    private float DistanceToPlayer => Vector2.Distance(new Vector2(Game.Manager.PlayerPosition.x, Game.Manager.PlayerPosition.z),
                                                       new Vector2(transform.position.x, transform.position.z));

    private bool _keepOpen;
    [HideInInspector] public bool KeepOpen {
        get => _keepOpen;
        set {
            _keepOpen = value;
            Room.GetPairedDoorway(FacingDirection)._keepOpen = value;
        }
    }

    public bool Open => _enabled;


    public void UpdateDoorway() {
        bool beingRendered = _doorwayRenderer.isVisible;
        if(!beingRendered && _lastRendered == true) {
            if (DistanceToPlayer < 1.65f) return;

            //have a chance to clear doorway or not
            bool toggleDoorway = Random.Range(0, 1f) >= 0.33f;
            
            if (toggleDoorway) {
                ToggleDoorway();
            }
        }

        _lastRendered = beingRendered;

        //if (_keepOpen) {
        //    Debug.DrawLine(_doorwayRenderer.bounds.min, _doorwayRenderer.bounds.max, Color.green);
        //}
    }


    private void ToggleDoorway() {
        SetDoorway(!_enabled, true);
    }

    public void SetKeepOpen(bool value) {
        KeepOpen = value;
    }

    public void SetDoorway(bool open, bool toggle = false) {
        if (KeepOpen && open) return;

        if(open != _enabled) {
            if (Game.Manager.CurrentRoom != Room) {
                Room.OnDoorToggleWhenNotCurrentRoom();
            }
        }

        _enabled = open;

        DoorwayController pairDoorway = Room.GetPairedDoorway(FacingDirection);
        if (pairDoorway == null || pairDoorway._doorwayRenderer.isVisible != false) return;

        if (!open) {
            //set material to invisible
            _doorwayRenderer.material = Game.Manager.InvisibleDoorwayMaterial;
            //disable collider
            _doorwayCollider.enabled = false;

            pairDoorway._doorwayRenderer.material = Game.Manager.InvisibleDoorwayMaterial;
            pairDoorway._doorwayCollider.enabled = false;
        } else {
            //set material to standard
            _doorwayRenderer.material = Game.Manager.BaseDoorwayMaterial;
            //enable collider
            _doorwayCollider.enabled = true;

            pairDoorway._doorwayRenderer.material = Game.Manager.BaseDoorwayMaterial;
            pairDoorway._doorwayCollider.enabled = true;
        }

        if (toggle) {
            pairDoorway.Room.RandomToggleDoors(FacingDirection * -1, Random.Range(1, 5));
        }
    }

    public void SetDoorway_Single(bool open, Material openMat, Material closedMat) {
        _enabled = open;

        if (!open) {
            //set material to invisible
            _doorwayRenderer.material = openMat;
            //disable collider
            _doorwayCollider.enabled = false;

        } else {
            //set material to standard
            _doorwayRenderer.material = closedMat;
            //enable collider
            _doorwayCollider.enabled = true;

        }
    }


    public DoorwayController GetPairedDoor() {
        RoomController pairedRoom = Game.Manager.Rooms(PairedDoorGridPos);
        if(pairedRoom != null) {
            //pairedRoom.doo
            return null;
        } else {
            return null;
        }
    }

    public bool IsVisible => _doorwayRenderer.isVisible;
}
