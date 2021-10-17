using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandSwivel : MonoBehaviour {
    [SerializeField] private float _swivelDuration = 1.7f;
    [SerializeField] private float _minAngle = -20;
    [SerializeField] private float _maxAngle = 20;
    [SerializeField] private float _angleOffset = 90;


    private float _halfDuration;
    private float _timer;
    private Quaternion _origRot;

    private Quaternion _minRot;
    private Quaternion _maxRot;
    private bool _flipped;


    private void Awake() {
        _origRot = transform.localRotation;
        _halfDuration = _swivelDuration / 2f;
        _timer = 0;

        _flipped = false;

        _minRot = _origRot * Quaternion.AngleAxis(_minAngle - _angleOffset, Vector3.up);
        _maxRot = _origRot * Quaternion.AngleAxis(_maxAngle - _angleOffset, Vector3.up);

        transform.localRotation = _minRot;
    }


    private void Update() {
        _timer += Time.deltaTime;
        if (_timer >= _halfDuration) {
            _timer -= _halfDuration;
            _flipped = !_flipped;
        }

        float frac = Mathf.SmoothStep(0, 1, _timer / _halfDuration);

        transform.localRotation = _flipped ? Quaternion.Lerp(_minRot, _maxRot, frac)
                                           : Quaternion.Lerp(_maxRot, _minRot, frac);
    }
}
