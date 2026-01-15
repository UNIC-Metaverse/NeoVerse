using Fusion.XR.Shared.Rig;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Fusion.XR.Shared.Locomotion
{
    /**
     * Locomotion system
     * - Left stick: smooth move (forward/back + optional strafe)
     * - Right stick X (optional): SNAP TURN using coroutine + fade (like original)
     * - Teleport: unchanged (RayBeamer onRelease)
     */
    [RequireComponent(typeof(HardwareRig))]
    public class RigLocomotion : MonoBehaviour
    {
#if ENABLE_INPUT_SYSTEM
        [Header("Move (Left stick)")]
        public InputActionProperty leftMoveAction;

        [Header("Snap Turn (Right stick X)")]
        public InputActionProperty rightControllerTurnAction;
#endif

        [Header("Authority / Local rig")]
        [Tooltip("Only the local rig should move. Set false for remote rigs.")]
        public bool isLocalRig = true;

        [Tooltip("Auto-disable locomotion if no XR headset is active (helps avoid moving remote rigs).")]
        public bool autoDetectLocalRig = true;

        [Header("Smooth Move")]
        public float moveSpeed = 1.6f;          // meters per second
        public float moveDeadzone = 0.2f;
        public bool headRelativeMovement = true;
        public bool strafe = true;
        public bool keepFeetOnGround = true;

        [Header("Snap Turn Settings")]
        public bool enableSnapTurn = true;
        public float debounceTime = 0.5f;
        public float snapDegree = 45f;
        public float rotationInputThreshold = 0.5f;

        [Header("Teleportation")]
        [Tooltip("Automatically found if not set")]
        public List<RayBeamer> teleportBeamers;

        public LayerMask locomotionLayerMask = 0;

        bool rotating = false;
        float timeStarted = 0f;

        HardwareRig rig;

        // If locomotion constraints are needed, a ILocomotionValidationHandler can restrict them
        ILocomotionValidationHandler locomotionValidationHandler;

        private void Awake()
        {
            rig = GetComponentInParent<HardwareRig>();
            locomotionValidationHandler = GetComponentInParent<ILocomotionValidationHandler>();

            if (teleportBeamers == null) teleportBeamers = new List<RayBeamer>();
            if (teleportBeamers.Count == 0) teleportBeamers = new List<RayBeamer>(GetComponentsInChildren<RayBeamer>());
            foreach (var beamer in teleportBeamers)
            {
                beamer.onRelease.AddListener(OnBeamRelease);
            }

#if ENABLE_INPUT_SYSTEM
            var bindings = new List<string> { "joystick" };

            // Left stick movement
            leftMoveAction.EnableWithDefaultXRBindings(leftBindings: bindings);

            // Right stick snap turn
            rightControllerTurnAction.EnableWithDefaultXRBindings(rightBindings: bindings);
#else
            Debug.LogError("Missing com.unity.inputsystem package");
#endif
        }

        private void Start()
        {
            if (locomotionLayerMask == 0)
                Debug.LogError("RigLocomotion: for locomotion to be possible, at least one layer has to be added to locomotionLayerMask, add used on locomotion surface colliders");
        }

        protected virtual void Update()
        {
            if (!ShouldRunLocomotion()) return;

            CheckMove();

            if (enableSnapTurn)
                CheckSnapTurnRightStick();
        }

        bool ShouldRunLocomotion()
        {
            if (!isLocalRig) return false;
            if (!autoDetectLocalRig) return true;

            // Practical heuristic: if there's no XR headset active, don't run locomotion.
            return UnityEngine.XR.XRSettings.isDeviceActive;
        }

        protected virtual void CheckMove()
        {
#if ENABLE_INPUT_SYSTEM
            Vector2 input = leftMoveAction.action.ReadValue<Vector2>();
            if (input.magnitude < moveDeadzone) return;

            Transform basis = headRelativeMovement ? rig.headset.transform : rig.transform;

            Vector3 forward = basis.forward;
            Vector3 right = basis.right;

            if (keepFeetOnGround)
            {
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
            }

            Vector3 move = forward * input.y;
            if (strafe) move += right * input.x;

            Vector3 delta = move * moveSpeed * Time.deltaTime;
            rig.transform.position += delta;
#else
            Debug.LogError("Missing com.unity.inputsystem package");
#endif
        }

        protected virtual void CheckSnapTurnRightStick()
        {
#if ENABLE_INPUT_SYSTEM
            if (rotating) return;

            if (timeStarted > 0f)
            {
                // Debounce: wait before allowing another turn
                if (timeStarted + debounceTime < Time.time)
                    timeStarted = 0f;

                return;
            }

            float x = rightControllerTurnAction.action.ReadValue<Vector2>().x;

            if (Mathf.Abs(x) > rotationInputThreshold)
            {
                timeStarted = Time.time;
                StartCoroutine(Rotate(Mathf.Sign(x) * snapDegree));
            }
#else
            Debug.LogError("Missing com.unity.inputsystem package");
#endif
        }

        IEnumerator Rotate(float angle)
        {
            timeStarted = Time.time;
            rotating = true;

            // Fade + rotate uses HardwareRig.FadedRotate()
            yield return rig.FadedRotate(angle);

            rotating = false;
        }

        public virtual bool ValidLocomotionSurface(Collider surfaceCollider)
        {
            // Check if the hit collider is in the locomotion layer mask
            bool colliderInLocomotionLayerMask = locomotionLayerMask == (locomotionLayerMask | (1 << surfaceCollider.gameObject.layer));
            return colliderInLocomotionLayerMask;
        }

        protected virtual void OnBeamRelease(Collider lastHitCollider, Vector3 position)
        {
            if (!enabled) return;

            // Checking potential validation handler
            if (locomotionValidationHandler != null)
            {
                var headsetPositionRelativeToRig = rig.transform.InverseTransformPoint(rig.headset.transform.position);
                Vector3 newHeadsetPosition = position + headsetPositionRelativeToRig.y * rig.transform.up;
                if (!locomotionValidationHandler.CanMoveHeadset(newHeadsetPosition)) return;
            }

            // Checking target surface layer
            if (ValidLocomotionSurface(lastHitCollider))
            {
                StartCoroutine(rig.FadedTeleport(position));
            }
        }
    }
}
