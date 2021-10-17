using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public abstract class AbstractEntity : MonoBehaviour {
    public bool CanMove;

    [HideInInspector]
    public float TimeScale = 1;
    protected float _deltaTime => Time.deltaTime * TimeScale;

    [SerializeField]
    protected Transform MainModel;
    [SerializeField]
    protected Animator MainAnimator;
    [SerializeField]
    protected Rigidbody MainRB;
    [SerializeField]
    protected Collider MainCollider;


    // Start is called before the first frame update
    protected virtual void Start() {
        StartCoroutine(StartAlive());
    }


    // The coroutine that handles what happens when the entity begins to exist
    protected abstract IEnumerator StartAlive();


    // Custom WaitForSeconds coroutine - used for custom local time scaling
    protected IEnumerator WaitForLocalSeconds(float delay) {
        float timer = delay;
        while (timer > 0) {
            timer -= _deltaTime;
            yield return null;
        }
    }
}
