using Doozy.Engine.UI;
using EPOOutline;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace gameplay.interactables {
    public class DialogueInteractable : Interactable {
        [Tooltip("Destroy on use is automatically enabled if auto interact is enabled.")]
        public bool autoInteract;
        public bool disableOnUse;
        public bool destroyOnUse;
        public bool disabled;

        public bool autoPlay = true;
        public float autoPlayDelay;
        public bool isSkippable;
        
        public string labelText;
        public TextAsset inkJsonAsset;
        public UIView label;
        [EventRef] public string dialogueSound = "event:/SFX/DialogueScroll";
        public TMP_FontAsset dialogueFont;
        public Color dialogueColor;
        [Space(50)] public UnityEvent eventActions;

        private TMP_Text tmpText;
        private Outlinable outline;

        private void Start() {
            if (autoInteract) destroyOnUse = true;
            tmpText = label.GetComponentInChildren<TMP_Text>();
            tmpText.text = labelText;
            outline = GetComponentInChildren<Outlinable>();
            if (outline != null) {
                outline.enabled = false;
            }
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

            Player.Get().DisableInteract();
            
            if (!autoPlay) {
                Player.Get().DisableMovement();
            }
            else {
                var currentDialogue = Player.Get().currentDialogue;
                if (currentDialogue != null) {
                    currentDialogue.End();
                }
            }

            Dialogue dialogue = gameObject.AddComponent<Dialogue>();
            dialogue.isSkippable = isSkippable;
            dialogue.dialogueSound = dialogueSound;
            dialogue.font = dialogueFont;
            dialogue.textColor = dialogueColor;
            dialogue.autoPlayDelay = autoPlayDelay;
            dialogue.autoPlay = autoPlay;
            eventActions.AddListener(HandleDisabled);
            dialogue.SetEvents(eventActions);
            dialogue.Init(inkJsonAsset);
            Player.Get().currentDialogue = dialogue;
        }

        public override Color OutlineColor() {
            return outline.OutlineParameters.Color;
        }

        public override void Highlight(bool highlight) {
            if (disabled) {
                return;
            }

            HighlightText(tmpText, highlight);
            if (outline != null) {
                outline.enabled = highlight;
            }
        }

        public void HandleDisabled() {
            if (disableOnUse) {
                disabled = true;
            }

            if (destroyOnUse) {
                Destroy(gameObject);
            }
        }

        public override void Enable() {
            if (disabled) {
                return;
            }

            if (autoInteract) {
                autoInteract = false;
                Player.Get().TriggerAutoInteract(this);
                return;
            }
            
            label.Show();
        }

        public override void Disable() {
            HideView(label);
        }
    }
}