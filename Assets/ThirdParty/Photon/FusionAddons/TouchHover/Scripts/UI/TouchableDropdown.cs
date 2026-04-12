using Fusion.Addons.Touch;
using Fusion.XR.Shared.Touch;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fusion.Addons.Touch.UI
{
    /// <summary>
    /// Adds VR touch support to TMP_Dropdown
    /// Should be a child of a TMP_Dropdown to give it touch capabilities
    /// </summary>
    public class TouchableDropdown : MonoBehaviour, ITouchableUIExtension
    {
        public TMP_Dropdown dropdown;
        public BoxCollider box;
        public RectTransform dropdownRectTransform;
        public Touchable touchable;

        bool adaptSize = true;

        // Track dynamically created touchable items
        private List<GameObject> touchableDropdownItems = new List<GameObject>();
        private bool isDropdownOpen = false;
        private GameObject currentDropdownListObject;

        #region ITouchableUIExtension
        public System.Type ExtenableUIComponent => typeof(TMP_Dropdown);
        #endregion

        private void Awake()
        {
            if (dropdown == null) dropdown = GetComponentInParent<TMP_Dropdown>();
            if (touchable == null) touchable = GetComponent<Touchable>();

            box = GetComponent<BoxCollider>();
            if (box == null)
            {
                box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
            }

            dropdownRectTransform = dropdown.GetComponent<RectTransform>();

            // Hook up the touch event to trigger dropdown
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

        private void OnDisable()
        {
            CleanupDropdownItems();
            isDropdownOpen = false;
        }

        // Adapt the size of the 3D collider according to the UI dropdown
        IEnumerator AdaptSize()
        {
            // We have to wait one frame for rect sizes to be properly set by Unity
            yield return new WaitForEndOfFrame();

            Vector3 newSize = new Vector3(
                dropdownRectTransform.rect.size.x / dropdown.transform.localScale.x,
                dropdownRectTransform.rect.size.y / dropdown.transform.localScale.y,
                box.size.z);
            box.size = newSize;
        }

        // When touched, toggle the dropdown open/close
        private void OnTouch()
        {
            if (dropdown != null && dropdown.IsInteractable())
            {
                Debug.Log($"TouchableDropdown OnTouch - isDropdownOpen: {isDropdownOpen}");

                if (isDropdownOpen)
                {
                    // Close the dropdown by clicking it again
                    CloseDropdown();
                }
                else
                {
                    // Open the dropdown
                    ExecuteEvents.Execute(
                        dropdown.gameObject,
                        new PointerEventData(EventSystem.current),
                        ExecuteEvents.pointerClickHandler);

                    // Wait a frame then make items touchable
                    StartCoroutine(MakeDropdownItemsTouchable());
                }
            }
        }

        private void CloseDropdown()
        {
            Debug.Log("Closing dropdown");

            // Cleanup first
            CleanupDropdownItems();
            isDropdownOpen = false;

            // Then close the dropdown
            dropdown.Hide();
        }

        private IEnumerator MakeDropdownItemsTouchable()
        {
            // Wait for dropdown to fully instantiate its items
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            // Find the dropdown template container
            Transform dropdownList = FindDropdownList();

            if (dropdownList != null)
            {
                Debug.Log($"Found dropdown list: {dropdownList.name}");
                currentDropdownListObject = dropdownList.gameObject;
                isDropdownOpen = true;
                CleanupDropdownItems(); // Clean any existing items first

                // Find all Toggle components (dropdown items)
                Toggle[] itemToggles = dropdownList.GetComponentsInChildren<Toggle>(true);
                Debug.Log($"Found {itemToggles.Length} toggles");

                foreach (Toggle toggle in itemToggles)
                {
                    // Skip the template item
                    if (!toggle.gameObject.activeInHierarchy)
                    {
                        Debug.Log($"Skipping inactive toggle: {toggle.name}");
                        continue;
                    }

                    Debug.Log($"Creating touchable for toggle: {toggle.name}");
                    CreateTouchableItemForToggle(toggle);
                }

                // Monitor when dropdown closes
                StartCoroutine(MonitorDropdownClose(dropdownList.gameObject));
            }
            else
            {
                Debug.LogWarning("Could not find dropdown list!");
                isDropdownOpen = false;
            }
        }

        private void CreateTouchableItemForToggle(Toggle toggle)
        {
            // Create a child GameObject with collider and Touchable
            GameObject touchableItem = new GameObject("TouchableItem");
            touchableItem.transform.SetParent(toggle.transform, false);
            touchableItem.transform.localPosition = Vector3.zero;
            touchableItem.transform.localRotation = Quaternion.identity;
            touchableItem.transform.localScale = Vector3.one;

            // Add RectTransform
            RectTransform rectTransform = touchableItem.AddComponent<RectTransform>();
            RectTransform toggleRect = toggle.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Add BoxCollider
            BoxCollider itemBox = touchableItem.AddComponent<BoxCollider>();
            itemBox.isTrigger = true;

            // Set collider size immediately based on the toggle's rect
            Vector3 size = new Vector3(
                toggleRect.rect.size.x,
                toggleRect.rect.size.y,
                10f); // Larger depth to ensure it's touchable

            itemBox.size = size;
            itemBox.center = Vector3.zero;

            Debug.Log($"Created collider for {toggle.name} with size: {size}");

            // Add Touchable component
            Touchable itemTouchable = touchableItem.AddComponent<Touchable>();

            // Configure touchable settings
            itemTouchable.timeBetweenTouchTrigger = 0.1f;
            itemTouchable.timeBetweenUnTouchTrigger = 0.1f;

            // Capture the toggle reference for the lambda
            Toggle currentToggle = toggle;
            itemTouchable.onTouch.AddListener(() => OnDropdownItemTouch(currentToggle));

            touchableDropdownItems.Add(touchableItem);
        }

        private void OnDropdownItemTouch(Toggle toggle)
        {
            Debug.Log($"Dropdown item touched: {toggle.name}");

            if (toggle != null && toggle.isActiveAndEnabled)
            {
                // Simulate a click on the toggle
                ExecuteEvents.Execute(
                    toggle.gameObject,
                    new PointerEventData(EventSystem.current),
                    ExecuteEvents.pointerClickHandler);

                // Give the dropdown a moment to process the selection, then ensure it closes
                StartCoroutine(EnsureDropdownClosed());
            }
        }

        private IEnumerator EnsureDropdownClosed()
        {
            yield return new WaitForEndOfFrame();

            if (isDropdownOpen)
            {
                CleanupDropdownItems();
                isDropdownOpen = false;
            }
        }

        private Transform FindDropdownList()
        {
            // TMP_Dropdown creates a template object in the canvas
            Canvas canvas = dropdown.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("No canvas found!");
                return null;
            }

            // Look for the blocker (which is parent of dropdown list)
            Transform blocker = canvas.transform.Find("Blocker");
            if (blocker != null)
            {
                Debug.Log($"Found blocker with {blocker.childCount} children");
                // The dropdown list is a child of the blocker
                if (blocker.childCount > 0)
                {
                    return blocker.GetChild(0);
                }
            }

            // Alternative: search for the dropdown list by checking for ScrollRect
            ScrollRect[] scrollRects = canvas.GetComponentsInChildren<ScrollRect>(false);
            Debug.Log($"Found {scrollRects.Length} ScrollRects");

            foreach (ScrollRect sr in scrollRects)
            {
                // Check if this scroll rect is likely our dropdown
                if (sr.transform.parent != null && sr.transform.parent.name.Contains("Blocker"))
                {
                    Debug.Log($"Found dropdown list via ScrollRect: {sr.name}");
                    return sr.transform;
                }
            }

            // Last resort: look for any object with "Dropdown" in the name
            foreach (Transform child in canvas.transform)
            {
                if (child.name.ToLower().Contains("dropdown") || child.name.ToLower().Contains("blocker"))
                {
                    Debug.Log($"Found potential dropdown via name search: {child.name}");
                    if (child.childCount > 0)
                    {
                        return child.GetChild(0);
                    }
                }
            }

            return null;
        }

        private IEnumerator MonitorDropdownClose(GameObject dropdownListObject)
        {
            // Wait until the dropdown list is destroyed or disabled
            while (dropdownListObject != null && dropdownListObject.activeInHierarchy)
            {
                yield return null;
            }

            // Dropdown closed, cleanup
            Debug.Log("Dropdown closed via monitor");
            isDropdownOpen = false;
            currentDropdownListObject = null;
            CleanupDropdownItems();
        }

        private void CleanupDropdownItems()
        {
            Debug.Log($"Cleaning up {touchableDropdownItems.Count} touchable items");

            foreach (GameObject item in touchableDropdownItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            touchableDropdownItems.Clear();
        }
    }
}