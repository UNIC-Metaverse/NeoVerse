using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Text;
using Photon.Voice.Unity;
using Fusion.XR;
using Photon.Voice.Fusion;

namespace Fusion.Samples.IndustriesComponents
{

    /***
     * 
     * DebugPanel is in charge to display the application logs into the menu.
     * To do so, it adds a listener on the LogCollector to update the text field only when required. .
     * Also, it display the connection status of Photon Fusion & Photon Voice.
     * 
     ***/
    public class DebugPanel : MonoBehaviour
    {
        public static string DisconnectCauseFusion = "";
        public static string DisconnectCauseVoice = "";

        public TextMeshProUGUI FusionConnectionText;
        public TextMeshProUGUI VoiceConnectionText;
        public TextMeshProUGUI LogText;

        public LogCollector logCollector;

        FusionVoiceClient fusionVoiceClient;
        Recorder recorder;

        bool updateRequired = false;

        public Managers manager;

        private void OnEnable()
        {
            if (manager == null) manager = Managers.FindInstance();
            if (logCollector == null) logCollector = GetComponentInParent<LogCollector>();
            logCollector.onNewLogs.AddListener(HandleLogs);
            UpdateLogs();
        }

        private void OnDisable()
        {
            logCollector.onNewLogs.RemoveListener(HandleLogs);
        }

        private void Update()
        {
            if (updateRequired)
            {
                updateRequired = false;
                UpdateLogs();
            }
            UpdateFusion();
            UpdateVoice();
        }

        void HandleLogs()
        {
            updateRequired = true;
        }

        void UpdateFusion()
        {
            FusionConnectionText.text = $"{manager.runner.State}";
        }

        void UpdateVoice()
        {
            if (fusionVoiceClient == null) fusionVoiceClient = manager.fusionVoiceClient;
            if (recorder == null) recorder = fusionVoiceClient.PrimaryRecorder;
        }

        void UpdateLogs()
        {
            var builder = new StringBuilder();
            foreach (var log in logCollector.queue)
            {
                string text = log.condition.Substring(0, Math.Min(log.condition.Length, 120));
                if (log.type == UnityEngine.LogType.Error)
                {
                    text = "<color=#FF3F79>" + text + "</color>";
                }
                if (log.type == UnityEngine.LogType.Warning)
                {
                    text = "<color=\"orange\">" + text + "</color>";
                }

                builder.Append(text).Append("\n");
            }
            LogText.text = builder.ToString();
        }
    }


}
