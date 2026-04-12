using System.Collections.Generic;
using UnityEngine;

/**
 * 
 * Add a prefab for some UI elements (Button, Slider, inputfield), so that these UI components can also be touched in VR
 *
 **/

namespace Fusion.Addons.Touch.UI
{
    public interface ITouchableUIExtension
    {
        // Specify which to UI component this component can provide touch capabilities
        System.Type ExtenableUIComponent { get; }
    }
    public class TouchableCanvas : MonoBehaviour
    {
        public List<GameObject> touchableExtensionPrefabs = new List<GameObject>();
        bool didInstallUITouchButton = false;

        private void OnEnable()
        {
            InstallUITouchButton();
        }

        void InstallUITouchButton()
        {
            if (didInstallUITouchButton) return;
            didInstallUITouchButton = true;
            foreach(var touchableExtensionPrefab in touchableExtensionPrefabs)
            {
                ITouchableUIExtension touchableExtension = touchableExtensionPrefab.GetComponentInChildren<ITouchableUIExtension>();
                System.Type uiClassToDetect = touchableExtension.ExtenableUIComponent;
                var uiItems = GetComponentsInChildren(uiClassToDetect, true);
                
                foreach (var uiItem in uiItems)
                {
                    if (uiItem.GetComponentInChildren(touchableExtension.GetType()) != null)
                    {
                        // Already has a uiClassToDetect component
                        continue;
                    }
                    SpawnTouchableExtension(touchableExtensionPrefab, uiItem);
                }
            }
        }

        public void SpawnTouchableExtension(Component uiItem)
        {
            foreach (var touchableExtensionPrefab in touchableExtensionPrefabs)
            {
                ITouchableUIExtension touchableExtension = touchableExtensionPrefab.GetComponentInChildren<ITouchableUIExtension>();
                System.Type uiClassToDetect = touchableExtension.ExtenableUIComponent;
                if (uiClassToDetect == uiItem.GetType())
                {
                    SpawnTouchableExtension(touchableExtensionPrefab, uiItem);
                }
            }
        }

        public void SpawnTouchableExtension(GameObject touchableExtensionPrefab, Component uiItem)
        {
            var touchable = GameObject.Instantiate(touchableExtensionPrefab, uiItem.transform);
            touchable.transform.position = uiItem.transform.position;
            touchable.transform.rotation = uiItem.transform.rotation;
            touchable.transform.localScale = uiItem.transform.localScale;
        }
    }

}
