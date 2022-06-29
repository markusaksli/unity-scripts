using System;
using System.Collections;
using Doozy.Engine.UI;
using FMODUnity;
using gameplay;
using JetBrains.Annotations;
using state;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace ui {
    public class IngameCanvasController : MonoBehaviour {
        [FormerlySerializedAs("dialogueView")] public UIView subtitlesView;
        public UIView pauseView;
        public ShaderUITransition transition;
        public UIView optionsView;
        public TextMeshProUGUI subtitles;
        private TMP_FontAsset defaultSubtitlesFont;

        private void Awake() {
            defaultSubtitlesFont = subtitles.font;
        }

        public void OnMenuClicked() {
            Time.timeScale = 1.0f;
            pauseView.Hide();
            StateManager.Get().StartCoroutine(StateManager.Get().LevelTransition(0, true));
        }

        public void HidePause() {
            Player.Get().HandlePause();
        }

        public void Subtitle(string text, StudioEventEmitter emitter, [CanBeNull] TMP_FontAsset fontAsset,
            UnityEvent onComplete) {
            StartCoroutine(PlaySubtitles(text, emitter, fontAsset, onComplete));
        }

        public void WorldSubtitle(StudioEventEmitter emitter, UIView label, UnityEvent onComplete, bool hide) {
            StartCoroutine(PlayWorldSubtitles(emitter, label, onComplete, hide));
        }

        private IEnumerator PlaySubtitles(string text, StudioEventEmitter emitter, [CanBeNull] TMP_FontAsset fontAsset,
            UnityEvent onComplete) {
            subtitles.text = text;
            if (fontAsset != null) {
                subtitles.font = fontAsset;
            }

            yield return new WaitUntil(() => !emitter.IsPlaying());
            subtitles.text = "";
            subtitles.font = defaultSubtitlesFont;
            onComplete.Invoke();
        }

        private IEnumerator PlayWorldSubtitles(StudioEventEmitter emitter, UIView label,
            UnityEvent onComplete, bool hide) {
            label.Show();
            yield return new WaitUntil(() => !emitter.IsPlaying());
            if (hide) {
                label.Hide();
            }

            onComplete.Invoke();
        }
    }
}