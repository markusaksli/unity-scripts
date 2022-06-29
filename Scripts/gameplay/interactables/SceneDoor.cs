using Doozy.Engine.UI;
using state;
using TMPro;
using UnityEngine;

namespace gameplay.interactables {
    public class SceneDoor : Interactable {
        public string labelText;
        public bool disabled;
        public bool saveOnUse;
        [Scene] public string connectedScene;
        public UIView label;

        private TMP_Text tmpText;

        private void Start() {
            tmpText = label.GetComponentInChildren<TMP_Text>();
            tmpText.text = labelText;
        }

        public override int Priority() {
            if (disabled) {
                return -1;
            }

            return 0;
        }

        public override void Interact() {
            StateManager.Get().LoadDoorScene(this);
        }

        public override void Enable() {
            if (disabled) {
                return;
            }
            
            label.Show();
        }

        public override void Disable() {
            HideView(label);
        }

        public override Color OutlineColor() {
            return Color.black;
        }

        public override void Highlight(bool highlight) {
            if (disabled) return;
            HighlightText(tmpText, highlight);
        }

    }
}