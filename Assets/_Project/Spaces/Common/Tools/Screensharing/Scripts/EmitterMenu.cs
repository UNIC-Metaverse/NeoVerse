#if U_WINDOW_CAPTURE_RECORDER_ENABLE
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
#define UWC_EMITTER_ENABLED
#endif
#endif
using Fusion.Addons.ScreenSharing;
using UnityEngine;
using UnityEngine.UI;
using uWindowCapture;

/***
 * 
 * EmitterMenu manages the UI for the screen recorder :
 *  - it configures UI buttons (one button for each screen, two screens max)
 *  - it displays desktop preview 
 *  - it updates the UI if the user clicks on a button
 *  - it controls the ScreenSharingEmitter
 *  - it updates screensharing status
 *  
 ***/


public class EmitterMenu : MonoBehaviour
{
    [SerializeField] Button selectDesktop0Button;
    [SerializeField] Button selectDesktop1Button;
    [SerializeField] TMPro.TextMeshProUGUI selectDesktop0Label;
    [SerializeField] TMPro.TextMeshProUGUI selectDesktop1Label;
    [SerializeField] ScreenSharingEmitter emitter;
    [SerializeField] EmissionOrchestrator emissionOrchestrator;
    [SerializeField] TMPro.TextMeshProUGUI statusLabel;
    [SerializeField] TMPro.TMP_InputField nameField;

    bool isScreenSharingUIDisplayed = false;

    private void Awake()
    {
        if (emitter == null) emitter = FindObjectOfType<ScreenSharingEmitter>(true);
        if (emissionOrchestrator == null) emissionOrchestrator = FindObjectOfType<EmissionOrchestrator>(true);
        ConfigureButtons();
        DisableScreenSharingUI();
    }

    void ConfigureButtons()
    {
        if(selectDesktop0Button == null || selectDesktop1Button == null)
        {
            foreach(var uwcImage in GetComponentsInChildren<UwcImage>(true))
            {
                if (uwcImage.desktopIndex == 0 && selectDesktop0Button == null)
                {
                    selectDesktop0Button = uwcImage.GetComponentInParent<Button>();
                }
                if (uwcImage.desktopIndex == 1 && selectDesktop1Button == null)
                {
                    selectDesktop1Button = uwcImage.GetComponentInParent<Button>();
                }
            }
        }
        if(selectDesktop0Button) selectDesktop0Button.onClick.AddListener(() => { ToggleDesktopConnection(0); });
        if(selectDesktop1Button) selectDesktop1Button.onClick.AddListener(() => { ToggleDesktopConnection(1); });
    }

    const string STOP_SCREENSHARING_TEXT = "Stop screen Sharing";
    const string START_SCREENSHARING_TEXT = "Share this screen";
    void ToggleDesktopConnection(int index)
    {
        if (!emitter) return;
        
        if(emitter.status != ScreenSharingEmitter.Status.NotEmitting)
        {
            DisableScreenSharingUI();
        }
        else
        {
            EnableScreenSharingUI(index);
            emitter.SelectDesktop(index);
            emitter.ConnectScreenSharing();
        }
    }

    void EnableScreenSharingUI(int index)
    {
        isScreenSharingUIDisplayed = true;
        if (index == 0)
        {
            ChangeScreenUIVisibility(0, true);
            ChangeScreenUIVisibility(1, false);
            selectDesktop0Label.text = STOP_SCREENSHARING_TEXT;
        }
        else if (index == 1)
        {
            ChangeScreenUIVisibility(1, true);
            ChangeScreenUIVisibility(0, false);
            selectDesktop1Label.text = STOP_SCREENSHARING_TEXT;
        }
    }

    void DisableScreenSharingUI()
    {
        isScreenSharingUIDisplayed = false;
        bool hasSecondScreen = UwcManager.desktopCount > 1;
        ChangeScreenUIVisibility(0, true);
        ChangeScreenUIVisibility(1, hasSecondScreen);
        selectDesktop0Label.text = START_SCREENSHARING_TEXT;
        selectDesktop1Label.text = START_SCREENSHARING_TEXT;
        if (emitter.status != ScreenSharingEmitter.Status.NotEmitting)
        {
            emitter.DisconnectScreenSharing();
        }            
    }

    private void Update()
    {
        CheckScreenCount();
        UpdateStatus();
        if (isScreenSharingUIDisplayed && emitter.status == ScreenSharingEmitter.Status.NotEmitting)
            DisableScreenSharingUI();
    }

    void UpdateStatus()
    {
#if UWC_EMITTER_ENABLED
        if (emissionOrchestrator.Object == null)
        {
            statusLabel.text = "Waiting to join the room ...";
        }
        else if (emitter.status == ScreenSharingEmitter.Status.Emitting)
        {
            statusLabel.text = "Everybody can see your screen." + emitter.DesktopIndex;
        }
        else if (emitter.status == ScreenSharingEmitter.Status.NotEmitting)
        {
            statusLabel.text = "Connected. Click on a preview to start screen sharing.";
        }
        else if (emitter.status == ScreenSharingEmitter.Status.WaitingVoiceConnection)
        {
            statusLabel.text = "Waiting for Photon Video connection...";
        }
        else if (emitter.status == ScreenSharingEmitter.Status.WaitingScreenCaptureAvailability)
        {
            statusLabel.text = "Waiting for screen capture availability...";
        }
#endif
    }

    void CheckScreenCount()
    {

#if UWC_EMITTER_ENABLED
        bool hasSecondScreen = UwcManager.desktopCount > 1;
        bool secondScreenVisible = hasSecondScreen;
        if (hasSecondScreen && emitter.status != ScreenSharingEmitter.Status.NotEmitting)
        {
            secondScreenVisible = emitter.DesktopIndex == 1;
        }
        ChangeScreenUIVisibility(1, secondScreenVisible);
        if (hasSecondScreen == false && emitter.status != ScreenSharingEmitter.Status.NotEmitting && emitter.DesktopIndex == 1)
        {
            // Second screen has been lost will we were sharing it
            emitter.DisconnectScreenSharing();
        }
#endif
    }

    void ChangeScreenUIVisibility(int index,  bool visible)
    {
        if (index == 0)
        {
            selectDesktop0Button.gameObject.SetActive(visible);
            if(selectDesktop0Label) selectDesktop0Label.gameObject.SetActive(visible);
        }
        if (index == 1)
        {
            selectDesktop1Button.gameObject.SetActive(visible);
            if(selectDesktop1Label) selectDesktop1Label.gameObject.SetActive(visible);
        }
    }

    public void UpdateEmitterName()
    {
        emissionOrchestrator.localEmitterName = nameField.text;
    }
}
