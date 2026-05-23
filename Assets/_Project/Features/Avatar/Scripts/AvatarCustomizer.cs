using Fusion.Addons.Avatar;
using Fusion.Addons.Avatar.SimpleAvatar;
using Fusion.XR.Shared.Locomotion;
using Fusion.XR.Shared.Rig;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/**
 *
 * AvatarCustomizer is in charge to display the avatar selection UI.
 * It creates the UI dynamically based on the "Simple Avatar" model.
 * Avatar customization are saved/restored using player preference settings.
 * At start, it displays the Simple Avatar or the Avaturn panel according to
 * the avatar model URL found in player preference settings.
 *
 **/

namespace Fusion.Samples.IndustriesComponents
{
    public class AvatarCustomizer : MonoBehaviour
    {
        public HorizontalLayoutGroup hairPalette;
        public HorizontalLayoutGroup clothPalette;
        public HorizontalLayoutGroup skinPalette;
        public SimpleAvatar referenceAvatar;
        public Button prefabChoiceButton;

        public string mainSceneName;

        public SimpleAvatarConfig simpleAvatarConfig;
        public AvatarRepresentation avatarRepresentation;

        public HardwareRig rig;
        public Camera desktopCamera;
        
        Fader fader;
        public float delayBeforeFaderFadeOut = 1;
        public float faderFadeOutDuration = 2;

        public GameObject desktopModeConnectButton;
        public GameObject vrModeConnectButton;

        public TabButtonUI tabButtonSimpleAvatar;

        [Header("Avaturn")]
        [Tooltip("Tab button that activates the Avaturn avatar panel")]
        public TabButtonUI tabButtonAvaturn;
        [Tooltip("The panel component that manages the Avaturn customiser UI")]
        public AvaturnAvatarPanel avaturnPanel;

        public string latestSimpleAvatarURL;

        [Header("VR settings")]
        public bool defaultVRMode = false;

        private void Awake()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            defaultVRMode = true;
#endif
#if UNITY_STANDALONE_OSX
            defaultVRMode = false;
            if (vrModeConnectButton) vrModeConnectButton.SetActive(false);
#endif
#if UNITY_WEBGL
            defaultVRMode = false;
            if (vrModeConnectButton) vrModeConnectButton.SetActive(false);
#endif

            fader = rig.GetComponentInChildren<Fader>();
            fader.SetFade(1);


            // Configure the UI according to the display mode : VR or Desktop
            if (defaultVRMode)
            {
                ActivateVRMode();
            }
            else
            {
                ActivateDesktopMode();                
            }

            PlayerPrefs.SetString("RigMode", "");



            if (tabButtonSimpleAvatar)
                tabButtonSimpleAvatar.onTabSelected.AddListener(SimpleAvatarTabSelected);

            if (tabButtonAvaturn)
                tabButtonAvaturn.onTabSelected.AddListener(AvaturnTabSelected);
        }

        void ActivateVRMode()
        {
            // In VR, hide the "Join Desktop Mode" button, disable the desktop camera and enable the local hardware rig
            desktopModeConnectButton.SetActive(false);
            desktopCamera.gameObject.SetActive(false);
            rig.gameObject.SetActive(true);
            RigInfo rigInfo = RigInfo.FindRigInfo(allowSceneSearch: true);
            if (!rigInfo || !rig) return;
            rigInfo.localHardwareRigKind = RigInfo.RigKind.VR;
            rigInfo.localHardwareRig = rig;
        }

        void ActivateDesktopMode()
        {
            // In Desktop mode, enable the desktop camera and disable the local hardware rig
            desktopCamera.gameObject.SetActive(true);
            rig.gameObject.SetActive(false);
            RigInfo rigInfo = RigInfo.FindRigInfo(allowSceneSearch: true);
            if (!rigInfo) return;
            rigInfo.localHardwareRigKind = RigInfo.RigKind.Desktop;
        }

        private void SimpleAvatarTabSelected()
        {
            if (!string.IsNullOrEmpty(latestSimpleAvatarURL))
            {
                avatarRepresentation.ChangeAvatar(latestSimpleAvatarURL);
            }
            else
            {
                simpleAvatarConfig = DefaultSimpleAvatar();
                avatarRepresentation.ChangeAvatar(simpleAvatarConfig.URL);
            }
        }

        private void AvaturnTabSelected()
        {
            if (avaturnPanel) avaturnPanel.OnTabSelected();
        }

        private void Start()
        {
            // try to restore previous avatar saved in player preferences
            RestoreAvatarFromUserPref();

            BuildSimpleAvatarButtons();

            if (defaultVRMode)
            {
  
                if (fader)
                {
                    StartCoroutine(fader.Blink(0, delayBeforeFaderFadeOut, faderFadeOutDuration));
                }
            }
        }

        private void BuildSimpleAvatarButtons()
        { 
            // create the hair UI dynamically based on the "Simple Avatar" model
            if (hairPalette)
            {
                int i = 0;

                // Create a button for each hair model found
                foreach (var hairMat in referenceAvatar.hairMaterials)
                {
                    // create the button
                    var b = GameObject.Instantiate(prefabChoiceButton);

                    // Customize the button color
                    b.colors = new ColorBlock
                    {
                        normalColor = hairMat.color,
                        highlightedColor = hairMat.color,
                        pressedColor = hairMat.color,
                        selectedColor = hairMat.color,
                        colorMultiplier = 1
                    };
                    var index = i;
                    b.transform.SetParent(hairPalette.gameObject.transform, false);

                    // Set the hair model when the button is selected
                    b.onClick.AddListener(() =>
                    {
                        SetHairMaterial(index);
                    });
                    i++;
                }
            }

            // create the cloth UI dynamically based on the "Simple Avatar" model
            if (clothPalette)
            {
                int i = 0;
                // For each cloth
                foreach (var mat in referenceAvatar.clothMaterials)
                {
                    // create the button
                    var b = GameObject.Instantiate(prefabChoiceButton);

                    // Customize the button color
                    b.colors = new ColorBlock
                    {
                        normalColor = mat.color,
                        highlightedColor = mat.color,
                        pressedColor = mat.color,
                        selectedColor = mat.color,
                        colorMultiplier = 1
                    };
                    var index = i;
                    b.transform.SetParent(clothPalette.gameObject.transform, false);

                    // Set the cloth model when the button is selected
                    b.onClick.AddListener(() =>
                    {
                        SetClothMaterial(index);
                    });
                    i++;
                }
            }

            // create the skin UI dynamically based on the "Simple Avatar" model
            if (skinPalette)
            {
                int i = 0;
                // For each skin
                foreach (var mat in referenceAvatar.skinMaterials)
                {
                    // create the button
                    var b = GameObject.Instantiate(prefabChoiceButton);

                    // Customize the button color
                    b.colors = new ColorBlock
                    {
                        normalColor = mat.color,
                        highlightedColor = mat.color,
                        pressedColor = mat.color,
                        selectedColor = mat.color,
                        colorMultiplier = 1
                    };
                    var index = i;
                    b.transform.SetParent(skinPalette.gameObject.transform, false);

                    // Set the skin color model when the button is selected
                    b.onClick.AddListener(() =>
                    {
                        SetSkinMaterial(index);
                    });
                    i++;
                }
            }
        }



        private SimpleAvatarConfig DefaultSimpleAvatar()
        {
            return referenceAvatar.RandomAvatarConfig();
        }

        // RestoreAvatarFromUserPref searches the avatar URL in the player preferences.
        // If found the avatar is restored, otherwise a new random SimpleAvatar is created.
        private void RestoreAvatarFromUserPref()
        {
            string avatarURL = PlayerPrefs.GetString(UserInfo.SETTINGS_AVATARURL);

            if (!string.IsNullOrEmpty(avatarURL))
            {
                if (SimpleAvatarConfig.IsValidURL(avatarURL))
                {
                    Debug.Log($"[AvatarCustomizer] Simple Avatar URL restored: {avatarURL}");
                    simpleAvatarConfig = SimpleAvatarConfig.FromURL(avatarURL);
                    avatarRepresentation.ChangeAvatar(simpleAvatarConfig.URL);
                    tabButtonSimpleAvatar.OnClick();
                }
                else if (IsAvaturnUrl(avatarURL))
                {
                    Debug.Log($"[AvatarCustomizer] Avaturn URL restored: {avatarURL}");
                    avatarRepresentation.ChangeAvatar(avatarURL);
                    // Activate the Avaturn tab if it exists; the panel handles the preview
                    if (tabButtonAvaturn) tabButtonAvaturn.OnClick();
                }
                else
                {
                    // Unknown URL type — load it and let AvatarRepresentation route it
                    Debug.LogWarning($"[AvatarCustomizer] Unknown avatar URL type, attempting to load: {avatarURL}");
                    avatarRepresentation.ChangeAvatar(avatarURL);
                }
            }
            else
            {
                Debug.Log("[AvatarCustomizer] No saved avatar found — generating random SimpleAvatar.");
                simpleAvatarConfig = DefaultSimpleAvatar();
                avatarRepresentation.ChangeAvatar(simpleAvatarConfig.URL);
            }
        }

        private static bool IsAvaturnUrl(string url) =>
            !string.IsNullOrEmpty(url) &&
            url.Contains("avaturn.me", System.StringComparison.OrdinalIgnoreCase);

        // Set the avatar hair mesh
        public void SetHairMesh(int val)
        {
            if (val >= referenceAvatar.hairMeshes.Count) return;
            simpleAvatarConfig.hairMesh = val;
            avatarRepresentation.ChangeAvatar(simpleAvatarConfig.URL);
        }

        // Set the avatar cloth mesh
        public void SetClothMesh(int val)
        {
            if (val >= referenceAvatar.clothMeshes.Count) return;
            simpleAvatarConfig.clothMesh = val;
            avatarRepresentation.ChangeAvatar(simpleAvatarConfig.URL);
        }

        // Set the avatar hair material
        public void SetHairMaterial(int val)
        {
            if (val >= referenceAvatar.hairMaterials.Count) return;
            simpleAvatarConfig.hairMat = val;
            avatarRepresentation.ChangeAvatar(simpleAvatarConfig.URL);
        }

        // Set the avatar cloth material
        public void SetClothMaterial(int val)
        {
            if (val >= referenceAvatar.clothMaterials.Count) return;
            simpleAvatarConfig.clothMat = val;
            avatarRepresentation.ChangeAvatar(simpleAvatarConfig.URL);
        }

        // Set the avatar skin material
        public void SetSkinMaterial(int val)
        {
            if (val >= referenceAvatar.skinMaterials.Count) return;
            simpleAvatarConfig.skinMat = val;
            avatarRepresentation.ChangeAvatar(simpleAvatarConfig.URL);
        }

        // ConnectForceVR is called when the user select the "Join in VR" button
        public void ConnectForceVR()
        {
            // backup user's choice
            PlayerPrefs.SetString("RigMode", "VR");
            // Launch the connection process
            Connect();
        }

        // ConnectForceDesktop is called when the user select the "Join in Desktop mode" button
        public void ConnectForceDesktop()
        {
            // backup user's choice
            PlayerPrefs.SetString("RigMode", "Desktop");
            // Launch the connection process
            Connect();
        }

        // Connect launches the connection process according to the VR or Desktop mode
        public void Connect()
        {
            if (rig && rig.isActiveAndEnabled)
            {
                StartCoroutine(ConnectCoroutine());
            }
            else
            {
                DoConnect();
            }

        }

        // ConnectCoroutine start the fadein and launch the connection process
        IEnumerator ConnectCoroutine()
        {
            Debug.Log("Connect coroutine");
            yield return fader.FadeIn(0.8f);
            DoConnect();
        }

        // DoConnect saves the avatar URL in player settings and load the main scene
        void DoConnect()
        {
            Debug.Log("DoConnect");
            Debug.Log("Save avatar model" + avatarRepresentation.currentAvatar.AvatarURL);
            PlayerPrefs.SetString(UserInfo.SETTINGS_AVATARURL, avatarRepresentation.currentAvatar.AvatarURL );

            PlayerPrefs.Save();
            SceneManager.LoadScene(mainSceneName);
        }



    }
}
