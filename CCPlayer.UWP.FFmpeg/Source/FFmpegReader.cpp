#include "pch.h"
#include "FFmpegReader.h"
#include "Source\Provider\MFSampleProvider.h"
#include "Source\Provider\Subtitle\SubtitleProvider.h"

using namespace CCPlayer::UWP::Common::Codec;

static void ffmpegLogCallback(void* ptr, int val, const char* msg, va_list ap)
{
	AVClass* avc = ptr ? *(AVClass**)ptr : NULL;

	if (ptr != NULL && avc != NULL)
	{
		//FILE* flog = fopen("Log_ffmpeg.log", "a");
		//fprintf(flog, "[%s @ %p] ", avc->item_name(ptr), avc);
		OutputDebugMessage(L"[%s @ %p] ", avc->item_name(ptr), avc);
		//vfprintf(flog, msg, ap);
		//fclose(flog);
	}
	std::string smsg(msg);
	std::wstring wmsg(smsg.begin(), smsg.end());

	OutputDebugMessage(wmsg.data(), ap);
}


FFmpegReader::FFmpegReader(AVFormatContext* avFormatCtx, 
	IAVDecoderConnector^ decoderConnector,
	ISubtitleDecoderConnector^ subtitleConnector,
	IAttachmentDecoderConnector^ attachmentConnector)
	: m_pAvFormatCtx(avFormatCtx)
	, m_avDecoderConnector(decoderConnector)
	, m_subtitleDecoderConnector(subtitleConnector)
	, m_attachmentDecoderConnector(attachmentConnector)
	, m_needTotalFrameCount(0)
	, m_completedAddFrames(false)
{
	//av_log_set_callback(ffmpegLogCallback);
}

FFmpegReader::~FFmpegReader()
{
	while (!m_providerList.empty())
	{
		MFSampleProvider* pSp = m_providerList.back();
		m_providerList.pop_back();

		delete pSp;
	}

	m_subtitleProviderIndexList.clear();
}

IAVDecoderConnector^ FFmpegReader::GetAVDecoderConnector()
{
	return m_avDecoderConnector;
}

ISubtitleDecoderConnector^ FFmpegReader::GetSubtitleDecoderConnector()
{
	return m_subtitleDecoderConnector;
}

IAttachmentDecoderConnector^ FFmpegReader::GetAttachmentDecoderConnector()
{
	return m_attachmentDecoderConnector;
}

bool FFmpegReader::IsSupportedMediaType(AVMediaType mediaType)
{
	return (mediaType == AVMEDIA_TYPE_VIDEO || mediaType == AVMEDIA_TYPE_AUDIO);
	//return mediaType == AVMEDIA_TYPE_VIDEO;
	//return mediaType == AVMEDIA_TYPE_AUDIO;
}

int FFmpegReader::ReadPacket()
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

	do
	{
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
			if (pSampleProvider->GetMediaType() == AVMediaType::AVMEDIA_TYPE_SUBTITLE)
			{
				if (!m_completedAddFrames)
				{
					//�ڸ� ť�� ����
					pSampleProvider->PushPacket(avPacket);
				}
			}
			else 
			{
				if (m_subtitleDecoderConnector != nullptr)
				{
					if (m_subtitleDecoderConnector->SourceType == SubtitleSourceTypes::External
						&& !m_subtitleDecoderConnector->IsSeeking)
					{
						int64_t pts = avPacket.pts;

						if (pts == AV_NOPTS_VALUE && avPacket.dts != AV_NOPTS_VALUE)
						{
							pts = avPacket.dts;
						}

						//����/���� ��Ŷ�� �ð�
						int64_t avPktTime = pSampleProvider->GetPts(pts);
						m_subtitleDecoderConnector->ConsumePacket(avPktTime);

					}
					else if (m_subtitleDecoderConnector->SourceType == SubtitleSourceTypes::Internal)
					{
						//����/����� ��Ʈ���� ��쿡 ���� �ڸ��� ���õǾ� ������ �ڸ� �̺�Ʈ�� �߻� ��Ŵ 
						// Use decoding timestamp if presentation timestamp is not valid
						int64_t pts = (avPacket.pts == AV_NOPTS_VALUE && avPacket.dts != AV_NOPTS_VALUE) ? avPacket.dts : avPacket.pts;

						for (unsigned int i = 0; i < m_subtitleProviderIndexList.size(); i++)
						{
							subtitleIndexInfo = m_subtitleProviderIndexList.at(i);
							pSubtitleProvider = dynamic_cast<SubtitleProvider*>(m_providerList.at(subtitleIndexInfo.Index));
							if (pSubtitleProvider)
							{
								int index = safe_cast<int>(m_subtitleDecoderConnector->ConnectedSource);
								pSubtitleProvider->ConsumePacket(index, pSubtitleProvider->GetPts(pts), 0);
							}
						}
					}
				}
				
				pSampleProvider->PushPacket(avPacket);
				break;
			}
		}
		else
		{
			av_packet_unref(&avPacket);
			break;
		}
	} while (true);

	return ret;
}

void FFmpegReader::AddStream(MFSampleProvider* pSampleProvider)
{
	if (pSampleProvider->GetMediaType() == AVMEDIA_TYPE_SUBTITLE)
	{
		SubtitleIndexInfo info = { 0 };
		info.Index = (int)m_providerList.size();
		info.StreamIndex = pSampleProvider->GetCurrentStreamIndex();
		SubtitleProvider* pSp = dynamic_cast<SubtitleProvider*>(pSampleProvider);
		if (pSp)
		{
			info.NeedPreloadFrameCount = pSp->NeedFrameCount;
			//XSUB�� ��� �ڸ��� �ε��ؾ߸� �ϴ� �ڸ��� ���ؼ� ǥ��
			m_needTotalFrameCount += pSp->NeedFrameCount;
		}

		m_subtitleProviderIndexList.push_back(info);
	}
	m_providerList.push_back(pSampleProvider);
}

HRESULT FFmpegReader::GetNextSample(int index, IMFSample** pSample)
{
	HRESULT hr = E_FAIL;
	
	for (unsigned int i = 0; i < m_providerList.size(); i++)
	{
		MFSampleProvider* pSp = m_providerList.at(i);
		if (pSp->GetCurrentStreamIndex() == index)
		{
			hr = pSp->GetNextSample(pSample);
			break;
		}
	}
	return hr;
}

HRESULT FFmpegReader::GetNextSampleTime(UINT64* time)
{
	HRESULT hr = E_FAIL;

	MFSampleProvider* pAudioSp = nullptr;
	MFSampleProvider* pVideoSp = nullptr;

	for (unsigned int i = 0; i < m_providerList.size(); i++)
	{
		MFSampleProvider* pSp = m_providerList.at(i);
		if (pSp->GetMediaType() == AVMEDIA_TYPE_VIDEO)
		{
			pVideoSp = pSp;
			//������ ���� ���� Ż��
			break;
		}
		else if (pSp->GetMediaType() == AVMEDIA_TYPE_AUDIO)
		{
			pAudioSp = pSp;
		}
	}

	if (pVideoSp != nullptr)
	{
		hr = pVideoSp->GetNextSampleTime(time);
	}
	else if (pAudioSp != nullptr)
	{
		hr = pAudioSp->GetNextSampleTime(time);
	}
	else
	{
		hr = E_FAIL;
	}

	return hr;
}

void FFmpegReader::Flush()
{
	for (unsigned int i = 0; i < m_providerList.size(); i++)
	{
		MFSampleProvider* pSp = m_providerList.at(i);
		pSp->Flush();
	}
}