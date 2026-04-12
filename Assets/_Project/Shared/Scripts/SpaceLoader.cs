using Fusion;
using Fusion.Addons.HapticAndAudioFeedback;
using Fusion.Addons.Spaces;
using Fusion.Samples.IndustriesComponents;
using Fusion.XR.Shared;
using Fusion.XR.Shared.Locomotion;
using Fusion.XR.Shared.Rig;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


/**
 * 
 * SpaceLoader is in charge to load a new scene when the player collides with the box collider
 * 
 **/
[DefaultExecutionOrder(-1)]
public class SpaceLoader : MonoBehaviour
{
    [Header("Target space")]
    [SerializeField] private string spaceId;
    [SerializeField] private SpaceDescription spaceDescription;

    public SpaceDescription SpaceDescription
    {
        get
        {
            LoadSpaceDescriptionInfo();
            return spaceDescription;
        }
    }

    public string SpaceId
    {
        get
        {
            LoadSpaceDescriptionInfo();
            return spaceId;
        }
    }

    string SceneName => (spaceDescription != null) ? spaceDescription.sceneName : spaceId;

    [Header("Automatically set")]
    public Managers managers;
    [SerializeField] private NetworkRunner runner;
    [SerializeField] private RigInfo rigInfo;
    [SerializeField] private SoundManager soundManager;

    // Position to spawn at when we come back from this scene
    [SerializeField] private Transform returnPosition;
    [SerializeField] private float returnRadius = 1f;

    public float exitTimeoutDuration = 2;
    public float exitingProgress = 0;
    private bool exiting = false;
    private Collider exitingTriggerCollider;

    private bool newSceneIsLoading = false;
    private bool waitForFadeIn = false;

    private List<NetworkRig> networkRigsOnPortal = new List<NetworkRig>();

    void LoadSpaceDescriptionInfo()
    {
        if (spaceDescription && string.IsNullOrEmpty(spaceId)) spaceId = spaceDescription.spaceId;
        if (spaceDescription == null && !string.IsNullOrEmpty(spaceId)) spaceDescription = SpaceDescription.FindSpaceDescription(spaceId);
    }
    private void Awake()
    {
        LoadSpaceDescriptionInfo();

        if (returnPosition == null) returnPosition = transform;
        SceneSpawnManager spawnManager = FindObjectOfType<SceneSpawnManager>(true);
        if (spawnManager) spawnManager.RegisterSpawnPosition(spaceId, returnPosition, returnRadius);
    }

    private void Start()
    {
        if (managers == null) managers = Managers.FindInstance();
        if (soundManager == null) soundManager = SoundManager.FindInstance();

        if (runner == null)
            runner = managers.runner;
        if (runner == null)
            Debug.LogError("Runner not found !");
        else
        {
            rigInfo = RigInfo.FindRigInfo(runner);
        }
    }

    [ContextMenu("SwitchScene")]
    private async void SwitchScene()
    {
        // The app manager might detect a network disconnection: we prevent him from thinking there is an error
        if (managers.applicationManager) managers.applicationManager.isQuitting = true;
        // Audio feedback to inform player that the avatar is loaded
        soundManager.PlayOneShot("OnSceneSwitch");

        waitForFadeIn = true;
        StartCoroutine(DisplayFaderScreen());
        while (waitForFadeIn) await AsyncTask.Delay(10);
        await runner.Shutdown(true);
        Debug.Log("Loading new scene " + SceneName);
        SpaceRoom.RegisterSpaceRequest(spaceDescription);
        SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
    }

    private IEnumerator DisplayFaderScreen()
    {
        waitForFadeIn = true;
        if (rigInfo.localHardwareRig && rigInfo.localHardwareRig.headset.fader)
        {
            yield return Fading(rigInfo.localHardwareRig.headset.fader);
            // We make sure we see the black screen for one frame
            yield return null;
        }
        else
        {
            Debug.LogError("Problem in fader configuration");
        }
        waitForFadeIn = false;
    }

    private IEnumerator Fading(Fader fader)
    {
        yield return fader.FadeIn(1);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<HardwareHand>())
        {
            exitingTriggerCollider = other;
            StartExiting();
        }
        else
        {
            // check if a remote player is on the portal to start vfx
            var networkHand = other.GetComponentInParent<NetworkHand>();
            if (networkHand && networkHand.IsLocalNetworkRig == false)
            {
                exitingProgress = 0.5f; // fake progress

                // register the remote player rig
                var networkRig = other.GetComponentInParent<NetworkRig>();
                if (networkRig)
                {
                    if (networkRigsOnPortal.Contains(networkRig) == false)
                        networkRigsOnPortal.Add(networkRig);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == exitingTriggerCollider)
        {
            CancelExiting();
        }

        var networkHand = other.GetComponentInParent<NetworkHand>();
        if (networkHand && networkHand.IsLocalNetworkRig == false)
        {
            // A remote player exit the portal
            var networkRig = other.GetComponentInParent<NetworkRig>();
            if (networkRig)
            {
                // remove it from the list
                if (networkRigsOnPortal.Contains(networkRig))
                {
                    networkRigsOnPortal.Remove(networkRig);
                }

                // stop VFX if the list is empty
                if (networkRigsOnPortal.Count == 0)
                {
                    exitingProgress = 0f;
                }
            }
            else
                Debug.LogError($"NetworkRig not found on remote player on the portal !!");
        }
    }

    // OnPortalUserDetroy is called by PortalUser OnDestroy()
    // It is required because OntriggerExit is not called when the remote player is destroyed
    internal void OnPortalUserDetroy(NetworkRig networkRig)
    {
        if (networkRigsOnPortal.Contains(networkRig))
        {
            networkRigsOnPortal.Remove(networkRig);
            if (networkRigsOnPortal.Count == 0)
            {
                // List empty : stop the VFX
                exitingProgress = 0f;
            }
        }
    }

    void StartExiting()
    {
        if (exiting || newSceneIsLoading) return;
        StartCoroutine(ExitingCoroutine());
    }
    IEnumerator ExitingCoroutine()
    {
        exiting = true;
        float exitStartTime = Time.time;
        exitingProgress = (exitTimeoutDuration == 0) ? 1 : 0;
        while (exiting && exitingProgress < 1)
        {
            exitingProgress = (Time.time - exitStartTime) / exitTimeoutDuration;
            yield return null;
        }
        if (exiting)
        {
            if (!newSceneIsLoading)
            {
                newSceneIsLoading = true;
                Debug.Log($"Switching to scene {SceneName}");
                SwitchScene();
            }
        }
    }

    void CancelExiting()
    {
        exiting = false;
        exitingProgress = 0;
        exitingTriggerCollider = null;
    }
}
