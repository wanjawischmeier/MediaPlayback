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

#include "pch.h"
#include "Unity/PlatformBase.h"
#include "MediaPlayerPlayback.h"

using namespace Microsoft::WRL;

static UnityGfxRenderer s_DeviceType = kUnityGfxRenderernullptr;
static IUnityInterfaces* s_UnityInterfaces = nullptr;
static IUnityGraphics* s_Graphics = nullptr;

//static ComPtr<IMediaPlayerPlayback> spMediaPlayback;
static float g_Time;

STDAPI_(BOOL) DllMain(
    _In_opt_ HINSTANCE hInstance, _In_ DWORD dwReason, _In_opt_ LPVOID lpReserved)
{
    UNREFERENCED_PARAMETER(lpReserved);

    if (DLL_PROCESS_ATTACH == dwReason)
    {
        //� Don't need per-thread callbacks
        DisableThreadLibraryCalls(hInstance);

        Module<InProc>::GetModule().Create();
    }
    else if (DLL_PROCESS_DETACH == dwReason)
    {
        Module<InProc>::GetModule().Terminate();
    }

    return TRUE;
}

STDAPI DllGetActivationFactory(
    _In_ HSTRING activatibleClassId, 
    _COM_Outptr_ IActivationFactory** factory)
{
    auto &module = Module< InProc>::GetModule();
    return module.GetActivationFactory(activatibleClassId, factory);
}

STDAPI DllCanUnloadNow()
{
    const auto &module = Module<InProc>::GetModule();
    return module.GetObjectCount() == 0 ? S_OK : S_FALSE;
}

extern "C" HRESULT UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API CreateMediaPlayback(_In_ StateChangedCallback fnCallback, void* clientObject, void** p_spMediaPlayback)
{
    ComPtr<IMediaPlayerPlayback> spPlayerPlayback;
    IFR(CMediaPlayerPlayback::CreateMediaPlayback(s_DeviceType, s_UnityInterfaces, fnCallback, clientObject, &spPlayerPlayback));

	*p_spMediaPlayback = spPlayerPlayback.Detach();

    return S_OK;
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API ReleaseMediaPlayback(IMediaPlayerPlayback* spMediaPlayback)
{
    if (spMediaPlayback != nullptr)
    {
		spMediaPlayback->Release();
    }
}


extern "C" HRESULT UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API LoadContent(_In_ IMediaPlayerPlayback* spMediaPlayback, _In_ LPCWSTR pszContentLocation)
{
    NULL_CHK(pszContentLocation);
    NULL_CHK(spMediaPlayback);
    
    return spMediaPlayback->LoadContent(pszContentLocation);
}

extern "C" HRESULT UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Play(_In_ IMediaPlayerPlayback* spMediaPlayback)
{
    NULL_CHK(spMediaPlayback);

    return spMediaPlayback->Play();
}

extern "C" HRESULT UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Pause(_In_ IMediaPlayerPlayback* spMediaPlayback)
{
    NULL_CHK(spMediaPlayback);

    return spMediaPlayback->Pause();
}

extern "C" HRESULT UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Stop(_In_ IMediaPlayerPlayback* spMediaPlayback)
{
    NULL_CHK(spMediaPlayback);

    return spMediaPlayback->Stop();
}


extern "C" HRESULT UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetPlaybackTexture(_In_ IMediaPlayerPlayback* spMediaPlayback, _Out_ IUnknown** d3d11TexturePtr, _Out_ LPBYTE isStereoscopic)
{
	NULL_CHK(spMediaPlayback);

	return spMediaPlayback->GetPlaybackTexture(d3d11TexturePtr, isStereoscopic);
}


extern "C" HRESULT UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetDurationAndPosition(_In_ IMediaPlayerPlayback* spMediaPlayback, _Out_ LONGLONG* duration, _Out_ LONGLONG* position)
{
	NULL_CHK(spMediaPlayback);

	return spMediaPlayback->GetDurationAndPosition(duration, position);
}

extern "C" HRESULT UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API Seek(_In_ IMediaPlayerPlayback* spMediaPlayback, _In_ LONGLONG position)
{
	NULL_CHK(spMediaPlayback);

	return spMediaPlayback->Seek(position);
}

extern "C" HRESULT UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetVolume(_In_ IMediaPlayerPlayback* spMediaPlayback, _In_ DOUBLE volume)
{
	NULL_CHK(spMediaPlayback);

	return spMediaPlayback->SetVolume(volume);
}

extern "C" HRESULT UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetMediaPlayer(_In_ IMediaPlayerPlayback* spMediaPlayback, _Out_ IUnknown** pIUnkForMediaPlayer)
{
	NULL_CHK(spMediaPlayback);

	return spMediaPlayback->GetIUnknown(pIUnkForMediaPlayer);
}

extern "C" HRESULT UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API IsHardware4KDecodingSupported(_In_ IMediaPlayerPlayback* spMediaPlayback, _Out_ BOOL* pSupportsHardware4KVideoDecoding)
{
	NULL_CHK(spMediaPlayback);

	return spMediaPlayback->IsHardware4KDecodingSupported(pSupportsHardware4KVideoDecoding);
}


extern "C" HRESULT UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetSubtitlesCallbacks(_In_ IMediaPlayerPlayback* spMediaPlayback, _In_ SubtitleItemEnteredCallback fnEnteredCallback, _In_ SubtitleItemExitedCallback fnExitedCallback)
{
	NULL_CHK(spMediaPlayback);

	return spMediaPlayback->SetSubtitlesCallbacks(fnEnteredCallback, fnExitedCallback);
}

extern "C" HRESULT UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetSubtitlesTracksCount(_In_ IMediaPlayerPlayback* spMediaPlayback, _Out_ unsigned int* count)
{
	NULL_CHK(spMediaPlayback);
	NULL_CHK(count);

	return spMediaPlayback->GetSubtitlesTrackCount(count);
}


extern "C" HRESULT UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetSubtitlesTrack(_In_ IMediaPlayerPlayback* spMediaPlayback, _In_ unsigned int index, _Out_ const wchar_t** trackId, _Out_ const wchar_t** trackLabel, _Out_ const wchar_t** trackLanguage)
{
	NULL_CHK(spMediaPlayback);

	unsigned int count = 0;
	spMediaPlayback->GetSubtitlesTrackCount(&count);
	if (index >= count)
	{
		return E_INVALIDARG;
	}

	return spMediaPlayback->GetSubtitlesTrack(index, trackId, trackLabel, trackLanguage);
}

// --------------------------------------------------------------------------
// UnitySetInterfaces

// GraphicsDeviceEvent
static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
{
    // Create graphics API implementation upon initialization
    if (eventType == kUnityGfxDeviceEventInitialize)
    {
        s_DeviceType = s_Graphics->GetRenderer();
		s_Graphics = s_UnityInterfaces->Get<IUnityGraphics>();
		CMediaPlayerPlayback::GraphicsDeviceReady(s_UnityInterfaces);
    }

    // Cleanup graphics API implementation upon shutdown
    if (eventType == kUnityGfxDeviceEventShutdown)
    {
        s_DeviceType = kUnityGfxRenderernullptr;
		CMediaPlayerPlayback::GraphicsDeviceShutdown();
    }
}

extern "C" void	UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
{
    s_UnityInterfaces = unityInterfaces;
    s_Graphics = s_UnityInterfaces->Get<IUnityGraphics>();
    s_Graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);

    // Run OnGraphicsDeviceEvent(initialize) manually on plugin load
    OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
    s_Graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
}


// --------------------------------------------------------------------------
// SetTimeFromUnity, an example function we export which is called by one of the scripts.
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetTimeFromUnity(float t)
{ 
    g_Time = t;
}

// --------------------------------------------------------------------------
// OnRenderEvent
// This will be called for GL.IssuePluginEvent script calls; eventID will
// be the integer passed to IssuePluginEvent. 
static void UNITY_INTERFACE_API OnRenderEvent(int eventID)
{
	CMediaPlayerPlayback::UnityRenderEvent();
}


// --------------------------------------------------------------------------
// GetRenderEventFunc, a function we export which is used to get a rendering event callback function.
extern "C" UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetRenderEventFunc()
{
    return OnRenderEvent;
}

