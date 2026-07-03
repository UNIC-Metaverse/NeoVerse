# Credentials & API keys

These files hold per-developer secrets (Convai API key, Photon App IDs) and are
**not tracked in git**. After cloning, create them locally from the templates in
`docs/credential-templates/` and paste in your own keys.

| Copy this template | To this location |
| --- | --- |
| `docs/credential-templates/ConvaiAPIKey.asset` | `Assets/_Project/Shared/Resources/ConvaiAPIKey.asset` |
| `docs/credential-templates/PhotonAppSettings.asset` | `Assets/ThirdParty/Photon/Fusion/Resources/PhotonAppSettings.asset` |
| `docs/credential-templates/PhotonServerSettings.asset` | `Assets/ThirdParty/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset` |

Then open each copied `.asset` in a text editor (or via the Unity inspector) and
replace the `PASTE_YOUR_..._HERE` placeholders:

- **Convai** – get your API key from the Convai dashboard (https://convai.com).
- **Photon** – get your App IDs from the Photon dashboard
  (https://dashboard.photonengine.com). Fusion, Chat and Voice each have their own App ID.

You can also let the built-in setup wizards create these assets:
- Convai: use the Convai setup window (paste key when prompted).
- Photon: the PUN/Fusion wizard prompts for the App ID on first use.

## Important
- Never commit real keys. `.gitignore` already excludes the three asset paths above.
- The repository is public, so any key committed here is effectively exposed —
  if that happens, rotate the key immediately rather than only removing the file.
