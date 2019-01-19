using CCPlayer.WP81.Extensions;
using CCPlayer.WP81.Helpers;
using CCPlayer.WP81.Models;
using CCPlayer.WP81.Models.DataAccess;
using CCPlayer.WP81.Views;
using FFmpegSupport;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Store;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

/**
    * FutureAccessList���� "All videos" �� "File Accosiation"�� ������ ���ϵ��̴�. 
    * 
    * !!!! ��������
    *  - File Accosication �� FutureAccessList�� ȿ���� Ȱ�� �׸��� SQL Lite�� �������.
    *  SQL Lite : 
    *  https://code.msdn.microsoft.com/windowsapps/WindowsPhone-8-SQLite-96a1e43b
    *  http://www.sqlite.org/docs.html
    *  http://channel9.msdn.com/Series/Building-Apps-for-Windows-Phone-8-1/19
    *  SQLite-NET vs SQLitePCL (SQLite portable class library)
    */
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
    public partial class MainViewModel : ViewModelBase
    {
        public static readonly string NAME = typeof(MainViewModel).Name;
        //���� command http://rarcher.azurewebsites.net/Post/PostContent/26
        //���� behavior http://www.reflectionit.nl/Blog/2013/windows-8-xaml-tips-conditional-behaviors
        #region ������ ��

        //��� Ÿ��Ʋ
        public string AppVersion { get { return VersionHelper.VersionName; } }
        
        //����
        public Settings Settings { get; set; }
        //�������� DAO
        private SettingDAO settingDAO;
        private Hub hub;
        //��ü ���� ��� ���� �����
        private HubSection currentHubSection;
        //��ü ���� ���� �����
        private HubSection allVideoHubSection;

        private bool isPlayerOpened;
        private bool isSearchOpened;
        private bool isSettingsOpened;

        private bool _LoadingPanelVisible;
        public bool LoadingPanelVisible
        {
            get { return _LoadingPanelVisible; } 
            set 
            {
                if (_LoadingPanelVisible != value)
                Set(ref _LoadingPanelVisible, value, true);
            }
        }

        private string _LoadingPanelText;
        public string LoadingPanelText
        {
            get { return _LoadingPanelText; }
            set { Set(ref _LoadingPanelText, value); }
        }

        #endregion

        #region Ŀ�ǵ�

        public ICommand SectionsInViewChangedCommand { get; private set; }
        public ICommand NavigateToSettingsCommand { get; set; }
        
        #endregion
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(SettingDAO settingDAO, Windows.Media.MediaExtensionManager extMgr)
        {
            this.settingDAO = settingDAO;

            this.CreateModels();
            this.CreateCommands();
            this.RegisterMessages();

            //���� ������ �ε�
            LoadSetting();
            //�ڷΰ��� ��ư �̺�Ʈ ���
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            //ȭ�� ȸ�� ����
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;
            //���� �̺�Ʈ �߰�
            App.Current.UnhandledException += Current_UnhandledException;
            //Ȱ��ȭ �̺�Ʈ 
            DispatcherHelper.CheckBeginInvokeOnUI(() => { Window.Current.Activated += Current_Activated; });
        }

        private void CreateModels()
        {
        }
        
        private void CreateCommands()
        {
            SectionsInViewChangedCommand = new RelayCommand<SectionsInViewChangedEventArgs>(SectionsInViewChangedCommandExecute);
            NavigateToSettingsCommand = new RelayCommand(NavigateToSettingsCommandExecute);
        }

        private void RegisterMessages()
        {
            MessengerInstance.Register<PropertyChangedMessage<bool>>(this, msg =>
                {
                    switch(msg.PropertyName)
                    {
                        case "IsPlayerOpened":
                            isPlayerOpened = msg.NewValue;
                            MessengerInstance.Send(isPlayerOpened, typeof(MainPage).FullName);
                            break;
                        case "IsSearchOpened":
                            isSearchOpened = msg.NewValue;
                            break;
                        case "IsSettingsOpened":
                            isSettingsOpened = msg.NewValue;
                            break;
                    }
                }
            );

            MessengerInstance.Register<Message>(this, msg =>
            {
                switch (msg.Key)
                {
                    case "RerfershAppVersion":
                        RaisePropertyChanged("AppVersion");
                        break;
                }
            });

            MessengerInstance.Register<Message>(this, NAME, (msg) =>
            {
                switch(msg.Key)
                {
                    case "ConfirmTermination" :
                        //���� Ȯ��
                        ApplicationExit();
                        break;
                    case "MoveToPlaylistSection":
                        //�÷��� ����Ʈ�� ��꼽�� �̵�
                        MessengerInstance.Send<Message>(new Message("MoveToSection", currentHubSection), PlaylistViewModel.NAME);
                        break;
                    case "CheckSearchElement":
                        OnCheckSearchElement();
                        break;
                    case "ShowLoadingPanel":
                        OnShowLoadingPanel(msg.GetValue<KeyValuePair<string, bool>>());
                        break;
                    case "ShowErrorMessage":
                        OnShowErrorMessage(msg.GetValue<StackPanel>());
                        break;
                    case "ShowSelectionAudioMessage":
                        ShowSelectionAudioMessage(msg.GetValue<StackPanel>());
                        break;
                    case "RemoveAllVideoSection":
                        if (hub != null)
                        {
                            if (hub.Sections.Any(x => x.Name == "AllVideoSection"))
                            {
                                allVideoHubSection = hub.SectionsInView.First(x => x.Name == "AllVideoSection");
                                hub.Sections.Remove(allVideoHubSection);
                            }
                        }
                        break;
                    case "InsertAllVideoSectino":
                        if (hub != null)
                        {
                            if (!hub.Sections.Any(x => x.Name == "AllVideoSection"))
                            {
                                hub.Sections.Insert(1, allVideoHubSection);
                            }
                        }
                        break;
                }
            });
        }
        
        async void OnShowLoadingPanel(KeyValuePair<string, bool> param)
        {
            await ThreadPool.RunAsync(async handler =>
            {
                await Task.Delay(100);
                await DispatcherHelper.RunAsync(() =>
                {
                    LoadingPanelText = param.Key;
                    LoadingPanelVisible = param.Value;
                });
            });
        }

        private void OnShowErrorMessage(StackPanel contentPanel)
        {
            try
            {
                //����г� �ݱ� ��û
                if (isPlayerOpened)
                {
                    MessengerInstance.Send(new Message("ExitPlay", true), CCPlayerViewModel.NAME);
                }

                //�ε� �г� ����
                if (LoadingPanelVisible)
                {
                    OnShowLoadingPanel(new KeyValuePair<string, bool>(string.Empty, false));
                }

                if (App.ContentDlgOp != null) return;

                ContentDialog contentDlg = new ContentDialog
                {
                    Content = contentPanel,
                    PrimaryButtonText = ResourceLoader.GetForCurrentView().GetString("Close")
                };
                //�޼��� â ���
                App.ContentDlgOp = contentDlg.ShowAsync();
                //��ó���� ���
                App.ContentDlgOp.Completed = new AsyncOperationCompletedHandler<ContentDialogResult>((op, status) =>
                {
                    App.ContentDlgOp = null;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            }
        }

        private void ShowSelectionAudioMessage(StackPanel contentPanel)
        {
            try
            {
                //����г� �ݱ� ��û
                if (isPlayerOpened)
                {
                    MessengerInstance.Send(new Message("ExitPlay", true), CCPlayerViewModel.NAME);
                }

                //�ε� �г� ����
                if (LoadingPanelVisible)
                {
                    OnShowLoadingPanel(new KeyValuePair<string, bool>(string.Empty, false));
                }

                if (App.ContentDlgOp != null) return;

                ContentDialog contentDlg = new ContentDialog
                {
                    Content = contentPanel,
                    PrimaryButtonText = ResourceLoader.GetForCurrentView().GetString("OK")
                };
                //�޼��� â ���
                App.ContentDlgOp = contentDlg.ShowAsync();
                //��ó���� ���
                App.ContentDlgOp.Completed = new AsyncOperationCompletedHandler<ContentDialogResult>((op, status) =>
                {
                    App.ContentDlgOp = null;
                    StreamInformation streamInfo = null;

                    foreach (StackPanel panel in contentPanel.Children.Where(x => x is StackPanel))
                    {
                        if (panel != null)
                        {
                            RadioButton rb = panel.Children.FirstOrDefault(x => x is RadioButton) as RadioButton;
                            if (rb != null && rb.IsChecked == true)
                            {
                                streamInfo = rb.Tag as StreamInformation;
                                break;
                            }
                        }
                    }

                    if (streamInfo != null)
                    {
                        MessengerInstance.Send(new Message("MultiAudioSelecting", streamInfo), CCPlayerViewModel.NAME);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            }
        }

        private void LoadSetting()
        {
            Settings = settingDAO.SelectAll();
        }

        #region Ŀ�ǵ��ڵ鷯

        void SectionsInViewChangedCommandExecute(SectionsInViewChangedEventArgs e)
        {
            if (hub == null)
            {
                HubSection section = null;
                if (e.AddedSections.Count > 0)
                {
                    section = e.AddedSections[0];
                }
                else if (e.RemovedSections.Count > 0)
                {
                    section = e.RemovedSections[0];
                }
                else
                {
//                    System.Diagnostics.Debug.WriteLine("��� ����");
                    return;
                }

                hub = Lime.Xaml.Helpers.ElementHelper.FindVisualParent<Hub>(section);
            }

            if (!Settings.General.UseAllVideoSection && hub.Sections.Any(x => x.Name == "AllVideoSection"))
            {
                allVideoHubSection = hub.Sections.First(x => x.Name == "AllVideoSection");
                hub.Sections.Remove(allVideoHubSection);
            }
            
            //if (e.AddedSections.Count > 0)
            //{
            //    //���� �۹� ó���� ���� �Ʒ��� ���� �밡�� ����
            //    HubSection newSection = null;
            //    if (e.AddedSections[0] == hub.SectionsInView.FirstOrDefault())
            //    {
            //        //�·� �̵�
            //        newSection = e.AddedSections[0];
            //    }
            //    else if (e.AddedSections[0] == hub.SectionsInView.LastOrDefault())
            //    {
            //        //��� �̵�
            //        newSection = hub.SectionsInView[1];
            //    }
            //    else
            //    {
            //        //ù �������� �Ųٷ� ���� ���
            //        newSection = hub.SectionsInView[0];
            //    }

            //    this.MessengerInstance.Send<Message>(new Message("Activated", newSection), newSection.ViewModelName());  
            //    currentHubSection = newSection;
            //}
            //else
            {
                //���� ��쿡�� �밡�� ������ ���� ���¸� �ݿ� ���ϴ� ��츦 ����
                var newSection = hub.SectionsInView[0];
                //������ �ٸ� ��츸 ó��
                if (currentHubSection != newSection)
                {
                    this.MessengerInstance.Send<Message>(new Message("Activated", newSection), newSection.ViewModelName());  
                    currentHubSection = newSection;
                }
            }
        }

        private void NavigateToSettingsCommandExecute()
        {
            MessengerInstance.Send(new Message("SettingsOpened", true), SettingsViewModel.NAME);
        }
        #endregion

        void Current_Activated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
            {
                MessengerInstance.Send(new Message("Deactivated"), CCPlayerViewModel.NAME);
            }
            else
            {
                MessengerInstance.Send(new Message("Activated"), CCPlayerViewModel.NAME);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Current_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            if (e != null)
            {
#if AD
                Exception exception = e.Exception;
                if (exception is NullReferenceException && exception.ToString().ToUpper().Contains("SOMA"))
                {
                    Debug.WriteLine("Handled Smaato null reference exception {0}", exception);
                    e.Handled = true;
                    return;
                }
                else if (exception is NullReferenceException && exception.ToString().ToUpper().Contains("MICROSOFT.ADVERTISING"))
                {
                    Debug.WriteLine("Handled Microsoft.Advertising exception {0}", exception);
                    e.Handled = true;
                    return;
                }


#endif
                // APP SPECIFIC HANDLING HERE
                if (Debugger.IsAttached)
                {
                    // An unhandled exception has occurred; break into the debugger
                    Debugger.Break();
                }

                //ó�� �Ϸ� ���� ����
                e.Handled = true;

                StackPanel content = new StackPanel();
                content.Children.Add(new TextBlock
                {
                    Text = e.Message,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 12, 0, 12)
                });

                OnShowErrorMessage(content);
            }
            
        }

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            var targetName = currentHubSection.ViewModelName();
            //�ε� �г��� ǥ�õǰ� ������ ����
            if (LoadingPanelVisible)
            {
                LoadingPanelVisible = false;
            }
            
            if (SimpleIoc.Default.ContainsCreated<CCPlayerViewModel>() && isPlayerOpened)
            {
                targetName = CCPlayerViewModel.NAME;
            }
            else if (SimpleIoc.Default.ContainsCreated<MediaSearchViewModel>()  && isSearchOpened)
            {
                targetName = MediaSearchViewModel.NAME;
            }
            else if (SimpleIoc.Default.ContainsCreated<SettingsViewModel>() && isSettingsOpened)
            {
                targetName = SettingsViewModel.NAME;
            }
            
            this.MessengerInstance.Send<Message>(new Message("BackPressed", e), targetName);       
        }

        private void ApplicationExit()
        {
            if (App.ContentDlgOp != null) return;

            var loader = ResourceLoader.GetForCurrentView();
            bool? result = null;
            var contentDlg = new ContentDialog()
            {
                Content = new TextBlock { Text = loader.GetString("Message/Exit"), TextWrapping = TextWrapping.Wrap },
                PrimaryButtonText = loader.GetString("Ok"),
                PrimaryButtonCommand = new RelayCommand(() => { result = true; }),
                SecondaryButtonText = loader.GetString("Cancel"),
                SecondaryButtonCommand = new RelayCommand(() => { result = false; })
            };

            //�޼��� â ���
            App.ContentDlgOp = contentDlg.ShowAsync();
            //��ó���� ���
            App.ContentDlgOp.Completed = new AsyncOperationCompletedHandler<ContentDialogResult>((op, status) =>
            {
                if (result == true)
                {
                    Window.Current.Activated -= Current_Activated;
                    //���α׷� ���� ��û
                    Application.Current.Exit();
                };
                App.ContentDlgOp = null;
            });
        }

        private async void OnCheckSearchElement()
        {
            if (!SimpleIoc.Default.ContainsCreated<MediaSearchViewModel>())
            {
//                System.Diagnostics.Debug.WriteLine("�˻� �г� ������...");
                var mediaSearchDataContext = SimpleIoc.Default.GetInstance<MediaSearchViewModel>();
                await DispatcherHelper.RunAsync(() =>
                {
                    var frame = Window.Current.Content as Frame;
                    if (mediaSearchDataContext != null &&
                        frame != null && frame.Content is Page)
                    {
                        var main = frame.Content as Page;
                        MediaSearch ms = null;
                        (main.Content as Panel).Children.Add(ms =
                            new MediaSearch()
                            {
                                DataContext = mediaSearchDataContext,
                                Visibility = Visibility.Collapsed
                            });
                        Grid.SetRowSpan(ms, 2);
//                        System.Diagnostics.Debug.WriteLine("�˻� �г� �߰� �Ϸ�");
                    }
                });
            }
        }
    }
}
