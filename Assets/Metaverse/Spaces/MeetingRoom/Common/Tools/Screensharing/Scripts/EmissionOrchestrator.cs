using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

/***
 * 
 * EmissionOrchestrator manages the screen sharing orchestration
 * There is no authorization management: the last user to start a broadcast takes control of the screen. 
 * When a client starts a screen share, the PlayerId is saved either directly by the client itself if it has the State Authority over the screen,
 * or using a RPC if the client doesn't have the State Authority.
 *  
 *  
 ***/
public class EmissionOrchestrator : NetworkBehaviour, ScreenSharingEmitter.IEmitterListener
{
    [Networked]
    public int EmittingPlayer { get; set; }

    [Networked]
    public NetworkString<_64> EmittingPlayerName { get; set; }

    public string localEmitterName;
    public ScreenSharingEmitter emitter;
    bool emissionNotified = false;
    bool isEmitting = false;
    void Awake()
    {
        if (emitter == null) emitter = FindObjectOfType<ScreenSharingEmitter>(true);
        if (emitter)
        {
            emitter.listener = this;
        }
    }


    private void Update()
    {
        if (isEmitting && emissionNotified == false && EmittingPlayer == Runner.LocalPlayer.PlayerId)
        {
            emissionNotified = true;
        }
        else if (isEmitting && emissionNotified && EmittingPlayer != Runner.LocalPlayer.PlayerId)
        {
            Debug.LogWarning($"Another player {EmittingPlayer} (local:{Runner.LocalPlayer.PlayerId}) starts emitting: stopping local emission");
            StopEmitting();
        }
    }

    #region ScreenSharingEmitter.IEmitterListener
    public void OnStartEmitting(ScreenSharingEmitter emitter)
    {
        Debug.Log("OnStartEmitting");
        SetEmitter(Runner.LocalPlayer.PlayerId, localEmitterName);
        isEmitting = true;
        emissionNotified = false;
    }

    public void OnStopEmitting(ScreenSharingEmitter emitter)
    {
        isEmitting = false;
        if (EmittingPlayer == Runner.LocalPlayer.PlayerId)
        {
            SetEmitter(default, "");
        }
    }
    #endregion

     // When a client starts a screen share, the PlayerId is saved
    public void SetEmitter(int emitter, string name)
    {
        // either directly by the client itself if it has the State Authority over the screen,
        if (Object.HasStateAuthority)
        {
            EmittingPlayer = emitter;
            EmittingPlayerName = name;
        }
        // or using a RPC if the client doesn't have the State Authority.
        else
        {
            RPC_SetEmitter(emitter, name);
        }
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
    public void RPC_SetEmitter(int emitter, string name)
    {
        EmittingPlayer = emitter;
        EmittingPlayerName = name;
    }

    [EditorButton("Start emitting")]
    public void StartEmitting()
    {
        emitter.ConnectScreenSharing();
    }

    [EditorButton("Stop emitting")]
    public void StopEmitting()
    {
        emitter.DisconnectScreenSharing();
    }
}
