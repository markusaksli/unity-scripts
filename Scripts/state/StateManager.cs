using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Doozy.Engine.UI;
using FMODUnity;
using gameplay;
using gameplay.interactables;
using Sirenix.OdinInspector;
using TMPro;
using ui;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace state {
    public sealed class StateManager : MonoBehaviour {
        private const string DEFINED_KEYS = "definedKeys";
        private const string ARBITRARY_KEYS = "arbitraryKeys";
        private const string NAVIGATION_MAP = "navigationMap";

        private static StateManager _instance;
        private static TaskCompletionSource<bool> isInitialized = new();

        [Scene] public string lastScene;
        
        private Dictionary<string, int> navigationMap;
        private Dictionary<string, int> scenes;

        private HashSet<SaveKey> definedKeys;
        private List<String> arbitraryKeys;

        public GameObject player;
        public UICanvas inGameCanvas;
        
        public TMP_FontAsset defaultFont;
        [EventRef] public string defaultDialgoueSound;

        public static StateManager Get() {
            return _instance;
        }

        public static async Task<StateManager> GetSafe() {
            await isInitialized.Task;
            return _instance;
        }

        private async void Awake() {
            if (_instance != null) {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            scenes = new Dictionary<string, int>();
            for (int i = 0; i < 10; i++) {
                string sceneName = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                if (string.IsNullOrEmpty(sceneName)) break;
                scenes.Add(sceneName, i);
            }

            await InitialLoad();

            if (SceneManager.GetActiveScene().buildIndex != 0) {
                Utils.LockCursor();
                var playerController = await Player.GetSafe();
                playerController.DisableInteract();
                playerController.DisableMovement();
                await isInitialized.Task;
                await Task.Delay(1000);
                PositionPlayerAfterLoad();
                playerController.EnableInteract();
                playerController.EnableMovement();
                Player.Get().canvasController.transition.Hide();
            }
            else {
                Utils.UnlockCursor();
                player.SetActive(false);
                inGameCanvas.gameObject.SetActive(false);
            }
        }

        public void LoadDoorScene(SceneDoor door) {
            lastScene = SceneManager.GetActiveScene().name;
            if (door.saveOnUse) {
                SaveNavigation(scenes[door.connectedScene]);
                SaveProgression();
            }

            Player.Get().DisableInteract();
            Player.Get().DisableMovement();
            StartCoroutine(LevelTransition(scenes[door.connectedScene], true));
        }
        
        public void LoadArbitraryScene(int scene) {
            Player.Get().DisableInteract();
            Player.Get().DisableMovement();
            StartCoroutine(LevelTransition(scene, true));
        }

        private Vector3 PositionPlayerAfterLoad() {
            Transform loadPos = SystemInitScript.GetCurrent().transform;
            Player.Get().Teleport(loadPos);
            return loadPos.position;
        }

        public IEnumerator LevelTransition(int scene, bool smooth) {
            Utils.LockCursor();
            IngameCanvasController ctr = Player.Get().canvasController;
            ShaderUITransition transition = ctr.transition;
            foreach (var emitter in SystemInitScript.GetCurrent().GetComponents<StudioEventEmitter>()) {
                emitter.Stop();
            }

            if (SceneManager.GetActiveScene().buildIndex != 0) { // Main menu
                Player.Get().ClearAndDisable();
                transition.gameObject.SetActive(true);
                transition.Show();
                //TODO: Scene-relevant sound should fade out and back in
                Debug.Log("Wait visible");
                yield return new WaitUntil(() => !transition.IsPlaying());
                player.SetActive(false);
            }
            
            var loadSceneAsync = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
            loadSceneAsync.allowSceneActivation = true;
            Debug.Log("Wait load");
            yield return new WaitUntil(() => loadSceneAsync.isDone);
            Debug.Log($"Loaded player into {scenes.FirstOrDefault(x => x.Value == scene).Key}");
            if (scene != 0) {
                player.SetActive(true);
                Player.Get().flashlight.enabled = false;
                var loadpos = PositionPlayerAfterLoad();
                yield return new WaitUntil(() => TransformEqualsLoadPos(loadpos));
                inGameCanvas.gameObject.SetActive(true);
                Player.Get().canvasController.transition.gameObject.SetActive(true);
                if (smooth) {
                    transition.HideAndDisable();
                }
                else {
                    transition.InstantHide();
                }

                //yield return new WaitUntil(Player.Get().controller.IsOnGround);
                Debug.Log("Enabling movement");
                Player.Get().EnableInteract();
                Player.Get().EnableMovement();
            }
            else {
                yield return 0;
                inGameCanvas.gameObject.SetActive(false);
                Utils.UnlockCursor();
            }
        }

        private bool TransformEqualsLoadPos(Vector3 loadpos) {
            return Utils.Vector3EqualsDelta(0.05f, player.transform.position, loadpos);
        }

        public bool NewGame() {
            return navigationMap["currentScene"] == 1;
        }
        
        //TODO: Seperate case to return to main menu (no doors)
        public void LoadSaveFileScene() {
            int scene = navigationMap["currentScene"];
            Debug.Log($"Starting LevelTransition to {scene}");
            Load();
            StartCoroutine(LevelTransition(scene, true));
        }

        public void FirstSave(bool wipe) {
            navigationMap = new Dictionary<string, int> {{"currentScene", 1}, {"lastScene", 0}};
            lastScene = Path.GetFileNameWithoutExtension(
                SceneUtility.GetScenePathByBuildIndex(navigationMap["lastScene"]));

            definedKeys = new HashSet<SaveKey> {SaveKey.NONE};
            if (!wipe) definedKeys.Add(SaveKey.GAME_STARTED);

            arbitraryKeys = new List<string> {"none"};

            ES3.Save(DEFINED_KEYS, definedKeys);
            ES3.Save(ARBITRARY_KEYS, arbitraryKeys);
            ES3.Save(NAVIGATION_MAP, navigationMap);
            if (wipe) {
                // completedEndings = new HashSet<Ending>();
                // ES3.Save(COMPLETED_ENDINGS, completedEndings);
            }

            Debug.Log("Created new save");
        }


        public void SaveNavigation(int currentScene) {
            navigationMap["lastScene"] = scenes[lastScene];
            navigationMap["currentScene"] = currentScene;
            ES3.Save(NAVIGATION_MAP, navigationMap);
        }

        public void AddDefinedKey(SaveKey key) {
            definedKeys.Add(key);
        }

        public bool CheckKey(SaveKey key) {
            return definedKeys.Contains(key);
        }

        public bool CheckArbitraryKey(string key) {
            return arbitraryKeys.Contains(key);
        }

        public void AddArbitraryKey(string key) {
            if (!CheckArbitraryKey(key)) {
                arbitraryKeys.Add(key);
            }
        }

        public void SaveProgression() {
            ES3.Save(DEFINED_KEYS, definedKeys);
            ES3.Save(ARBITRARY_KEYS, arbitraryKeys);
        }

        public void Load() {
            navigationMap = ES3.Load<Dictionary<string, int>>(NAVIGATION_MAP);
            definedKeys = ES3.Load<HashSet<SaveKey>>(DEFINED_KEYS);
            arbitraryKeys = ES3.Load<List<string>>(ARBITRARY_KEYS);
            lastScene = Path.GetFileNameWithoutExtension(
                SceneUtility.GetScenePathByBuildIndex(navigationMap["lastScene"]));
        }

        private async Task InitialLoad() {
            ES3.Init();
            if (!ES3.FileExists()) {
                //TODO: In a full game you shouldn't just overwrite the whole save file when all the values don't exist
                //!(ES3.KeyExists(NAVIGATION_MAP) && ES3.KeyExists(DEFINED_KEYS)) && ES3.KeyExists(ARBITRARY_KEYS) && ES3.KeyExists(CHOSEN_PATH) && ES3.KeyExists(COMPLETED_ENDINGS)
                FirstSave(true);
                Debug.Log("Created new save due to missing data");
            }
            else {
                Load();
            }

            await Player.GetSafe();
            OptionsMenu.InitOptions();
            isInitialized.SetResult(true);
            Debug.Log("Found working save data");
        }
    }
}
