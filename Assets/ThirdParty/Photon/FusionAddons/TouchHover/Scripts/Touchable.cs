using Fusion.Addons.HapticAndAudioFeedback;
using Fusion.XR.Shared.Touch;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Fusion.Addons.Touch
{
    /**
     * Allow to touch an object with a Toucher, and trigger events. Should be associated with a trigger Collider
     * 
     * Provide audio and haptic feedback
     */
    public class Touchable : MonoBehaviour, ITouchable
    {
        public const string DefaultTouchFeedback = "OnTouchButton";

        private MeshRenderer meshRenderer;


        private float lastTouchTime = 0;
        private float timeSinceLastTouch;
        private float lastUnTouchTime;
        private float timeSinceLastUnTouch;
        private bool isIncontact = false;

        [Header("Current state")]
        public bool buttonStatus = false;
        [Header("Anti-bounce")]
        public float timeBetweenTouchTrigger = 0.3f;
        public float timeBetweenUnTouchTrigger = 0.3f;
        [Header("Button")]
        public bool isToggleButton = false;
        private Material materialAtStart;
        public Material touchMaterial;
        public bool restoreMaterialAtUntouch = false;
        [Header("Feedback")]
        public Feedback feedback;
        public string audioFeedbackType = Touchable.DefaultTouchFeedback;

        [SerializeField] private bool playSoundWhenTouched = true;
        [SerializeField] private bool playHapticWhenTouched = true;

        [Header("Events")]
        public UnityEvent onTouch;
        public UnityEvent onUnTouch;

        [HideInInspector]
        // Can be true if another class handle the toucher contact analysis (for instance to align it with fun)
        public bool analyseTouchersContactsImmediatly = true;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            feedback = GetComponent<Feedback>();
            if (feedback == null)
            {
                feedback = gameObject.AddComponent<Feedback>();
            }

            if (meshRenderer) materialAtStart = meshRenderer.material;
        }

        private void OnEnable()
        {
            // We need to clear IsTouching status in case the component was disabled because the TriggerExit has not be called
            if (isIncontact && touchers.Count > 0)
            {
                isIncontact = false;
                touchers.Clear();
            }
        }

        [ContextMenu("OnPress")]
        public void OnTouch()
        {

            if (isToggleButton)
                buttonStatus = !buttonStatus;
            else
                buttonStatus = true;
            if (onTouch != null) onTouch.Invoke();

            UpdateButton();

            lastUnTouchTime = 0;
            if (feedback)
            {
                if (playHapticWhenTouched)
                {
                    foreach (var toucher in touchers)
                    {
                        feedback.PlayHapticFeedback(toucher.hardwareHand);
                    }
                }

                if (playSoundWhenTouched)
                    feedback.PlayAudioFeeback(audioFeedbackType);
            }
        }

        void UpdateButton()
        {
            if (!meshRenderer) return;
            if (touchMaterial && buttonStatus)
            {
                meshRenderer.material = touchMaterial;
            }
            else if (materialAtStart && !buttonStatus && restoreMaterialAtUntouch)
            {
                RestoreMaterial();
            }
        }

        private async void RestoreMaterial()
        {
            await System.Threading.Tasks.Task.Delay(100);

            if (!meshRenderer) return;
            meshRenderer.material = materialAtStart;
        }

        public void SetButtonStatus(bool status)
        {
            buttonStatus = status;
            UpdateButton();
        }

        public void OnUnTouch()
        {
            if (onUnTouch != null) onUnTouch.Invoke();

            if (!isToggleButton)
            {
                buttonStatus = false;
            }
            UpdateButton();
        }

        #region Toucher analysis
        List<Toucher> touchers = new List<Toucher>();

        void RegisterToucher(Toucher toucher)
        {
            if (touchers.Contains(toucher)) return;
            touchers.Add(toucher);
        }

        void UnregisterToucher(Toucher toucher)
        {
            if (!touchers.Contains(toucher)) return;
            touchers.Remove(toucher);
        }

        // Trigger a touch if not already in contact, and if a touch did not occured too recently
        public bool TryTouch()
        {
            bool didTouch = false;

            if (!isIncontact)
            {
                timeSinceLastTouch = Time.time - lastTouchTime;
                if (timeSinceLastTouch > timeBetweenTouchTrigger)
                {
                    lastTouchTime = Time.time;



                    OnTouch();
                    didTouch = true;
                }
                isIncontact = true;
            }

            return didTouch;
        }

        // Trigger an untouch if already in contact, and if an untouch did not occured too recently

        public bool TryUnTouch()
        {
            bool didUntouch = false;

            if (isIncontact)
            {
                timeSinceLastUnTouch = Time.time - lastUnTouchTime;
                if (timeSinceLastUnTouch > timeBetweenUnTouchTrigger)
                {
                    lastUnTouchTime = Time.time;
                    OnUnTouch();
                    didUntouch = true;
                }
                isIncontact = false;
            }
            return didUntouch;
        }

        // Check if touchers are in contact and trigger touch/untouch verifications accordingly
        public void CheckTouchers()
        {
            // Check if there was a Touch previously
            if (touchers.Count > 0)
            {
                TryTouch();
            }

            // Check if there is a new touch 
            if (touchers.Count == 0)
            {
                TryUnTouch();
            }
        }
        #endregion

        // Simulate a touch followed by an immediate untouch (no involment of touchers required here)
        public bool TryInstantTouch()
        {
            bool didTouch = TryTouch();
            TryUnTouch();
            return didTouch;
        }

        #region ITouchable
        public void OnToucherContactStart(Toucher toucher)
        {
            // Store toucher in contact
            RegisterToucher(toucher);

            // If networked, we will analyse it in FUN, otherwise, right now
            if (analyseTouchersContactsImmediatly)
            {
                CheckTouchers();
            }
        }

        public void OnToucherStay(Toucher toucher)
        {
            // Store toucher in contact
            RegisterToucher(toucher);

            // If networked, we will analyse it in FUN, otherwise, right now
            if (analyseTouchersContactsImmediatly)
            {
                CheckTouchers();
            }
        }

        public void OnToucherContactEnd(Toucher toucher)
        {
            // Forget toucher in contact
            UnregisterToucher(toucher);

            // If networked, we will analyse it in FUN, otherwise, right now
            if (analyseTouchersContactsImmediatly)
            {
                CheckTouchers();
            }
        }
        #endregion
    }

}
