using Doozy.Engine.UI;
using FMODUnity;
using state;
using TMPro;
using UnityEngine;

namespace gameplay.interactables {
    public class RegularDoor : Interactable {
        public string labelText;
        public bool disabled;
        public bool disableOnUse;
        public UIView frontLabel;
        public UIView backLabel;
        public SaveKey keyToCheck;
        public TextAsset blockedDialogue;
        [EventRef] public string openDoorSound;
        [EventRef] public string closeDoorSound;

        private TMP_Text frontText;
        private TMP_Text backText;
        private Animator anim;
        private static readonly int Open = Animator.StringToHash("open");

        private void Start() {
            frontText = frontLabel.GetComponentInChildren<TMP_Text>();
            frontText.text = labelText;
            backText = backLabel.GetComponentInChildren<TMP_Text>();
            backText.text = labelText;
            anim = GetComponentInChildren<Animator>();
        }

        public override int Priority() {
            if (disabled) {
                return -1;
            }

            return 0;
        }

        public override void Interact() {
            if (disabled) {
                return;
            }
            
            if (!StateManager.Get().CheckKey(keyToCheck)) {
                Player.Get().DisableInteract();
                Player.Get().DisableMovement();
                Dialogue dialogue = gameObject.AddComponent<Dialogue>();
                dialogue.isSkippable = false;
                Player.Get().currentDialogue = dialogue;
                dialogue.Init(blockedDialogue);
                return;
            }

            if (disableOnUse) {
                anim.SetBool(Open, true);
                disabled = true;
                RuntimeManager.PlayOneShot(openDoorSound, transform.position);
            }
            else {
                bool isOpen = anim.GetBool(Open);
                if (isOpen) {
                    RuntimeManager.PlayOneShot(closeDoorSound, transform.position);
                }
                else {
                    RuntimeManager.PlayOneShot(openDoorSound, transform.position);
                }

                anim.SetBool(Open, !isOpen);
            }
        }

        public override void Enable() {
            if (disabled) {
                return;
            }
            
            frontLabel.Show();
            backLabel.Show();
        }

        public override void Disable() {
            HideView(frontLabel);
            HideView(backLabel);
        }

        public override Color OutlineColor() {
            return Color.black;
        }

        public override void Highlight(bool highlight) {
            if (disabled) {
                return;
            }

            HighlightText(frontText, highlight);
            HighlightText(backText, highlight);
        }
    }
}