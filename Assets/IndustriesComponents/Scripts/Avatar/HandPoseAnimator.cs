using UnityEngine;
using UnityEngine.InputSystem;

/***
 * 
 * HandPoseAnimator updates the animation variables according to the controller's buttons values
 * 
 ***/
public class HandPoseAnimator : MonoBehaviour
{
    [HideInInspector]
    public Animator handAnimator;                   // handanimator is set by RPM avatar loader
    public string whichHand = "";
    public InputActionProperty thumbAction;
    public InputActionProperty gripAction;
    public InputActionProperty triggerAction;

    private float gripPressure;
    private float triggerPressure;
    private bool thumbDetection;

    [SerializeField]
    private float gripPressureThresholdMin = 0.01f;
    [SerializeField]
    private float triggerPressureThresholdMin = 0.01f;
    [SerializeField]
    private float triggerPressureThresholdMax = 0.85f;

    void OnEnable()
    {
        thumbAction.action.Enable();
        gripAction.action.Enable();
        triggerAction.action.Enable();
    }


    // Update is called once per frame
    void Update()
    {
        if (handAnimator)
        {
            gripPressure = gripAction.action.ReadValue<float>();
            triggerPressure = triggerAction.action.ReadValue<float>();
            thumbDetection = thumbAction.action.IsPressed();

            handAnimator.SetFloat("Grip_Hand_" + whichHand, gripPressure);
            handAnimator.SetFloat("Trigger_Hand_" + whichHand, triggerPressure);

            if ((gripPressure < gripPressureThresholdMin) && thumbDetection)
            {
                if (triggerPressure < triggerPressureThresholdMin)
                    handAnimator.SetInteger("PoseID_Hand_" + whichHand, 1);
                else
                {
                    if (triggerPressure > triggerPressureThresholdMax)
                        handAnimator.SetInteger("PoseID_Hand_" + whichHand, 2);
                }
            }
            else
                handAnimator.SetInteger("PoseID_Hand_" + whichHand, 0);
  
        }
    }
}

