using Fusion.Addons.Avatar;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/**
 *
 * AvaturnAvatarPanel manages the Avaturn avatar selection tab in the Avatar
 * Customiser scene.
 *
 * FLOW
 * ─────
 * 1. Player opens the Avaturn tab.
 * 2. Panel displays the Avaturn customiser URL as a QR code / link to scan or
 *    click on a companion device.
 * 3. After customising on the web, the player receives a GLB export URL
 *    (shown at the end of the Avaturn flow or emailed).
 * 4. Player pastes that URL into the input field and presses Apply.
 * 5. The avatar is loaded via AvaturnAvatar (glTFast) and the URL is saved to
 *    PlayerPrefs ready for the session.
 *
 * WEBVIEW UPGRADE
 * ────────────────
 * When the Avaturn Unity WebView SDK is installed
 * (https://github.com/avaturn/avaturn-unity-webview-sdk.git)
 * add AVATURN_WEBVIEW to Project Settings → Player → Scripting Define Symbols.
 * The panel will then open the customiser in-engine without a companion device.
 *
 **/

namespace Fusion.Samples.IndustriesComponents
{
    public class AvaturnAvatarPanel : MonoBehaviour
    {
        // ── Inspector references ─────────────────────────────────────────────
        [Header("Avaturn Configuration")]
        [Tooltip("Your Avaturn subdomain, e.g. 'demo' → https://demo.avaturn.me")]
        public string avaturnSubdomain = "demo";

        [Tooltip("Optional user ID to persist avatar between sessions. Leave empty for anonymous.")]
        public string avaturnUserId = "";

        [Header("UI References")]
        [Tooltip("TextMeshPro that displays the Avaturn customiser link for the player to open on their phone/PC")]
        public TextMeshProUGUI customiserLinkTMP;

        [Tooltip("Input field where the player pastes the exported GLB URL from Avaturn")]
        public TMP_InputField glbUrlInputField;

        [Tooltip("Button that applies the pasted GLB URL and loads the avatar")]
        public Button applyButton;

        [Tooltip("Shown while the avatar is loading; hidden otherwise")]
        public GameObject loadingIndicator;

        [Tooltip("Shown when the URL is invalid or loading failed")]
        public TextMeshProUGUI statusMessageTMP;

        [Header("References")]
        [Tooltip("The AvatarRepresentation that owns the AvaturnAvatar component")]
        public AvatarRepresentation avatarRepresentation;

        // ── Events ────────────────────────────────────────────────────────────
        [Header("Events")]
        [Tooltip("Fired with the final GLB URL once the avatar has been accepted")]
        public UnityEvent<string> onAvaturnAvatarSelected;

        // ── Private state ─────────────────────────────────────────────────────
        private string _pendingUrl = "";

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            if (avatarRepresentation == null)
                avatarRepresentation = FindObjectOfType<AvatarRepresentation>();
        }

        private void OnEnable()
        {
            // Register button listener
            if (applyButton) applyButton.onClick.AddListener(OnApplyClicked);

            // Update the displayed customiser link
            RefreshCustomiserLink();

            // Restore last-used Avaturn URL if present
            string saved = PlayerPrefs.GetString(UserInfo.SETTINGS_AVATARURL, "");
            if (IsAvaturnUrl(saved) && glbUrlInputField != null)
                glbUrlInputField.text = saved;

            SetStatus("");
            SetLoading(false);
        }

        private void OnDisable()
        {
            if (applyButton) applyButton.onClick.RemoveListener(OnApplyClicked);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Called by the tab system when the Avaturn tab is activated.
        /// If a valid Avaturn URL is already saved, reload it immediately.
        /// </summary>
        public void OnTabSelected()
        {
            RefreshCustomiserLink();

            string saved = PlayerPrefs.GetString(UserInfo.SETTINGS_AVATARURL, "");
            if (IsAvaturnUrl(saved))
            {
                ApplyUrl(saved);
            }
        }

        /// <summary>
        /// Called externally (e.g. by a WebView callback) when the Avaturn web
        /// customiser has produced an export URL.
        /// </summary>
        public void OnAvaturnExportReceived(string glbUrl)
        {
            if (glbUrlInputField != null) glbUrlInputField.text = glbUrl;
            ApplyUrl(glbUrl);
        }

        // ── Button handlers ───────────────────────────────────────────────────
        private void OnApplyClicked()
        {
            string url = glbUrlInputField != null ? glbUrlInputField.text.Trim() : "";
            if (string.IsNullOrEmpty(url))
            {
                SetStatus("Please paste your Avaturn GLB URL above.");
                return;
            }

            if (!IsAvaturnUrl(url))
            {
                SetStatus("URL does not look like an Avaturn export link.\nExpected: https://...avaturn.me/.../model.glb");
                return;
            }

            ApplyUrl(url);
        }

        // ── Internal helpers ──────────────────────────────────────────────────
        private void ApplyUrl(string url)
        {
            SetStatus("");
            SetLoading(true);
            _pendingUrl = url;

            if (avatarRepresentation != null)
                avatarRepresentation.ChangeAvatar(url);

            // Persist immediately — DoConnect() will re-save it, but storing
            // here ensures the URL survives a hot-reload during development.
            PlayerPrefs.SetString(UserInfo.SETTINGS_AVATARURL, url);

            onAvaturnAvatarSelected?.Invoke(url);
            SetLoading(false);
        }

        private void RefreshCustomiserLink()
        {
            if (customiserLinkTMP == null) return;
            string link = BuildCustomiserUrl();
            customiserLinkTMP.text = $"Open in browser or scan QR code:\n<b>{link}</b>";
        }

        private string BuildCustomiserUrl()
        {
            string subdomain = string.IsNullOrEmpty(avaturnSubdomain) ? "demo" : avaturnSubdomain;
            string url       = $"https://{subdomain}.avaturn.me";
            if (!string.IsNullOrEmpty(avaturnUserId))
                url += $"?user_id={avaturnUserId}";
            return url;
        }

        private void SetLoading(bool isLoading)
        {
            if (loadingIndicator) loadingIndicator.SetActive(isLoading);
            if (applyButton)      applyButton.interactable = !isLoading;
        }

        private void SetStatus(string message)
        {
            if (statusMessageTMP)
            {
                statusMessageTMP.text    = message;
                statusMessageTMP.gameObject.SetActive(!string.IsNullOrEmpty(message));
            }
        }

        private static bool IsAvaturnUrl(string url) =>
            !string.IsNullOrEmpty(url) &&
            url.Contains("avaturn.me", System.StringComparison.OrdinalIgnoreCase);

#if AVATURN_WEBVIEW
        // ── WebView integration (requires AVATURN_WEBVIEW scripting define) ──
        // Install the SDK: Window → Package Manager → + → Add package from git URL
        //   https://github.com/avaturn/avaturn-unity-webview-sdk.git
        // Then add AVATURN_WEBVIEW to: Project Settings → Player → Scripting Define Symbols

        [Header("WebView (requires AVATURN_WEBVIEW define)")]
        [Tooltip("Container GameObject that holds the AvaturnSDK WebView component")]
        public GameObject webViewContainer;

        private Avaturn.SDK.AvaturnSDK _sdk;

        private void InitWebView()
        {
            if (webViewContainer == null) return;
            _sdk = webViewContainer.GetComponentInChildren<Avaturn.SDK.AvaturnSDK>();
            if (_sdk == null) return;

            _sdk.OnAvatarExported += OnSdkAvatarExported;
        }

        private void OnSdkAvatarExported(string glbUrl)
        {
            Debug.Log($"[AvaturnAvatarPanel] WebView export received: {glbUrl}");
            OnAvaturnExportReceived(glbUrl);
        }

        public void OpenWebView()
        {
            if (_sdk == null) InitWebView();
            if (_sdk == null)
            {
                Debug.LogError("[AvaturnAvatarPanel] AvaturnSDK component not found.");
                return;
            }
            _sdk.Init(avaturnSubdomain, avaturnUserId);
        }
#endif
    }
}
