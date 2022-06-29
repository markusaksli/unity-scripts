using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ui {
    public class ShaderUITransition : MonoBehaviour {
        public float fade;
        public float rotation;

        private Material fadeMat;
        private int fadeProperty;
        private int rotationProperty;
        public Animation anim;

        public void Awake() {
            fadeMat = GetComponent<Image>().material;
            fadeProperty = Shader.PropertyToID("_DirectionalAlphaFadeFade");
            rotationProperty = Shader.PropertyToID("_DirectionalAlphaFadeRotation");
            anim = GetComponent<Animation>();
        }

        private void Start() {
            StartCoroutine(DisableAfterHide());
        }

        private void Update() {
            fadeMat.SetFloat(fadeProperty, fade);
            fadeMat.SetFloat(rotationProperty, rotation);
        }

        public void Show() {
            anim.Play("ShowAlphaFadeTransition");
        }
        
        public void Hide() {
            anim.Play("HideAlphaFadeTransition");
        }

        public void HideAndDisable() {
            StartCoroutine(DisableAfterHide());
        }

        public bool IsPlaying() {
            return anim.isPlaying;
        }

        IEnumerator DisableAfterHide() {
            anim.Play("HideAlphaFadeTransition");
            yield return new WaitUntil(() => !IsPlaying());
            gameObject.SetActive(false);
        }

        public void InstantHide() {
            if (anim.isPlaying) {
                anim.Stop();
            }

            rotation = 0f;
            fade = 5f;
            Update();
            
            gameObject.SetActive(false);
        }
        
        public void InstantShow() {
            if (anim.isPlaying) {
                anim.Stop();
            }

            rotation = 0f;
            fade = -8.5f;
            Update();
        }
    }
}