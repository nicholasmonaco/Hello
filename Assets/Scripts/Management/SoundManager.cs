using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour {
    [SerializeField] private AudioSource ProgressStatic;
    [SerializeField] private float _maxStaticVolume = 0.2f;



    public void SetProgressStaticVolume(float ratio) {
        ProgressStatic.volume = ratio * _maxStaticVolume;
    }
}
