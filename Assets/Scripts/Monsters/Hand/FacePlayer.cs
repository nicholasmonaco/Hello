using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FacePlayer : MonoBehaviour {
    [SerializeField] private float _speed = 3;
    [SerializeField] private float _angleOffset = 105;


    private void Update() {
        Vector3 dirToPlayer = Game.Manager.PlayerPosition - transform.position;
        dirToPlayer.y = 0;
        dirToPlayer.Normalize();

        float angle = Mathf.Atan2(dirToPlayer.x, dirToPlayer.z) * Mathf.Rad2Deg;
        angle = (angle + 360) % 360f + _angleOffset;
        Quaternion facingPlayerRot = Quaternion.AngleAxis(angle, Vector3.up);

        transform.rotation = Quaternion.Lerp(transform.rotation, facingPlayerRot, Time.deltaTime * _speed);
    }
}
