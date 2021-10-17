using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquareRoomCorner : MonoBehaviour {
    public MeshRenderer CornerRenderer;
    public Collider CornerCollider;
    public Vector2Int CornerDirection = Vector2Int.zero;

    public bool IsVisible => CornerRenderer.isVisible;

    [HideInInspector] public bool IsOn = false;
}
