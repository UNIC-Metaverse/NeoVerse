using Fusion.Addons.AudioRoomAddon;
using UnityEngine;

/***
 * 
 * TriggerDoorAnimation handles the door animation according to the event received from the AudioDoor
 * 
 ***/
public class TriggerDoorAnimation : MonoBehaviour
{
    [SerializeField] private AudioDoor audioDoor;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private bool isDoorOpened = false;
    private bool AudioDoorOpen => audioDoor && audioDoor.Object && audioDoor.IsOpened;

    private bool previousDoorStatus;
    private void Awake()
    {
        if (audioDoor == null)
            Debug.LogError("AudioDoor is not defined");

        audioDoor.OnStatusChange.AddListener(OnStatusChange);
    }

    private void OnStatusChange()
    {
        if (AudioDoorOpen != isDoorOpened)
            ToggleDoorAnimation();

    }

    private void Start()
    {
        if (!doorAnimator)
            doorAnimator = GetComponent<Animator>();

        previousDoorStatus = isDoorOpened;
    }


    public void ToggleDoorAnimation()
    {
        isDoorOpened = !isDoorOpened;

        if (audioSource)
            audioSource.Play();

        if (isDoorOpened)
        {
            doorAnimator.SetTrigger("TriggerOpen");
            previousDoorStatus = isDoorOpened;
        }

        else
        {
            doorAnimator.SetTrigger("TriggerClose");
            previousDoorStatus = isDoorOpened;
        }
    }
}
