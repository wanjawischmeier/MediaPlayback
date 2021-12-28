//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.IO;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(MediaPlayer.Playback))]
public class MediaPlaybackRunner : MonoBehaviour {

    public string mediaURI = string.Empty;
    public bool playing = true;
    public TextMeshProUGUI text;

    MediaPlayer.Playback _player;

    private void Awake()
    {
        _player = GetComponent<MediaPlayer.Playback>();
    }

    void Start()
    {
        if (!string.IsNullOrEmpty(mediaURI))
        {
            string uriStr = mediaURI;

            if (Uri.IsWellFormedUriString(mediaURI, UriKind.Absolute))
            {
                uriStr = mediaURI;
            }
            else if (Path.IsPathRooted(mediaURI))
            {
                uriStr = "file:///" + mediaURI;
            }
            else
            {
                uriStr = "file:///" + Path.Combine(Application.streamingAssetsPath, mediaURI);
            }

            _player.Load(uriStr);
        }
    }

    void Update ()
    {
        text.text = _player.GetPosition().ToString();

        if (playing)
        {
            if (_player.State == MediaPlayer.PlaybackState.Paused)
            {
                _player.Play();
                Debug.Log("continued");
            }
        }
        else
        {
            if (_player.State == MediaPlayer.PlaybackState.Playing)
            {
                _player.Pause();
                Debug.Log("paused");
            }
        }
	}
}
