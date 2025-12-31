using Fusion.Addons.ExtendedRigSelectionAddon;
using Fusion.Samples.IndustriesComponents;
using UnityEngine;
using UnityEngine.SceneManagement;

/***
 * 
 * ScreenShareLoadMainSceneInRecorderMode is in charge to load directly the main scene if the applicaiton is in Recorder compilation mode.
 * It is required because the scene list must be identical for the normal client and for the recorder
 * 
 ***/

[DefaultExecutionOrder (1000)]
public class ScreenShareLoadMainSceneInRecorderMode : MonoBehaviour
{
    public bool IsRecorderCompilationMode = false;
    public AvatarCustomizer avatarCustomizer;

    void Awake()
    {
        if (IsRecorderCompilationMode && avatarCustomizer)
        {
            Debug.Log("Loading " + avatarCustomizer.mainSceneName);
            ExtendedRigSelection.SavePreference("Recorder");
            SceneManager.LoadScene(avatarCustomizer.mainSceneName);
        }
    }
}
