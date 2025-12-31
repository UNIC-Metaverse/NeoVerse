using Fusion;
using Fusion.Addons.HapticAndAudioFeedback;
using Fusion.XR.Shared.Grabbing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(PolaroidCamera.EXECUTION_ORDER)]
public class PolaroidCamera : PhotoCamera
{
    const int EXECUTION_ORDER = NetworkGrabbable.EXECUTION_ORDER + 10;
    CameraPicture lastCreatedPicture;
    NetworkTransform lastCreatedPictureNT;
    Grabbable lastCreatedPictureGrabbable;
    float startPicturePreparationTime;
    [SerializeField] float picturePreparationDuration = 2f;
    [SerializeField] Transform pictureStartPosition;
    [SerializeField] Transform pictureEndPosition;
    [SerializeField] float defaultHapticAmplitude = 0.2f;
    [SerializeField] float defaultHapticDuration = 0.05f;
    [SerializeField] AudioSource audioSource;
    private SoundManager soundManager;
    [SerializeField] string audioType = "CameraTakingPicture";

    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (soundManager == null) soundManager = SoundManager.FindInstance();
    }

    public override CameraPicture CreatePicture()
    {
        lastCreatedPicture = base.CreatePicture();
        lastCreatedPictureNT = lastCreatedPicture.GetComponent<NetworkTransform>();
        lastCreatedPictureGrabbable = lastCreatedPicture.GetComponent<Grabbable>();
        lastCreatedPictureGrabbable.enabled = false;
        startPicturePreparationTime = Time.time;
        SendHapticFeedback();
        if (!audioSource.isPlaying)
            soundManager.Play(audioType, audioSource);

        return lastCreatedPicture;
    }
    public override void Render()
    {
        if (lastCreatedPicture != null && lastCreatedPicture.Object.HasStateAuthority)
        {
            if (startPicturePreparationTime + picturePreparationDuration > Time.time)
            {
                MovePicture();
            }
            else
            {
                Debug.Log("Picture is Ready!");
                lastCreatedPictureGrabbable.enabled = true;
                lastCreatedPicture = null;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (lastCreatedPicture != null && lastCreatedPicture.Object.HasStateAuthority)
        {
            if (startPicturePreparationTime + picturePreparationDuration > Time.time)
            {
                MovePicture();
            }
        }
    }

    void MovePicture()
    {
        float elapsedTime = Time.time - startPicturePreparationTime;
        float t = elapsedTime / picturePreparationDuration;
        lastCreatedPictureNT.transform.position = Vector3.Lerp(pictureStartPosition.position, pictureEndPosition.position, t);
        lastCreatedPictureNT.transform.rotation = Quaternion.Slerp(pictureStartPosition.rotation, pictureEndPosition.rotation, t);
    }

    void SendHapticFeedback()
    {
        if (grabbable == null) return;
        if (!IsGrabbedByLocalPLayer || grabbable.CurrentGrabber.hand.LocalHardwareHand == null) return;

        grabbable.CurrentGrabber.hand.LocalHardwareHand.SendHapticImpulse(amplitude: defaultHapticAmplitude, duration: defaultHapticDuration);
    }
}
