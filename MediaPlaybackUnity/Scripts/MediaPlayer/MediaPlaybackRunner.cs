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

    MediaPlayer color, maps;
    public Texture2D texture;
    public Texture2DArray textureChunk;

    void Start()
    {
        color = new MediaPlayer(mediaURI, fps, this);

        color.TextureUpdated += MediaPlaybackRunner_TextureUpdated;
        color.FrameRendered += MediaPlaybackRunner_FrameRendered;
    }

    private void MediaPlaybackRunner_TextureUpdated(object sender, Texture2D newVideoTexture)
    {
        texture = newVideoTexture;
    }

    private void MediaPlaybackRunner_FrameRendered(object sender, Texture2D texture)
    {
        int chunkIndex = (int)(color.Frame % 10);
        // Graphics.CopyTexture(texture, 0, textureChunk, chunkIndex);
    }

    void Update ()
    {
        color.Update();
        text.text = color.Frame.ToString();
	}

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(color.playbackTexture == null ? source : color.playbackTexture, destination);
    }

    private void OnDisable()
    {
        color.Release();
    }
}
