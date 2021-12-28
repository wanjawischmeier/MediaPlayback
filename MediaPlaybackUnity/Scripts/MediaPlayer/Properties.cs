using UnityEngine;

namespace MediaPlayback
{
    public partial class MediaPlayer
    {
        public bool Hardware4KDecodingSupported
        {
            get
            {
                return hw4KDecodingSupported;
            }
        }

        // texture size
        public uint CurrentPlaybackTextureWidth
        {
            get
            {
                return textureWidth;
            }
        }
        public uint CurrentPlaybackTextureHeight
        {
            get
            {
                return textureHeight;
            }
        }

        public Texture2D CurrentVideoTexture
        {
            get
            {
                return playbackTexture;
            }
        }


        public PlaybackState State
        {
            get { return currentState; }
            private set
            {
                if (currentState != value)
                {
                    previousState = currentState;
                    currentState = value;
                    var args = new ChangedEventArgs<PlaybackState>(previousState, currentState);

#if UNITY_WSA_10_0
                    if (!UnityEngine.WSA.Application.RunningOnAppThread())
                    {
                        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                        {
                            TriggerPlaybackStateChangedEvent(args);

                        }, false);
                    }
                    else
                    {
                        TriggerPlaybackStateChangedEvent(args);
                    }
#else
                    TriggerPlaybackStateChangedEvent(args);
#endif
                }
            }
        }

        public long Duration
        {
            get
            {
                long duration = 0;
                long position = 0;

                CheckHR(Plugin.GetDurationAndPosition(pluginInstance, ref duration, ref position));
                return duration;
            }
        }


        public long Position
        {
            get
            {
                long duration = 0;
                long position = 0;

                CheckHR(Plugin.GetDurationAndPosition(pluginInstance, ref duration, ref position));
                return position;
            }
            set
            {
                CheckHR(Plugin.Seek(pluginInstance, value));
            }
        }

        public long Frame
        {
            get
            {
                return (long)(Position / (double)positionFactor * fps);
            }
            set
            {
                Position = (long)(value / (double)fps * positionFactor);
            }
        }

        public double Volume
        {
            set
            {
                CheckHR(Plugin.SetVolume(pluginInstance, value));
            }
        }


        public uint VideoWidth
        {
            get
            {
                return currentMediaDescription.width;
            }
        }

        public uint VideoHeight
        {
            get
            {
                return currentMediaDescription.height;
            }
        }

        public bool Ready
        {
            get
            {
                return loaded;
            }
        }
    }
}
