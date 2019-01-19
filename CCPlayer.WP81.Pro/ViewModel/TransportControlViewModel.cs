using CCPlayer.UI.Xaml.Controls;
using CCPlayer.UI.Xaml.Controls.WP81;
using CCPlayer.HWCodecs.Text.Models;
using CCPlayer.HWCodecs.Text.Parsers;
using CCPlayer.WP81.Helpers;
using CCPlayer.WP81.Models;
using CCPlayer.WP81.Models.DataAccess;
using FFmpegSupport;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Lime.Encoding;
using Lime.Helpers;
using Lime.Models;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using Windows.System.Threading;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls.Primitives;

namespace CCPlayer.WP81.ViewModel
{
    public enum ControlPanelType
    {
        None,
        General,
        Unlock
    }

    public class ContainerDisplayInfomation : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ContainerDisplayInfomation() { }

        public ContainerDisplayInfomation(bool isActivate, Visibility visible) 
        {
            this.IsTriggerOn = isActivate;
            this.Visibility = visible;
        }

        private bool _IsTriggerOn;
        public bool IsTriggerOn
        {
            get
            {
                return _IsTriggerOn;
            }
            set
            {
                if (_IsTriggerOn != value)
                {
                    _IsTriggerOn = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private Visibility _Visibility;
        public Visibility Visibility
        {
            get
            {
                return _Visibility;
            }
            set
            {
                if (_Visibility != value)
                {
                    _Visibility = value;
                }
            }
        }

        public void Toggle()
        {
            if (this.Visibility == Visibility.Collapsed)
            {
                if (this.IsTriggerOn)
                {
                    this.IsTriggerOn = false;
                }
                this.IsTriggerOn = true;
            }
            else
            {
                if (!this.IsTriggerOn)
                {
                    this.IsTriggerOn = true;
                }
                this.IsTriggerOn = false;
            }
        }
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
    public partial class TransportControlViewModel : ViewModelBase
    {
        public static readonly string NAME = typeof(TransportControlViewModel).Name;
        private Regex crExp = new Regex(@"\\n", RegexOptions.IgnoreCase);

        #region ������ ��

        //�������� DAO
        private SettingDAO settingDAO;
        //���ϰ��� DAO
        private FileDAO fileDAO;
        //���� �Ǵ� ���� ��� ���� ����
        private MediaInfo NextPlayMediaInfo;

        private ControlPanelType panelType;
        private int turn;
        private bool sliderPressed;
        private bool isEntryDevice;

        private DispatcherTimer timer;
        private ThreadPoolTimer controlPanelDispalyTimer;

        public Settings Settings { get; set; }
        public MediaElementWrapper Me { get; set; }
        public ContainerDisplayInfomation GeneralState { get; private set; }
        public ContainerDisplayInfomation LockingState { get; private set; }
        public ContainerDisplayInfomation SubtitleState { get; private set; }

        private MediaInfo _MediaInfo;
        public MediaInfo MediaInfo
        {
            get { return _MediaInfo; }
            set { this.Set(ref _MediaInfo, value); }
        }

        private bool _IsExternalSubtitle;
        public bool IsExternalSubtitle
        {
            get { return _IsExternalSubtitle; }
            set { Set(ref _IsExternalSubtitle, value); }
        }

        #region Ʈ������Ʈ ������

        private bool _EnableDecoderType;
        public bool EnableDecoderType
        {
            get { return _EnableDecoderType; }
            set { this.Set(ref _EnableDecoderType, value); }
        }

        public DecoderType DecoderType
        {
            get { return DecoderSupport.Instance().DecoderTypes.Current; }
            set
            {
                DecoderSupport.Instance().DecoderTypes.Current = value;
                RaisePropertyChanged("DecoderType");
                RaisePropertyChanged("DecoderType2");
            }
        }

        public DecoderType DecoderType2
        {
            get { return DecoderSupport.Instance().DecoderTypes.Next; }
        }

        private bool _SupportedStretch;
        public bool SupportedStretch
        {
            get { return _SupportedStretch; }
            set { this.Set(ref _SupportedStretch, value); }
        }

        private bool _SupportedPlaybackRate;
        public bool SupportedPlaybackRate
        {
            get { return _SupportedPlaybackRate; }
            set { this.Set(ref _SupportedPlaybackRate, value); }
        }

        private double _PlaybackRate;
        public double PlaybackRate
        {
            get { return _PlaybackRate; }
            set
            {
                if (this.Set(ref _PlaybackRate, value))
                {
                    Me.PlaybackRate = value;
                }
            }
        }

        private double _Volume;
        public double Volume
        {
            get { return _Volume; }
            set
            {
                if (this.Set(ref _Volume, value))
                {
                    Me.Volume = _Volume / 20d;
                }
            }
        }

        private double _Zoom;
        public double Zoom
        {
            get { return _Zoom; }
            set
            {
                if (this.Set(ref _Zoom, value))
                {
                    Me.Zoom = _Zoom;
                }
            }
        }

        public double Balance
        {
            get { return Me.Balance; }
            set
            {
                if (Me.Balance != value)
                {
                    Me.Balance = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _IsPaused;
        public bool IsPaused
        {
            get { return _IsPaused; }
            set { this.Set(ref _IsPaused, value); }
        }

        private bool _NotInitializeTime;
        public double CurrentTime
        {
            get
            {
                if (Me == null) return 0;
                return Me.Position.TotalSeconds;
            }
            set
            {
                if (Me != null && Me.CanSeek && Me.Position.TotalSeconds != value)
                {
                    if ((long)Me.Position.TotalSeconds != (long)value)
                    {
                        if (!_NotInitializeTime)
                        {
                            Me.Position = TimeSpan.FromSeconds(value);
                        }
                        else
                        {
                            _NotInitializeTime = false;
                        }
                        VerifyPropertyName("LeftTime");
                        RaisePropertyChanged("LeftTime");
                    }
                }
            }
        }
        
        public double ExpectedTime { get; set; }

        public double LeftTime
        {
            get
            {
                if (Me == null) return 0;
                return Me.NaturalDuration.TimeSpan.TotalSeconds - CurrentTime;
            }
        }

        private double _TimeMovement;
        public double TimeMovement
        {
            get { return _TimeMovement; }
            set { this.Set(ref _TimeMovement, value); }
        }

        private bool _IsSubtitleOn;
        public bool IsSubtitleOn
        {
            get { return _IsSubtitleOn; }
            set { this.Set(ref _IsSubtitleOn, value, true); }
        }

        #endregion

        private bool _IsSubtitleFlyoutOpen;
        public bool IsSubtitleFlyoutOpen
        {
            get { return _IsSubtitleFlyoutOpen; }
            set { this.Set(ref _IsSubtitleFlyoutOpen, value); }
        }

        private bool _IsFlyoutClosed;
        public bool IsFlyoutClosed
        {
            get { return _IsFlyoutClosed; }
            set
            {
                this.Set(ref _IsFlyoutClosed, value);
                //if (value)
                //    IsFlyoutClosed = false;
            }
        }

        private long _TimelineStepFrequency;
        public long TimelineStepFrequency
        {
            get { return _TimelineStepFrequency; }
            set { this.Set(ref _TimelineStepFrequency, value); }
        }

        private PickerItem<string, int> _SelectedAudioLanguage;
        public PickerItem<string, int> SelectedAudioLanguage
        {
            get { return _SelectedAudioLanguage; }
            set
            {
                if (_SelectedAudioLanguage != value)
                {
                    if (value != null)
                    {
                        Me.AudioStreamIndex = value.Key;
                    }
                    _SelectedAudioLanguage = value;
                    RaisePropertyChanged();
                }
            }
        }

        private PickerItem<string, int> _SelectedAspectRatio;
        public PickerItem<string, int> SelectedAspectRatio
        {
            get { return _SelectedAspectRatio; }
            set
            {
                if (_SelectedAspectRatio != value)
                {
                    if (value != null)
                    {
                        Me.AspectRatio = (AspectRatio)value.Key;
                    }
                    _SelectedAspectRatio = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double _SubtitleSyncTime;
        public double SubtitleSyncTime
        {
            get { return _SubtitleSyncTime; }
            set { this.Set(ref _SubtitleSyncTime, value, true); }
        }

        private int _SubtitleSyncTimeMin;
        public int SubtitleSyncTimeMin
        {
            get { return _SubtitleSyncTimeMin; }
            set { this.Set(ref _SubtitleSyncTimeMin, value); }
        }

        private int _SubtitleSyncTimeMax;
        public int SubtitleSyncTimeMax
        {
            get { return _SubtitleSyncTimeMax; }
            set { this.Set(ref _SubtitleSyncTimeMax, value); }
        }

        public string _CurrentSubtitleLanguageCode;
        public string CurrentSubtitleLanguageCode
        {
            get
            {
                return _CurrentSubtitleLanguageCode;
            }
            set
            {
                Set(ref _CurrentSubtitleLanguageCode, value);
                //�ڸ� ���
                var subtitleLanguage = SubtitleLanguageList.FirstOrDefault(x => x.Key == value);
                if (subtitleLanguage != null && subtitleLanguage.Payload is Subtitle)
                {
                    var subtitle = subtitleLanguage.Payload as Subtitle;
                    //�ܺ�/���� �ڸ��� ���� ��ũ/���ڵ� �޺� Ȱ��ȭ ���� ����
                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                    {
                        CurrentSubtitleLanguageName = subtitleLanguage.Name;
                        //                        IsExternalSubtitle = subtitle.SubtitleFileKind == SubtitleFileKind.External;
                        IsExternalSubtitle = true;
                        //�ڸ� ���� �̺�Ʈ ����
                        MessengerInstance.Send<Message>(new Message("SubtitleChanged",
                            new KeyValuePair<string, Subtitle>(subtitleLanguage.Key, subtitle)), CCPlayerViewModel.NAME);
                    });
                }
            }
        }

        public string _CurrentSubtitleLanguageName;
        public string CurrentSubtitleLanguageName
        {
            get
            {
                return _CurrentSubtitleLanguageName;
            }
            set { Set(ref _CurrentSubtitleLanguageName, value); }
        }

        //private int _SubtitleLanguageIndex;
        //public int SubtitleLanguageIndex
        //{
        //    get { return _SubtitleLanguageIndex; }
        //    set
        //    {
        //        this.Set(ref _SubtitleLanguageIndex, value);
        //        //�ڸ� ���
        //        var subtitleLanguage = SubtitleLanguageList.ElementAtOrDefault(value);
        //        if (subtitleLanguage != null && subtitleLanguage.Payload is Subtitle)
        //        {
        //            var subtitle = subtitleLanguage.Payload as Subtitle;
        //            //�ܺ�/���� �ڸ��� ���� ��ũ/���ڵ� �޺� Ȱ��ȭ ���� ����
        //            DispatcherHelper.CheckBeginInvokeOnUI(() => 
        //            {
        //                //                        IsExternalSubtitle = subtitle.SubtitleFileKind == SubtitleFileKind.External;
        //                IsExternalSubtitle = true;
        //                //�ڸ� ���� �̺�Ʈ ����
        //                MessengerInstance.Send<Message>(new Message("SubtitleChanged",
        //                    new KeyValuePair<string, Subtitle>(subtitleLanguage.Key, subtitle)), CCPlayerViewModel.NAME);
        //            });
        //        }
        //    }
        //}

        public int CurrentCodePage
        {
            get
            {
                return Settings.Subtitle.DefaultCharset;
            }
            set
            {
                if (Settings.Subtitle.DefaultCharset != value)
                {
                    Settings.Subtitle.DefaultCharset = value;
                    RaisePropertyChanged();

                    CurrentCodePageName = SubtitleEncodingList.First(x => x.Key == value).Name;

                    //���ڼ�(���ڵ�) ���� ó��
                    DispatcherHelper.CheckBeginInvokeOnUI(() =>
                    {
                        //var subtitleLanguage = SubtitleLanguageList.ElementAtOrDefault(SubtitleLanguageIndex);
                        var subtitleLanguage = SubtitleLanguageList.FirstOrDefault(x => x.Key == CurrentSubtitleLanguageCode);
                        //���ڵ� ���� ����
                        var subtitle = subtitleLanguage.Payload as Subtitle;
                        subtitle.CurrentCodePage = value;

                        MessengerInstance.Send<Message>(new Message("SubtitleCharsetChanged",
                            new KeyValuePair<string, Subtitle>(subtitleLanguage.Key, subtitle)), CCPlayerViewModel.NAME);
                    });
                }
            }
        }

        public string _CurrentCodePageName;
        public string CurrentCodePageName
        {
            get
            {
                //���ڼ� ��
                if (string.IsNullOrEmpty(_CurrentCodePageName))
                {
                    _CurrentCodePageName = SubtitleEncodingList.First(x => x.Key == Settings.Subtitle.DefaultCharset).Name;
                }
                return _CurrentCodePageName;
            }
            set { Set(ref _CurrentCodePageName, value); }
        }

        private string _InformationMessage;
        public string InformationMessage
        {
            get { return _InformationMessage; }
            set { Set(ref _InformationMessage, value); }
        }

        private bool _SupportedRotationLock;
        public bool SupportedRotationLock
        {
            get { return _SupportedRotationLock; }
            set { Set(ref _SupportedRotationLock, value, true); }
        }

        private int _AdvRow;
        public int AdvRow
        {
            get { return _AdvRow; }
            set { Set(ref _AdvRow, value); }
        }

        private int _AdvColumn;
        public int AdvColumn
        {
            get { return _AdvColumn; }
            set { Set(ref _AdvColumn, value); }
        }

        public string Battery
        {
            get { return Windows.Phone.Devices.Power.Battery.GetDefault().RemainingChargePercent.ToString(); }
        }

        public string TimeTT
        {
            get { return DateTime.Now.ToString("tt", CultureInfo.InvariantCulture); }
        }

        public string TimeHMM
        {
            get { return DateTime.Now.ToString("h:mm", CultureInfo.InvariantCulture); }
        }

        public ObservableCollection<PickerItem<string, int>> AudioLanguageList { get; private set; }
        public ObservableCollection<PickerItem<string, string>> SubtitleLanguageList { get; private set; }
        public ObservableCollection<PickerItem<string, int>> AspectRatioList { get; private set; }
        public List<PickerItem<string, int>> SubtitleEncodingList { get; private set; }

        #endregion

        #region Ŀ�ǵ�

        //public ICommand LoadedMainCommand { get; private set; }
        public ICommand PointerEnteredCommand { get; private set; }
        
        //������ �̺�Ʈ
        public ICommand ManipulationStartedCommand { get; private set; }
        public ICommand ManipulationDeltaCommand { get; private set; }
        public ICommand ManipulationCompletedCommand { get; private set; }

        //��� �̺�Ʈ
        public ICommand CloseTappedCommand { get; private set; }

        //�ٵ� �̺�Ʈ
        public ICommand OtherMediaTappedCommand { get; private set; }
        public ICommand RewindClickCommand { get; private set; }
        public ICommand PlayPauseTappedCommand { get; private set; }
        public ICommand FastForwardClickCommand { get; private set; }

        //Ǫ�� �̺�Ʈ
        public ICommand SubtitlePickerTappedCommand { get; private set; }
        public ICommand SubtitleMoveTappedCommand { get; private set; }
        public ICommand RotationLockTappedCommand { get; private set; }
        public ICommand LockTappedCommand { get; private set; }
        public ICommand UnlockTappedCommand { get; private set; }
        public ICommand SubtitleSettingsTappedCommand { get; private set; }
        public ICommand FlyoutOpenedCommand { get; private set; }
        public ICommand FlyoutClosedCommand { get; private set; }
        public ICommand DecoderTypeTappedCommand { get; private set; }
        
        //�ڸ� �ö��̾ƿ�
        public ICommand SyncRangeCommand { get; private set; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public TransportControlViewModel(SettingDAO settingDAO, FileDAO fileDAO)
        {
            this.settingDAO = settingDAO;
            this.fileDAO = fileDAO;
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}

            this.CreateModels();
            this.CreateCommands();
            this.RegisterMessages();

            isEntryDevice = Lime.Common.Helpers.DeviceHelper.CheckDeviceId(new String[]{
                            /*Lumia 520 */ "RM-914", "RM-915",
		                    /*Lumia 520T*/ "RM-913",
		                    /*Lumia 521 */ "RM-917",
		                    /*Lumia 525 */ "RM-997", "RM-998",
		                    /*Lumia 620 */ "RM-846",
		                    /*Lumia 625 */ "RM-941", "RM-942", "RM-943",
		                    /*Lumia 530 */ "RM-1017", "RM-1018", "RM-1019", "RM-1020",
		                    /*Lumia 532 */ "RM-1031", "RM-1032", "RM-1034", "RM-1115",
		                    /*Lumia 535 */ "RM-1089", "RM-1090", "RM-1091", "RM-1092",
		                    /*Lumia 430 */ "RM-1066", "RM-1067", "RM-1099",
		                    /*Lumia 435 */ "RM-1068", "RM-1069", "RM-1070", "RM-1071", "RM-1114"
                        });
        }

        private void CreateModels()
        {
            var loader = ResourceLoader.GetForCurrentView();
            Settings = settingDAO.SettingCache;
            panelType = ControlPanelType.General;
            sliderPressed = false;
            timer = new DispatcherTimer();
            _Volume = 1d;

            GeneralState = new ContainerDisplayInfomation(true, Visibility.Collapsed);
            LockingState = new ContainerDisplayInfomation(false, Visibility.Collapsed);
            SubtitleState = new ContainerDisplayInfomation(false, Visibility.Collapsed);
            AudioLanguageList = new ObservableCollection<PickerItem<string, int>>();
            SubtitleLanguageList = new ObservableCollection<PickerItem<string, string>> ();
            SubtitleEncodingList = CodePage.Instance.Select(x =>
                new PickerItem<string, int> 
                {
                    Key = x.Value,
                    Name = loader.GetString(string.Format("Charset{0}", x.Key))
                }
            ).ToList();
            AspectRatioList = new ObservableCollection<PickerItem<string, int>>();
        }
        
        private void CreateCommands()
        {
            PointerEnteredCommand = new RelayCommand<PointerRoutedEventArgs>(PointerEnteredCommandExecute);
            //������
            ManipulationStartedCommand = new RelayCommand<ManipulationStartedRoutedEventArgs>(ManipulationStartedCommandExecute);
            ManipulationDeltaCommand = new RelayCommand<ManipulationDeltaRoutedEventArgs>(ManipulationDeltaCommandExecute);
            ManipulationCompletedCommand = new RelayCommand<ManipulationCompletedRoutedEventArgs>(ManipulationCompletedCommandExecute);
            //���
            CloseTappedCommand = new RelayCommand<RoutedEventArgs>(CloseTappedCommandExecute);
            //�ٵ�
            OtherMediaTappedCommand = new RelayCommand<MediaInfo>(OtherMediaTappedCommandExecute);
            RewindClickCommand = new RelayCommand<RoutedEventArgs>(RewindClickCommandExecute);
            PlayPauseTappedCommand = new RelayCommand<TappedRoutedEventArgs>(PlayPauseTappedCommandExecute);
            FastForwardClickCommand = new RelayCommand<RoutedEventArgs>(FastForwardClickCommandExecute);
            //Ǫ��
            SubtitlePickerTappedCommand = new RelayCommand<TappedRoutedEventArgs>(SubtitlePickerTappedCommandExecute);
            SubtitleMoveTappedCommand = new RelayCommand<TappedRoutedEventArgs>(SubtitleMoveTappedCommandExecute);
            RotationLockTappedCommand = new RelayCommand(RotationLockTappedCommandExecute);
            LockTappedCommand = new RelayCommand<TappedRoutedEventArgs>(LockTappedCommandExecute);
            UnlockTappedCommand = new RelayCommand<TappedRoutedEventArgs>(UnlockTappedCommandExecute);
            DecoderTypeTappedCommand = new RelayCommand<TappedRoutedEventArgs>(DecoderTypeTappedCommandExecute);
      
            SubtitleSettingsTappedCommand = new RelayCommand(SubtitleSettingsTappedCommandExecute);
            FlyoutOpenedCommand = new RelayCommand<object>(FlyoutOpenedCommandExecute);
            FlyoutClosedCommand = new RelayCommand<object>(FlyoutClosedCommandExecute);

            SyncRangeCommand = new RelayCommand<object>(SyncRangeCommandExecute);
        }

        #region Ŀ�ǵ��ڵ鷯

        private void PointerEnteredCommandExecute(PointerRoutedEventArgs args)
        {
            if (!_IsManipulating)
            {
                if (controlPanelDispalyTimer != null)
                {
                    controlPanelDispalyTimer.Cancel();
                }

                controlPanelDispalyTimer = ThreadPoolTimer.CreateTimer(async handler =>
                {
                    await DispatcherHelper.RunAsync(() =>
                    {
                        ToggleShow();
                    });
                }, TimeSpan.FromMilliseconds(200));
            }
        }

        private void OtherMediaTappedCommandExecute(MediaInfo otherMediaInfo)
        {
            //���� ����/���� ��ư ����
            NextPlayMediaInfo = otherMediaInfo;
            //���� ��ġ ����
            SaveLastPosition();
            //�̵�� ���� => ���� �̺�Ʈ�� �߻��Ͽ� CurrentState�� Stop���°� �Ǹ� ���� �̵� ȣ�� 
            Me.Stop();
        }

        private string _VisibleIndicator;
        public string VisibleIndicator
        {
            get
            {
                return _VisibleIndicator;
            }
            set
            {
                if (_VisibleIndicator != value)
                {
                    _VisibleIndicator = value;
                    RaisePropertyChanged();
                }
            }
        }

        private bool _IsManipulating;
        private double _PrevZoomScale = 0;

        private void ManipulationStartedCommandExecute(ManipulationStartedRoutedEventArgs args)
        {
            if (panelType == ControlPanelType.General)
            {
                _IsManipulating = true;
                turn = 0;
                VisibleIndicator = "None";
                TimeMovement = 0;
                ExpectedTime = CurrentTime;

                if (controlPanelDispalyTimer != null)
                {
                    controlPanelDispalyTimer.Cancel();
                }

                _PrevZoomScale = Zoom;
            }
        }

        private void ManipulationDeltaCommandExecute(ManipulationDeltaRoutedEventArgs e)
        {
            if (panelType == ControlPanelType.General)
            {
                
                if (e.Cumulative.Expansion != 0)
                {
                    if (VersionHelper.IsPaidFeature)
                    {
                        double scale = e.Cumulative.Scale * _PrevZoomScale;
                        if (scale >= 0.5 && scale <= 4)
                        {
                            Zoom = scale;
                        }

                        if (VisibleIndicator == "None")
                        {
                            VisibleIndicator = "Zoom";
                        }
                    }
                }
                else
                {
                    var radians = Math.Atan2(e.Cumulative.Translation.Y, e.Cumulative.Translation.X);
                    var angle = radians * (180 / Math.PI);

                    if ((angle < -70 && angle > -110) || (angle > 70 && angle < 110))
                    {
                        //�� : angle < -70 && angle > -110
                        //�� : angle > 70 && angle < 110
                        //if (!displayedIndicator)
                        if (VisibleIndicator == "None")
                        {
                            VisibleIndicator = (e.Position.X < Window.Current.Bounds.Width / 2) ? "Brightness" : "Volume";
                        }

                        var val = e.Delta.Translation.Y * -1;
                        turn++;
                        //�� ��ȯ
                        if (val > 0) val = 1;
                        else if (val < 0) val = -1;

                        //���� ��������
                        if (e.Position.X > 0 && e.Position.X < Window.Current.Bounds.Width / 2)
                        {
                            if (turn % 2 == 0)
                            {
                                val = Settings.General.DisplayBrightness + val;

                                if (val > 100) val = 100;
                                else if (val < 20) val = 20;
                                //��� ����
                                Settings.General.DisplayBrightness = (int)val;
                                turn = 0;
                            }
                        }
                        else
                        {
                            if (turn % 5 == 0)
                            {
                                //System.Diagnostics.Debug.WriteLine(Volume);
                                //���� ��ȯ
                                val = Volume + val;
                                if (val > 20) val = 20;
                                else if (val < 0) val = 0;
                                //���� ����
                                Volume = val;
                                turn = 0;
                            }
                        }
                    }
                    else if ((angle > -20 && angle < 20) || (angle > 160 && angle <= 180) || (angle > -179.9 && angle < -160))
                    {
                        //�� : angle > -20 && angle < 20
                        //�� : (angle > 160 && angle <= 180) || (angle > -179.9 && angle < -160)
                        if (VersionHelper.IsPaidFeature)
                        {
                            if (VisibleIndicator == "None")
                            {
                                VisibleIndicator = "Time";
                            }
                            //���� ��������
                            var val = e.Delta.Translation.X * 0.4;
                            sliderPressed = true;

                            if ((long)(ExpectedTime + val) <= MediaInfo.RunningTime)
                            {
                                TimeMovement += val;
                                ExpectedTime += val;
                                //��� ���� �ð��� ���̳ʽ�(����)�� �� �� ����.
                                if (ExpectedTime < 0)
                                {
                                    ExpectedTime = 0;
                                }

                                RaisePropertyChanged("ExpectedTime");
                            }
                        }
                    }
                    //    System.Diagnostics.Debug.WriteLine(angle);
                }
            }
        }

        private void ManipulationCompletedCommandExecute(ManipulationCompletedRoutedEventArgs args)
        {
            if (panelType == ControlPanelType.General)
            {
                if (VersionHelper.IsPaidFeature && sliderPressed)
                {
                    sliderPressed = false;

                    CurrentTime = ExpectedTime;
                    RaisePropertyChanged("CurrentTime");
                }
                VisibleIndicator = "None";
            }
            _IsManipulating = false;
        }

        private void CloseTappedCommandExecute(RoutedEventArgs obj)
        {
            MessengerInstance.Send<Message>(new Message("ExitPlay", null), CCPlayerViewModel.NAME);
        }

        private void RewindClickCommandExecute(RoutedEventArgs e)
        {
            Seek(TimelineStepFrequency * -1);
        }

        private void PlayPauseTappedCommandExecute(TappedRoutedEventArgs e)
        {
            if (Me.CanPause)
            {
                IsPaused = !IsPaused;
                if (IsPaused)
                {
                    Me.Pause();
                }
                else
                {
                    Me.Play();
                    SetupTimer();
                }
            }
        }

        private void OnPlayPause(bool isPause)
        {
            if (Me.CanPause)
            {
                IsPaused = isPause;
                if (IsPaused)
                {
                    Me.Pause();
                }
                else
                {
                    Me.Play();
                    SetupTimer();
                }
            }
        }

        private void FastForwardClickCommandExecute(RoutedEventArgs e)
        {
            Seek(TimelineStepFrequency);
        }

        private void SubtitleMoveTappedCommandExecute(TappedRoutedEventArgs obj)
        {
            panelType = ControlPanelType.None;
            GeneralState.IsTriggerOn = false;
            LockingState.IsTriggerOn = false;
            SubtitleState.IsTriggerOn = true;
            MessengerInstance.Send<Message>(new Message("MoveSubtitle", null), CCPlayerViewModel.NAME);
        }

        private void SubtitlePickerTappedCommandExecute(TappedRoutedEventArgs obj)
        {
            MessengerInstance.Send<Message>(new Message("OpenSubtitlePicker", null), CCPlayerViewModel.NAME);
        }

        private void RotationLockTappedCommandExecute()
        {
            if (!VersionHelper.IsPaidFeature)
            {
                InformationMessage = ResourceLoader.GetForCurrentView().GetString("Message/Info/Pro/Feature1");
                VisibleIndicator = "None";
                VisibleIndicator = "Information";
                return;
            }
            InformationMessage = string.Empty;
            VisibleIndicator = "None";
            Settings.Playback.IsRotationLock = !Settings.Playback.IsRotationLock;
            MessengerInstance.Send(new Message("ApplyCurrentRotation", SimpleOrientationSensor.GetDefault().GetCurrentOrientation()), CCPlayerViewModel.NAME);
        }

        private void LockTappedCommandExecute(TappedRoutedEventArgs obj)
        {
            panelType = ControlPanelType.Unlock;
            GeneralState.IsTriggerOn = false;
            SubtitleState.IsTriggerOn = false;
            LockingState.IsTriggerOn = true;
        }

        private void UnlockTappedCommandExecute(TappedRoutedEventArgs obj)
        {
            panelType = ControlPanelType.General;
            LockingState.IsTriggerOn = false;
            SubtitleState.IsTriggerOn = false;
            GeneralState.IsTriggerOn = true;
        }

        public void DecoderTypeTappedCommandExecute(TappedRoutedEventArgs obj)
        {
            MessengerInstance.Send<Message>(new Message("DecoderChanging", DecoderSupport.Instance().DecoderTypes.Next), CCPlayerViewModel.NAME);
        }

        private void SubtitleSettingsTappedCommandExecute()
        {
            IsSubtitleFlyoutOpen = false;
            IsFlyoutClosed = true;
            MessengerInstance.Send<Message>(new Message("SubtitleSettingsTapped", null), CCPlayerViewModel.NAME);
        }

        private void FlyoutOpenedCommandExecute(object e)
        {
            IsFlyoutClosed = false;
            GeneralState.IsTriggerOn = false;
            LockingState.IsTriggerOn = false;
            SubtitleState.IsTriggerOn = false;
        }

        private void FlyoutClosedCommandExecute(object e)
        {
            //�ڷΰ��⸦ ���� �ݴ� ��� ���� ���¸� �� �� ������, �̺�Ʈ�� ���� ��ŷ
            _IsFlyoutClosed = true;
        }

        private void SyncRangeCommandExecute(object direction)
        {
            if (direction.ToString() == "Decrease")
            {
                SubtitleSyncTimeMin -= 10;
                SubtitleSyncTimeMax -= 10;
            }
            else
            {
                SubtitleSyncTimeMin += 10;
                SubtitleSyncTimeMax += 10;
            }
        }
        #endregion

        private void RegisterMessages()
        {
            MessengerInstance.Register<Message>(this, NAME, (msg) =>
            {
                switch(msg.Key)
                {
                    case "MediaFileChanged":
                        MediaInfo = msg.GetValue<MediaInfo>();
                        //�ڸ��� �̵� ����ɶ� ���� �ε�ǹǷ� ���⼭ �ʱ�ȭ
                        SubtitleLanguageList.Clear();
                        SubtitleSupport.Instance().ClearQueue();
                        SubtitleCompensationData.Clear();
                        break;
                    case "BeforeMediaOpen":
                        //���ڴ� ��� �ʱ�ȭ
                        DecoderSupport.Instance().DecoderTypes.Reset();
                        EnableDecoderType = true;
                        break;
                    case "MediaOpend":
                        DecoderSupport decoderSupport = DecoderSupport.Instance();
                        DecoderChangePayload payload = decoderSupport.Payload;
                        payload.Status = DecoderChangeStatus.Succeeded;

                        this.DecoderType = payload.ResDecoderType;
                        this.Me = msg.Value as MediaElementWrapper;

                        //�̵�� �ʱ�ȭ
                        OnLoadedMedia();
                        break;
                    case "ClearSubtitles":
                        //�̵� �ý��� Resume� ���� ����� ��� �ٽ� ����ϸ� MediaFileChanged�� �߻����� �����Ƿ� ���� �ʱ�ȭ
                        SubtitleLanguageList.Clear();
                        break;
                    case "SubtitlesLoaded":
                        //�ε� �Ǵ� ������� �ڸ� �߰�
                        var subtitleList = msg.GetValue<List<PickerItem<string, string>>>();
                        DispatcherHelper.CheckBeginInvokeOnUI(() =>
                        {
                            subtitleList.AddRange(SubtitleLanguageList);

                            foreach (var subtitle in subtitleList.OrderBy(x => (byte)x.Payload2))
                            {
                                //������ ��ü�̸� ��ŵ
                                if (SubtitleLanguageList.Contains(subtitle)) continue;
                                //�����Ͽ� �μ�Ʈ
                                var order = (byte)subtitle.Payload2;
                                var item = SubtitleLanguageList.FirstOrDefault(x => order < (byte)x.Payload2);

                                if (item != null)
                                {
                                    int index = SubtitleLanguageList.IndexOf(item);
                                    SubtitleLanguageList.Insert(index, subtitle);
                                }
                                else
                                {
                                    SubtitleLanguageList.Add(subtitle);
                                }
                            }

                            //�⺻�ڸ��� ǥ���ϵ��� �����Ѵ�.
                            if (SubtitleLanguageList.Count > 0)
                            {
                               //SubtitleLanguageIndex = 0;
                                CurrentSubtitleLanguageCode = SubtitleLanguageList.ElementAt(0).Key;
                            }
                        });
                        break;
                    case "SubtitleUpdated":
                        var subtitleItem = msg.GetValue<PickerItem<string, string>>();
                        DispatcherHelper.CheckBeginInvokeOnUI(() =>
                        {
                            var found = SubtitleLanguageList.FirstOrDefault(x => x.Key == subtitleItem.Key);

                            if (found != null)
                            {
                                found.Payload = subtitleItem.Payload;
                            }
                        });
                        break;
                    case "SubtitlesManuallyLoaded":
                        var manualSubtitleList = msg.GetValue<List<PickerItem<string, string>>>();
                        DispatcherHelper.CheckBeginInvokeOnUI(() =>
                        {
                            //�������� �������� �߰��� �ڸ��� �߰�
                            foreach (var subtitle in manualSubtitleList)
                            {
                                SubtitleLanguageList.Add(subtitle);
                            }
                            //�⺻�ڸ��� ǥ���ϵ��� �����Ѵ�.
                            if (SubtitleLanguageList.Count > 0)
                            {
                                //SubtitleLanguageIndex = SubtitleLanguageList.Count - 1;
                                CurrentSubtitleLanguageCode = SubtitleLanguageList.ElementAt(SubtitleLanguageList.Count - 1).Key;
                            }
                        });
                        break;
                    case "AudioLoadedInMKV":
                        var audioList = msg.GetValue<IEnumerable<PickerItem<string, int>>>();

                        //����� ����Ʈ �ʱ�ȭ
                        AudioLanguageList.Clear();

                        foreach(var audio in audioList)
                        {
                            AudioLanguageList.Add(audio);
                        }
                        
                        if (AudioLanguageList.Count > 0)
                        {
                            //Me�� �����Ǳ� ������.
                            _SelectedAudioLanguage = AudioLanguageList[0];
                            RaisePropertyChanged("SelectedAudioLanguage");
                        }
                        break;
                    //case "SubtitleFrameInMKV":
                    //    AddSubtitleFrameInMKV(msg.GetValue<IEnumerator<FrameBufferData>>());
                    //    break;
                    case "SubtitleInFFmpeg":
                        if (!string.IsNullOrEmpty(CurrentSubtitleLanguageCode))
                        //if (SubtitleLanguageIndex > -1 && SubtitleLanguageList.Count > SubtitleLanguageIndex)
                        {
                            //���õ� �ڸ��� ���� �ڸ��� ���� �߰�
                            //var lang = SubtitleLanguageList[SubtitleLanguageIndex];
                            var lang = SubtitleLanguageList.FirstOrDefault(x => x.Key == CurrentSubtitleLanguageCode);
                            if (lang.Payload != null && lang.Payload is Subtitle
                                && (lang.Payload as Subtitle).SubtitleFileKind == SubtitleFileKind.Internal)
                            {
                                AddSubtitleInFFmpeg(msg.GetValue<SubtitlePacket>());
                            }
                        }
                        break;
                    case "SubtitleMoved":
                        panelType = ControlPanelType.General;
                        GeneralState.IsTriggerOn = false;
                        LockingState.IsTriggerOn = false;
                        SubtitleState.IsTriggerOn = false;
                        break;
                    case "SettingsOpened":
                        //�ڸ� ���� �ö��̾ƿ��� ����. (���� : �ڸ� ���ڵ� �˻� ���нÿ��� ȣ���)
                        IsSubtitleFlyoutOpen = true;
                        FlyoutOpenedCommandExecute(null);
                        break;
                    case "CurrentStateChanged":
                        OnCurrentStateChanged(msg.GetValue<MediaElementState>());
                        break;
                    case "OrientationChanged":
                        OnOrientationChanged();
                        break;
                    case "NextPlay":
                        NextPlayMediaInfo = msg.GetValue<MediaInfo>();
                        break;
                    case "IsPaused":
                        OnPlayPause(msg.GetValue<bool>());
                        break;
                    case "DecoderChangingFailed":
                        var decoderType = msg.GetValue<DecoderType>();
                        InformationMessage = string.Format(ResourceLoader.GetForCurrentView().GetString("NotSupportedDecoder"), decoderType);
                        VisibleIndicator = "None";
                        VisibleIndicator = "Information";
                        break;
                }
            });
        }

        public Dictionary<TimeSpan, List<String>> SubtitleCompensationData = new Dictionary<TimeSpan, List<String>>();

        async void AddSubtitleInFFmpeg(SubtitlePacket subtitlePacket)
        {
//            System.Diagnostics.Debug.WriteLine("cc pop event ====> " + subtitlePacket.rects.ass);

            PickerItem<string, string> subtitleInfo = null;
            await DispatcherHelper.RunAsync(() =>
            {
                if (SubtitleLanguageList.Count > 0)
                {
                    //subtitleInfo = SubtitleLanguageList[SubtitleLanguageIndex];
                    subtitleInfo = SubtitleLanguageList.FirstOrDefault(x => x.Key == CurrentSubtitleLanguageCode);
                }
            });

            if (subtitleInfo != null)
            {
                var subtitle = subtitleInfo.Payload as Subtitle;
                var parser = subtitle.Parser;
                SubtitleTypes type = SubtitleTypes.NA;
                string text = string.Empty;

                if (parser is SrtParser)
                {
                    type = SubtitleTypes.SRT;
                    text = subtitlePacket.rects.text;
                }
                else if (parser is AssParser)
                {
                    type = SubtitleTypes.ASS;
                    text = subtitlePacket.rects.ass;
                }

                if (type != SubtitleTypes.NA)
                {
                    try
                    {
                        TimeSpan pts = TimeSpan.FromTicks(subtitlePacket.pts);
                        TimeSpan sTime = pts.Add(TimeSpan.FromMilliseconds(subtitlePacket.start_display_time));//.Subtract(TimeSpan.FromMilliseconds(100));
                        TimeSpan eTime = pts.Add(TimeSpan.FromMilliseconds(subtitlePacket.end_display_time));

//                        System.Diagnostics.Debug.WriteLine("Current Position : " + Me.Position.ToString(@"h\:mm\:ss\.ff"));
//                        System.Diagnostics.Debug.WriteLine("Subtitle Position: " + sTime.ToString(@"h\:mm\:ss\.ff"));

                        switch (type)
                        {
                            case SubtitleTypes.SRT:
                                //���� ��ȯ
                                text = (parser as SrtParser).ConvertLine(text);
                                break;
                            case SubtitleTypes.ASS:
                                text = crExp.Replace(text, "<br/>");
                                AssParser assParser = parser as AssParser;
                                Dictionary<string, string> tmp = new Dictionary<string, string>();

                                text = text.Replace("Dialogue:", string.Empty);
                                string[] valueArray = text.Split(new char[] { ',' }, assParser.EventsList.Count);
                                List<string> values = valueArray.ToList<string>(); 
                                
                                //���� �ð�
                                values.RemoveAt(1);
                                values.Insert(1, sTime.ToString(@"h\:mm\:ss\.ff"));
                                //���� �ð�
                                //values.RemoveAt(2);
                                //values.Insert(2, eTime.ToString(@"h\:mm\:ss\.ff"));
                                
                                for (int i = 0; i < assParser.EventsList.Count; i++)
                                {
                                    tmp[assParser.EventsList[i].ToLower().Trim()] = values[i];
                                }

                                //�±� ��ȯ
                                text = assParser.ConvertLine(tmp["text"]);

                                //��Ÿ�� ���� (V4+ Style)
                                string styleName = string.Empty;
                                if (tmp.TryGetValue("style", out styleName))
                                {
                                    //���� ��Ÿ�� ����
                                    assParser.SetV4Style(ref text, styleName);
                                }
                          //      System.Diagnostics.Debug.WriteLine(sTime.ToString(@"h\:mm\:ss\.ff") + " ~ " + eTime.ToString(@"h\:mm\:ss\.ff") + " : " + text);
                                break;
                        }

                        await DispatcherHelper.RunAsync(() =>
                        {
                            if (Me != null)
                            {
                                var exisMarker = Me.Markers.FirstOrDefault(x => x.Time == sTime);
                                //���� �ð��� ���� ��Ŀ�� ����
                                if (exisMarker != null)
                                {
                                    if (exisMarker.Text != text)
                                    {
                                        if (string.IsNullOrEmpty(exisMarker.Text))
                                        {
                                            exisMarker.Text = text;
                                        }
                                        else if (!string.IsNullOrEmpty(text))
                                        {
                                            if (!SubtitleCompensationData.ContainsKey(sTime))
                                            {
                                                SubtitleCompensationData[sTime] = new List<string>();
                                            }
                                            //�ߺ��� �߰����� ����
                                            if (!SubtitleCompensationData[sTime].Any(x => x == text))
                                            {
                                                SubtitleCompensationData[sTime].Add(text);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Me.Markers.Add(new TimelineMarker
                                    {
                                        Type = type.ToString(),
                                        Time = sTime,
                                        Text = text
                                    });
                                }

                                //ǥ���ڸ��� ���� �ڸ��� �ʹ� ������ �־ �̺�Ʈ�� �߻����� �ʴ°��� �����Ѵ�.
                                if (Me.Markers.Any(x => (x.Time.Subtract(sTime).TotalMilliseconds > -500 && x.Time.Subtract(sTime).TotalMilliseconds < 500) && string.IsNullOrEmpty(x.Text.Trim())))
                                {
                                    var markers = Me.Markers.Where(x => (x.Time.Subtract(sTime).TotalMilliseconds > -500 && x.Time.Subtract(sTime).TotalMilliseconds < 500) && string.IsNullOrEmpty(x.Text.Trim())).ToArray();
                                    foreach (var marker in markers)
                                    {
                                        Me.Markers.Remove(marker);
                                    }
                                }
                                //var removeMarker = Me.Markers.FirstOrDefault(x => (x.Time.Subtract(sTime).TotalMilliseconds > -500 && x.Time.Subtract(sTime).TotalMilliseconds < 500) && string.IsNullOrEmpty(x.Text));
                                //if (removeMarker != null)
                                //{
                                //    Me.Markers.Remove(removeMarker);
                                //}
                                //else
                                //{
                                //    //System.Diagnostics.Debug.WriteLine("��ȿ�� �����ڸ�");
                                //}

                                if (!Me.Markers.Any(x => x.Time == eTime))
                                {
                                    //���� �ڸ�
                                    if (eTime > sTime)
                                    {
                                        Me.Markers.Add(new TimelineMarker
                                        {
                                            Type = type.ToString(),
                                            Time = eTime,
                                            Text = string.Empty
                                        });
                                    }
                                }
                                
                                //System.Diagnostics.Debug.WriteLine("==================================================>");
                                //foreach(var mk in Me.Markers)
                                //{
                                //    System.Diagnostics.Debug.WriteLine(mk.Time + " : " + mk.Text);
                                //}
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("FFmpeg ���� �ڸ� ���� �߻�. " + e.Message);
                    }
                }
            }
        }
        
        private MediaElementState previousState = MediaElementState.Closed;
        private void OnCurrentStateChanged(MediaElementState state)
        {
            if (previousState != state)
            {
                switch (state)
                {
                    case MediaElementState.Buffering:
                        break;
                    case MediaElementState.Closed:
                        break;
                    case MediaElementState.Opening:
                        break;
                    case MediaElementState.Playing:
                        if (previousState != MediaElementState.Opening 
                            && previousState != MediaElementState.Stopped 
                            && previousState != MediaElementState.Buffering)
                        {
                            SaveLastPosition();
                        }

                        //����ӵ� ����
                        if (Me.PlaybackRate != PlaybackRate)
                        {
                            Me.PlaybackRate = PlaybackRate;
                        }
                        break;
                    case MediaElementState.Paused:
                        SaveLastPosition();
                        break;
                    case MediaElementState.Stopped:
                        timer.Stop();
                        timer.Tick -= Timer_Tick;
                        GeneralState.IsTriggerOn = false;
                        LockingState.IsTriggerOn = false;
                        SubtitleState.IsTriggerOn = false;

                        if (NextPlayMediaInfo != null)
                        {
                            //�����û
                            MessengerInstance.Send(new Message("PlayItem", NextPlayMediaInfo), PlaylistViewModel.NAME);
                            NextPlayMediaInfo = null;
                        }
                        break;
                }
                previousState = state;
            }
        }

        private void OnOrientationChanged()
        {
            UpdateSupportedStretch();
        }

        private void UpdateSupportedStretch()
        {
            var isPortrait = DisplayInformation.AutoRotationPreferences == DisplayOrientations.Portrait || DisplayInformation.AutoRotationPreferences == DisplayOrientations.PortraitFlipped;
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var displayRatio = isPortrait ? bounds.Height / bounds.Width : bounds.Width / bounds.Height;
            var videoRatio = (double)Me.NaturalVideoWidth / Me.NaturalVideoHeight;
            SupportedStretch = displayRatio.ToString("#.##") != videoRatio.ToString("#.##");
            //Ÿ��Ʋ ����� ���� ǥ�� ����, �Ǵ� ����
            AdvColumn = isPortrait ? 0 : 1;
            AdvRow = isPortrait ? 1 : 0;
        }
        
        private void SaveLastPosition()
        {
            MediaInfo.PausedTime = (long)CurrentTime;
            MessengerInstance.Send<Message>(new Message("UpdatePausedTime", MediaInfo), PlaylistViewModel.NAME);
        }

        private void SetupTimer()
        {
            if (!timer.IsEnabled)
            {
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += Timer_Tick;
                timer.Start();
            }
        }

        public void ToggleShow()
        {
            if (!IsFlyoutClosed)
            {
                IsFlyoutClosed = true;
            }
            else
            {
                switch (panelType)
                {
                    case ControlPanelType.None:
                        SubtitleState.Toggle();
                        break;
                    case ControlPanelType.General:
                        RaisePropertyChanged("Battery");
                        RaisePropertyChanged("TimeTT");
                        RaisePropertyChanged("TimeHMM");
                        GeneralState.Toggle();
                        break;
                    case ControlPanelType.Unlock:
                        LockingState.Toggle();
                        break;
                }
            }
        }

        #region Timeline Slider interaction

        private void Timer_Tick(object sender, object e)
        {
            // Don't update the Slider's position while the user is interacting with it
            if (!sliderPressed)
            {
                RaisePropertyChanged("CurrentTime");
                RaisePropertyChanged("LeftTime");
            }
        }

        private long CalculateSliderFrequency(TimeSpan timevalue)
        {
            long stepFrequency = 1;
            var seekTime = Settings.Playback.SeekTimeInterval;

            if (seekTime == 0)
            {
                // Calculate the slider step frequency based on the timespan length
                if (timevalue.TotalHours >= 1)
                {
                    stepFrequency = 60;
                }
                else if (timevalue.TotalMinutes > 30)
                {
                    stepFrequency = 30;
                }
                else if (timevalue.TotalMinutes > 15)
                {
                    stepFrequency = 15;
                }
                else if (timevalue.TotalMinutes > 5)
                {
                    stepFrequency = 5;
                }
                else
                {
                    stepFrequency = (long)Math.Round(timevalue.TotalSeconds / 10, MidpointRounding.AwayFromZero);

                    if (stepFrequency == 0)
                    {
                        stepFrequency = (long)timevalue.TotalSeconds;
                    }
                }
            }
            else
            {
                stepFrequency = seekTime;
            }

            return stepFrequency;
        }
        #endregion

        private void OnLoadedMedia()
        {
            _NotInitializeTime = true;
            sliderPressed = true;
            DecoderSupport ds = DecoderSupport.Instance();
            EnableDecoderType = ds.DecoderTypes.Current != ds.DecoderTypes.Next;

            //ȭ�� ��� ��� ��� ���� (520�� ����� ��⿡�� ���� ���� ������ �߻� MKV)
            if (!Me.IsMfMediaElement && isEntryDevice)
            {
                SupportedRotationLock = false;
                MessengerInstance.Send(new Message("ApplyForceRotation", SimpleOrientation.Rotated90DegreesCounterclockwise), CCPlayerViewModel.NAME);    
            }
            else
            {
                SupportedRotationLock = true;
                //���� ������ ȭ�� ȸ�� ����
                MessengerInstance.Send(new Message("ApplyForceRotation", Settings.Playback.LastPlaybackOrientation), CCPlayerViewModel.NAME);
            }
            
            //�ö��̾ƿ� ���� �ʱ�ȭ
            IsFlyoutClosed = true;
            
            PlaybackRate = 1;
            Volume = 20d;
            Zoom = 1;
            IsPaused = false;
            //�ڸ� ��뿩�� (CCPlayerElemet���� �˸�)
            IsSubtitleOn = true;

            //������ ���� �ٸ� �׸�
            SupportedPlaybackRate = Me.IsMfMediaElement;
            
            //������ �� ��� �ð�
            TimelineStepFrequency = CalculateSliderFrequency(Me.NaturalDuration.TimeSpan);
            
            //�ø��� ��� ��� ����
            UpdateSupportedStretch();
            
            //Ÿ�̸� �ʱ�ȭ
            SetupTimer();
            
            //�̵�� �� �ð� ����
            MediaInfo.RunningTime = (long)Me.NaturalDuration.TimeSpan.TotalSeconds;

            //���� ����̷��� �ð� ����
            if (Me != null && Me.CanSeek)
            {
                var timeDif = MediaInfo.RunningTime - MediaInfo.PausedTime;
                if (timeDif < 3)
                {
                    //����ð� ����
                    MediaInfo.PausedTime = 0;
                }

                if (MediaInfo.PausedTime > 0)
                {
                    CurrentTime = MediaInfo.PausedTime;
                }
            }
            
            //�ڸ� ��ũ 
            SubtitleSyncTimeMin = -10;
            SubtitleSyncTimeMax = 10;
            SubtitleSyncTime = 0;
            
            //����� ����Ʈ �ʱ�ȭ
            AudioLanguageList.Clear();

            for (var i = 0; i < Me.AudioStreamCount; i++)
            {
                string langNativeName = string.Empty;
                try
                {
                    string tmp = string.Empty;

                    // if (MediaInfo.ContentType != "video/x-ms-wmv")
                    //{
                    tmp = Me.GetAudioStreamLanguage(i);
                    //}

                    CultureInfo cultureInfo = null;
                    string[] langs = tmp.Split(new string[] { "___" }, StringSplitOptions.RemoveEmptyEntries);

                    string langEngName = string.Empty;

                    if (langs != null && langs.Length >= 1 && !string.IsNullOrEmpty(langs[0]))
                    {
                        if (langs[0].Length == 2 || (langs[0].Length == 5 && langs[0].ElementAt(2) == '-'))
                        {
                            cultureInfo = new CultureInfo(langs[0]);
                        }
                        else if (langs[0].Length == 3)
                        {
                            cultureInfo = LanguageCodeHelper.LangCodeToCultureInfo(langs[0]);
                        }

                        if (cultureInfo != null)
                        {
                            langNativeName = cultureInfo.NativeName;
                            langEngName = cultureInfo.EnglishName;
                        }

                        if (langs.Length >= 2 && !string.IsNullOrEmpty(langs[1]))
                        {
                            if (string.IsNullOrEmpty(langNativeName))
                            {
                                langNativeName = langs[1];
                            }
                            else
                            {
                                string[] titles = langs[1].Split(new char[] { ':' });

                                if (titles.Length >= 1)
                                {
                                    langNativeName += " : ";
                                    if (titles[0] != langEngName && titles[0] != langNativeName)
                                    {
                                        langNativeName += langs[1].Trim();
                                    }
                                    else if (titles.Length >= 2 && !string.IsNullOrEmpty(titles[1]))
                                    {
                                        langNativeName += titles[1].Trim();
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)  
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message); 
                }

                if (string.IsNullOrEmpty(langNativeName))
                {
                    langNativeName = LanguageCodeHelper.UNKOWN_LANGUAGE;
                }

                AudioLanguageList.Add(new PickerItem<string, int>
                {
                    Name = langNativeName,
                    Key = i
                });
            }

            if (AudioLanguageList.Count > 0)
            {
                SelectedAudioLanguage = AudioLanguageList[0];
            }

            var resource = ResourceLoader.GetForCurrentView();
            AspectRatioList.Clear();
            AspectRatioList.Add(new PickerItem<string, int>() { Key = (int)CCPlayer.UI.Xaml.Controls.AspectRatio.Uniform, Name = resource.GetString("AspectRatioItems/Uniform") });
            AspectRatioList.Add(new PickerItem<string, int>() { Key = (int)CCPlayer.UI.Xaml.Controls.AspectRatio.UniformToFill, Name = resource.GetString("AspectRatioItems/UniformToFill") });
            AspectRatioList.Add(new PickerItem<string, int>() { Key = (int)CCPlayer.UI.Xaml.Controls.AspectRatio.Fill, Name = resource.GetString("AspectRatioItems/Fill") });

            if (Me.IsMfMediaElement && !Me.ForceUseMediaElement)
            {
                AspectRatioList.Add(new PickerItem<string, int>() { Key = (int)CCPlayer.UI.Xaml.Controls.AspectRatio.R16_10, Name = "16 : 10" });
                AspectRatioList.Add(new PickerItem<string, int>() { Key = (int)CCPlayer.UI.Xaml.Controls.AspectRatio.R16_9, Name = "16 : 9" });
                AspectRatioList.Add(new PickerItem<string, int>() { Key = (int)CCPlayer.UI.Xaml.Controls.AspectRatio.R185_1, Name = "1.85 : 1" });
                AspectRatioList.Add(new PickerItem<string, int>() { Key = (int)CCPlayer.UI.Xaml.Controls.AspectRatio.R235_1, Name = "2.35 : 1" });
                AspectRatioList.Add(new PickerItem<string, int>() { Key = (int)CCPlayer.UI.Xaml.Controls.AspectRatio.R4_3, Name = "4 : 3" });
            }
            //�⺻��
            SelectedAspectRatio = AspectRatioList[0];

            //���� �� ���� ���� �ε�
            var asyncAction = ThreadPool.RunAsync(async handler =>
            {
                //���� �� ���� ���� �񵿱� �ε�
                var prev = fileDAO.GetPrevMediaInfo(MediaInfo.Path);
                var next = fileDAO.GetNextMediaInfo(MediaInfo.Path);
                //UI �����忡 ������ �ݿ�
                await DispatcherHelper.RunAsync(() =>
                {
                    MediaInfo.PreviousMediaInfo = prev;
                    MediaInfo.NextMediaInfo = next;
                });
            });

            GeneralState.Toggle();
            sliderPressed = false;
        }
        
        private void Seek(long sec)
        {
            if (!Me.CanSeek)
            {
                return;
            }
            var pos = CurrentTime + sec;

            if (pos < 0)
            {
                pos = 0;
            }
            else if (pos >= Me.NaturalDuration.TimeSpan.TotalSeconds)
            {
                pos = Me.NaturalDuration.TimeSpan.TotalSeconds;
            }
            CurrentTime = pos;
        }
    }
}
