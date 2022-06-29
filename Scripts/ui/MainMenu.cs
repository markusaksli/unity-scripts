using System.Collections;
using Doozy.Engine.Nody;
using Doozy.Engine.UI;
using gameplay;
using state;
using UnityEngine;

namespace ui {
    public class MainMenu : MonoBehaviour {
        public GraphController controller;
        public ShaderUITransition transition;
        public UIButton loadButton;
        public UIButton exitButton;
        private StateManager manager;

        private void Awake() {
            transition.Awake();
            transition.InstantShow();
        }

        private void Start() {
            WaitForStateInit();
        }

        public void HandleLoadButton() {
            loadButton.Interactable = false;
            transition.gameObject.SetActive(true);
            transition.Show();
            Debug.Log("Loading into game from main menu");
            Load();
        }

        private async void WaitForStateInit() {
            if (Application.platform == RuntimePlatform.WebGLPlayer) {
                exitButton.enabled = false;
            }

            manager = await StateManager.GetSafe();

            if (!manager.NewGame()) {
                loadButton.TextMeshProLabel.text = "Continue";
            }

            controller.GoToNodeByName("Main");
            transition.HideAndDisable();
        }

        public void Load() {
            StartCoroutine(LoadFromSave());
        }

        private IEnumerator LoadFromSave() {
            yield return new WaitUntil(() => !transition.IsPlaying());
            var ingameCanvasController = Player.Get().canvasController;
            ingameCanvasController.gameObject.SetActive(true);
            ingameCanvasController.transition.InstantShow();
            yield return 0;
            manager.LoadSaveFileScene();
        }

        public void Exit() {
            Application.Quit();
        }
    }
}