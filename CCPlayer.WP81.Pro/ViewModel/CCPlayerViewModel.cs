using CCPlayer.UI.Xaml.Controls.WP81;
using CCPlayer.HWCodecs.Text.Models;
using CCPlayer.WP81.Helpers;
using CCPlayer.WP81.Managers;
using CCPlayer.WP81.Models;
using CCPlayer.WP81.Models.DataAccess;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Lime.Models;
using Lime.Xaml.Controls;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Media.Core;
using Windows.Phone.UI.Input;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.System.Display;
using Windows.System.Threading;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using CCPlayer.HWCodecs.Text.Parsers;
using Lime.Common.Helpers;
using Windows.UI.Xaml.Documents;
using Windows.UI.Text;
using FFmpegSupport;
using System.Reflection;
using System.Globalization;
using Lime.Helpers;
using System.Text;

namespace CCPlayer.WP81.ViewModel
{
    internal class PagePreviousStatus
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsStatusBar { get; set; }
    }

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
    public partial class CCPlayerViewModel : ViewModelBase, IFileOpenPickerContinuable
    {
        public static readonly string NAME = typeof(CCPlayerViewModel).Name;
        private bool visibleLoadingBar;
        private bool supportedRotationLock;
        private bool isPausedByFlip;
        private Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation deviceInfo;
        private UInt32 cntAttachment;
        private MediaElementState previousState;
        private int tryAudioStreamIndex;

        #region ������ ��

        public Settings Settings { get; private set; }
        private FileDAO fileDAO;
        private SettingDAO settingDAO;
        private Windows.Media.MediaExtensionManager extMgr;

        private SimpleOrientationSensor orientationSenser;
        private PagePreviousStatus PagePreviousStatus;
        private MediaElementWrapper Me;
        private SubtitleSupport ffmpegSubtitleSupport;
        private AttachmentSupport ffmpegAttachmentSupport;

        private bool requestedMoveToPlaylist;

        private MediaInfo _CurrentMediaInfo;
        public MediaInfo CurrentMediaInfo
        {
            get
            {
                return _CurrentMediaInfo;
            }
            set
            {
                if (Set(ref _CurrentMediaInfo, value))
                {
                    MessengerInstance.Send<Message>(new Message("MediaFileChanged", value), TransportControlViewModel.NAME);
                }
            }
        }

        private bool _IsFullWindow;
        public bool IsFullWindow 
        { 
            get
            {
                return _IsFullWindow;
            }
            set
            {
                if (Set(ref _IsFullWindow, value))
                {
                    if (Me != null)
                    {
                        Me.IsFullWindow = value;
                        if (value)
                        {
                            // When displaying in full-window mode, center transport controls at the bottom of the window
                            //���簪 ���
                            PagePreviousStatus.Width = this.Width;
                            PagePreviousStatus.Height = this.Height;
                            //������ �ִ�ȭ
                            var bounds = Window.Current.Bounds;
                            this.Width = bounds.Width;
                            this.Height = bounds.Height;

                            //��� ���¹��� ȣ�� ǥ�ÿ��� ����
                            if (StatusBar.GetForCurrentView().OccludedRect.Width > 0 || StatusBar.GetForCurrentView().OccludedRect.Height > 0)
                            {
                                PagePreviousStatus.IsStatusBar = true;
                                DispatcherHelper.CheckBeginInvokeOnUI(async () => { await StatusBar.GetForCurrentView().HideAsync(); });
                            }
                        }
                        else
                        {
                            //������ ����
                            this.Width = PagePreviousStatus.Width;
                            this.Height = PagePreviousStatus.Height;

                            //��� ���¹� ����
                            if (PagePreviousStatus.IsStatusBar)
                            {
                                DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                                {
                                    try
                                    {
                                        await StatusBar.GetForCurrentView().ShowAsync();
                                    }
                                    catch (Exception) { }
                                });
                            }
                        }
                    }
                }
            }
        }

        private List<PickerItem<string, string>> _FontList;
        public List<PickerItem<string, string>> FontList
        {
            get
            {
                return _FontList;
            }
            set
            {
                Set(ref _FontList, value);
            }
        }

        private double _Width;
        public double Width
        {
            get
            {
                return _Width;
            }
            set
            {
                Set(ref _Width, value);
            }
        }

        private double _Height;
        public double Height
        {
            get
            {
                return _Height;
            }
            set
            {
                Set(ref _Height, value);
            }
        }

        //���� ��� ���� üũ ������Ƽ
        private bool _IsPlayerOpened;
        public bool IsPlayerOpened
        {
            get
            {
                return _IsPlayerOpened;
            }
            set
            {
                Set(ref _IsPlayerOpened, value, true);
            }
        }

        private bool _IsSubtitleOn;
        public bool IsSubtitleOn
        {
            get
            {
                return _IsSubtitleOn;
            }
            set
            {
                Set(ref _IsSubtitleOn, value);
            }
        }

        private bool _IsSubtitleSettingsOn;
        public bool IsSubtitleSettingsOn
        {
            get
            {
                return _IsSubtitleSettingsOn;
            }
            set
            {
                Set(ref _IsSubtitleSettingsOn, value);
            }
        }

        private string _SubtitleText;
        public string SubtitleText
        {
            get
            {
                return _SubtitleText;
            }
            set
            {
                Set(ref _SubtitleText, value);
            }
        }

        private bool _IsSubtitleMoveOn;
        public bool IsSubtitleMoveOn
        {
            get
            {
                return _IsSubtitleMoveOn;
            }
            set
            {
                Set(ref _IsSubtitleMoveOn, value);
            }
        }

        #endregion

        #region Ŀ�ǵ�

        public ICommand LoadedCommand { get; private set; }
        public ICommand MediaOpenedCommand { get; private set; }
        public ICommand MediaEndedCommand { get; private set; }
        public ICommand MediaFailedCommand { get; private set; }
        public ICommand CurrentStateChangedCommand { get; private set;}
        public ICommand SeekCompletedCommand { get; private set; }
        public ICommand MarkerReachedCommand { get; private set; }
        public ICommand SubtitlePositionManipulationDeltaCommand { get; private set; }
        public ICommand SubtitlePositionManipulationCompletedCommand { get; private set; }
        public ICommand TappedCommand { get; private set; }

        #endregion

        #region Ŀ�ǵ� �ڵ鷯
        
        private async void MediaOpenedCommandExecute(RoutedEventArgs args)
        {
//            System.Diagnostics.Debug.WriteLine("����");

            //������ �ε� �г� �ݱ�
            HideLoadingBar();
            //��ü ȭ�� ��� ����
            ShowPlayer();

            MessengerInstance.Send(new Message("MediaOpend", Me), TransportControlViewModel.NAME);
            
            //��Ʈ �ε�
            FontList = await FontHelper.GetAllFont();
            //�ڸ� �ʱ�ȭ
            SubtitleText = string.Empty;
        }

        private void MediaEndedCommandCommandExecute(RoutedEventArgs obj)
        {
            //����� ��� ���������� �����ϸ� �������� ���
            if (CurrentMediaInfo.NextMediaInfo != null)
            {
                if (!Settings.Playback.RemoveCompletedVideo)
                {
                    //������ ��ġ ����
                    SaveLastPosition();
                }
                else
                {
                    MessengerInstance.Send(new Message("RemovePlayList", CurrentMediaInfo), PlaylistViewModel.NAME);
                }
                //�̵�� ����
                StopMedia();

                if (Settings.Playback.UseConfirmNextPlay)
                {
                    try
                    {
                        if (App.ContentDlgOp != null) return;
                        
                        ContentDialog contentDlg = new ContentDialog
                        {
                            Content = new TextBlock()
                            {
                                Text = ResourceLoader.GetForCurrentView().GetString("Message/Play/Next"),
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Thickness(0, 12, 0, 12)
                            },
                            PrimaryButtonText = ResourceLoader.GetForCurrentView().GetString("Ok"),
                            SecondaryButtonText = ResourceLoader.GetForCurrentView().GetString("Cancel")
                        };

                        //�޼��� â ���
                        App.ContentDlgOp = contentDlg.ShowAsync();
                        App.ContentDlgOp.Completed = new AsyncOperationCompletedHandler<ContentDialogResult>(async (op, status) =>
                        {
                            var result = await op;
                            if (result == ContentDialogResult.Primary)
                            {
                                //���� ��� ��û
                                MessengerInstance.Send(new Message("PlayItem", CurrentMediaInfo.NextMediaInfo), PlaylistViewModel.NAME);
                            }
                            else
                            {
                                //���ȭ�� �ݱ�
                                HidePlayer();
                            }

                            App.ContentDlgOp = null;
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                    }
                }
                else
                {
                    //���� ��� ��û
                    MessengerInstance.Send(new Message("NextPlay", CurrentMediaInfo.NextMediaInfo), TransportControlViewModel.NAME);              
                }
            }
            else
            {
                if (!Settings.Playback.RemoveCompletedVideo)
                {
                    //������ ����ó��
                    SaveData();
                }
                else
                {
                    MessengerInstance.Send(new Message("RemovePlayList", CurrentMediaInfo), PlaylistViewModel.NAME);
                }
                //�̵�� ����
                StopMedia();
                //���ȭ�� �ݱ�
                HidePlayer();
            }
        }
       
        private async void MediaFailedCommandExecute(RoutedEventArgs e)
        {
            ResourceLoader resource = ResourceLoader.GetForCurrentView();
            var errCode = 0;
            var errMsg = "99";
                
            if (e is ExceptionRoutedEventArgs)
            {
                //�̵�� ������Ʈ�� ���
                var exArgs = e as ExceptionRoutedEventArgs;
                if (exArgs.ErrorMessage.Contains("MF_MEDIA_ENGINE_ERR_NOERROR")) errCode = 0;
                else if (exArgs.ErrorMessage.Contains("MF_MEDIA_ENGINE_ERR_ABORTED")) errCode = 1;
                else if (exArgs.ErrorMessage.Contains("MF_MEDIA_ENGINE_ERR_NETWORK")) errCode = 2;
                else if (exArgs.ErrorMessage.Contains("MF_MEDIA_ENGINE_ERR_DECODE")) errCode = 3;
                else if (exArgs.ErrorMessage.Contains("MF_MEDIA_ENGINE_ERR_SRC_NOT_SUPPORTED")) errCode = 4;
                else if (exArgs.ErrorMessage.Contains("MF_MEDIA_ENGINE_ERR_ENCRYPTED")) errCode = 5;
            }
            else
            {
                //MF������Ʈ�� ���
                errCode = Me.MediaErrorCode;
            }

            //���� �ڵ尡 �����ϴ� ���
            if (errCode >= 1 && errCode <= 5) 
            {
                errMsg = string.Format("0{0}", errCode);
            }
            //���� �޼��� ����
            errMsg = resource.GetString(string.Format("Message/Error/MFEngine{0}", errMsg));
            StackPanel contentPanel = new StackPanel();

            DecoderSupport decoderSupport = DecoderSupport.Instance();
            DecoderChangePayload payload = decoderSupport.Payload;
            //������ ���ڴ� Ÿ��
            var reqDecoderType = payload.ReqDecoderType;
            
            if (errCode == 3)
            {
                if (payload.Status == DecoderChangeStatus.Succeeded
                    && Me.CurrentState == MediaElementState.Closed
                    && Me.Position.TotalMilliseconds == 0
                    && reqDecoderType == DecoderType.MIX
                    && payload.ResDecoderType == DecoderType.MIX)
                {
                    MessengerInstance.Send(new Message("DecoderChangingFailed", reqDecoderType), TransportControlViewModel.NAME);
                    //������ ���ڴ��� ��Ͽ��� ����
                    decoderSupport.DecoderTypes.Remove(reqDecoderType);
                    //Mix ���� ���������� ������ �Ǿ�����, �ϵ���� �ڵ����� ���ڵ��� �����Ѱ�� SW�ڵ����� ����
                    OpenSupportedMediaFile(CurrentMediaInfo, true, DecoderType.SW);
                    return;
                }
            }
            else if (errCode == 4)
            {
                if ((payload.Status == DecoderChangeStatus.CheckError) 
                    || (payload.Status == DecoderChangeStatus.Requested && reqDecoderType == DecoderType.HW && payload.ResDecoderType == DecoderType.HW))
                {
                    MessengerInstance.Send(new Message("DecoderChangingFailed", reqDecoderType), TransportControlViewModel.NAME);
                    //������ ���ڴ��� ��Ͽ��� ����
                    decoderSupport.DecoderTypes.Remove(reqDecoderType);
                    //HW�ڵ��� ��û������ ������ ���, MIX�ڵ� ���� �ٽ� �����ϰ� ���� �޼��� ���
                    OpenSupportedMediaFile(CurrentMediaInfo, true, DecoderType.MIX);
                    return;
                }
                else if (payload.Status == DecoderChangeStatus.Requested && reqDecoderType == DecoderType.AUTO && payload.ResDecoderType == DecoderType.AUTO)
                {
                    //����Ʈ ��Ʈ�� �ڵ鷯�� �ش� Ÿ���� ���� ��Ų��.
                    OpenSupportedMediaFile(CurrentMediaInfo, true, DecoderType.AUTO, true);
                    return;
                }
                else if (payload.Status == DecoderChangeStatus.Succeeded && reqDecoderType != DecoderType.SW && payload.ResDecoderType != DecoderType.SW)
                {
                    MessengerInstance.Send(new Message("DecoderChangingFailed", reqDecoderType), TransportControlViewModel.NAME);
                    //������ ���ڴ��� ��Ͽ��� ����
                    decoderSupport.DecoderTypes.Remove(reqDecoderType);
                    //SW�ڵ��� �ƴ� �ڵ� ���� �����Ͽ� ������ ���, SW�ڵ� ���� �ٽ� ����
                    OpenSupportedMediaFile(CurrentMediaInfo, true, DecoderType.SW);
                    return;
                }
                
                int audioCount = decoderSupport.StreamInformationList.Count(x => x.CodecType == 1); //AVMediaType::AVMEDIA_TYPE_AUDIO
                if (audioCount > 1 && tryAudioStreamIndex == -1)
                {
                    //���� �޼��� 
                    var content = new TextBlock()
                    {
                        Text = resource.GetString("Message/Confirm/Source/SelectAudio"),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 12, 0, 12)
                    };
                    contentPanel.Children.Add(content);

                    foreach (var streamInfo in decoderSupport.StreamInformationList)
                    {
                        if (streamInfo.CodecType == 1 && streamInfo.CodecId > 0)
                        {
                            CultureInfo cultureInfo = null;
                            string lang = streamInfo.Language;

                            if (!string.IsNullOrEmpty(lang))
                            {
                                if (lang.Length == 2 || (lang.Length == 5 && lang.ElementAt(2) == '-'))
                                {
                                    cultureInfo = new CultureInfo(lang);
                                }
                                else if (lang.Length == 3)
                                {
                                    cultureInfo = LanguageCodeHelper.LangCodeToCultureInfo(lang);
                                }

                                if (cultureInfo != null && !string.IsNullOrEmpty(cultureInfo.NativeName))
                                {
                                    streamInfo.Language = cultureInfo.NativeName;
                                }
                            }

                            StackPanel innerPanel = new StackPanel()
                            {
                                Orientation = Windows.UI.Xaml.Controls.Orientation.Horizontal,
                                HorizontalAlignment = HorizontalAlignment.Left
                            };

                            var current = new TextBlock()
                            {
                                Text = string.Format(" - {0} {1}", streamInfo.Language, streamInfo.Title),
                                FontSize = (double)App.Current.Resources["TextStyleMediumFontSize"],
                                HorizontalAlignment = HorizontalAlignment.Left
                            };
                            innerPanel.Children.Add(new RadioButton()
                            {
                                IsChecked = streamInfo.IsBestStream,
                                HorizontalAlignment = HorizontalAlignment.Left,
                                Content = current,
                                GroupName = "audio",
                                Tag = streamInfo
                            });
                            contentPanel.Children.Add(innerPanel);
                        }
                    }

                    //���� ���̾�α� ǥ��
                    MessengerInstance.Send(new Message("ShowSelectionAudioMessage", contentPanel), MainViewModel.NAME);
                    return;
                }
                else
                {
                    //���� ������ ��� �����Ǵ� �ڵ��� �޼��� â�� ���
                    //���� �޼��� 
                    var content = new TextBlock()
                    {
                        Text = errMsg,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 12, 0, 12)
                    };
                    //������ ���� �гο� �߰�
                    contentPanel.Children.Add(content);

                    foreach (var streamInfo in decoderSupport.StreamInformationList)
                    {
                        if (streamInfo.CodecId > 0)
                        {
                            //��õ��� ����� ��Ƽ���� ���, �ش� ����� ��Ʈ���� �ƴϸ� ��ŵ ó��
                            if (tryAudioStreamIndex != -1 && streamInfo.CodecType == 1 && tryAudioStreamIndex != streamInfo.StreamId)
                            {
                                continue;
                            }

                            var current = new TextBlock()
                            {
                                Text = string.Format(" - {0} {1}", streamInfo.CodecLongName, string.IsNullOrEmpty(streamInfo.CodecProfileName) ? string.Empty : streamInfo.CodecProfileName) ,
                                FontSize = (double)App.Current.Resources["TextStyleMediumFontSize"]
                            };
                            contentPanel.Children.Add(current);
                            if (streamInfo.CodecType == 0) //����
                            {
                                var detail = new TextBlock()
                                {
                                    Text = string.Format("      {0} x {1} {2}Fps", streamInfo.Width, streamInfo.Height, streamInfo.Fps),
                                    FontSize = (double)App.Current.Resources["TextStyleMediumFontSize"]
                                };
                                contentPanel.Children.Add(detail);
                            }
                            else if (streamInfo.CodecType == 1) //�����
                            {
                                var detail = new TextBlock()
                                {
                                    Text = string.Format("      {0}Hz  {1}Ch {2}Bit", streamInfo.SampleRate, streamInfo.Channels, streamInfo.Bps),
                                    FontSize = (double)App.Current.Resources["TextStyleMediumFontSize"],
                                    Margin = new Thickness(0, 0, 0, 12)
                                };
                                contentPanel.Children.Add(detail);
                            }
                        
                        }
                    }

                    //�����Ǵ� �ڵ� ��ũ
                    var hyperLink = new HyperlinkButton()
                    {
                        NavigateUri = new Uri("http://msdn.microsoft.com/library/windows/apps/ff462087(v=vs.105).aspx"),
                        Margin = new Thickness(6, 0, 0, 0)
                    };
                    var linkTitle = new TextBlock()
                    {
                        Margin = new Thickness(0, 12, 0, 0)
                    };
                    var underLine = new Underline();
                    var linkText = new Run()
                    {
                        Text = resource.GetString("Supported/Media/Format"),
                        FontWeight = FontWeights.Bold,
                        FontSize = (double)App.Current.Resources["TextStyleMediumFontSize"]
                    };
                    hyperLink.Content = linkTitle;
                    linkTitle.Inlines.Add(underLine);
                    underLine.Inlines.Add(linkText);
                    contentPanel.Children.Add(hyperLink);

                    await ThreadPool.RunAsync(handler =>
                    {
                        //����Ʈ�� ���Ͼ����ۿ� ���� ǥ��
                        var msg = new Message("ShowErrorFile", new KeyValuePair<string, MediaInfo>(ResourceLoader.GetForCurrentView().GetString("Message/Error/CodecNotSupported"), CurrentMediaInfo));
                        MessengerInstance.Send(msg, ExplorerViewModel.NAME);
                        MessengerInstance.Send(msg, AllVideoViewModel.NAME);
                        MessengerInstance.Send(msg, PlaylistViewModel.NAME);
                    });
                }
            }
            else if (App.ContentDlgOp == null)
            {
                //��Ÿ �ٸ� ������ ��� �޼��� ���
                contentPanel.Children.Add(new TextBlock
                {
                    Text = errMsg,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(6, 0, 0, 12),
                });
            }
            //���� ���̾�α� ǥ��
            MessengerInstance.Send(new Message("ShowErrorMessage", contentPanel), MainViewModel.NAME);
        }

        private void CurrentStateChangedCommandExecute(RoutedEventArgs obj)
        {
            switch (Me.CurrentState)
            {
                case MediaElementState.Opening:
                case MediaElementState.Closed:
                case MediaElementState.Stopped:
                    //�ڸ� �ʱ�ȭ
                    SubtitleText = string.Empty;
                    break;
            }

//            System.Diagnostics.Debug.WriteLine(Me.CurrentState);
            MessengerInstance.Send<Message>(new Message("CurrentStateChanged", Me.CurrentState), TransportControlViewModel.NAME);
        }

        private void SeekCompletedCommandExecute(RoutedEventArgs obj)
        {
            SubtitleText = string.Empty;
        }

        private void MarkerReachedCommandExecute(TimelineMarkerRoutedEventArgs args)
        {
            var transportVm = Microsoft.Practices.ServiceLocation.ServiceLocator.Current.GetInstance<TransportControlViewModel>();
            if (transportVm != null)
            {
                if (transportVm.SubtitleCompensationData.ContainsKey(args.Marker.Time))
                {
                    var subList = transportVm.SubtitleCompensationData[args.Marker.Time];
                    transportVm.SubtitleCompensationData.Remove(args.Marker.Time);
                    if (subList.Count > 0)
                    {
                        var line = subList.Aggregate((x, y) => 
                        {
                            StringBuilder tmp = new StringBuilder();
                            
                            if (!string.IsNullOrEmpty(x))
                            {
                                tmp.Append(x.Trim());
                            }

                            if (!string.IsNullOrEmpty(y))
                            {
                                if (tmp.Length > 0)
                                {
                                    tmp.Append("<br/>");
                                }

                                tmp.Append(y.Trim());
                            }
                            //return x + "<br/>" + y;
                            return tmp.ToString();
                        });
                        //args.Marker.Text += "<br/>" + line;
                        
                        if (!string.IsNullOrEmpty(line))
                        {
                            args.Marker.Text += "<br/>" + line;
                        }
                    }
                }
            }
            
            if (SubtitleText != args.Marker.Text)
            {
                //System.Diagnostics.Debug.WriteLine(SubtitleText + "   |   " + args.Marker.Text + "|" + args.Marker.Time + " =====>" + Me.Position);
                SubtitleText = args.Marker.Text;
            }
        }

        private void LoadedCommandExecute(MediaElementWrapper me)
        {
            Me = me;

            var task = ThreadPool.RunAsync(async handler =>
            {
                //��Ʈ �ε�
                await DispatcherHelper.RunAsync(async () => { FontList = await FontHelper.GetAllFont(); });
            });
        }

        private void SubtitlePositionManipulationDeltaCommandExecute(ManipulationDeltaRoutedEventArgs args)
        {
            if (args.Container is Panel)
            {
                var child = args.Container as FrameworkElement;
                var panel = child.Parent as Panel;
                SetSubtitlePosition(panel, child, args.Delta.Translation.Y);
            }
        }

        private void SetSubtitlePosition(Panel parent, FrameworkElement child, double translationY)
        {
            //Ʈ������
            var childTransform = child.RenderTransform as CompositeTransform;
            //�̵�
            childTransform.TranslateY += translationY;
            //�̵��� ��ġ�� �������� ��ġ���� ����
            var rectDiff = child.TransformToVisual(parent).TransformPoint(new Point());
            //��ġ�� ���� ó��
            if (rectDiff.Y < parent.ActualHeight / 2)
            {
                //����
                if (child.VerticalAlignment != VerticalAlignment.Top)
                {
                    childTransform.TranslateY = parent.ActualHeight + childTransform.TranslateY - child.ActualHeight;
                    Settings.Subtitle.VerticalAlignment = VerticalAlignment.Top.ToString();
                }
                //������� �Ѿ�� ��ܿ� ����
                if (rectDiff.Y < 0)
                {
                    childTransform.TranslateY = 0;
                }
            }
            else if (rectDiff.Y == parent.ActualHeight / 2)
            {
                //���
                Settings.Subtitle.VerticalAlignment = VerticalAlignment.Center.ToString();
                childTransform.TranslateY = 0;
            }
            else
            {
                //�Ʒ���
                if (child.VerticalAlignment != VerticalAlignment.Bottom)
                {
                    childTransform.TranslateY = childTransform.TranslateY - parent.ActualHeight + child.ActualHeight;
                    Settings.Subtitle.VerticalAlignment = VerticalAlignment.Bottom.ToString();
                }
                //�ϴ��� �Ѿ�� ����
                if (rectDiff.Y + child.ActualHeight > parent.ActualHeight)
                {
                    childTransform.TranslateY = 0;
                }
            }
            //���� �ؽ�Ʈ ��ü�� ���������� �θ� �����. (�׷��� ������ �θ� �ణ ��Ʋ �Ÿ��� ����/�Ʒ������� �ٴ� ������ ����)
            if (child is Panel)
            {
                var panel = child as Panel;
                var html = panel.Children.FirstOrDefault(x => x is HtmlTextBlock) as HtmlTextBlock;
                if (html != null)
                {
                    html.VerticalAlignment = child.VerticalAlignment;
                }
            }
        }

        private void SubtitlePositionManipulationCompletedCommandExecute(ManipulationCompletedRoutedEventArgs args)
        {
            if (args.Container is Panel)
            {
                var child = args.Container as FrameworkElement;
                var childTransform = child.RenderTransform as CompositeTransform;

                //DB������Ʈ
                settingDAO.Replace(Settings);
            }
        }

        private void TappedCommandExecute(RoutedEventArgs args)
        {
            if (IsSubtitleMoveOn && args.OriginalSource is Grid)
            {
                IsSubtitleMoveOn = false;
                MessengerInstance.Send(new Message("SubtitleMoved", true), TransportControlViewModel.NAME);
            }
        }

        #endregion

        public CCPlayerViewModel(FileDAO fileDAO, SettingDAO settingDAO, Windows.Media.MediaExtensionManager extMgr)
        {
            this.tryAudioStreamIndex = -1;
            this.fileDAO = fileDAO;
            this.settingDAO = settingDAO;
            this.Settings = settingDAO.SettingCache;

            //�ڵ� ���
            this.extMgr = extMgr;
            this.extMgr.RegisterByteStreamHandler("FFmpegSource.FFmpegByteStreamHandler", ".*", "");
            this.extMgr.RegisterByteStreamHandler("FFmpegSource.FFmpegByteStreamHandler", ".*", "video/x-matroska");
            this.extMgr.RegisterByteStreamHandler("FFmpegSource.FFmpegByteStreamHandler", ".*", "video/mp4");
            this.extMgr.RegisterByteStreamHandler("FFmpegSource.FFmpegByteStreamHandler", ".*", "video/avi");
            //�ű� �߰�
            this.extMgr.RegisterByteStreamHandler("FFmpegSource.FFmpegByteStreamHandler", ".*", "video/x-ms-asf");
            //this.extMgr.RegisterByteStreamHandler("FFmpegSource.FFmpegByteStreamHandler", ".*", "video/x-ms-wmv");
            //          if (VersionHelper.WindowsVersion == 10)
            //            {
            //                this.extMgr.RegisterByteStreamHandler("FFmpegSource.FFmpegByteStreamHandler", ".*", "video/x-matroska");
            //            }

            //MFVideoFormat_FFmpeg_SW : {C1FC552A-B7B8-4DBB-8A93-5B918B2A082A}
            this.extMgr.RegisterVideoDecoder("FFmpegDecoder.FFmpegUncompressedVideoDecoder", Guid.Parse("{C1FC552A-B7B8-4DBB-8A93-5B918B2A082A}"), Guid.Empty);
            //MFAudioFormat_FFmpeg_SW : {6BAE7E7C-1560-4217-8636-71D18D67A9D2}
            this.extMgr.RegisterAudioDecoder("FFmpegDecoder.FFmpegUncompressedAudioDecoder", Guid.Parse("{6BAE7E7C-1560-4217-8636-71D18D67A9D2}"), Guid.Empty);

            this.CreateModels();
            this.CreateCommands();
            this.RegisterMessages();

            //ȭ�� ȸ�� �̺�Ʈ ���
            this.orientationSenser.OrientationChanged += orientationSenser_OrientationChanged;
            //ffmpeg�ڸ� �̺�Ʈ ���
            this.ffmpegSubtitleSupport.SubtitleFoundEvent += ffmpegSubtitleSupport_SubtitleFoundEvent;
            this.ffmpegSubtitleSupport.SubtitleUpdatedEvent += ffmpegSubtitleSupport_SubtitleUpdatedEvent;
            this.ffmpegSubtitleSupport.SubtitlePopulatedEvent += ffmpegSubtitleSupport_SubtitlePopulatedEvent;
            //ffmpeg÷�� �̺�Ʈ ���
            this.ffmpegAttachmentSupport.AttachmentFoundEvent += ffmpegAttachmentSupport_AttachmentFoundEvent;
            this.ffmpegAttachmentSupport.AttachmentCompletedEvent += ffmpegAttachmentSupport_AttachmentCompletedEvent;
        }
                        
        private void CreateModels()
        {
            this.deviceInfo = new Windows.Security.ExchangeActiveSyncProvisioning.EasClientDeviceInformation();
            this.PagePreviousStatus = new PagePreviousStatus();
            this.orientationSenser = SimpleOrientationSensor.GetDefault();
            this.ffmpegSubtitleSupport = SubtitleSupport.Instance();
            //UI����ó ����
            this.ffmpegSubtitleSupport.SetUIDispatcher(DispatcherHelper.UIDispatcher);
            this.ffmpegAttachmentSupport = AttachmentSupport.Instance();
        }

        private void CreateCommands()
        {
            LoadedCommand = new RelayCommand<MediaElementWrapper>(LoadedCommandExecute);
            MediaOpenedCommand = new RelayCommand<RoutedEventArgs>(MediaOpenedCommandExecute);
            MediaEndedCommand = new RelayCommand<RoutedEventArgs>(MediaEndedCommandCommandExecute);
            MediaFailedCommand = new RelayCommand<RoutedEventArgs>(MediaFailedCommandExecute);
            CurrentStateChangedCommand = new RelayCommand<RoutedEventArgs>(CurrentStateChangedCommandExecute);
            SeekCompletedCommand = new RelayCommand<RoutedEventArgs>(SeekCompletedCommandExecute);
            MarkerReachedCommand = new RelayCommand<TimelineMarkerRoutedEventArgs>(MarkerReachedCommandExecute);
            SubtitlePositionManipulationDeltaCommand = new RelayCommand<ManipulationDeltaRoutedEventArgs>(SubtitlePositionManipulationDeltaCommandExecute);
            SubtitlePositionManipulationCompletedCommand = new RelayCommand<ManipulationCompletedRoutedEventArgs>(SubtitlePositionManipulationCompletedCommandExecute);
            TappedCommand = new RelayCommand<RoutedEventArgs>(TappedCommandExecute);
        }

        private void RegisterMessages()
        {
            MessengerInstance.Register<PropertyChangedMessage<double>>(this, msg =>
            {
                switch(msg.PropertyName)
                {
                    case "SubtitleSyncTime":
                        OnChangeSubtitleSync(msg.NewValue, msg.OldValue);
                        break;
                }
            });

            MessengerInstance.Register<PropertyChangedMessage<bool>>(this, msg =>
            {
                switch (msg.PropertyName)
                {
                    case "IsSubtitleOn":
                        IsSubtitleOn = msg.NewValue;
                        break;
                    case "LoadingPanelVisible":
                        visibleLoadingBar = msg.NewValue;
                        break;
                    case "SupportedRotationLock":
                        supportedRotationLock = msg.NewValue;
                        break;
                }
            });

            MessengerInstance.Register<Message>(this, NAME, (msg) => 
            {
                switch (msg.Key)
                {
                    case "Play":
                        OnPlay(msg.GetValue<MediaInfo>());
                        break;
                    case "MoveToPlaylistSection":
                        requestedMoveToPlaylist = msg.GetValue<bool>();
                        break;
                    case "BackPressed":
                        msg.GetValue<BackPressedEventArgs>().Handled = true;
                        if (IsSubtitleSettingsOn)
                        {
                            //���� �г� ����
                            IsSubtitleSettingsOn = false;
                            MessengerInstance.Send<Message>(new Message("SettingsOpened", null), TransportControlViewModel.NAME);
                        }
                        else
                        {
                            SaveData();
                            StopMedia();
                            HidePlayer();
                        }
                        break;
                    case "ExitPlay":
                        SaveData();
                        StopMedia();
                        HidePlayer();
                        break;
                    case "Activated":
                        OnActivated();
                        break;
                    case "Deactivated":
                        OnDeactivated();
                        break;
                    case "OpenSubtitlePicker":
                        OnOpenSubtitlePicker();
                        break;
                    case "MoveSubtitle":
                        //�̵��ϱ� �غ� ��ŷ
                        IsSubtitleMoveOn = true;
                        break;
                    case "SubtitleChanged":
                        var param = msg.GetValue<KeyValuePair<string, Subtitle>>();
                        //�ڸ� ���� ó��
                        OnSubtitleChanged(param.Key, param.Value);
                        break;
                    case "SubtitleCharsetChanged":
                        var subCharset = msg.GetValue<KeyValuePair<string, Subtitle>>();
                        OnSubtitleCharsetChanged(subCharset.Key, subCharset.Value);
                        break;
                    case "SubtitleSettingsTapped":
                        IsSubtitleSettingsOn = true;
                        break;
                    case "ApplyCurrentRotation":
                        OnOrientationChanged(msg.GetValue<SimpleOrientation>(), !Settings.Playback.IsRotationLock);
                        break;
                    case "ApplyForceRotation":
                        OnOrientationChanged(msg.GetValue<SimpleOrientation>(), true);
                        break;
                    case "DecoderChanging":
                        //��� ���� ����
                        SaveData();
                        //������ ��ġ ����
                        SaveLastPosition();
                        //���� ����
                        StopMedia();
                        //���ڴ� Ÿ��
                        DecoderType decoderType = msg.GetValue<DecoderType>();
                        OpenSupportedMediaFile(CurrentMediaInfo, true, decoderType);
                        break;
                    case "MultiAudioSelecting":
                        //���� ����
                        StopMedia();
                        //���ڴ� Ÿ��
                        StreamInformation streamInfo = msg.GetValue<StreamInformation>();
                        tryAudioStreamIndex = streamInfo.StreamId;
                        OpenSupportedMediaFile(CurrentMediaInfo, true, DecoderType.MIX);
                        break;
                }
            });   
        }

        private void OnActivated()
        {
            if (previousState == MediaElementState.Playing)
            {
                //����� ������Ű�� ���� ������ ���ƿö� ���� ���� (Work around)
                Me.Pause();
                Me.Position = Me.Position.Subtract(TimeSpan.FromSeconds(1));
                Me.Play();
            }
        }

        private void OnDeactivated()
        {
            if (Me.CurrentState == MediaElementState.Playing)
            {
                previousState = Me.CurrentState;
                Me.Pause();
            }

            SaveLastPosition();
            Me.Trim();
        }

        private void OnPlay(MediaInfo mii)
        {
            //File Association���� �Ѿ���� ��� CCPlayerElement �������� �Ѿ���Ƿ� ��ü ������ �ɶ����� ������ ���� ���
            if (Me == null)
            {
                ThreadPoolTimer.CreateTimer(handler =>
                {
                    OnPlay(mii);
                }, TimeSpan.FromMilliseconds(300));
            }
            else
            {
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    //audio�ε��� �ʱ�ȭ
                    tryAudioStreamIndex = -1;
                    MessengerInstance.Send(new Message("BeforeMediaOpen"), TransportControlViewModel.NAME);
                    OpenSupportedMediaFile(mii, true, DecoderType.AUTO);
                });
            }
        }


        /// <summary>
        /// �̵�� ����
        /// </summary>
        private void StopMedia()
        {
            if (Me.CurrentState != Windows.UI.Xaml.Media.MediaElementState.Stopped)
            {
                //���� ��������� �����ϱ� ���� ����
                Me.Pause();
            }

            //�̵�� ������Ʈ�� ���Ḧ ��û
            Me.Stop();
        }

        /// <summary>
        /// ������ ����
        /// </summary>
        private void SaveData()
        {
            //����� ���� ������ DB�ݿ�
            if (settingDAO.SettingCache.Any(x => x.IsUpdated))
            {
                settingDAO.Replace(settingDAO.SettingCache);
            }
        }

        /// <summary>
        /// ������ ���� �̵��Ѵ�.
        /// </summary>
        private void MoveToPlaylist()
        {
            if (requestedMoveToPlaylist)
            {
                MessengerInstance.Send<Message>(new Message("MoveToPlaylistSection", null), MainViewModel.NAME);
            }
        }

        /// <summary>
        /// �̵�� �÷��� ȭ���� ǥ���Ѵ�.
        /// </summary>
        private void ShowPlayer()
        {
            if (!IsPlayerOpened)
            {
                IsFullWindow = true;
                IsPlayerOpened = true;
            }
        }

        /// <summary>
        /// �̵�� �÷��̾� ȭ���� �����.
        /// </summary>
        private void HidePlayer()
        {
            if (IsPlayerOpened)
            {
                SubtitleText = string.Empty;
                IsPlayerOpened = false;
                IsFullWindow = false;
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
                //�̵�� ������Ʈ ���ۿ� ����˸� (����ȭ ���� �����Ƿ� ������ ����) => ���߿� ����ȭ �ǵ��� ���ľ���.
                Orientation = DisplayOrientations.Portrait;
            }
        }

        /// <summary>
        /// �ε�â ǥ��
        /// </summary>
        /// <param name="msg"></param>
        private void ShowLoadingBar(string msg)
        {
            MessengerInstance.Send(new Message("ShowLoadingPanel", new KeyValuePair<string, bool>(msg, true)), MainViewModel.NAME);
        }

        /// <summary>
        /// �ε�â ����
        /// </summary>
        private void HideLoadingBar()
        {
            MessengerInstance.Send(new Message("ShowLoadingPanel", new KeyValuePair<string, bool>(string.Empty, false)), MainViewModel.NAME);
        }

        /// <summary>
        /// ������ ��������� �����Ѵ�.
        /// </summary>
        private void SaveLastPosition()
        {
            if (CurrentMediaInfo != null && Me != null)
            {
                CurrentMediaInfo.PausedTime = (long)Me.Position.TotalSeconds;
                MessengerInstance.Send<Message>(new Message("UpdatePausedTime", CurrentMediaInfo), PlaylistViewModel.NAME);
            }
        }
        
        #region Event Handler Method
        //�̵�� ������Ʈ ������ ȸ������ ���ε� ����
        private DisplayOrientations _Orientation;
        public DisplayOrientations Orientation
        {
            get { return _Orientation; }
            set { Set(ref _Orientation, value); }
        }
        
        async void orientationSenser_OrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
        {
            await DispatcherHelper.RunAsync(() =>
            {
                var orientation = args.Orientation;
                OnOrientationChanged(orientation, !Settings.Playback.IsRotationLock);
            });
        }
        
        async void OnOrientationChanged(SimpleOrientation orientation, bool canRotate)
        {
            await DispatcherHelper.RunAsync(() =>
            {
                if (IsPlayerOpened)
                {
                    if (Settings.Playback.UseFlipToPause 
                        && (orientation == SimpleOrientation.Faceup || orientation == SimpleOrientation.Facedown))
                    {
                        //�������� ����
                        if (orientation == SimpleOrientation.Facedown && Me.CurrentState == MediaElementState.Playing)
                        {
                            MessengerInstance.Send(new Message("IsPaused", true), TransportControlViewModel.NAME);
                            isPausedByFlip = true;
                        }
                        else if (orientation == SimpleOrientation.Faceup && isPausedByFlip)
                        {
                            MessengerInstance.Send(new Message("IsPaused", false), TransportControlViewModel.NAME);
                            isPausedByFlip = false;
                        }
                        return;
                    }

                    if (canRotate)
                    {
                        switch (orientation)
                        {
                            case SimpleOrientation.NotRotated:
                                if (supportedRotationLock)
                                {
                                    if (DisplayInformation.GetForCurrentView().NativeOrientation == DisplayOrientations.Portrait)
                                    {
                                        DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
                                        SwapDimension(this.Width > this.Height);
                                    }
                                    else
                                    {
                                        DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
                                        SwapDimension(this.Width < this.Height);
                                    }
                                }
                                break;
                            case SimpleOrientation.Rotated180DegreesCounterclockwise:
                                if (supportedRotationLock)
                                {
                                    if (DisplayInformation.GetForCurrentView().NativeOrientation == DisplayOrientations.Portrait)
                                    {
                                        DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
                                        SwapDimension(this.Width > this.Height);
                                    }
                                    else
                                    {
                                        DisplayInformation.AutoRotationPreferences = DisplayOrientations.LandscapeFlipped;
                                        SwapDimension(this.Width < this.Height);
                                    }
                                }
                                break;
                            case SimpleOrientation.Rotated270DegreesCounterclockwise:
                                if (DisplayInformation.GetForCurrentView().NativeOrientation == DisplayOrientations.Portrait)
                                {
                                    DisplayInformation.AutoRotationPreferences = DisplayOrientations.LandscapeFlipped;
                                    SwapDimension(this.Width < this.Height);
                                }
                                else
                                {
                                    DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
                                    SwapDimension(this.Width > this.Height);
                                }
                                break;
                            case SimpleOrientation.Rotated90DegreesCounterclockwise:
                                if (DisplayInformation.GetForCurrentView().NativeOrientation == DisplayOrientations.Portrait)
                                {
                                    DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
                                    SwapDimension(this.Width < this.Height);
                                }
                                else
                                {
                                    DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
                                    SwapDimension(this.Width > this.Height);
                                }
                                break;
                        }

                        //�̵�� ������Ʈ ���ۿ� ����˸�
                        Orientation = DisplayInformation.AutoRotationPreferences;
                        //��Ʈ�� �гο� �˸�
                        MessengerInstance.Send(new Message("OrientationChanged"), TransportControlViewModel.NAME);
                        //������ ȸ�� ���� ����
                        if (Settings.Playback.LastPlaybackOrientation != orientation)
                        {
                            Settings.Playback.LastPlaybackOrientation = orientation;
                        }
                    }
                }
            });
        }

        private void SwapDimension(bool isSwap)
        {
            if (isSwap)
            {
                double tmp = this.Width;
                this.Width = this.Height;
                this.Height = tmp;
            }
        }

        #endregion

        public async void ContinueFileOpenPicker(Windows.ApplicationModel.Activation.FileOpenPickerContinuationEventArgs args)
        {
            // The "args" object contains information about selected file(s).
            if (args.Files.Any())
            {
                var file = args.Files[0];

                var defaultCodePage = Settings.Subtitle.DefaultCharset;;
                var stream = (await file.OpenReadAsync()).AsStream();
                var parser = SubtitleParserFactory.CreateParser(file.Name);

                //������ �ڸ� �ε�
                LoadSubtitleManually(file);

                //�⺻ �ڸ� ��Ÿ�� �ε�
                LoadSubtitleStylesBySetting();
            }

            Me.Play();
            Me.Position = Me.Position.Subtract(TimeSpan.FromMilliseconds(1000));
        }
        
        public async void OpenSupportedMediaFile(MediaInfo mi, bool autoPlay, DecoderType decoderType, bool registerContentType)
        {
            //�ڸ� ���ڼ�
            if (Settings.Subtitle.DefaultCharset == Lime.Encoding.CodePage.AUTO_DETECT_VALUE)
            {
                this.ffmpegSubtitleSupport.CodePage = Lime.Encoding.CodePage.UTF8_CODE_PAGE;
            }
            else
            {
                this.ffmpegSubtitleSupport.CodePage = Settings.Subtitle.DefaultCharset;
            }

            //�̵�� ������Ʈ ���� ��� ���
            Me.ForceUseMediaElement = Settings.Playback.ForceUseMediaElement;
            //Ǯ��ũ�� ���� ���
            Me.DisableFullScreenMediaElement = Settings.Playback.UseOptimizationEntryModel;
            //�ڸ� ��� ����
            Me.Markers.Clear();
            //ȭ�鿡�� �ڸ� ����
            SubtitleText = string.Empty;
            //����� �������� ����
            CurrentMediaInfo = mi;
            var file = await mi.GetStorageFile(false);
            //�ڸ� �ʱ�ȭ
            ClearSubtitles();
            //�ڸ� �ε�
            LoadSubtitles(mi);

            if (registerContentType)
            {
                this.extMgr.RegisterByteStreamHandler("FFmpegSource.FFmpegByteStreamHandler", file.FileType, file.ContentType);
            }

            //this.extMgr.RegisterByteStreamHandler("FFmpegSource.FFmpegByteStreamHandler", ".*", "");
            //this.extMgr.RegisterByteStreamHandler("FFmpegSource.FFmpegByteStreamHandler", ".*", file.ContentType);

            //if (decoderType == DecoderType.AUTO || decoderType == DecoderType.HW)
            //{
            //    //workaround : mp4�� ��� MF���� �ͼ����� ��Ʈ���� ������ �߻����� ����
            //    //if (file.ContentType == "video/mp4" || file.ContentType == "video/3gpp2")
            //    if (!ignoreContentType && !string.IsNullOrEmpty(file.ContentType))
            //    {
            //        this.extMgr.RegisterByteStreamHandler("FFmpegSource.FFmpegByteStreamHandler", ".*", file.ContentType);
            //    }

            //    //if (ignoreContentType)
            //    //{
            //    //    this.extMgr.RegisterByteStreamHandler("FFmpegSource.FFmpegByteStreamHandler", ".*", "--");
            //    //}
            //    //else
            //    {
            //        this.extMgr.RegisterByteStreamHandler("FFmpegSource.FFmpegByteStreamHandler", ".*", "");
            //    }

            //}
            //else
            //{
            //    this.extMgr.RegisterByteStreamHandler("FFmpegSource.FFmpegByteStreamHandler", ".*", file.ContentType);
            //}

            //���ڴ� ���� ��û
            DecoderSupport decoderSupport = DecoderSupport.Instance();
            decoderSupport.WindowsVersion = VersionHelper.WindowsVersion;
            decoderSupport.RequestDecoderChange(decoderType);
            decoderSupport.EnforceAudioStreamId = tryAudioStreamIndex;
            decoderSupport.UseGPUShader = Settings.Playback.UseGpuShader;

            //�ڸ� ���� ���� ����
            this.ffmpegAttachmentSupport.IsSaveAttachment = Settings.General.UseSaveFontInMkv;
            //�̵�� ����
            var randomStream = await file.OpenReadAsync();
            Me.AutoPlay = true;
            Me.SetSource(randomStream, file.Path);
        }

        public void OpenSupportedMediaFile(MediaInfo mi, bool autoPlay, DecoderType decoderType)
        {
            OpenSupportedMediaFile(mi, autoPlay, decoderType, false);
        }
    }
}