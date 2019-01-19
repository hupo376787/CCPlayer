using CCPlayer.HWCodecs.Mp4;
using CCPlayer.WP81.Models;
using MediaParsers;
using MediaParsers.FlvParser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;

namespace CCPlayer.WP81.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public partial class CCPlayerViewModel
    {
        private byte[] NALUnitHeader;
        private byte[] h264StartCode = new byte[] { 0, 0, 1 };

        private void SetNalUnitParameterSets(FlvTag videoInfoFlvTag)
        {
            List<byte[]> sequenceParameterSets = new List<byte[]>();
            List<byte[]> pictureParameterSets = new List<byte[]>();

            //Video
            byte[] abyte = videoInfoFlvTag.VideoData.AVCVideoPacket.AVCDecoderConfigurationRecord;
            using (MemoryStream sm = new MemoryStream(abyte))
            {
                long offset = 0;
                uint configurationVersion = sm.ReadUInt8(ref offset);
                uint AVCProfileIndication = sm.ReadUInt8(ref offset);

                uint profile_compatibility = sm.ReadUInt8(ref offset);
                uint AVCLevelIndication = sm.ReadUInt8(ref offset);
                uint t1 = sm.ReadUInt8(ref offset);

                uint lengthSizeMinusOne = (t1 & 3);
                uint lengthSizeMinusOnePaddingBits = (t1 >> 2);

                uint t2 = sm.ReadUInt8(ref offset);

                uint numberOfSeuqenceParameterSets = (t2 & 7);
                uint numberOfSequenceParameterSetsPaddingBits = (t2 >> 5);

                for (int i = 0; i < numberOfSeuqenceParameterSets; i++)
                {
                    byte[] by = new byte[2];
                    sm.Read(by, 0, by.Length);

                    Array.Reverse(by);

                    int sequenceParameterSetLength = BitConverter.ToInt16(by, 0);
                    byte[] sequenceParameterSetNALUnit = new byte[sequenceParameterSetLength];
                    sm.Read(sequenceParameterSetNALUnit, 0, sequenceParameterSetLength);
                    sequenceParameterSets.Add(sequenceParameterSetNALUnit);
                }
                offset = sm.Position;
                uint numberOfPictureParameterSets = sm.ReadUInt8(ref offset);
                for (int i = 0; i < numberOfPictureParameterSets; i++)
                {
                    byte[] by = new byte[2];
                    sm.Read(by, 0, by.Length);
                    Array.Reverse(by);

                    int pictureParameterSetLength = BitConverter.ToInt16(by, 0);
                    byte[] pictureParameterSetNALUnit = new byte[pictureParameterSetLength];
                    sm.Read(pictureParameterSetNALUnit, 0, pictureParameterSetLength);
                    pictureParameterSets.Add(pictureParameterSetNALUnit);
                }

                //NAL Sequence Parameter Set, Picture Parameter Set
                using (MemoryStream ms = new MemoryStream())
                {
                    var sps = sequenceParameterSets[0];
                    var pps = pictureParameterSets[0];

                    ms.Write(h264StartCode, 0, h264StartCode.Length);
                    ms.Write(sps, 0, sps.Length);
                    ms.Write(h264StartCode, 0, h264StartCode.Length);
                    ms.Write(pps, 0, pps.Length);

                    NALUnitHeader = ms.ToArray();
                }
            }
        }

        IMediaStreamDescriptor GetFlvVideoDescriptor(List<FlvTag> scriptFlvTagList)
        {
            var key = scriptFlvTagList.FirstOrDefault().ScriptData.Values[1].Key;
            var value = scriptFlvTagList.FirstOrDefault().ScriptData.Values[1].Value;

            uint iWidth = UInt32.Parse((value as ScriptObject)["width"].ToString());
            uint iHeight = UInt32.Parse((value as ScriptObject)["height"].ToString());

            VideoEncodingProperties videoEncodingProperties = VideoEncodingProperties.CreateH264();
            VideoStreamDescriptor descriptor = new VideoStreamDescriptor(videoEncodingProperties);

            descriptor.EncodingProperties.Width = iWidth;
            descriptor.EncodingProperties.Height = iHeight;

            return descriptor;
        }

        IMediaStreamDescriptor GetFlvAudioDescriptor(FlvTag audioInfoFlvTag)
        {
            AudioEncodingProperties audioEncodingProperites = null;
            AudioStreamDescriptor audioStreamDescriptor = null;
            uint sampleRate = 0;
            uint channelCount = 0;
            uint bitRate = 0;

            switch (audioInfoFlvTag.AudioData.SoundRate)
            {
                case SoundRate._5kHz:
                    sampleRate = 5512;
                    break;
                case SoundRate._11kHz:
                    sampleRate = 11025;
                    break;
                case SoundRate._22kHz:
                    sampleRate = 22050;
                    break;
                case SoundRate._44kHz:
                    sampleRate = 44100;
                    break;
            }

            switch (audioInfoFlvTag.AudioData.SoundSize)
            {
                case SoundSize.snd8Bit:
                    bitRate = 8;
                    break;
                case SoundSize.snd16Bit:
                    bitRate = 16;
                    break;
            }


            switch (audioInfoFlvTag.AudioData.SoundType)
            {
                case SoundType.sndMono:
                    channelCount = 1;
                    break;
                case SoundType.sndStereo:
                    channelCount = 2;
                    break;
            }

            switch (audioInfoFlvTag.AudioData.SoundFormat)
            {
                case SoundFormat.AAC:
                    if ((audioInfoFlvTag.AudioData.SoundData as AACAudioData).AudioSpecificConfig != null)
                    {
                        var acfg = (audioInfoFlvTag.AudioData.SoundData as AACAudioData).AudioSpecificConfig;

                        int offset = 0;
                        var type = BitHelper.Read(acfg, ref offset, 5);
                        if (type == 31)
                        {
                            type = BitHelper.Read(acfg, ref offset, 6) + 32;
                        }
                        var feqIndex = BitHelper.Read(acfg, ref offset, 4);
                        if (feqIndex == 15)
                        {
                            sampleRate = (uint)BitHelper.Read(acfg, ref offset, 24);
                        }
                        else
                        {
                            sampleRate = Mp4AudioSpecificConfig.GetSamplingFrequency(feqIndex);
                        }
                        channelCount = (uint)BitHelper.Read(acfg, ref offset, 4);
                    }
                    audioEncodingProperites = AudioEncodingProperties.CreateAac(sampleRate, channelCount, bitRate);
                    break;
                case SoundFormat.ADPCM:
                    audioEncodingProperites = AudioEncodingProperties.CreatePcm(sampleRate, channelCount, bitRate);
                    break;
                case SoundFormat.MP3:
                    audioEncodingProperites = AudioEncodingProperties.CreateMp3(sampleRate, channelCount, bitRate);
                    break;
                default:
                    break;
            }

            audioStreamDescriptor = new AudioStreamDescriptor(audioEncodingProperites);
            return audioStreamDescriptor;
        }

        void PrepareSeekForFlv(FlvFile flvFile)
        {
            // seek data �ε�
            if (!flvFile.FlvFileBody.SeekData.IsScriptLoad)
            {
                var fsdDic = fileDAO.GetSeekingList(flvFile.Path);
                if (fsdDic != null)
                {
                    flvFile.FlvFileBody.SeekData = new FlvSeekData(fsdDic);
                }

                //��ũ �̺�Ʈ ���
                flvFile.FlvFileBody.SeekData.ProgressStarted += SeekData_ProgressStarted;
                flvFile.FlvFileBody.SeekData.ProgressChanged += SeekData_ProgressChanged;
                flvFile.FlvFileBody.SeekData.ProgressCompleted += SeekData_ProgressCompleted;
            }
        }

        void SeekData_ProgressStarted(object sender, EventArgs e)
        {
            ResourceLoader resource = ResourceLoader.GetForCurrentView();
            ShowLoadingBar(resource.GetString("Message/Info/Seek/Create"));
        }

        void SeekData_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ResourceLoader resource = ResourceLoader.GetForCurrentView();
            ShowLoadingBar(string.Format("{0} {1}%", resource.GetString("Message/Info/Seek/Create"), e.ProgressPercentage));
        }

        void SeekData_ProgressCompleted(object sender, EventArgs e)
        {
            HideLoadingBar();
            var flvFile = mediaStreamFileSource as FlvFile;
            if (!flvFile.FlvFileBody.SeekData.IsScriptLoad)
            {
                fileDAO.InsertSeekingData(flvFile.Path, flvFile.FlvFileBody.SeekData);
            }
        }

        private long SeekForFlv(FlvFile flvFile, long seekToTime)
        {
            //��ũ �����Ϳ� 2�� �̻� ���̰� ����ٸ� ��ũ �Ұ� ��Ȳ���� �Ǵ�??? ������ ����????
            if (Math.Abs(seekToTime - flvFile.FlvFileBody.SeekData.FindSeekFrame(seekToTime).Key) > TimeSpan.FromSeconds(2).Ticks)
            {
                //��ũ ������ �ε���...
                flvFile.FlvFileBody.SeekData.Make(flvFile.Stream, seekToTime);
            }

            return flvFile.FlvFileBody.SeekQueue(seekToTime);
        }

        bool ValidateCodecForFlv(FlvFile flvFile)
        {
            ResourceLoader loader = ResourceLoader.GetForCurrentView();
            DialogContent dc = new DialogContent();

            if (flvFile.FlvHeader.Signature != "FLV")
            {
                CurrentMediaInfo.MediaType = MediaType.Unkown;
                dc.Content = string.Format(loader.GetString("WrongFileFormat"), "FLV");
                dc.OccueredErrorMediaInfo = dc.Content;
            }

            if (flvFile.FlvFileBody.VideoInfoFlvTag.VideoData.CodecID != CodecID.AVC)
            {
                dc.Content = loader.GetString("NotSupportedVideoCodec");
                dc.Description1 = flvFile.FlvFileBody.VideoInfoFlvTag.VideoData.CodecID.ToString();
                dc.Description2 = string.Format(loader.GetString("NotSupportedCodecDesc"), "H264");
            }

            if (flvFile.FlvFileBody.AudioInfoFlvTag.AudioData.SoundFormat != SoundFormat.AAC
                && flvFile.FlvFileBody.AudioInfoFlvTag.AudioData.SoundFormat != SoundFormat.MP3
                && flvFile.FlvFileBody.AudioInfoFlvTag.AudioData.SoundFormat != SoundFormat.ADPCM)
            {
                dc.Content = loader.GetString("NotSupportedAudioCodec");
                dc.Description1 = flvFile.FlvFileBody.AudioInfoFlvTag.AudioData.SoundFormat.ToString();
                dc.Description2 = string.Format(loader.GetString("NotSupportedCodecDesc"), "AAC, MP3, PCM");
            }

            if (!string.IsNullOrEmpty(dc.Content))
            {
                //��� ����
                StopMedia();
                //ȭ�� �ݱ�
                if (IsPlayerOpened)
                {
                    IsPlayerOpened = false;
                }

                //���� �޼��� ó��
                if (string.IsNullOrEmpty(dc.OccueredErrorMediaInfo))
                {
                    dc.OccueredErrorMediaInfo = ResourceLoader.GetForCurrentView().GetString("Message/Error/CodecNotSupported");
                }
                //�ε� �г� ����
                HideLoadingBar();
                //���� �޼���
                ShowDialogMediaStreamSourceError(dc);
                return false;
            }
            return true;
        }

        async void OpenFlvFile(StorageFile file)
        {
            long offset = 0;
            FlvFile flvFile = null;
            var randomStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            mediaStreamFileSource = flvFile = new FlvFile(randomStream.AsStream(), ref offset) { Path = file.Path };

            //���� üũ (�ͼ��� �߻�)
            if (ValidateCodecForFlv(flvFile))
            {
                //�̵�� Ÿ�� ����
                CurrentMediaInfo.MediaType = MediaType.FlashVideo;

                //H264 NalUnit Header ����
                SetNalUnitParameterSets(flvFile.FlvFileBody.VideoInfoFlvTag);
                //����/����� ������ ��������� ����
                IMediaStreamDescriptor videoDescriptor = GetFlvVideoDescriptor(flvFile.FlvFileBody.ScriptTageList);
                IMediaStreamDescriptor audioDescriptor = GetFlvAudioDescriptor(flvFile.FlvFileBody.AudioInfoFlvTag);

                //�̵�� ��Ʈ�� �ҽ� ����
                MediaStreamSource flvStreamSource = new MediaStreamSource(videoDescriptor, audioDescriptor);

                //�⺻ �Ӽ� ����
                var value = flvFile.FlvFileBody.ScriptTageList.FirstOrDefault().ScriptData.Values[1].Value;
                flvStreamSource.Duration = TimeSpan.FromSeconds(double.Parse(((value as ScriptObject)["duration"]).ToString(), Settings.NumberFormat));
                flvStreamSource.CanSeek = true;

                //�̺�Ʈ ���
                flvStreamSource.VideoProperties.Title = CurrentMediaInfo.Title;
                flvStreamSource.Starting += flvStreamSource_Starting;
                flvStreamSource.SampleRequested += flvStreamSource_SampleRequested;
                flvStreamSource.Closed += flvStreamSource_Closed;

                //�÷��̾� �⺻ ���� �� ��Ʈ�� ����
                this.SetMediaStreamSource(flvStreamSource);

                //FVL Ž���� ������ �غ�
                PrepareSeekForFlv(flvFile);
            }
        }

        private void flvStreamSource_Starting(MediaStreamSource sender, MediaStreamSourceStartingEventArgs args)
        {
            MediaStreamSourceStartingRequest request = args.Request;

            if (request.StartPosition.HasValue && sender.CanSeek)
            {
                var deferal = request.GetDeferral();
                var flvFile = mediaStreamFileSource as FlvFile;

                if (flvFile != null)
                {
                    var newTime = SeekForFlv(flvFile, (long)(request.StartPosition.Value.TotalMilliseconds * 10000));
                    request.SetActualStartPosition(TimeSpan.FromTicks(newTime));
                }

                deferal.Complete();
            }
        }

        private void flvStreamSource_SampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
        {
            MediaStreamSourceSampleRequest request = args.Request;
            MediaStreamSourceSampleRequestDeferral deferal = request.GetDeferral();

            FlvFile flvFile = mediaStreamFileSource as FlvFile;
            FlvTag flvTag = null;
            MemoryStream stream = null;
            MediaStreamSample sample = null;

            try
            {
                if (flvFile != null)
                {
                    if (request.StreamDescriptor is VideoStreamDescriptor)
                    {
                        flvTag = flvFile.FlvFileBody.CurrentVideoTag;

                        if (flvTag.VideoData.CodecID == CodecID.AVC)
                        {
                            byte[] by = flvTag.VideoData.AVCVideoPacket.NALUs;

                            if (by != null && by.Length > 0)
                            {
                                MemoryStream srcStream = new MemoryStream(by);
                                stream = new MemoryStream();

                                if (flvTag.VideoData.FrameType == FrameType.Keyframe)
                                {
                                    if (NALUnitHeader != null)
                                    {
                                        stream.Write(NALUnitHeader, 0, NALUnitHeader.Length);
                                    }
                                }

                                using (BinaryReader reader = new BinaryReader(srcStream))
                                {
                                    var sampleSize = srcStream.Length;
                                    while (sampleSize > 4L)
                                    {
                                        var ui32 = reader.ReadUInt32();
                                        var count = OldSkool.swaplong(ui32);
                                        stream.Write(h264StartCode, 0, h264StartCode.Length);
                                        stream.Write(reader.ReadBytes((int)count), 0, (int)count);
                                        sampleSize -= 4 + (uint)count;
                                    }
                                }

                                if (stream != null && stream.Length > 0)
                                {
                                    IBuffer buffer = stream.ToArray().AsBuffer();
                                    stream.Position = 0;
                                    sample = MediaStreamSample.CreateFromBuffer(buffer, TimeSpan.FromTicks(flvTag.Timestamp));
                                    sample.KeyFrame = flvTag.VideoData.FrameType == FrameType.Keyframe;
                                }
                            }
                        }
                        else
                        {
                            IBuffer buffer = flvTag.VideoData.RawData.AsBuffer();
                            sample = MediaStreamSample.CreateFromBuffer(buffer, TimeSpan.FromTicks(flvTag.Timestamp));
                            sample.KeyFrame = flvTag.VideoData.FrameType == FrameType.Keyframe;
                        }
                    }
                    else
                    {
                        byte[] by = null;
                        flvTag = flvFile.FlvFileBody.CurrentAudioTag;

                        switch (flvTag.AudioData.SoundFormat)
                        {
                            case SoundFormat.AAC:
                                by = (flvTag.AudioData.SoundData as AACAudioData).RawAACFrameData;
                                break;
                            case SoundFormat.MP3:
                                by = flvTag.AudioData.SoundData.RawData;
                                break;
                            case SoundFormat.ADPCM:
                                by = flvTag.AudioData.SoundData.RawData;
                                break;
                        }


                        if (by != null && by.Length > 0)
                        {
                            stream = new MemoryStream(by);
                            IBuffer buffer = by.AsBuffer();

                            sample = MediaStreamSample.CreateFromBuffer(buffer, TimeSpan.FromTicks(flvTag.Timestamp));
                            sample.KeyFrame = true;
                            request.Sample = sample;
                        }
                    }
                }

                //���ú���
                request.Sample = sample;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("���ÿ��� " + e.Message);
                sender.NotifyError(MediaStreamSourceErrorStatus.DecodeError);
            }
            finally
            {
                if (deferal != null)
                {
                    deferal.Complete();
                }
            }
        }

        private void flvStreamSource_Closed(MediaStreamSource sender, MediaStreamSourceClosedEventArgs args)
        {
            sender.SampleRequested -= flvStreamSource_SampleRequested;
            sender.Starting -= flvStreamSource_Starting;
            sender.Closed -= flvStreamSource_Closed;
            sender = null;
        }
    }
}