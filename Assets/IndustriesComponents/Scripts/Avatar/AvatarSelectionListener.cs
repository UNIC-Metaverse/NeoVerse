using Fusion.Addons.Avatar;
using Fusion.Addons.Avatar.ReadyPlayerMe;
using Fusion.Addons.Avatar.SimpleAvatar;
using Fusion.Samples.IndustriesComponents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarSelectionListener : MonoBehaviour, IAvatarRepresentationListener
{
    AvatarRepresentation avatarRepresentation;
    public AvatarCustomizer avatarCustomizer;

    private void Awake()
    {
        if (avatarCustomizer == null)
            avatarCustomizer = FindObjectOfType<AvatarCustomizer>();
    }
    public void OnAvailableAvatarsListed(AvatarRepresentation avatarRepresentation)
    {
    }

    public void OnRepresentationAvailable(IAvatar avatar, bool isLocalUserAvatar)
    {
        if(avatar is SimpleAvatar)
        {
            avatarCustomizer.latestSimpleAvatarURL = avatar.AvatarURL;
        }
        else if (avatar is RPMAvatarLoader)
        {
            avatarCustomizer.latestRPMAvatarURL = avatar.AvatarURL;

        }
    }

    public void OnRepresentationUnavailable(IAvatar avatar)
    {
    }

}
