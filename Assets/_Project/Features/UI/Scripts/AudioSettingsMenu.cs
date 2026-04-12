using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Voice.Unity;
using Photon.Voice;
using Photon.Voice.Fusion;
using Fusion.Addons.Touch.UI;

/**
 *
 * AudioSettingsMenu restores volume sliders with the audio setting manager values.
 * A listener is created for each slider in order to call the audio manager and save the new value.
 * It also creates a button for each microphone. The microphone button state is updated when it is selected by the user and then, saved in preference settings.
 * 
 **/

namespace Fusion.Samples.IndustriesComponents
{
    public class AudioSettingsMenu : VoiceComponent
    {
        public Slider masterVolume;
        public Slider voiceVolume;
        public Slider ambienceVolume;
        public Slider effectVolume;

        public RectTransform microphoneParent;
        public GameObject buttonPrefab;
        public GameObject labelPrefab;

        public FusionVoiceClient fusionVoiceClient;
        public Recorder recorder;
        public AudioSettingsManager audioSettingsManager;

        public AudioSource audioSource;

        public Managers managers;


        private void OnEnable()
        {
            if (managers == null) managers = Managers.FindInstance();
            if (fusionVoiceClient == null) fusionVoiceClient = managers.fusionVoiceClient;
            if (recorder == null) recorder = fusionVoiceClient.PrimaryRecorder;
            if (recorder == null)
            {
                return;
            }

            if (audioSettingsManager == null) audioSettingsManager = managers.audioSettingsManager;
            if (audioSettingsManager == null)
            {
                Debug.LogError("Audio Settings Manager not found");
                return;
            }

            if (audioSource == null)
            {
                Debug.LogError("AudioSource not found");
                return;
            }


            masterVolume.onValueChanged.RemoveListener(audioSettingsManager.SetMasterVolume);
            voiceVolume.onValueChanged.RemoveListener(audioSettingsManager.SetVoiceVolume);
            ambienceVolume.onValueChanged.RemoveListener(audioSettingsManager.SetAmbienceVolume);
            effectVolume.onValueChanged.RemoveListener(audioSettingsManager.SetEffectVolume);

            // restore saved volume parameters
            masterVolume.value = audioSettingsManager.masterVolume;
            voiceVolume.value = audioSettingsManager.voiceVolume;
            ambienceVolume.value = audioSettingsManager.ambienceVolume;
            effectVolume.value = audioSettingsManager.effectVolume;

            // Add listeners on volume sliders
            masterVolume.onValueChanged.AddListener(audioSettingsManager.SetMasterVolume);
            voiceVolume.onValueChanged.AddListener(audioSettingsManager.SetVoiceVolume);
            ambienceVolume.onValueChanged.AddListener(audioSettingsManager.SetAmbienceVolume);
            effectVolume.onValueChanged.AddListener(audioSettingsManager.SetEffectVolume);

            CreateMicrophoneButtons();

        }


        IDeviceEnumerator photonMicEnum;
        // create microphones buttons
        public void CreateMicrophoneButtons()
        {
            if (photonMicEnum == null)
            {
                photonMicEnum = Platform.CreateAudioInEnumerator(this.Logger);
            }
            photonMicEnum.Refresh();
            DestroyAllChildren(microphoneParent);

#if (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
            return;
#endif

            List<string> options = new List<string>();
            int selectedIndex = -1;
            if (photonMicEnum.IsSupported)
            {

#if !UNITY_WEBGL
                // Search for the microphone index
                if (recorder.MicrophoneType == Recorder.MicType.Unity)
                {
                    // Unity microphone
                    options = new List<string>(Microphone.devices);
                    if (recorder.MicrophoneDevice.IsDefault)
                    {
                        selectedIndex = 0;
                    }
                    else
                    {
                        selectedIndex = options.FindIndex(unityMicName => new DeviceInfo(unityMicName) == recorder.MicrophoneDevice);
                    }
                }
                else
                {   // Photon microphone
                    int i = 0;
                    foreach (var device in photonMicEnum)
                    {
                        options.Add(device.Name);
                        if (device == recorder.MicrophoneDevice)
                        {
                            selectedIndex = i;
                        }
                        i++;
                    }
                    if (selectedIndex == -1) Debug.LogError("Mic not found");
                }
#endif

                // Instantiate microphone buttons
                for (int i = 0; i < options.Count; ++i)
                {
                    GameObject go = null;
                    if (i == selectedIndex)
                    {
                        go = Instantiate(labelPrefab, microphoneParent);
                    }
                    else
                    {
                        int index = i;
                        go = Instantiate(buttonPrefab, microphoneParent);
                        var button = go.GetComponent<Button>();

                        TouchableCanvas touchableCanvas = GetComponentInParent<TouchableCanvas>();
                        if (touchableCanvas)
                        {
                            touchableCanvas.SpawnTouchableExtension(button);
                        }

                        if (button != null)
                        {
                            button.onClick.AddListener(delegate ()
                            {
                                OnInputDeviceChanged(index);
                            });
                        }
                    }

                    var label = go.GetComponentInChildren<TextMeshProUGUI>();
                    label.text = options[i];
                }
            }
        }

        // Destroy all space holder microphones
        public void DestroyAllChildren(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            int childCount = parent.childCount;
            for (int i = 0; i < childCount; ++i)
            {
                GameObject.Destroy(parent.GetChild(i).gameObject);
            }
        }

        // Update the microphone selected by the user and save it in preference settings
        void OnInputDeviceChanged(int value)
        {
#if !UNITY_WEBGL
            if (recorder.MicrophoneType == Recorder.MicType.Unity)
            {
                try
                {
                    recorder.MicrophoneDevice = new DeviceInfo(Microphone.devices[value]);
                    PlayerPrefs.SetString("UnityMic", recorder.MicrophoneDevice.Name);
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                }
            }
            else
            {
                try
                {
                    if (photonMicEnum == null)
                    {
                        photonMicEnum = Platform.CreateAudioInEnumerator(this.Logger);
                    }
                    int i = 0;
                    foreach (var device in photonMicEnum)
                    {
                        if (i == value)
                        {
                            recorder.MicrophoneDevice = device;
                            PlayerPrefs.SetInt("PhotonMic", recorder.MicrophoneDevice.IDInt);
                            break;
                        }
                        i++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                }
            }

            recorder.RestartRecording();

            CreateMicrophoneButtons();
#endif
        }
    }
}