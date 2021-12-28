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

using UnityEngine;
using TMPro;
using MediaPlayback;

public class MediaPlaybackRunner : MonoBehaviour
{

    public string mediaURI = string.Empty;
    public TextMeshProUGUI text;
    public uint fps;
    public uint instances = 1;

    MediaPlayer[] players;

    void Start()
    {
        players = new MediaPlayer[instances];
        
        for (int i = 0; i < instances; i++)
            players[i] = new MediaPlayer(mediaURI, fps, this);
    }

    void Update ()
    {
        foreach (var player in players)
        {
            player.Update();
        }
        text.text = players[0].Frame.ToString();
	}

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(players[0].playbackTexture == null ? source : players[0].playbackTexture, destination);
    }

    private void OnDisable()
    {
        foreach (var player in players)
        {
            player.Release();
        }
    }
}
