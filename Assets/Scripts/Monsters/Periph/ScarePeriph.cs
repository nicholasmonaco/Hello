using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScarePeriph : MonoBehaviour {

    [SerializeField] private float _speed = 1;
    [SerializeField] private Renderer _renderer;
    private Vector3 _moveDir = Vector3.left;


    private void Awake() {
        float xOff = Random.Range(5f, 9f);
        xOff *= Random.Range(0, 2) == 0 ? -1 : 1;

        float yOff = Random.Range(4f, 6f);
        yOff *= Random.Range(0, 2) == 0 ? -1 : 1;

        Vector3 startOffset = new Vector3(xOff, yOff, 0);
        transform.position += startOffset;

        Quaternion startRotOffset = Quaternion.AngleAxis(Random.Range(0, 360f), Vector3.forward);
        transform.localRotation *= startRotOffset;


        _moveDir = new Vector3(Random.Range(3, 5f) * Mathf.Sign(xOff),
                               Random.Range(3, 5f) * Mathf.Sign(yOff),
                               0);
        _moveDir.Normalize();

        StartCoroutine(FlyOffscreen());
    }


    private IEnumerator FlyOffscreen() {
        float timer = 2;

        while (timer > 0) {
            yield return null;
            timer -= Time.deltaTime;
            transform.position += _moveDir * Time.deltaTime * _speed;
        }

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        Destroy(this.gameObject);
    }

    
}
