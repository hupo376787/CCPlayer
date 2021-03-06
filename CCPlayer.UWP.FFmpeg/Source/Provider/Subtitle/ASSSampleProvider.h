#pragma once

#include "Source\Provider\Subtitle\SubtitleProvider.h"
#include "Subtitle\Subtitle.h"

class ASSSampleProvider :
	public SubtitleProvider
{
public:
	ASSSampleProvider(
		FFmpegReader* reader,
		AVFormatContext* avFormatCtx,
		AVCodecContext* avCodecCtx,
		int streamIndex,
		int codePage);
	
	virtual void ConsumePacket(int index, int64_t pts, int64_t syncts);
	virtual void LoadHeader();

private:	
	PropertySet^ m_scriptInfoProp;
	Windows::Foundation::Collections::IMap<String^, Windows::Data::Json::JsonObject^>^ m_styleMap;
	std::vector<std::wstring> m_eventList;
};

