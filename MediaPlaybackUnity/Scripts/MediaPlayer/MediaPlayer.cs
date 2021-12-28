using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MediaPlayback
{
    public partial class MediaPlayer : MonoBehaviour
    {
        // state handling
        public delegate void PlaybackStateChangedHandler(object sender, ChangedEventArgs<PlaybackState> args);
        public delegate void PlaybackFailedHandler (object sender, long hresult);
        public delegate void TextureUpdatedHandler(object sender, Texture2D newVideoTexture);

        public event PlaybackStateChangedHandler PlaybackStateChanged;
        public event PlaybackFailedHandler PlaybackFailed;
        public event TextureUpdatedHandler TextureUpdated;

        // Renderer component to the object the frame will be rendered to
        public Material targetMaterial;

        // Texture to update on the Target Renderer (must be material's shader variable name)
        public string targetRendererTextureName = "_MainTex";

        public Texture2D playbackTexture = null;

        private uint textureWidth = 0;
        private uint textureHeight = 0;
        private bool needToUpdateTexture = false;

        private IntPtr pluginInstance = IntPtr.Zero;
        private GCHandle thisObject;
        private bool hw4KDecodingSupported = true;

        private PlaybackState currentState = PlaybackState.None;
        private PlaybackState previousState = PlaybackState.None;

        private readonly Plugin.StateChangedCallback stateCallback = new Plugin.StateChangedCallback(MediaPlayback_Changed);

        private bool loaded = false;
        private Plugin.MEDIA_DESCRIPTION currentMediaDescription = new Plugin.MEDIA_DESCRIPTION();

        public void Load(string uriOrPath)
        {
            Stop();

            string uriStr = uriOrPath.Trim();

            if (uriStr.ToLower().StartsWith("file:///") || Uri.IsWellFormedUriString(uriOrPath, UriKind.Absolute))
            {
                uriStr = uriOrPath;
            }
            else if (System.IO.Path.IsPathRooted(uriOrPath))
            {
                uriStr = "file:///" + uriOrPath;
            }
            else
            {
                uriStr = "file:///" + System.IO.Path.Combine(Application.streamingAssetsPath, uriOrPath);
            }

            loaded = (0 == CheckHR(Plugin.LoadContent(pluginInstance, uriStr)));
        }

        public void Play()
        {
            if (loaded)
            {
                CheckHR(Plugin.Play(pluginInstance));
            }
            else
            {
                Debug.LogError("Player instance not loaded.");
            }
        }


        public void Pause()
        {
            CheckHR(Plugin.Pause(pluginInstance));
        }

        public void Stop()
        {
            Plugin.Stop(pluginInstance);
            currentMediaDescription = new Plugin.MEDIA_DESCRIPTION();
            State = PlaybackState.None;

            if (playbackTexture != null)
            {
                try
                {
                    byte[] dummyData = new byte[playbackTexture.width * playbackTexture.height * 4];

                    Texture2D dummyTex = new Texture2D(playbackTexture.width, playbackTexture.height, playbackTexture.format, false);
                    dummyTex.LoadRawTextureData(dummyData);
                    dummyTex.Apply();
                    Graphics.CopyTexture(dummyTex, playbackTexture);
                    UnityEngine.Object.Destroy(dummyTex);
                }
                catch { }
            }

            textureWidth = 0;
            textureHeight = 0;
        }

        public void UpdateTexture(uint newTextureWidth, uint newTextureHeight)
        {
            textureWidth = newTextureWidth;
            textureHeight = newTextureHeight;

            Debug.LogFormat("Video texture updated: {0}x{1}", textureWidth, textureHeight);

            // create native texture for playback
            CheckHR(Plugin.GetPlaybackTexture(pluginInstance, out IntPtr nativeTexture, out _));

            if (nativeTexture != IntPtr.Zero)
            {
                var oldTexture = playbackTexture;

                if (playbackTexture == null || playbackTexture.width != (int)textureWidth || playbackTexture.height != (int)textureHeight)
                {
                    // create a new Unity texture2d 
                    playbackTexture = Texture2D.CreateExternalTexture((int)textureWidth, (int)textureHeight, TextureFormat.BGRA32, false, false, nativeTexture);
                }
                else
                {
                    // we can just update our old Unity texture 
                    oldTexture = null; 
                    playbackTexture.UpdateExternalTexture(nativeTexture);
                }

                if (targetMaterial != null)
                {
                    if (!string.IsNullOrEmpty(targetRendererTextureName))
                    {
                        targetMaterial.SetTexture(targetRendererTextureName, playbackTexture);
                    }
                    else
                    {
                        targetMaterial.mainTexture = playbackTexture;
                    }
                }

#if UNITY_WSA_10_0
                if (!UnityEngine.WSA.Application.RunningOnAppThread())
                {
                    UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        TextureUpdated(this, playbackTexture);
                    }, true);
                }
                else
                {
                    TextureUpdated(this, playbackTexture);
                }
#else
                TextureUpdated?.Invoke(this, playbackTexture);
#endif

                if (oldTexture != null)
                {
                    try
                    {
                        UnityEngine.Object.Destroy(oldTexture);
                    }
                    catch { }
                }

            }

        }


        private IEnumerator Start()
        {
            yield return StartCoroutine(CallPluginAtEndOfFrames());
        }


        private void Update()
        {
            if(needToUpdateTexture)
            {
                needToUpdateTexture = false;
                UpdateTexture(currentMediaDescription.width, currentMediaDescription.height);
            }
            else
            {
                GL.IssuePluginEvent(Plugin.GetRenderEventFunc(), -1);
            }
        }

        private void OnEnable()
        {
            // create callback
            thisObject = GCHandle.Alloc(this, GCHandleType.Normal);
            IntPtr thisObjectPtr = GCHandle.ToIntPtr(thisObject);

            // create media playback
            CheckHR(Plugin.CreateMediaPlayback(stateCallback, thisObjectPtr, out pluginInstance));

            Plugin.IsHardware4KDecodingSupported(pluginInstance, out hw4KDecodingSupported);

            Debug.LogFormat("MediaPlayback has been created. Hardware decoding of 4K+ is {0}.", hw4KDecodingSupported ? "supported" : "not supported");
        }

        private void OnDisable()
        {
            loaded = false;
            if (pluginInstance != IntPtr.Zero)
            {
                if (currentState == PlaybackState.Playing)
                {
                    try
                    {
                        Stop();
                    }
                    catch { }
                }
                Plugin.ReleaseMediaPlayback(pluginInstance);
                pluginInstance = IntPtr.Zero;
            }

            if (thisObject.Target != null)
            {
                try
                {
                    thisObject.Free();
                }
                catch { }
            }

            if (playbackTexture != null)
            {
                try
                {
                    UnityEngine.Object.Destroy(playbackTexture);
                    playbackTexture = null;
                }
                catch { }
            }
        }

        [AOT.MonoPInvokeCallback(typeof(Plugin.StateChangedCallback))]
        private static void MediaPlayback_Changed(IntPtr thisObjectPtr, Plugin.PLAYBACK_STATE args)
        {
            if (thisObjectPtr == IntPtr.Zero)
            {
                Debug.LogError("MediaPlayback_Changed: requires thisObjectPtr.");
                return;
            }

            var handle = GCHandle.FromIntPtr(thisObjectPtr);
            MediaPlayer thisObject = handle.Target as MediaPlayer;
            if (thisObject == null)
            {
                Debug.LogError("MediaPlayback_Changed: thisObjectPtr is not null, but seems invalid.");
                return;
            }

#if UNITY_WSA_10_0
            if (!UnityEngine.WSA.Application.RunningOnAppThread())
            {
                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
                    thisObject.OnStateChanged(args);
                }, false);
            }
            else
            {
                thisObject.OnStateChanged(args);
            }
#else
            thisObject.OnStateChanged(args);
#endif
        }


        private void TriggerPlaybackStateChangedEvent(ChangedEventArgs<PlaybackState> args)
        {
            if (PlaybackStateChanged != null)
            {

                try
                {
                    PlaybackStateChanged(this, args);
                }
                catch { }
            }
        }


        private void OnStateChanged(Plugin.PLAYBACK_STATE args)
        {
            var stateType = (Plugin.StateType)Enum.ToObject(typeof(Plugin.StateType), args.type);

            switch (stateType)
            {
                case Plugin.StateType.StateType_None:
                    var newState0 = (PlaybackState)Enum.ToObject(typeof(PlaybackState), args.state);
                    if (newState0 == PlaybackState.Ended || newState0 == PlaybackState.None)
                        currentMediaDescription = new Plugin.MEDIA_DESCRIPTION();
                    loaded = false;
                    Debug.Log("Playback State: " + stateType.ToString());
                    break;

                case Plugin.StateType.StateType_NewFrameTexture:
                    currentMediaDescription.duration = args.description.duration;
                    currentMediaDescription.width = args.description.width;
                    currentMediaDescription.height = args.description.height;
                    currentMediaDescription.isSeekable = args.description.isSeekable;
                    needToUpdateTexture = true;
                    break;

                case Plugin.StateType.StateType_StateChanged:
                    var newState = (PlaybackState)Enum.ToObject(typeof(PlaybackState), args.state);
                    if (newState == PlaybackState.None)
                    {
                        if (State == PlaybackState.Playing || State == PlaybackState.Paused)
                        {
                            Debug.Log("Video ended");
                            newState = PlaybackState.Ended;
                        }
                        currentMediaDescription = new Plugin.MEDIA_DESCRIPTION();
                        loaded = false;
                    }
                    else if (newState == PlaybackState.Ended)
                    {
                        currentMediaDescription = new Plugin.MEDIA_DESCRIPTION();
                        loaded = false;
                    }
                    else if (newState != PlaybackState.Buffering && args.description.width != 0 && args.description.height != 0)
                    {
                        currentMediaDescription.duration = args.description.duration;
                        currentMediaDescription.width = args.description.width;
                        currentMediaDescription.height = args.description.height;
                        currentMediaDescription.isSeekable = args.description.isSeekable;
                    }
                    State = newState;
                    Debug.Log("Playback State: " + stateType.ToString() + " - " + State.ToString());
                    break;
                case Plugin.StateType.StateType_Opened:
                    currentMediaDescription.duration = args.description.duration;
                    currentMediaDescription.width = args.description.width;
                    currentMediaDescription.height = args.description.height;
                    currentMediaDescription.isSeekable = args.description.isSeekable;
                    Debug.Log("Media Opened: " + args.description.ToString());
                    break;
                case Plugin.StateType.StateType_Failed:
                    loaded = false;
                    CheckHR(args.hresult);
                    PlaybackFailed?.Invoke(this, args.hresult);
                    State = PlaybackState.None;
                    loaded = false;
                    break;
                case Plugin.StateType.StateType_GraphicsDeviceShutdown:
                    Debug.LogWarning("Graphics device was lost!");
                    break;
                case Plugin.StateType.StateType_GraphicsDeviceReady:
                    Debug.LogWarning("Graphics device was restored! Recreating the playback texture!");
                    break;
                default:
                    break;
            }
        }

        private IEnumerator CallPluginAtEndOfFrames()
        {
            while (true)
            {
                // Wait until all frame rendering is done
                yield return new WaitForEndOfFrame();

                // Set time for the plugin
                Plugin.SetTimeFromUnity(Time.timeSinceLevelLoad);

                // Issue a plugin event with arbitrary integer identifier.
                // The plugin can distinguish between different
                // things it needs to do based on this ID.
                // For our simple plugin, it does not matter which ID we pass here.
                GL.IssuePluginEvent(Plugin.GetRenderEventFunc(), 1);
            }
        }

        public static long CheckHR(long hresult)
        {
            if (hresult != 0)
            {
                Debug.Log("Media Failed: HRESULT = 0x" + hresult.ToString("X", System.Globalization.NumberFormatInfo.InvariantInfo));
            }
            return hresult;
        }
    }
}
