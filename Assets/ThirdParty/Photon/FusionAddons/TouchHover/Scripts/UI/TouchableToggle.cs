using Fusion.Addons.Touch;
using Fusion.XR.Shared.Touch;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fusion.Addons.Touch.UI
{
    /// <summary>
    /// Adds VR touch support to Unity UI Toggle
    /// Should be a child of a Toggle to give it touch capabilities
    /// </summary>
    public class TouchableToggle : MonoBehaviour, ITouchableUIExtension
    {
        public Toggle toggle;
        public BoxCollider box;
        public RectTransform toggleRectTransform;
        public Touchable touchable;

        bool adaptSize = true;

        #region ITouchableUIExtension
        public System.Type ExtenableUIComponent => typeof(Toggle);
        #endregion

        private void Awake()
        {
            if (toggle == null) toggle = GetComponentInParent<Toggle>();
            if (touchable == null) touchable = GetComponent<Touchable>();

            box = GetComponent<BoxCollider>();
            if (box == null)
            {
                box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
            }

            toggleRectTransform = toggle.GetComponent<RectTransform>();

            // Hook up the touch event to trigger toggle
            if (touchable != null)
            {
                touchable.onTouch.AddListener(OnTouch);
            }
        }

        private void OnEnable()
        {
            if (adaptSize)
                StartCoroutine(AdaptSize());
        }

        // Adapt the size of the 3D collider according to the UI toggle
        IEnumerator AdaptSize()
        {
            // We have to wait one frame for rect sizes to be properly set by Unity
            yield return new WaitForEndOfFrame();

            Vector3 newSize = new Vector3(
                toggleRectTransform.rect.size.x / toggle.transform.localScale.x,
                toggleRectTransform.rect.size.y / toggle.transform.localScale.y,
                box.size.z);
            box.size = newSize;
        }

        // When touched, toggle the state
        private void OnTouch()
        {
            if (toggle != null && toggle.IsInteractable())
            {
                // Simulate a pointer click to toggle the state
                ExecuteEvents.Execute(
                    toggle.gameObject,
                    new PointerEventData(EventSystem.current),
                    ExecuteEvents.pointerClickHandler);
            }
        }
    }
}