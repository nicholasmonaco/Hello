using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadeToBlack : MonoBehaviour
{
    [SerializeField] private Image _panel;


    public void Fade(float time) {
        StartCoroutine(Fade_C(time));
    }

    public IEnumerator Fade_C(float duration) {
        float timer = duration;


        while(timer > 0) {
            timer -= Time.deltaTime;
            _panel.color = Color.Lerp(Color.black, Color.clear, timer / duration);

            yield return null;
        }

        _panel.color = Color.black;
        yield return new WaitForEndOfFrame();
    }
}
