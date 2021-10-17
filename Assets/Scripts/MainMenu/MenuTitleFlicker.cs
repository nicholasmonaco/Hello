using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MenuTitleFlicker : MonoBehaviour {
    [SerializeField] private TMP_Text _title;

    private float _timer;
    private bool _last = true;


    private void Awake() {
        _timer = Random.Range(-10000, 0);
    }


    private void Update() {
        _timer += Time.deltaTime * 0.25f;

        bool white = Mathf.PerlinNoise(_timer, _timer) <= 0.7f;

        if(white != _last) {
            _last = white;
            string color = white ? "white" : "black";
            _title.text = $"Hell<color={color}>o</color>";
        }
    }


}
