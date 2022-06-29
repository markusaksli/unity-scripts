using System.Collections.Generic;
using Doozy.Engine.UI;
using ECM2.Components;
using FMOD.Studio;
using gameplay;
using state;
using UnityEngine;
using UnityEngine.UI;

namespace ui {
    public class OptionsMenu : MonoBehaviour {
        public TMPro.TMP_Dropdown resolutionsDropdown;
        public UIToggle fullscreenToggle;
        public Slider volumeSlider;
        public Slider musicVolumeSlider;
        public Slider sfxVolumeSlider;
        public Slider mouseSensitivitySlider;
        Resolution[] resolutions;
        private Bus masterBus;
        private Bus musicBus;
        private Bus sfxBus;

        public void Start() {
            masterBus = FMODUnity.RuntimeManager.GetBus("bus:/");
            masterBus.getVolume(out float tempVolume);
            volumeSlider.value = tempVolume;

            musicBus = FMODUnity.RuntimeManager.GetBus("bus:/Music");
            musicBus.getVolume(out tempVolume);
            musicVolumeSlider.value = tempVolume;

            sfxBus = FMODUnity.RuntimeManager.GetBus("bus:/SFX");
            sfxBus.getVolume(out tempVolume);
            sfxVolumeSlider.value = tempVolume;

            mouseSensitivitySlider.value = Player.Get().controller.GetCharacterLook().mouseHorizontalSensitivity;

            fullscreenToggle.IsOn = Screen.fullScreen;
            resolutions = Screen.resolutions;
            resolutionsDropdown.ClearOptions();

            List<string> options = new List<string>();

            int currentResolutionIndex = 0;


            // Gets all the possible resolutions and puts them
            // into a TMP_dropdown list.
            // Also sets your current resolution as the dropdown's
            // default value.

            for (int i = 0; i < resolutions.Length; i++) {
                string option = resolutions[i].width + "x" + resolutions[i].height + "@" + resolutions[i].refreshRate +
                                "HZ";
                options.Add(option);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height) {
                    currentResolutionIndex = i;
                }
            }

            resolutionsDropdown.AddOptions(options);
            resolutionsDropdown.value = currentResolutionIndex;
            resolutionsDropdown.RefreshShownValue();
        }

        public void SetFullscreen(bool isFullscreen) {
            Screen.fullScreen = isFullscreen;
        }

        // Changes resolution depending on the choice in a dropdown.
        public void SetResolution(int resolutionIndex) {
            Resolution resolution = resolutions[resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        }

        public void SetVolume(float volume) {
            masterBus.setVolume(volume);
        }

        public void SetMusicVolume(float volume) {
            musicBus.setVolume(volume);
        }

        public void SetSfxVolume(float volume) {
            sfxBus.setVolume(volume);
        }

        public void SetMouseSens(float sens) {
            CharacterLook characterLook = Player.Get().controller.GetCharacterLook();
            characterLook.mouseHorizontalSensitivity = sens;
            characterLook.mouseVerticalSensitivity = sens;
        }

        public void SaveAndHide() {
            PlayerPrefs.SetFloat("volume", volumeSlider.value);
            PlayerPrefs.SetFloat("musicVolume", musicVolumeSlider.value);
            PlayerPrefs.SetFloat("sfxVolume", sfxVolumeSlider.value);
            PlayerPrefs.SetFloat("mouseSens", mouseSensitivitySlider.value);
            PlayerPrefs.Save();
            if (Time.timeScale == 0f) {
                Player.Get().canvasController.pauseView.Show();
                GetComponent<UIView>().Hide();
            }
        }

        public static void InitOptions() {
            Bus masterBus = FMODUnity.RuntimeManager.GetBus("bus:/");
            if (PlayerPrefs.HasKey("volume")) {
                masterBus.setVolume(PlayerPrefs.GetFloat("volume"));
            }
            else {
                masterBus.setVolume(1f);
                PlayerPrefs.SetFloat("volume", 1f);
            }

            Bus musicBus = FMODUnity.RuntimeManager.GetBus("bus:/Music");
            if (PlayerPrefs.HasKey("musicVolume")) {
                musicBus.setVolume(PlayerPrefs.GetFloat("musicVolume"));
            }
            else {
                musicBus.setVolume(1f);
                PlayerPrefs.SetFloat("musicVolume", 1f);
            }

            Bus sfxBus = FMODUnity.RuntimeManager.GetBus("bus:/SFX");
            if (PlayerPrefs.HasKey("sfxVolume")) {
                sfxBus.setVolume(PlayerPrefs.GetFloat("sfxVolume"));
            }
            else {
                sfxBus.setVolume(1f);
                PlayerPrefs.SetFloat("sfxVolume", 1f);
            }

            if (PlayerPrefs.HasKey("mouseSens")) {
                float sens = PlayerPrefs.GetFloat("mouseSens");
                CharacterLook characterLook = Player.Get().controller.GetCharacterLook();
                characterLook.mouseHorizontalSensitivity = sens;
                characterLook.mouseVerticalSensitivity = sens;
            }
            else {
                float sens = Player.Get().controller.GetCharacterLook().mouseHorizontalSensitivity;
                PlayerPrefs.SetFloat("mouseSens", sens);
                Player.Get().controller.GetCharacterLook().mouseVerticalSensitivity = sens;
            }

            PlayerPrefs.Save();
        }

        public void WipeSave() {
            ES3.DeleteFile();
            StateManager.Get().FirstSave(true);
            Player.Get().HandlePause();
            StateManager.Get().StartCoroutine(StateManager.Get().LevelTransition(0, true));
        }

        public void PlayClick() {
            FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/silentClick");
        }

        public void PlayLoudClick() {
            FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/loudClick");
        }

        public void PlayToggle() {
            FMODUnity.RuntimeManager.PlayOneShot("event:/SFX/UI/toggleClick");
        }
    }
}