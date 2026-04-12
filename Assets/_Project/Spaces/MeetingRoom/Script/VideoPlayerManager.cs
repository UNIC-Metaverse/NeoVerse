using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

/***
 * 
 * VideoPlayerManager starts the video at start (OnEnable)
 * 
 ***/
public class VideoPlayerManager : MonoBehaviour
{
    [SerializeField] string videofile;
    [SerializeField] VideoPlayer videoPlayer;

    private void Awake()
    {
        if (!videoPlayer)
            videoPlayer = GetComponent <VideoPlayer>();
        if (!videoPlayer)
            Debug.LogError("VideoPlayer not found");
    }

    private void OnEnable()
    {
        if (videoPlayer)
        {
            string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, videofile);
            videoPlayer.url = videoPath;
            videoPlayer.Play();
        }
    }

}
