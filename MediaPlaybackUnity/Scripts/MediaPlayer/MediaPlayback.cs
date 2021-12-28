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

namespace MediaPlayback
{
    public enum PlaybackState
    {
        None = 0,
        Opening,
        Buffering,
        Playing,
        Paused,
        Ended,
        NA = 255
    };

    public class ChangedEventArgs<T>
    {
        public T PreviousState;
        public T CurrentState;

        public ChangedEventArgs(T previousState, T currentState)
        {
            PreviousState = previousState;
            CurrentState = currentState;
        }
    }
}
