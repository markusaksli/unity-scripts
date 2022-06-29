using Doozy.Engine.UI;
using UnityEngine;

namespace gameplay.interactables {
    public class PureTextInteractable : Interactable {
        public UIView label;
        public bool disabled;
        public bool autoHide = true;
        
        public override int Priority() {
            return 100;
        }

        public override void Interact() { }

        public override Color OutlineColor() {
            throw new System.NotImplementedException();
        }

        public override void Highlight(bool highlight) {
            throw new System.NotImplementedException();
        }
        
        public override void Enable() {
            if (disabled) {
                return;
            }
            
            label.Show();
        }

        public override void Disable() {
            if(autoHide) HideView(label);
        }

        public void DisableAndTurnOff() {
            disabled = true;
            Disable();
        }

        public void EnableAndTurnOn() {
            disabled = false;
            Enable();
        }
    }
}