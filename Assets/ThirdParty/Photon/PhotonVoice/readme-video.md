# Photon Voice 2 Video Unity SDK

## Names
For now, we use Voice term in a meaning of Stream. PhotonVoice2 supports Audio Voices. This SDK adds Video Voices support.
We may change this naming to less confusing later.

## Video Processing
H264 codec is used for video streaming. Encoding and decoding are implemented with native platform API.

Video capture is implemented via native platform API on mobile platforms and UWP. On desktops, capture uses Unity WebCam API.

All platforms support video output to Unity textures (Platform.Create[PreviewManager,VideoRecorder,VideoPlayer]UnityTexture calls). This is the preferred output method. To use an object for video output, apply a material with "PhotonVoiceApi/VideoTexture3D" shader ("PhotonVoiceApi/VideoTextureExt3D" for Android) to it and assign PlatformView property of recorder or player as material's texture.

Mobile platforms and UWP also support video output via native platform API (Platform.CreateDefault[PreviewManager,VideoRecorder,VideoPlayer] methods).

## Supported platforms
- Windows
- MacOS
- iOS
- Android
- UWP (HL2)
- Unity WebGL (Chrome, Opera, Edge)

IL2CPP is the recommended scripting backend. Mono is not guaranteed to work.

There is no Unty integration for video streaming. Voice client should be used directly. It's possible to reuse existing audio Voice client in Unity or PUN app.

## Installation
- If updating existing project, remove PhotonVoiceLibs and PhotonVoiceApi folders in Assets\Photon\PhotonVoice\
- Import Photon Video SDK package
- Select *Window -> Photon Voice -> Enable Video* from the Editor menu (or manually add *PHOTON_VOICE_VIDEO_ENABLE* scripting define symbol for each platform).
- Open TestVideo/scene
- Set App Id in Settings component of Client object
- Make sure that Camera and Microphone permissions enabled
## iOS
- Add VideoToolbox framework to UnityFramework target link dependencies (Build Phases)
## Android
- Android 5.0 (API level 21) is required for video capture.
- If Unity Texture is used for video output, Multithreading render is not supported, Vulkan Graphics API is not supported
- Video encoder and decoder do not work on some Unuty versions beause of the bug: https://issuetracker.unity3d.com/issues/android-sbyte-type-is-considered-to-be-not-primitive-when-compiling-il2cpp-code
## UWP
- If using MediaPlayerElement for video output, export XAML project.
- If using IMGUI (OnGUI driven) user interface, limit fps by setting QualitySettings.vSyncCount = 1 or Application.targetFrameRate = 60, otherwise UI methoda calls make D3D video stall.
## Mac
- In case Unity can't load a bundle library and reports that it's damaged, try to:
  1. Open the MacOS terminal
  2. ```cd``` to ```Assets/Photon/PhotonVoice/PhotonVoiceLibs/OSX```
  3. Run ```xattr -d com.apple.quarantine *.bundle```

## Video Settings
- Bitrate. Currently Photon Cloud servers do not support rates higher than 400 kbit.
- Resolution. Please stick to the following video sizes which are tested with PhotonVoice Video:
   - 320x240
   - 640x480
   - 768x432
   - 848x480
   - 1024x576
   - 1280x720
   - 1920x1440
   
## Streaming Settings (VoiceCreateOptions and LocalVoice properties)
- Fragment. Set it to true to split large video frames into small packets sent independently by Photon transports. In unreliable mode, if some small packets are lost, the frame buffer is still passed to the decoder with 0s instead of lost data.
- Reliable. All packets are guaranteed to deliver. May be used in combination with Fragment.
- FEC. If > 0, after the specified numnber of frames, a forward error correction packet is sent. Do not use with Reliable. Mostly useful in Fragment mode because it reduces losses of large packet fragments.

## Multiple streams from the same camera
- Supported on Windows, Mac and WebGL.
- Create a 2nd video stream and specify the same camera name, but different bitrate.
- On Windows and Mac, also different resolutions are supported. High-res video stream must be created before low-res to avoid camera initialization with lower resolution.
