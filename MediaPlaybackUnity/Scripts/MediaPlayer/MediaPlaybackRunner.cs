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
    public bool playing = true;
    public TextMeshProUGUI text;
    public Material material;

    MediaPlayer player;

    private void Awake()
    {
        player = GetComponent<MediaPlayer>();
    }

    void Start()
    {
        if (!string.IsNullOrEmpty(mediaURI))
        {
            player.Load(mediaURI);
            player.Play();
            player.TextureUpdated += Player_TextureUpdated;
        }
    }

    void Update ()
    {
        text.text = player.Position.ToString();
	}

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(player.playbackTexture, destination);
    }

    private void Player_TextureUpdated(object sender, Texture2D newVideoTexture)
    {

    }
}
