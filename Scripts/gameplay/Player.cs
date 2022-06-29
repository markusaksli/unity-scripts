using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECM2.Characters;
using ECM2.Components;
using FMOD;
using FMODUnity;
using gameplay.interactables;
using MoreMountains.Feedbacks;
using state;
using ui;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace gameplay {
    public class Player : MonoBehaviour {
        public Light flashlight;
        private static Player _instance;
        private static readonly TaskCompletionSource<bool> isInitialized = new();

        private readonly List<Interactable> interactables = new();
        private CapsuleCollider pCollider;

        public IngameCanvasController canvasController;
        public Dialogue currentDialogue;
        public ParticleSystem grassParticle;

        private Camera cam;
        public FirstPersonCharacter controller;
        public CharacterLook characterLook;

        public bool moveDisabled;
        public bool interactDisabled;
        public bool pauseDisabled;

        public LayerMask interactRaycastLayerMask = 1 << 20; // Bit mask for layer 20 (interactables), for ray casting.
        public float interactRaycastMaxDistance = 8.0f;
        public float groundRaycastMaxDistance = 8.0f;
        [Range(1, 4)] public float stride = 2;
        [Range(0, 1)] public float strideElongation = 0.3f;
        private float strideRef;
        private bool isLookingAtInteractable;
        private bool doneRotating = true;
        private bool rotateBack;
        private Quaternion targetRotation;
        private float lookRotationRef;
        private Interactable lookedAtInteractable;
        private bool previousLockState;
        private string lastGroundTag;
        private Rigidbody rb;

        private float defaultFov;
        private float fovKickAmount;
        private Quaternion previousCameraRotation;

        public MMFeedbacks stepFeedback;
        public MMFeedbacks jumpFeedback;
        public MMFeedbacks landFeedback;

        private enum GroundTag {
            Concrete = 0,
            Carpet = 1,
            Grass = 2
        }

        public static Player Get() {
            return _instance;
        }

        public static async Task<Player> GetSafe() {
            await isInitialized.Task;
            return _instance;
        }

        private void Awake() {
            _instance = this;
            rb = GetComponent<Rigidbody>();
            pCollider = GetComponent<CapsuleCollider>();
            controller = GetComponent<FirstPersonCharacter>();
            controller.WillLand += PlayLand;
            controller.Jumped += PlayJump;
            jumpFeedback.Initialization();
            characterLook = GetComponent<CharacterLook>();
            previousLockState = characterLook.lockCursor;
            cam = Camera.main;
            var inputAsset = controller.actions;
            var interactAction = inputAsset.FindAction("Interact");
            interactAction.started += OnInteract;
            interactAction.Enable();
            var pauseAction = inputAsset.FindAction("Pause");
            pauseAction.started += OnPause;
            pauseAction.Enable();
            var moveAction = inputAsset.FindAction("Movement");
            moveAction.started += OnMove;
            var jumpAction = inputAsset.FindAction("Jump");
            jumpAction.started += OnMove;
            isInitialized.SetResult(true);
            flashlight = GetComponentInChildren<Light>();
            flashlight.enabled = false;
        }

        private void SetGroundTag() {
            GroundTag groundTag = GroundTag.Concrete;
            var ray = new Ray(transform.position, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, groundRaycastMaxDistance)) {
                Enum.TryParse(hit.collider.tag, out groundTag);
            }

            var result = RuntimeManager.StudioSystem.setParameterByName("GroundTag", (float) groundTag);
            if (!result.Equals(RESULT.OK))
                Debug.LogWarning($"{result}: Could not register ground tag {(float) groundTag} for FMOD");
        }

        private void Update() {
            HandleFootsteps();
            if (controller.IsFalling() && grassParticle.isPlaying) {
                grassParticle.Pause();
            }
            else if (controller.IsOnGround() && !grassParticle.isPlaying) {
                grassParticle.Play();
            }
        }

        private void FixedUpdate() {
            HandleInteractionRaycast();
        }

        private void HandleFootsteps() {
            if (!controller.GetMovementMode().Equals(MovementMode.Walking)) {
                strideRef = stride / 2;
                return;
            }

            var velocity = controller.GetVelocity().magnitude;
            if (velocity < 0.01f) {
                strideRef = stride / 2;
                return;
            }

            var adjustedStride = stride + stride * strideElongation * (velocity / controller.maxWalkSpeed);
            if (strideRef >= adjustedStride) {
                PlayFootstep();
                strideRef = 0;
            }

            strideRef += velocity * Time.deltaTime;
        }

        private void PlayFootstep() {
            SetGroundTag();
            RuntimeManager.PlayOneShotAttached("event:/SFX/Ground/Footstep", gameObject);
            stepFeedback.PlayFeedbacks();
        }

        private void PlayJump() {
            SetGroundTag();
            RuntimeManager.PlayOneShotAttached("event:/SFX/Ground/Jump", gameObject);
            jumpFeedback.PlayFeedbacks();
        }

        private void PlayLand() {
            SetGroundTag();
            RuntimeManager.PlayOneShotAttached("event:/SFX/Ground/Land", gameObject);
            landFeedback.PlayFeedbacks();
        }

        private void HandleInteractionRaycast() {
            if (interactDisabled) {
                if (lookedAtInteractable != null) {
                    lookedAtInteractable.Highlight(false);
                }

                lookedAtInteractable = null;
                return;
            }

            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactRaycastMaxDistance, interactRaycastLayerMask)
                && currentDialogue == null) {
                Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);
                isLookingAtInteractable = true;

                GameObject hitGameObject = hit.collider.gameObject;
                if (lookedAtInteractable == null || !lookedAtInteractable.gameObject.Equals(hitGameObject)) {
                    if (lookedAtInteractable != null) {
                        lookedAtInteractable.Highlight(false);
                    }

                    lookedAtInteractable = hitGameObject.GetComponentInParent<Interactable>();
                    if (interactables.Contains(lookedAtInteractable)) lookedAtInteractable.Highlight(true);
                }
            }
            else {
                Debug.DrawRay(ray.origin, ray.direction * interactRaycastMaxDistance, Color.yellow);
                isLookingAtInteractable = false;
                if (lookedAtInteractable != null) {
                    lookedAtInteractable.Highlight(false);
                }

                lookedAtInteractable = null;
            }
        }

        private void OnMove(InputAction.CallbackContext ctx) {
            if (!moveDisabled) return;
            if (currentDialogue != null) {
                if (currentDialogue.isSkippable) {
                    currentDialogue.End();
                }
                else {
                    currentDialogue.scrolling = false;
                }
            }
        }

        private void OnInteract(InputAction.CallbackContext ctx) {
            if (currentDialogue != null) {
                currentDialogue.scrolling = false;
                return;
            }
            
            if (interactDisabled 
                || !isLookingAtInteractable 
                || lookedAtInteractable == null 
                || interactables.Count <= 0 
                || !interactables.Contains(lookedAtInteractable)) {
                return;
            }

            // Clean list from disabled and destroyed interactables
            foreach (var interactable in interactables.Where(interactable =>
                         interactable == null || interactable.Priority() == -1)) {
                RemoveInteractable(interactable);
            }

            // If third-person or 2D
            // interactables.Sort();
            // interactables[0].Interact();

            lookedAtInteractable.Interact();

            Debug.Log($"Interacted with {lookedAtInteractable.name}");
        }

        private void OnPause(InputAction.CallbackContext ctx) {
            HandlePause();
        }

        public void HandlePause() {
            if (SceneManager.GetActiveScene().buildIndex == 0) {
                return;
            }

            if (Time.timeScale == 0f) {
                if (canvasController.optionsView.IsVisible || canvasController.optionsView.IsShowing) {
                    canvasController.optionsView.Hide();
                    canvasController.pauseView.Show();
                    return;
                }

                canvasController.pauseView.Hide();
                Time.timeScale = 1.0f;
                if (currentDialogue == null || (currentDialogue?.autoPlay ?? true)) {
                    EnableMovement();
                }

                if (previousLockState) {
                    Utils.LockCursor();
                }
                else {
                    Utils.UnlockCursor();
                }
            }
            else if (!pauseDisabled) {
                previousLockState = characterLook.lockCursor;
                Utils.UnlockCursor();
                DisableMovement();
                canvasController.pauseView.Show();
                Time.timeScale = 0f;
            }
        }

        public void AddInteractable(Interactable interactable) {
            if (interactDisabled || interactables.Contains(interactable) || interactable.Priority() == -1) {
                return;
            }

            print("Added " + interactable.name + " to interactables");
            interactables.Add(interactable);
        }

        public void RemoveInteractable(Interactable interactable) {
            if (interactables.Remove(interactable)) {
                print("Removed " + interactable.name + " from interactables");
            }
        }

        public void EnableMovement() {
            Debug.Log("Enable movement");
            controller.SetMovementMode(MovementMode.Walking);
            controller.GetCharacterLook().enabled = true;
            SystemInitScript.SetPlayerMovementConstraints();
            moveDisabled = false;
        }

        public void EnableInteract() {
            interactDisabled = false;
            interactables.Clear();
            Vector3 capsulePos = transform.position + pCollider.center;
            foreach (var col in Physics.OverlapCapsule(
                         new Vector3(capsulePos.x, capsulePos.y - pCollider.height / 2, capsulePos.z),
                         new Vector3(capsulePos.x, capsulePos.y + pCollider.height / 2, capsulePos.z),
                         pCollider.radius)) {
                if (!(col.GetComponent<Interactable>() is { } interactable)) continue;
                interactable.Enable();
                AddInteractable(interactable);
            }
        }

        public void DisableMovement() {
            Debug.Log("Disable movement");
            controller.SetMovementMode(MovementMode.None);
            controller.GetCharacterLook().enabled = false;
            moveDisabled = true;
        }

        public void DisableInteract() {
            interactDisabled = true;
            foreach (var interactable in interactables) {
                interactable.Disable();
            }
        }

        public void ClearAndDisable() {
            if (currentDialogue != null) {
                currentDialogue.Interrupt();
            }

            DisableInteract();
            DisableMovement();
        }

        public void Teleport(Transform location) {
            controller.SetPosition(location.position);
            var rotation = location.rotation;
            controller.SetYaw(rotation.eulerAngles.y);
            controller.eyePivot.localRotation = Quaternion.Euler(rotation.eulerAngles.x, 0f, 0f);
            rb.velocity = Vector3.zero;
        }

        public void TriggerAutoInteract(Interactable interactable) {
            lookedAtInteractable = interactable;
            lookedAtInteractable.Interact();
        }
    }
}