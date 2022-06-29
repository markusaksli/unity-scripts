using System;
using System.Collections;
using FMODUnity;
using Ink.Runtime;
using state;
using TMPro;
using ui;
using UnityEngine;
using UnityEngine.Events;

namespace gameplay {
    public class Dialogue : MonoBehaviour {
        private const int DIALOGUE_SOUND_FREQ = 3;
        private const float TEXT_SPEED = 0.04f;
        private const string WAIT_CHARS = ".,!?";

        public bool isSkippable;
        public bool autoPlay;
        public float autoPlayDelay;
        public string dialogueSound;
        public Color textColor = Color.white;
        public TMP_FontAsset font;
        public bool scrolling;
        private Story story;
        private Player player;
        private IngameCanvasController canvas;
        private RectTransform textRect;

        private UnityEvent eventsToUse;

        // Creates a new Story object with the compiled story which we can then play!
        public void Init(TextAsset inkJsonAsset) {
            story = new Story(inkJsonAsset.text);
            canvas = Player.Get().canvasController;
            RefreshView();
            if (font == null) {
                font = StateManager.Get().defaultFont;
            }

            canvas.subtitlesView.Show();
            if (dialogueSound == null) {
                dialogueSound = StateManager.Get().defaultDialgoueSound;
            }

            canvas.subtitles.color = textColor;
            canvas.subtitles.font = font;
        }

        void RefreshView() {
            StartCoroutine(ScrollText());
        }

        IEnumerator ScrollText() {
            string text = "";
            // Read all the content until we can't continue any more
            while (story.canContinue) {
                scrolling = true;
                string displayText = "";
                text = story.Continue();
                if (story.currentTags.Count != 0) {
                    for (int i = 0; i < story.currentTags.Count; i++) {
                        string command = story.currentTags[i];
                        switch (command) {
                            case "key":
                                i++;
                                Enum.TryParse(story.currentTags[i], out SaveKey key);
                                StateManager.Get().AddDefinedKey(key);
                                Debug.Log($"Added {key} to defined keys");
                                break;
                            case "akey":
                                i++;
                                StateManager.Get().AddArbitraryKey(story.currentTags[i]);
                                Debug.Log($"Added {story.currentTags[i]} to arbitrary keys");
                                break;
                            case "play_music":
                                SystemInitScript.GetCurrent().levelMusic.Play();
                                Debug.Log("Started playing music");
                                break;
                            case "stop_music":
                                SystemInitScript.GetCurrent().levelMusic.Stop();
                                Debug.Log("Stopped music");
                                break;
                            case "change_music":
                                i++;
                                SystemInitScript.GetCurrent().levelMusic.Event = $"event:/{story.currentTags[i]}";
                                Debug.Log($"Changed music to {story.currentTags[i]}");
                                break;
                        }
                    }
                }

                int count = 0;
                foreach (char character in text) {
                    count++;
                    if (WAIT_CHARS.Contains(character.ToString()) || count >= DIALOGUE_SOUND_FREQ) {
                        RuntimeManager.PlayOneShot(dialogueSound);
                        count = 0;
                    }

                    if (!scrolling) {
                        canvas.subtitles.text = text;
                        break;
                    }

                    displayText += character;
                    canvas.subtitles.text = displayText;
                    if (WAIT_CHARS.Contains(character.ToString())) {
                        yield return new WaitForSeconds(TEXT_SPEED * 10);
                    }
                    else {
                        yield return new WaitForSeconds(TEXT_SPEED);
                    }
                }

                if (!story.canContinue) continue;
                scrolling = true;
                Coroutine smallStep = null;
                if (autoPlay) {
                    smallStep = StartCoroutine(AutoPlayStep());
                }

                yield return new WaitUntil(() => !scrolling);
                if (smallStep != null) {
                    StopCoroutine(smallStep);
                }
            }
            
            scrolling = true;
            Coroutine step = null;
            if (autoPlay) {
                step = StartCoroutine(AutoPlayStep());
            }

            yield return new WaitUntil(() => !scrolling);
            if (step != null) {
                StopCoroutine(step);
            }

            End();
        }

        private IEnumerator AutoPlayStep() {
            yield return new WaitForSeconds(autoPlayDelay);
            scrolling = !scrolling;
        }

        public void End() {
            canvas.subtitlesView.Hide();
            Utils.LockCursor();
            Player.Get().currentDialogue = null;

            eventsToUse?.Invoke();
            Player.Get().EnableInteract();
            if (!autoPlay) {
                Player.Get().EnableMovement();
            }

            Destroy(this);
        }

        public void Interrupt() {
            canvas.subtitlesView.Hide();
            Player.Get().currentDialogue = null;
            Utils.LockCursor();
            Destroy(this);
        }

        public void SetEvents(UnityEvent events) {
            eventsToUse = events;
        }
    }
}