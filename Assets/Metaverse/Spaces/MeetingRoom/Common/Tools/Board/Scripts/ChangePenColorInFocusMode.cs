using Fusion.Addons.Drawing;
using Fusion.XR.Shared;
using Fusion.XR.Shared.Rig;
using UnityEngine;

/***
 * 
 * ChangePenColorInFocusMode is used to change the color of the drawing when the player activates the focus mode.
 * User interface buttons in focus mode call SetUserPenColor() with a color index which is passed to ColorSelection's ChangePenColor() (to synchronize the pen color change).
 * 
 ***/
public class ChangePenColorInFocusMode : MonoBehaviour
{
    [SerializeField] private RigInfo rigInfo;
    [SerializeField] private NetworkRig localNetworkRig;
    [SerializeField] private ColorSelection colorSelection;

    private void Awake()
    {
        SetColorSelection();
    }

    private async void SetColorSelection()
    {
        while (!rigInfo)
        {
            rigInfo = FindObjectOfType<RigInfo>();
            await AsyncTask.Delay(1000);
        }
        localNetworkRig = rigInfo.localNetworkedRig;
        colorSelection = localNetworkRig.GetComponentInChildren<ColorSelection>();
        if (colorSelection == null)
            Debug.LogError("ColorSelection not found on local network rig");
    }


    public void SetUserPenColor(int colorIndex )
    {
        colorSelection.ChangePenColor(colorIndex);
    }
}
