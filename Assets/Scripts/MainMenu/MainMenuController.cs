using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private AudioSource GlobalAudioPlayer;
    [SerializeField] private AudioClip _highlightSFX;
    [SerializeField] private Transform _mainMenu;
    [SerializeField] private Transform _optionsMenu;
    [SerializeField] private Transform _creditsMenu;

    [SerializeField] private Slider _volumeSlider;

    private Transform _currentMenu;

    private void Awake() {
        _currentMenu = _mainMenu;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined; //move these to a game manager thing later

        _volumeSlider.value = Game.VolumeScale;
    }


    public void PlaySelectNoise() {
        GlobalAudioPlayer.PlayOneShot(_highlightSFX, 0.25f);
    }

    public void AdjustVolume(float newVolume) {
        Game.VolumeScale = newVolume;

        GlobalAudioPlayer.volume = Game.VolumeScale;
    }

    public void PlayGame() {
        SceneManager.LoadScene(1); // Game scene
    }

    public void OpenSettings() {
        _currentMenu.gameObject.SetActive(false);
        _optionsMenu.gameObject.SetActive(true);

        _currentMenu = _optionsMenu;
    }

    public void OpenCredits() {
        _currentMenu.gameObject.SetActive(false);
        _creditsMenu.gameObject.SetActive(true);

        _currentMenu = _creditsMenu;
    }

    public void BackToMainMenu() {
        _currentMenu.gameObject.SetActive(false);
        _mainMenu.gameObject.SetActive(true);

        _currentMenu = _mainMenu;
    }

    public void ExitGame() {
        // This should work as far as I know.
        Application.Quit(); 
    }
}
