using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
 * 
 * Manage mouse sensitivity slider
 * 
 **/

namespace Fusion.XR.Shared.Desktop
{
    public class MouseSensitivity : MonoBehaviour
    {
        public Slider mouseSensitivitySlider;
        public List<MouseCamera> mouseCameras;
        public float mouseSensitivity { get; private set; }


        private void Awake()
        {
            if (mouseSensitivitySlider == null) Debug.Log("Slider not set");
            if(mouseCameras == null || mouseCameras.Count == 0) mouseCameras = new List<MouseCamera>(FindObjectsOfType<MouseCamera>(true));
        }

        // restore previous settings
        private void OnEnable()
        {
            mouseSensitivitySlider.onValueChanged.RemoveListener(SetMouseSensivity);

            // restore saved volume parameters
            RestoreMouseSensivity();

            // Add listeners on volume sliders
            mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensivity);

        }

        // Apply change
        public void SetMouseSensivity(float value)
        {
            Debug.Log($"Set mouse sensivity to {value} ({mouseCameras.Count} mouse camera)");
            PlayerPrefs.SetFloat("MouseSensivity", value);
            foreach(var mouseCamera in mouseCameras) mouseCamera.sensitivity = new Vector2(value * 100, value * 100);
        }

        public void RestoreMouseSensivity()
        {
            float previousMouseSensitivity = PlayerPrefs.GetFloat("MouseSensivity", 0.65f);
            Debug.Log($"Restore previous mouse sensitivity : {previousMouseSensitivity}");
            mouseSensitivitySlider.value = previousMouseSensitivity;
            foreach (var mouseCamera in mouseCameras) mouseCamera.sensitivity = new Vector2(previousMouseSensitivity * 100, previousMouseSensitivity * 100);
        }

    }
}
