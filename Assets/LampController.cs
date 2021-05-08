using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LampController : MonoBehaviour
{
    private bool _on = true;
    public bool On {
        get => _on;
        set { SetOn(value); }
    }


    #region Variables
    private Color _origColor;

    private Color _offColor => Game.Manager.LightOffColor; 

    [SerializeField] private Light _lightSource;
    [SerializeField] private MeshRenderer _bulbRenderer;
    #endregion



    private void Start() {
        _origColor = _bulbRenderer.material.color;
    }

    private void SetOn(bool on) {
        _on = on;

        if (on) {
            _lightSource.gameObject.SetActive(true);
            _bulbRenderer.material.color = _origColor;
            _bulbRenderer.material.EnableKeyword("_EMISSION");
        } else {
            _lightSource.gameObject.SetActive(false);
            _bulbRenderer.material.color = _offColor;
            _bulbRenderer.material.DisableKeyword("_EMISSION");
        }
    }

    public void ToggleLight() {
        On = !_on;
    }
}
