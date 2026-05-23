using System.Collections.Generic;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

/**
 *
 * AvaturnAvatar implements the IAvatar interface to load photorealistic avatars
 * from the Avaturn platform (https://avaturn.me).
 *
 * Avatars are delivered as GLB files (glTF 2.0) via HTTPS URLs and loaded at
 * runtime using the glTFast package (already present in this project).
 *
 * URL routing: any URL containing "avaturn.me" is claimed as Compatible.
 * URLs using the internal "avaturn://" scheme are treated as placeholders and
 * rejected until a real export URL is available.
 *
 * Integration: add this component as a sibling of SimpleAvatar under the
 * AvatarRepresentation GameObject. AvatarRepresentation.FindSuitableAvatar()
 * will automatically route Avaturn URLs here via SupportForURL().
 *
 **/

namespace Fusion.Addons.Avatar
{
    public class AvaturnAvatar : MonoBehaviour, IAvatar
    {
        [Tooltip("Parent transform under which the loaded GLB will be instantiated. Defaults to this transform.")]
        public Transform avatarRoot;

        [Tooltip("Uniform scale applied to the loaded avatar model. Adjust if the Avaturn avatar appears too large or small.")]
        public float avatarScale = 1f;

        // ── Internal state ───────────────────────────────────────────────────
        private string             _avatarURL        = "";
        private GameObject         _loadedModel      = null;
        private List<Renderer>     _loadedRenderers  = new List<Renderer>();
        private AvatarStatus       _avatarStatus     = AvatarStatus.NotLoaded;
        private AvatarDescription  _avatarDescription;
        private AvatarRepresentation _avatarRepresentation;
        private bool               _loadInProgress   = false;

        // ── IAvatar ──────────────────────────────────────────────────────────
        public AvatarStatus       AvatarStatus      => _avatarStatus;
        public string             AvatarURL         => _avatarURL;
        public AvatarDescription  AvatarDescription => _avatarDescription;
        public int                TargetLODLevel    => 0;
        public bool               ShouldLoadLocalAvatar => true;
        public GameObject         AvatarGameObject  => gameObject;

        /// <summary>
        /// Returns Compatible for any avaturn.me HTTPS URL.
        /// Returns Incompatible for the internal avaturn:// placeholder and all other URLs.
        /// </summary>
        public AvatarUrlSupport SupportForURL(string url)
        {
            if (string.IsNullOrEmpty(url))
                return AvatarUrlSupport.Incompatible;

            if (url.Contains("avaturn.me", System.StringComparison.OrdinalIgnoreCase))
                return AvatarUrlSupport.Compatible;

            // Internal placeholder — not yet a real URL
            if (url.StartsWith("avaturn://", System.StringComparison.OrdinalIgnoreCase))
                return AvatarUrlSupport.Incompatible;

            return AvatarUrlSupport.Incompatible;
        }

        /// <summary>
        /// Begins asynchronous loading of the avatar GLB from the provided URL.
        /// Notifies AvatarRepresentation of loading start and completion.
        /// </summary>
        public void ChangeAvatar(string avatarURL)
        {
            if (string.IsNullOrEmpty(avatarURL))
            {
                RemoveAvatar();
                return;
            }

            _avatarURL    = avatarURL;
            _avatarStatus = AvatarStatus.RepresentationLoading;
            if (_avatarRepresentation) _avatarRepresentation.LoadingRepresentation(this);

            _ = LoadAvatarAsync(avatarURL);
        }

        /// <summary>
        /// Destroys the currently loaded avatar and resets state.
        /// </summary>
        public void RemoveAvatar()
        {
            _loadInProgress = false;
            DestroyLoadedModel();
            _avatarStatus = AvatarStatus.NotLoaded;
            _avatarURL    = "";
            if (_avatarRepresentation) _avatarRepresentation.RepresentationUnavailable(this);
        }

        /// <summary>
        /// Avaturn does not support random avatar generation — returns empty string.
        /// </summary>
        public string LoadRandomAvatar() => "";

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            _avatarRepresentation = GetComponentInParent<AvatarRepresentation>();
            if (avatarRoot == null) avatarRoot = transform;
        }

        // ── Loading ──────────────────────────────────────────────────────────
        private async Task LoadAvatarAsync(string url)
        {
            // Guard against overlapping loads triggered during scene teardown
            _loadInProgress = true;
            DestroyLoadedModel();

            if (!this || !gameObject)
            {
                _loadInProgress = false;
                return;
            }

            Debug.Log($"[AvaturnAvatar] Loading GLB from: {url}");

            var gltf = new GltfImport();
            bool loaded = await gltf.Load(url);

            // Check the GameObject still exists after the await
            if (!this || !gameObject || !_loadInProgress)
            {
                gltf.Dispose();
                _loadInProgress = false;
                return;
            }

            if (!loaded)
            {
                Debug.LogError($"[AvaturnAvatar] glTFast failed to load avatar from: {url}");
                _avatarStatus = AvatarStatus.RepresentationMissing;
                if (_avatarRepresentation) _avatarRepresentation.RepresentationUnavailable(this);
                _loadInProgress = false;
                return;
            }

            // Instantiate into the scene
            var instantiator = new GameObjectInstantiator(gltf, avatarRoot);
            bool instantiated = await gltf.InstantiateMainSceneAsync(instantiator);

            if (!this || !gameObject || !_loadInProgress)
            {
                // Component was destroyed mid-load — clean up what was spawned
                if (instantiator.RootTransform != null)
                    Destroy(instantiator.RootTransform.gameObject);
                gltf.Dispose();
                _loadInProgress = false;
                return;
            }

            if (!instantiated || instantiator.RootTransform == null)
            {
                Debug.LogError($"[AvaturnAvatar] glTFast instantiation failed for: {url}");
                _avatarStatus = AvatarStatus.RepresentationMissing;
                if (_avatarRepresentation) _avatarRepresentation.RepresentationUnavailable(this);
                _loadInProgress = false;
                return;
            }

            _loadedModel = instantiator.RootTransform.gameObject;
            _loadedModel.transform.localScale    = Vector3.one * avatarScale;
            _loadedModel.transform.localPosition = Vector3.zero;
            _loadedModel.transform.localRotation = Quaternion.identity;

            _loadedRenderers = new List<Renderer>(_loadedModel.GetComponentsInChildren<Renderer>(true));

            _avatarStatus      = AvatarStatus.RepresentationAvailable;
            _avatarDescription = new AvatarDescription { colorMode = AvatarDescription.ColorMode.NoColorInfo };
            _loadInProgress    = false;

            Debug.Log($"[AvaturnAvatar] Avatar ready — {_loadedRenderers.Count} renderer(s) found.");
            if (_avatarRepresentation) _avatarRepresentation.RepresentationAvailable(this, _loadedRenderers);

            gltf.Dispose();
        }

        private void DestroyLoadedModel()
        {
            if (_loadedModel != null)
            {
                if (_avatarRepresentation)
                    _avatarRepresentation.RemoveRepresentation(this, _loadedRenderers);

                Destroy(_loadedModel);
                _loadedModel = null;
                _loadedRenderers.Clear();
            }
        }

        private void OnDestroy()
        {
            _loadInProgress = false;
            if (_loadedModel != null) Destroy(_loadedModel);
        }
    }
}
