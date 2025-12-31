using UnityEngine;
using System.Collections;

namespace TestVideo
{
    public class Settings : MonoBehaviour
    {
#if PHOTON_VOICE_VIDEO_ENABLE
        // flips webcam bitmap vertically to match orientation of video from android captured with RGBA RenderScript Allocation
        public const bool FlipVertically = false;

        public string AppId; // set in editor
        public string AppVersion; // set in editor
        public string Region = "EU"; // set in editor
        public const string RoomName = "PhotonVideo";
        public const POpusCodec.Enums.SamplingRate AudioEncoderSamplingRate = POpusCodec.Enums.SamplingRate.Sampling24000;
        public const int AudioMicSamplingRate = 24000;
        public Photon.Voice.Codec VideoCodec = Photon.Voice.Codec.VideoH264;
        public int VideoWidth = 640;
        public int VideoHeight = 480;
        public int VideoBitrate = 400000;
        public int AudioBitrate = 30000;
        public int VideoFPS = 30;
        public int VideoKeyFrameInt = 30;
        [Space(7)]
        public bool VideoEnable = true;
        public bool VideoLowEnable = false;
        public bool ScreenShareEnable = false;
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN) && U_WINDOW_CAPTURE_RECORDER_ENABLE
        public bool WindowShareEnable = false;
#endif
#if UNITY_WEBGL && UNITY_2021_2_OR_NEWER // requires ES6
        [Space(7)]
        public bool WebGLScreenShareEnable;
        public Photon.Voice.VideoSourceSizeMode WebGLScreenShareSizeMode;
#endif
        [Space(7)]
        public bool AudioEnable = true;
        [Space(7)]
        public bool VideoEcho = false;
        public bool AudioEcho = false;
        [Space(7)]
        public bool VideoMute = false;
        public bool AudioMute = false;
        [Space(7)]
        public bool VideoReliable = true;
        public bool AudioReliable = false;
        [Space(7)]
        public bool VideoEncrypt = false;
        public bool AudioEncrypt = false;
        [Space(7)]
        public bool VideoFragment = true;
        public bool AudioFragment = false;
        [Space(7)]
        public int VideoFEC = 0;
        [Space(7)]
        public int AudioJitterBufferMs = 200;
        public int AudioJitterBufferMsHigh = 400;
        public int AudioJitterBufferMsMax = 1000;
        public int VideoDelay = 0;

        internal Photon.Voice.DeviceInfo CamDevice = Photon.Voice.DeviceInfo.Default;
        internal Photon.Voice.DeviceInfo MicDevice = Photon.Voice.DeviceInfo.Default;
#endif
    }
}