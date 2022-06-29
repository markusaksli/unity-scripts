using System;
using System.Collections;
using Doozy.Engine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace gameplay.interactables {
    public abstract class Interactable : MonoBehaviour, IComparable<Interactable> {
        public abstract int Priority();

        public abstract void Interact();

        public abstract void Enable();

        public abstract void Disable();

        public abstract Color OutlineColor();

        public abstract void Highlight(bool highlight);

        protected void Awake() {
            Assert.IsNotNull(gameObject.GetComponent<Collider>());
        }

        private void OnTriggerEnter(Collider other) {
            if (!other.gameObject.tag.Equals("Player")) return;
            if (Player.Get().interactDisabled) return;
            Player.Get().AddInteractable(this);
            Enable();
        }

        private void OnTriggerExit(Collider other) {
            if (!other.gameObject.tag.Equals("Player")) return;
            Player.Get().RemoveInteractable(this);
            Disable();
        }

        private void OnDestroy() {
            Player.Get().RemoveInteractable(this);
        }

        public int CompareTo(Interactable other) {
            return Priority().CompareTo(other.Priority());
        }


        protected void HideView(UIView view) {
            if (view.IsVisible) {
                view.Hide();
            }
            else if (view.IsShowing) {
                StartCoroutine(WaitHideView(view));
            }
        }
        
        protected void HighlightText(TMP_Text label, bool highlight) {
            if (highlight) {
                label.outlineWidth = 0.2f;
                label.outlineColor = OutlineColor();
            }
            else {
                label.outlineWidth = 0.0f;
            }
        }

        private static IEnumerator WaitHideView(UIView view) {
            yield return new WaitUntil(() => view.IsVisible);
            view.Hide();
        }
    }
}