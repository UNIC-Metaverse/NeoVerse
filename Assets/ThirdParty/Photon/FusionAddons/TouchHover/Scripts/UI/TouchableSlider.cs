using Fusion.Addons.HapticAndAudioFeedback;
using Fusion.XR.Shared.Touch;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/**
 * Add support for VR touch interaction to an UI slider
 * Auto adapt the touchable slider collider to the actual slider size
 **/

namespace Fusion.Addons.Touch.UI
{
    /// <summary>
    /// Should be stored as a child of a Slider to give touch capabilities to it
    /// </summary>
    public class TouchableSlider : MonoBehaviour, ITouchableUIExtension
    {
        public Slider slider;
        public BoxCollider box;
        public RectTransform sliderRectTransform;

        bool adaptSize = true;

        Collider lastToucherCollider = null;

        public AudioSource audioSource;
        private SoundManager soundManager;

        public string audioFeedbackType = Touchable.DefaultTouchFeedback;

        #region ITouchableUIExtension
        public System.Type ExtenableUIComponent => typeof(Slider);
        #endregion

        private void Awake()
        {
            if (slider == null) slider = GetComponentInParent<Slider>();
            box = GetComponentInParent<BoxCollider>();
            if (box == null) gameObject.AddComponent<BoxCollider>();
            sliderRectTransform = slider.GetComponent<RectTransform>();            
        }

        void Start()
        {
            if (soundManager == null) soundManager = SoundManager.FindInstance();
        }

        private void OnEnable()
        {
            if (adaptSize)
                StartCoroutine(AdaptSize());
        }

        // Adapt the size of the 3D button collider according to the UI button
        IEnumerator AdaptSize()
        {
            // We have to wait one frame for rect sizes to be properly set by Unity
            yield return new WaitForEndOfFrame();

            Vector3 newSize = new Vector3(sliderRectTransform.rect.size.x / slider.transform.localScale.x, sliderRectTransform.rect.size.y / slider.transform.localScale.y, box.size.z);
            box.size = newSize;
        }

        private void OnTriggerStay(Collider other)
        {
            // Note, we cannot use the ITouchable callbacks, as we need to know which Toucher collider hit the slider.
            Toucher toucher = other.GetComponentInParent<Toucher>();
            if (lastToucherCollider == other || toucher != null)
            {
                lastToucherCollider = other;
                MoveSliderToPosition(other.transform.position);

                toucher.hardwareHand.SendHapticImpulse();
            }
        }

        public void MoveSliderToPosition(Vector3 position, bool instantMove = false)
        {
            float xStart = -box.size.x / 2;
            float xEnd = box.size.x / 2;

            float x = transform.InverseTransformPoint(position).x;
            float progress = (x - xStart) / (xEnd - xStart);
            slider.value = progress;

            if (instantMove && soundManager)
                soundManager.PlayOneShot(audioFeedbackType, audioSource);

        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<Toucher>() != null)
            {
                if (soundManager) soundManager.PlayOneShot(audioFeedbackType, audioSource);
            }
        }
    }
}
