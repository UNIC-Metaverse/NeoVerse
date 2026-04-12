using Fusion.Addons.HapticAndAudioFeedback;
using UnityEngine;
using UnityEngine.Events;

namespace Fusion.Addons.Hover

{
    /**
     * Allow to touch an object with a BeamToucher, and trigger events. Should be associated with a trigger Collider
     * 
     * Provide visual, audio and haptic feedback
     */
    public class BeamHoverable : MonoBehaviour, IBeamHoverListener
    {
        public GameObject onHoverVisualFeedback;

        public Material onHoverableMaterial;
        Material initialMaterial;
        public Renderer targetRenderer;

        public AudioSource audioSource;
        private SoundManager soundManager;
        public string audioFeedbackType = "OnTouchButton";
        public UnityEvent onBeamRelease = new UnityEvent();
        public UnityEvent onBeamHoverStart;
        public UnityEvent onBeamHoverEnd;

        private void Awake()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            if (targetRenderer) initialMaterial = targetRenderer.material;
        }

        void Start()
        {
            if (soundManager == null) soundManager = SoundManager.FindInstance();
        }

        public void OnHoverEnd(BeamHoverer beamHoverer)
        {
            if (onHoverVisualFeedback)
                onHoverVisualFeedback.SetActive(false);

            if (targetRenderer) 
                targetRenderer.material = initialMaterial;

            if (onBeamHoverEnd != null) onBeamHoverEnd.Invoke();
        }

        public void OnHoverStart(BeamHoverer beamHoverer)
        {
            if (onHoverVisualFeedback)
                onHoverVisualFeedback.SetActive(true);

            if (targetRenderer && onHoverableMaterial) 
                targetRenderer.material = onHoverableMaterial;

            if (onBeamHoverStart != null) onBeamHoverStart.Invoke();

            beamHoverer.hardwareHand.SendHapticImpulse();

            if (soundManager)
                soundManager.PlayOneShot(audioFeedbackType, audioSource);
        }

        public void OnHoverRelease(BeamHoverer beamHoverer)
        {
            if (targetRenderer)
                targetRenderer.material = initialMaterial;

            if (onBeamRelease != null) onBeamRelease.Invoke();
        }
    }

}
