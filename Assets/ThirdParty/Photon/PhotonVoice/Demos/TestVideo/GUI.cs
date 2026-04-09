#define VIDEO_RECORDER_UNITY_MEM_LOOP

using UnityEngine;
using Photon.Voice;
using Photon.Voice.Unity;

namespace TestVideo
{
    public class GUI : MonoBehaviour
    {
#if PHOTON_VOICE_VIDEO_ENABLE
        internal IVideoPreview FullscreenVideo;

        GUIStyle lStyle;
        GUIStyle bStyle;
        GUIStyle tStyle;
        GUIStyle tStyleDisabled;

        void toggle(ref bool x, string label, System.Action<bool> act)
        {
            if (GUILayout.Button((x ? "[X]" : "[ ]") + label, bStyle))
            {
                x = !x;
                act(x);
            }
        }

        void OnGUI()
        {

            lStyle = new GUIStyle("Label");
            bStyle = new GUIStyle("Button");
            tStyle = new GUIStyle("TextField");

            lStyle.fontSize =
            bStyle.fontSize =
            tStyle.fontSize = Screen.width / 40;

            tStyleDisabled = new GUIStyle(tStyle);
            tStyleDisabled.normal.textColor = new Color(0.5f, 0.5f, 0.5f);

            bStyle.wordWrap = true;
            lStyle.stretchWidth =
            bStyle.stretchWidth =
            tStyle.stretchWidth = false;

            lStyle.alignment =
            bStyle.alignment =
            tStyle.alignment = TextAnchor.UpperLeft;

            lStyle.margin.left =
            bStyle.margin.left =
            tStyle.margin.left = 20;

            var lStyleNoWrap = new GUIStyle(lStyle);
            lStyleNoWrap.wordWrap = false;

            var lStyleSmall = new GUIStyle(lStyle);
            lStyleSmall.fontSize = Screen.width / 60;

            var myPreviewHeight = Screen.height / 8;
            var previewHeight = myPreviewHeight * 12 / 10;
            var previewOffset = 0;

            var client = GetComponent<TestVideo.Client>();
            var set = GetComponent<Settings>();

            bool uiSetup = !client.IsConnected;
            bool uiConnected = !uiSetup && uiConnectedOn;
            int fsWidth = uiConnected ? Screen.width / 2 : Screen.width;

            // start of preview ui
            // places preview buttons and updates PreviewManager with preview positions and sizes

            var fsVideo = FullscreenVideo;
            if (fsVideo == null) // draw any if nothing is selected yet
            {
                var en = client.VideoPlayers.GetEnumerator();
                if (set != null && en.MoveNext())
                {
                    fsVideo = en.Current;
                }
            }
            if (fsVideo != null)
            {
                var w = fsWidth;
                var h = Screen.height;// - previewHeight;
                if (w * fsVideo.Height > h * fsVideo.Width)
                {
                    w = h * fsVideo.Width / fsVideo.Height;
                }
                else
                {
                    h = w * fsVideo.Height / fsVideo.Width;
                }
                var r = new Rect(0, 0, w, h);
                client.PreviewManager.SetBounds(fsVideo, (int)r.x, (int)r.y, (int)r.width, (int)r.height, fsVideo.Flip);
            }

            // draw my cam preview
            if (set != null)
            {
                foreach (var v in client.VideoRecorders)
                {
                    if (v.Height > 0)
                    {
                        var w = myPreviewHeight * v.Width / v.Height;
                        var r = new Rect(0, Screen.height - myPreviewHeight, w, myPreviewHeight);
                        if (client.PreviewManager.Has(v))
                        {
                            client.PreviewManager.SetBounds(v, previewOffset + (int)r.x, (int)r.y, (int)r.width, (int)r.height, v.Flip);
                            previewOffset += w;
                        }
                    }
                }
            }

            foreach (var v in client.VideoPlayers)
            {
                var w = previewHeight * v.Width / v.Height;
                var r = new Rect(previewOffset, Screen.height - previewHeight, w, previewHeight);

                GUILayout.BeginArea(r);
                if (v != fsVideo)
                {
                    client.PreviewManager.SetBounds(v, (int)r.x, (int)r.y, (int)r.width, (int)r.height, v.Flip);
                }

                if (GUILayout.Button(fsVideo == v ? "[     ]" : "(preview)", GUILayout.Width(w), GUILayout.Height(previewHeight)))
                {
                    FullscreenVideo = v;
                }

                GUILayout.EndArea();

                previewOffset += w;
            }

            if (client.PreviewManager is PreviewManagerUnityGUI)
            {
                (client.PreviewManager as PreviewManagerUnityGUI).OnGUI();
            }
            // end of preview ui

            GUILayout.BeginArea(new Rect(Screen.width - previewHeight, Screen.height - previewHeight, previewHeight, previewHeight));
            if (!uiSetup)
            {
                if (GUILayout.Button("=", GUILayout.Width(previewHeight), GUILayout.Height(previewHeight)))
                {
                    uiConnectedOn = !uiConnectedOn;
                }
            }
            GUILayout.EndArea();

            if (uiConnected)
            {
                GUILayout.BeginArea(new Rect(fsWidth, 0, Screen.width - fsWidth, Screen.height));
            }

            GUILayout.BeginHorizontal();
            if (uiConnected)
            {
                GUILayout.Label(string.Format(SystemInfo.graphicsDeviceType + " fps: {0:F2}", (1 / Time.smoothDeltaTime)), lStyle);
            }
            if (uiConnected || uiSetup)
            {
                GUILayout.Label("transport: " + client.StateStr, lStyle);
                GUILayout.Label("rtt: " + client.RoundTripTime, lStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                var en = client.VideoRecorders.GetEnumerator();
                if (uiConnected)
                {
                    foreach (var v in client.VideoRecorders)
                    {
                        GUILayout.Label("rec: " + v.Width + "x" + v.Height + (v.Error == null ? "" :" " + v.Error), lStyle);
                    }
                    GUILayout.Label("fps: " + set.VideoFPS, lStyle);
                    GUILayout.Label("kfi: " + set.VideoKeyFrameInt, lStyle);
                }
            }
            if (uiSetup)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();

                GUILayout.Label("Mic: ", lStyleNoWrap);
                foreach (DeviceInfo d in client.MicDevices)
                {
                    if (GUILayout.Button((set.MicDevice == d ? "[X] " : "[ ] ") + d, bStyle))
                    {
                        set.MicDevice = d;
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();

                GUILayout.Label("Cam: ", lStyleNoWrap);
                if (client.CamDevices.Error != null)
                {
                    GUILayout.Label("Error: " + client.CamDevices.Error, lStyle);
                }
                else
                {
                    if (GUILayout.Button((set.CamDevice.IsDefault ? "[X] " : "[ ] ") + "Default", bStyle))
                    {
                        set.CamDevice = DeviceInfo.Default;
                    }
                    foreach (DeviceInfo d in client.CamDevices)
                    {
                        if (GUILayout.Button((set.CamDevice == d ? "[X] " : "[ ] ") + d.Name + (d.Features.CameraFacing == CameraFacing.Undef ? "" : " (" + d.Features.CameraFacing + ")"), bStyle))
                        {
                            set.CamDevice = d;
                        }
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();

                GUILayout.Label("Codec:", lStyle);
                foreach (var c in System.Enum.GetValues(typeof(Photon.Voice.Codec)))
                {
                    var cc = (Photon.Voice.Codec)c;
                    if (cc.ToString().StartsWith("Video") && GUILayout.Button((set.VideoCodec == cc ? "[X] " : "[ ] ") + cc, bStyle))
                    {
                        set.VideoCodec = cc;
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();

                GUILayout.Label("Width:", lStyle);
                var x = GUILayout.TextField(set.VideoWidth.ToString(), tStyle, GUILayout.MinWidth(tStyle.fontSize * 4));
                int.TryParse(x, out set.VideoWidth);

                GUILayout.Label("Height:", lStyle);
                x = GUILayout.TextField(set.VideoHeight.ToString(), tStyle, GUILayout.MinWidth(tStyle.fontSize * 4));
                int.TryParse(x, out set.VideoHeight);

                GUILayout.Label("fps:", lStyle);
                x = GUILayout.TextField(set.VideoFPS.ToString(), tStyle, GUILayout.MinWidth(tStyle.fontSize * 4));
                int.TryParse(x, out set.VideoFPS);

                GUILayout.Label("kfi:", lStyle);
                x = GUILayout.TextField(set.VideoKeyFrameInt.ToString(), tStyle, GUILayout.MinWidth(tStyle.fontSize * 4));
                int.TryParse(x, out set.VideoKeyFrameInt);

                GUILayout.EndHorizontal();

#if UNITY_WEBGL && !UNITY_EDITOR
                GUILayout.BeginHorizontal();
                if (GUILayout.Button((set.WebGLScreenShareEnable ? "[X]" : "[ ]") + "Screen Share", bStyle))
                {
                    set.WebGLScreenShareEnable = !set.WebGLScreenShareEnable;
                }
                GUILayout.Label("Size:", lStyle);
                if (GUILayout.Button((set.WebGLScreenShareSizeMode.Init == VideoSourceSizeMode.Mode.Source ? "[X]" : "[ ]") + " Source", bStyle))
                {
                    set.WebGLScreenShareSizeMode.Init = set.WebGLScreenShareSizeMode.Init == VideoSourceSizeMode.Mode.Source ? VideoSourceSizeMode.Mode.Fixed : VideoSourceSizeMode.Mode.Source;
                }

                if (GUILayout.Button((set.WebGLScreenShareSizeMode.Update ? "[X]" : "[ ]") + " Update", bStyle))
                {
                    set.WebGLScreenShareSizeMode.Update = !set.WebGLScreenShareSizeMode.Update;
                }
                GUILayout.EndHorizontal();
#endif

                GUILayout.BeginHorizontal();

                GUILayout.Label("Video Bitrate:", lStyle);
                x = GUILayout.TextField(set.VideoBitrate.ToString(), tStyle, GUILayout.MinWidth(tStyle.fontSize * 4));
                int.TryParse(x, out set.VideoBitrate);
                GUILayout.Label(" Audio Bitrate:", lStyle);
                x = GUILayout.TextField(set.AudioBitrate.ToString(), tStyle, GUILayout.MinWidth(tStyle.fontSize * 4));
                int.TryParse(x, out set.AudioBitrate);
                GUILayout.Label("Audio Buffer ms:", lStyle);
                x = GUILayout.TextField(set.AudioJitterBufferMs.ToString(), tStyle, GUILayout.MinWidth(tStyle.fontSize * 3));
                int.TryParse(x, out set.AudioJitterBufferMs);
                if (GUILayout.Button("Connect", bStyle))
                {
                    client.Connect();
                }
                GUILayout.EndHorizontal();
            }
            else if (uiConnected)
            {
                if (GUILayout.Button("Disconnect", bStyle))
                {
                    client.Disconnect();
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
#if VIDEO_RECORDER_UNITY_MEM_LOOP && (UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX)
                var en = client.VideoRecorders.GetEnumerator();
                if (en.MoveNext())
                {
                    var vru = en.Current as VideoRecorderUnity;
                    if (vru != null)
                    {
                        if (GUILayout.Button("Mem Loop " + (vru.MemLoop == 0 ? "[ ]" : "[X]"), bStyle))
                        {
                            vru.MemLoop = vru.MemLoop == 0 ? 130 : 0;
                        }
                    }
                }
#endif
                if (GUILayout.Button("Stats", bStyle))
                {
                    client.VoiceClient.LogStats();
                    client.VoiceClient.LogSpacingProfiles();
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.EndHorizontal();
            }

            if (uiConnected || uiSetup)
            {
                GUILayout.BeginVertical();
                foreach (var s in client.StatsStr)
                {
                    GUILayout.Label(s, lStyleSmall);
                }

                GUILayout.EndVertical();

                GUILayout.BeginHorizontal();

                toggle(ref set.AudioEnable, "A", x => client.ToggleLocalVoice(Client.StreamType.Mic, x));
                toggle(ref set.VideoEnable, "VH", x => client.ToggleLocalVoice(Client.StreamType.Camera, x));
                toggle(ref set.VideoLowEnable, "VL", x => client.ToggleLocalVoice(Client.StreamType.CameraLow, x));
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN) && U_WINDOW_CAPTURE_RECORDER_ENABLE
                toggle(ref set.WindowShareEnable, "WS", x => client.ToggleLocalVoice(Client.StreamType.WindowShare, x));
#endif
#if UNITY_WEBGL && UNITY_2021_2_OR_NEWER && !UNITY_EDITOR // requires ES6
                toggle(ref set.WebGLScreenShareEnable, "SSW", x => client.ToggleLocalVoice(Client.StreamType.WebGLScreenShare, x));
#endif
                toggle(ref set.ScreenShareEnable, "SS", x => client.ToggleLocalVoice(Client.StreamType.ScreenShare, x));

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Video Delay: ", lStyle);
                var videoDelayStr = GUILayout.TextField(set.VideoDelay.ToString(), tStyle, GUILayout.MinWidth(tStyle.fontSize * 3));
                int videoDelayNew = 0;
                int.TryParse(videoDelayStr, out videoDelayNew);
                if (set.VideoDelay != videoDelayNew)
                {
                    set.VideoDelay = videoDelayNew;
                    client.VoiceClient.SetRemoteVoiceDelayFrames(Codec.VideoH264, set.VideoDelay);
                    client.VoiceClient.SetRemoteVoiceDelayFrames(Codec.VideoVP8, set.VideoDelay);
                }

                GUILayout.Label("Audio Lag:", lStyle);
                foreach (var p in client.AudioPlayers)
                {
                    GUILayout.Label("lag=" + p.Lag, lStyle);
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button((set.VideoEcho ? "[X]" : "[ ]") + "V", bStyle))
                {
                    set.VideoEcho = !set.VideoEcho;
                }
                if (GUILayout.Button((set.AudioEcho ? "[X]" : "[ ]") + "A", bStyle))
                {
                    set.AudioEcho = !set.AudioEcho;
                }
                GUILayout.Label("Echo", lStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button((set.VideoReliable ? "[X]" : "[ ]") + "V", bStyle))
                {
                    set.VideoReliable = !set.VideoReliable;
                }
                if (GUILayout.Button((set.AudioReliable ? "[X]" : "[ ]") + "A", bStyle))
                {
                    set.AudioReliable = !set.AudioReliable;
                }
                GUILayout.Label("Reliable", lStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button((set.VideoFragment ? "[X]" : "[ ]") + "V", bStyle))
                {
                    set.VideoFragment = !set.VideoFragment;
                }
                if (GUILayout.Button((set.AudioFragment ? "[X]" : "[ ]") + "A", bStyle))
                {
                    set.AudioFragment = !set.AudioFragment;
                }
                GUILayout.Label("Frag.", lStyle);
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button((set.VideoMute ? "[X]" : "[ ]") + "V", bStyle))
                {
                    set.VideoMute = !set.VideoMute;
                }
                if (GUILayout.Button((set.AudioMute ? "[X]" : "[ ]") + "A", bStyle))
                {
                    set.AudioMute = !set.AudioMute;
                }
                GUILayout.Label("Mute", lStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button((set.VideoEncrypt ? "[X]" : "[ ]") + "V", bStyle))
                {
                    set.VideoEncrypt = !set.VideoEncrypt;
                }
                if (GUILayout.Button((set.AudioEncrypt ? "[X]" : "[ ]") + "A", bStyle))
                {
                    set.AudioEncrypt = !set.AudioEncrypt;
                }
                GUILayout.Label("Encr.", lStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("FEC:", lStyle);
                var x = GUILayout.TextField(set.VideoFEC.ToString(), tStyle, GUILayout.MinWidth(tStyle.fontSize));
                int.TryParse(x, out set.VideoFEC);
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }

            if (uiConnected)
            {
                GUILayout.EndArea();
            }
        }

        private bool uiConnectedOn = true;
#endif
    }
}
