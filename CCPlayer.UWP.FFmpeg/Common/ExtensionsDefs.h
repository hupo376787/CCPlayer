//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//// PARTICULAR PURPOSE.
////
//// Copyright (c) Microsoft Corporation. All rights reserved
#pragma once

#include <initguid.h>

#ifndef INITGUID
#define INITGUID
#endif

using namespace Platform;
using namespace Microsoft::WRL;

#define AUTO_DETECT_CODE_PAGE -1
#define AV_DEC_CONN "AVDecoderConnector"
#define SUB_DEC_CONN "SubtitleDecoderConnector"
#define ATC_DEC_CONN "AttachmentDecoderConnector"
#define FFMPEG_OPTION "FFmpegOptionProperties"

// {C1FC552A-B7B8-4DBB-8A93-5B918B2A082A}
DEFINE_GUID(MFVideoFormat_FFmpeg_SW,
	0xc1fc552a, 0xb7b8, 0x4dbb, 0x8a, 0x93, 0x5b, 0x91, 0x8b, 0x2a, 0x8, 0x2a);

// {6BAE7E7C-1560-4217-8636-71D18D67A9D2}
DEFINE_GUID(MFAudioFormat_FFmpeg_SW,
	0x6bae7e7c, 0x1560, 0x4217, 0x86, 0x36, 0x71, 0xd1, 0x8d, 0x67, 0xa9, 0xd2);

// {BC564D89-F1D5-461A-96B1-CD64F834D12A}
DEFINE_GUID(MF_MT_MY_FFMPEG_TIME_BASE, 
	0xbc564d89, 0xf1d5, 0x461a, 0x96, 0xb1, 0xcd, 0x64, 0xf8, 0x34, 0xd1, 0x2a);

// {6BAE7E7C-1560-4217-8636-71D18D67A9D2}
DEFINE_GUID(MF_MT_MY_FFMPEG_AUDIO_CHANNEL_LAYOUT,
	0x6bae7e7c, 0x1560, 0x4217, 0x86, 0x36, 0x71, 0xd1, 0x8d, 0x67, 0xa9, 0xd2);

// {3ABD274E-E645-499A-B728-E10C79EA5977}
DEFINE_GUID(MF_MT_MY_FFMPEG_SAMPLE_FMT,
	0x3abd274e, 0xe645, 0x499a, 0xb7, 0x28, 0xe1, 0xc, 0x79, 0xea, 0x59, 0x77);

// {887DB868-A28A-4C8A-AD4D-75AD04824DF4}
DEFINE_GUID(MF_MT_MY_FFMPEG_AVPACKET_PTS,
	0x887db868, 0xa28a, 0x4c8a, 0xad, 0x4d, 0x75, 0xad, 0x4, 0x82, 0x4d, 0xf4);

// {77AFDC93-345F-434B-836F-25F759038282}
DEFINE_GUID(MF_MT_MY_FFMPEG_AVPACKET_DTS,
	0x77afdc93, 0x345f, 0x434b, 0x83, 0x6f, 0x25, 0xf7, 0x59, 0x3, 0x82, 0x82);

// {21773D17-61B8-41DF-8176-F0669BEB8AE1}
DEFINE_GUID(MF_MT_MY_FFMPEG_AVPACKET_STREAM_INDEX,
	0x21773d17, 0x61b8, 0x41df, 0x81, 0x76, 0xf0, 0x66, 0x9b, 0xeb, 0x8a, 0xe1);

// {10FA40D2-47C2-4616-985F-E2514F027444}
DEFINE_GUID(MF_MT_MY_FFMPEG_AVPACKET_FLAGS,
	0x10fa40d2, 0x47c2, 0x4616, 0x98, 0x5f, 0xe2, 0x51, 0x4f, 0x2, 0x74, 0x44);

// {D3D1E35C-E23C-4523-9E45-CB4B1374A0BC}
DEFINE_GUID(MF_MT_MY_FFMPEG_AVPACKET_DURATION,
	0xd3d1e35c, 0xe23c, 0x4523, 0x9e, 0x45, 0xcb, 0x4b, 0x13, 0x74, 0xa0, 0xbc);

// {394BFAA3-9321-4E9B-91C4-E75A85295DB7}
DEFINE_GUID(MF_MT_MY_FFMPEG_AVPACKET_POS,
	0x394bfaa3, 0x9321, 0x4e9b, 0x91, 0xc4, 0xe7, 0x5a, 0x85, 0x29, 0x5d, 0xb7);

// {84586852-4E1A-4C6B-9CA8-880CD248DCC0}
DEFINE_GUID(MF_MT_MY_SAMPLE_SEQ ,
	0x84586852, 0x4e1a, 0x4c6b, 0x9c, 0xa8, 0x88, 0xc, 0xd2, 0x48, 0xdc, 0xc0);

// {CE377A3F-A287-49D5-89B3-1470E15CEE7C}
DEFINE_GUID(MF_MT_MY_FFMPEG_CODEC_PARAMETERS,
	0xce377a3f, 0xa287, 0x49d5, 0x89, 0xb3, 0x14, 0x70, 0xe1, 0x5c, 0xee, 0x7c);

// {7C3B0801-7D34-4AED-95E2-1BA9D4D713EC}
DEFINE_GUID(MF_MT_MY_FFMPEG_CODEC_CONTEXT,
	0x7c3b0801, 0x7d34, 0x4aed, 0x95, 0xe2, 0x1b, 0xa9, 0xd4, 0xd7, 0x13, 0xec);

// {5C9BFF5A-997A-4AC2-899B-AB4221E4D9E3}
DEFINE_GUID(MF_MT_MY_FFMPEG_CODEC_LICENSE,
	0x5c9bff5a, 0x997a, 0x4ac2, 0x89, 0x9b, 0xab, 0x42, 0x21, 0xe4, 0xd9, 0xe3);

//for DIVX\XVID\MJPG
DEFINE_GUID(MF_MT_AVI_AVG_FPS,
	0xc496f370, 0x2f8b, 0x4f51, 0xae, 0x46, 0x9c, 0xfc, 0x1b, 0xc8, 0x2a, 0x47);

DEFINE_GUID(MF_MT_AVI_FLAGS,
	0x24974215, 0x1b7b, 0x41e4, 0x86, 0x25, 0xac, 0x46, 0x9f, 0x2d, 0xed, 0xaa); 

DEFINE_GUID(MF_MT_ORIGINAL_4CC,
	0xd7be3fe0, 0x2bc7, 0x492d, 0xb8, 0x43, 0x61, 0xa1, 0x91, 0x9b, 0x70, 0xc3);

// Disable debug string output on non-debug build
#if _DEBUG
#define DebugMessage(x) OutputDebugString(x)
#else
#define DebugMessage(x)
#endif

inline void ThrowIfError(HRESULT hr)
{
    if (FAILED(hr))
    {
        throw ref new Platform::COMException(hr);
    }
}

inline void ThrowException(HRESULT hr)
{
    assert(FAILED(hr));
    throw ref new Platform::COMException(hr);
}

inline void OutputDebugMessage(const wchar_t *fmt, ...)
{
#if _DEBUG
	wchar_t buf[1024];
	va_list args;
	va_start(args, fmt);
	vswprintf_s(buf, fmt, args);
	va_end(args);
	OutputDebugStringW(buf);
#endif
}

inline String^ ToStringHat(const char* ch)
{
	if (ch == nullptr) return nullptr;

	//std::string* s_str = new std::string(ch);
	//std::wstring* wid_str = new std::wstring(s_str->begin(), s_str->end());
	
	std::string s_str(ch);
	std::wstring wid_str(s_str.begin(), s_str.end());

	return ref new String(wid_str.c_str());
}

inline String^ ToStringHat(const char* ch, int length, UINT codePage)
{
	if (ch == nullptr) return nullptr;

	Platform::String^ szOutput;
	WCHAR* output = NULL;
	int cchRequiredSize = 0;
	unsigned int cchActualSize = 0;

	cchRequiredSize = MultiByteToWideChar(codePage, 0, ch, length, output, cchRequiredSize); // determine required buffer size
	//cchRequiredSize = MultiByteToWideChar(codePage, 0, ch, -1, output, 0); // determine required buffer size

	output = (WCHAR*)HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, (cchRequiredSize + 1) * sizeof(wchar_t)); // fix: add 1 to required size and zero memory on alloc
	cchActualSize = MultiByteToWideChar(codePage, 0, ch, length, output, cchRequiredSize);

	if (cchActualSize > 0)
	{
		szOutput = ref new Platform::String(output);
#if _DEBUG
		//OutputDebugStringW(L"문자열 변환됨");
		//OutputDebugStringW(output);
#endif
	}
	else
	{
		szOutput = ToStringHat(ch);
		DWORD errorCode = GetLastError();
#if _DEBUG
		//OutputDebugStringW(L"문자열 변환 안됨");
#endif
	}
	HeapFree(GetProcessHeap(), 0, output);  // fix: release buffer reference to fix memory leak.

	return szOutput;
}

inline String^ ToStringHat(const char* ch, UINT codePage)
{
	if (ch == nullptr) return nullptr;

	Platform::String^ szOutput;
	WCHAR* output = NULL;
	int cchRequiredSize = 0;
	unsigned int cchActualSize = 0;

	//int length = strnlen_s(ch, size);
	int length = strlen(ch);
	return ToStringHat(ch, length, codePage);

	return szOutput;
}

inline Platform::Array<byte>^ GetStringBytes(Platform::String^ input, int codePage)
{
	int len = WideCharToMultiByte(codePage, 0, input->Data(), -1, NULL, 0, NULL, NULL);
	Platform::Array<byte>^ szOutput = ref new Platform::Array<byte>(len + 1);
	WideCharToMultiByte(codePage, 0, input->Data(), -1, (char*)szOutput->Data, len + 1, NULL, NULL);

	return szOutput;
}

inline bool IsOmegaVersion()
{
	Windows::ApplicationModel::PackageId^ packageId = Windows::ApplicationModel::Package::Current->Id;

	//DebugMessage(packageId->FamilyName->Data());

	if (packageId->FamilyName == "D3DB5ACE.CCPlayerOmega_gt1c0ekgxeeqr")
	{
		return true;
	}
	return false;
}

inline void CheckPlatform()
{
	Windows::ApplicationModel::PackageId^ packageId = Windows::ApplicationModel::Package::Current->Id;

	//DebugMessage(packageId->FamilyName->Data());

	if (!(packageId->FamilyName == "D3DB5ACE.CCPlayerPro_gt1c0ekgxeeqr"
		|| packageId->FamilyName == "D3DB5ACE.CCPlayer_gt1c0ekgxeeqr"
		|| packageId->FamilyName == "D3DB5ACE.CCPlayerOmega_gt1c0ekgxeeqr"
		|| packageId->FamilyName == "D3DB5ACE.CCPlayerUWPAd_gt1c0ekgxeeqr"
		
		//|| packageId->FamilyName == "542b7e5d-93d9-437c-9396-40290e205fca_b5ntggqe1ba04" //지워야함
		//|| packageId->FamilyName == "13560100-22bd-4a66-9638-137b9d73b5a4_b5ntggqe1ba04" //지워야함
		
		))
	{
		throw ref new Platform::Exception(MF_NOT_SUPPORTED_ERR);
	}
}



