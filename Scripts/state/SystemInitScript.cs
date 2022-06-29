using System.Collections;
using FMODUnity;
using gameplay;
using UnityEngine;

namespace state {
    public class SystemInitScript : MonoBehaviour {
        private static SystemInitScript levelConfig;
        public GameObject system;
        public bool playerCanJump;
        public bool playerCanCrouch;
        public StudioEventEmitter levelMusic;

        public static SystemInitScript GetCurrent() {
            return levelConfig;
        }
        
        public static void SetPlayerMovementConstraints() {
            var player = Player.Get();
            //player.controller.canJump = levelConfig.playerCanJump;
            //player.controller._crouchModifiers.useCrouch = levelConfig.playerCanCrouch;
        }
        
        private void Awake() {
            levelMusic = GetComponent<StudioEventEmitter>();
            if (StateManager.Get() == null) {
                Instantiate(system);
            }
            levelConfig = this;
        }

        public void Quit() {
            StartCoroutine(QuitTime());
        }

        public void PlayFMODEvent(string eventPath, Transform position) {
            RuntimeManager.PlayOneShot(eventPath, position.position);
        }

        private IEnumerator QuitTime() {
            yield return new WaitForSeconds(6);
            StateManager.Get().FirstSave(true);
            StateManager.Get().LoadArbitraryScene(0);
        }
    }
}
