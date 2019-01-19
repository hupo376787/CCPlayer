//*****************************************************************************
//
//	Copyright 2015 Microsoft Corporation
//
//	Licensed under the Apache License, Version 2.0 (the "License");
//	you may not use this file except in compliance with the License.
//	You may obtain a copy of the License at
//
//	http ://www.apache.org/licenses/LICENSE-2.0
//
//	Unless required by applicable law or agreed to in writing, software
//	distributed under the License is distributed on an "AS IS" BASIS,
//	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//	See the License for the specific language governing permissions and
//	limitations under the License.
//
//*****************************************************************************

#include "pch.h"

#include "ExternalSubtitleReader.h"
#include "Source\Provider\Subtitle\SubtitleProvider.h"

using namespace CCPlayer::UWP::FFmpeg::Subtitle;

ExternalSubtitleReader::ExternalSubtitleReader(AVFormatContext* avFormatCtx,
	CCPlayer::UWP::Common::Interface::ISubtitleDecoderConnector^ subtitleDecoderConnector) : FFmpegReader(avFormatCtx, nullptr, subtitleDecoderConnector, nullptr)
{
}

bool ExternalSubtitleReader::IsSupportedMediaType(AVMediaType mediaType)
{
	return (mediaType == AVMEDIA_TYPE_SUBTITLE);
}

ProviderList ExternalSubtitleReader::GetProviderList()
{
	return m_providerList;
}

int ExternalSubtitleReader::ReadPacket()
{
	AVPacket avPacket;
	SubtitleIndexInfo subtitleIndexInfo;
	SubtitleProvider* pSubtitleProvider = nullptr;
	MFSampleProvider* pSampleProvider = nullptr;
	int ret;
	bool bPreload = m_needTotalFrameCount > 0;

	//XSUB�� �ڸ��� �̸� ��� �ε� �ؾ� �ϴ� ��� Provider���� NeedFrameCount�� �����Ͽ� �ش� ���� ��ŭ �̸� �о� ����.
	while (m_needTotalFrameCount > 0)
	{
		av_init_packet(&avPacket);
		avPacket.data = NULL;
		avPacket.size = 0;

		if (av_read_frame(m_pAvFormatCtx, &avPacket) < 0) break;

		for (unsigned int i = 0; i < m_subtitleProviderIndexList.size(); i++)
		{
			subtitleIndexInfo = m_subtitleProviderIndexList.at(i);
			pSubtitleProvider = dynamic_cast<SubtitleProvider*>(m_providerList.at(subtitleIndexInfo.Index));
			if (pSubtitleProvider)
			{
				if (pSubtitleProvider->GetCurrentStreamIndex() == avPacket.stream_index)
				{
					pSubtitleProvider->PushPacket(avPacket);
					subtitleIndexInfo.NeedPreloadFrameCount--;
					m_needTotalFrameCount--;
				}
			}
		}
	}

	if (bPreload)
	{
		//��ġ�� ó������ �̵�
		av_seek_frame(m_pAvFormatCtx, -1, 0, 0);
		//�������� ��� �ε� ���߾ ��Ʈ���� ���� ���������Ƿ�, �ٽ� �ε� ���� �ʵ��� ó��
		m_needTotalFrameCount = 0;
		m_completedAddFrames = true;
	}

	av_init_packet(&avPacket);
	avPacket.data = NULL;
	avPacket.size = 0;

	ret = av_read_frame(m_pAvFormatCtx, &avPacket);

	if (ret < 0)
	{
		return ret;
	}

	for (unsigned int i = 0; i < m_providerList.size(); i++)
	{
		MFSampleProvider* pSp = m_providerList.at(i);
		if (avPacket.stream_index == pSp->GetCurrentStreamIndex())
		{
			pSampleProvider = pSp;
			break;
		}
	}

	if (pSampleProvider != nullptr)
	{
		pSampleProvider->PushPacket(avPacket);
	}
	else
	{
		av_packet_unref(&avPacket);
	}

	return ret;
}
