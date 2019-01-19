using CCPlayer.WP81.Extensions;
using CCPlayer.WP81.Helpers;
using CCPlayer.WP81.Managers;
using CCPlayer.WP81.Models;
using CCPlayer.WP81.Models.DataAccess;
using CCPlayer.WP81.Strings;
using CCPlayer.WP81.Views;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using GalaSoft.MvvmLight.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Lime.Xaml.Helpers;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.System.Threading;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

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
    public partial class PlaylistViewModel : ViewModelBase
    {
        public static readonly string NAME = typeof(PlaylistViewModel).Name;
        #region ������ ��

        private LoadingMode loadingModel;
        public ObservableCollection<MediaInfo> PlaylistSource { get; set; }
        
        private ListViewReorderMode _ReorderMode;
        public ListViewReorderMode ReorderMode 
        {
            get
            {
                return _ReorderMode;
            }
            set
            {
                var prev = _ReorderMode;
                if (Set(ref _ReorderMode, value))
                {
                    //����� ��� ���������� ��ư ���� �� ���� ����Ʈ ����
                    if (prev == ListViewReorderMode.Enabled)
                    {
                        MainButtonGroupVisible = true;
                        CheckListButtonGroupVisible = false;
                        ReorderButtonGroupVisible = false;

                        //�ٽ� �ε�
                        PlaylistSource.Clear();
                        LoadFiles();
                    }
                }
            }
        }

        public MediaInfo SelectedItem { get; set; }
        public Settings.GeneralSetting GeneralSetting { get; private set; }
        private FileDAO fileDAO;

        private ListViewSelectionMode _SelectionMode;
        public ListViewSelectionMode SelectionMode
        {
            get
            {
                return _SelectionMode;
            }
            set
            {
                Set(ref _SelectionMode, value);
            }
        }

        private bool _MainButtonGroupVisible;
        public bool MainButtonGroupVisible
        {
            get
            {
                return _MainButtonGroupVisible;
            }
            set
            {
                Set(ref _MainButtonGroupVisible, value);
            }
        }

        private bool _CheckListButtonGroupVisible;
        public bool CheckListButtonGroupVisible
        {
            get
            {
                return _CheckListButtonGroupVisible;
            }
            set
            {
                Set(ref _CheckListButtonGroupVisible, value);
            }
        }

        private bool _ReorderButtonGroupVisible;
        public bool ReorderButtonGroupVisible
        {
            get
            {
                return _ReorderButtonGroupVisible;
            }
            set
            {
                Set(ref _ReorderButtonGroupVisible, value);
            }
        }

        private bool _CheckListButtonEnable;
        public bool CheckListButtonEnable
        {
            get
            {
                return _CheckListButtonEnable;
            }
            set
            {
                Set(ref _CheckListButtonEnable, value);
            }
        }

        private bool _ReorderButtonEnable;
        public bool ReorderButtonEnable
        {
            get
            {
                return _ReorderButtonEnable;
            }
            set
            {
                Set(ref _ReorderButtonEnable, value);
            }
        }

        private bool _RemoveButtonEnable;
        public bool RemoveButtonEnable
        {
            get
            {
                return _RemoveButtonEnable;
            }
            set
            {
                Set(ref _RemoveButtonEnable, value);
            }
        }
        #endregion

        #region Ŀ�ǵ�
        public ICommand LoadedPlaylistCommand { get; private set; }
        public ICommand SelectionChangedCommand { get; private set; }
        public ICommand ItemClickCommand { get; private set; }

        public ICommand CheckListButtonClickCommand { get; private set; }
        public ICommand SynchronizeButtonClickCommand { get; private set; }
        public ICommand ReorderButtonClickCommand { get; private set; }
        public ICommand BackButtonClickCommand { get; set; }
        public ICommand SelectAllButtonClickCommand { get; private set; }
        public ICommand ResetPositionClickCommand { get; private set; }
        public ICommand RemoveButtonClickCommand { get; private set; }
        public ICommand AcceptButtonClickCommand { get; private set; }

        #endregion

        #region Ŀ�ǵ� �ڵ鷯

        void SelectionChangedCommandExecute(SelectionChangedEventArgs e)
        {
            RemoveButtonEnable = SelectedItem != null;
        }

        void ItemClickCommandExecute(ItemClickEventArgs e)
        {
            var mediaInfo = e.ClickedItem as MediaInfo;
            if (string.IsNullOrEmpty(mediaInfo.OccuredError))
            {
                //�ε� �г� ǥ��
                var loader = ResourceLoader.GetForCurrentView();
                var msg = string.Format(loader.GetString("Loading"), loader.GetString("Video"));
                MessengerInstance.Send(new Message("ShowLoadingPanel", new KeyValuePair<string, bool>(msg, true)), MainViewModel.NAME);

                //��� ����
                PlayItem(mediaInfo);
            }
        }

        private void CheckListButtonClickCommandExecute()
        {
            //���� ��� �����
            SelectionMode = ListViewSelectionMode.Multiple;
            MainButtonGroupVisible = false;
            CheckListButtonGroupVisible = true;
            ReorderButtonGroupVisible = false;
            RemoveButtonEnable = false;
        }

        private void SynchronizeButtonClickCommandExecute()
        {
            PlaylistSource.Clear();
            LoadFiles();
        }

        private void ReorderButtonClickCommandExecute()
        {
            MainButtonGroupVisible = false;
            CheckListButtonGroupVisible = false;
            ReorderButtonGroupVisible = true;
            ReorderMode = ListViewReorderMode.Enabled;
        }

        private void BackButtonClickCommandExecute()
        {
            //���� ��� ����
            SelectionMode = ListViewSelectionMode.None;
            ReorderMode = ListViewReorderMode.Disabled;
            MainButtonGroupVisible = true;
            CheckListButtonGroupVisible = false;
            ReorderButtonGroupVisible = false;
        }

        private void SelectAllButtonClickCommandExecute(ListView listView)
        {
            if (listView.SelectedItems.Count > 0)
            {
                listView.SelectedItems.Clear();
            }
            else
            {
                listView.SelectAll();
            }
        }

        private void ResetPositionClickCommandExecute(ListView listView)
        {
            for (int j = listView.SelectedItems.Count - 1; j >= 0; j--)
            {
                MediaInfo mi = listView.SelectedItems[j] as MediaInfo;
                mi.PausedTime = 0;
            }
            //����ð� �ʱ�ȭ ����
            fileDAO.UpdatePlayList(listView.SelectedItems.AsEnumerable());

            SelectionMode = ListViewSelectionMode.None;
            MainButtonGroupVisible = true;
            CheckListButtonGroupVisible = false;
            ReorderButtonGroupVisible = false;
        }

        private void RemoveButtonClickCommandExecute(ListView listView)
        {
            for (int j = listView.SelectedItems.Count - 1; j >= 0; j--)
            {
                OnRemovePlayList(listView.SelectedItems[j] as MediaInfo);
            }

            CheckPlaylistCount();
        }

        private void AcceptButtonClickCommandExecute(ListView listView)
        {
            //�ٽ� ����
            SaveFiles();

            MainButtonGroupVisible = true;
            CheckListButtonGroupVisible = false;
            ReorderButtonGroupVisible = false;
            //���� ó���� ���� ���� ȭ�� ����
            _ReorderMode = ListViewReorderMode.Disabled;
            RaisePropertyChanged("ReorderMode");
        }

        #endregion

        public PlaylistViewModel(FileDAO fileDAO, SettingDAO settingDAO)
        {
            this.fileDAO = fileDAO;
            this.GeneralSetting = settingDAO.SettingCache.General;

            this.CreateModels();
            this.CreateCommands();
            this.RegisterMessages();
        }

        private void CreateModels()
        {
            loadingModel = LoadingMode.Caching;
            PlaylistSource = new ObservableCollection<MediaInfo>();
            MainButtonGroupVisible = true;
            CheckListButtonGroupVisible = false;
            ReorderButtonGroupVisible = false;
        }

        private void CreateCommands()
        {
            //������ �ε�
            SelectionChangedCommand = new RelayCommand<SelectionChangedEventArgs>(SelectionChangedCommandExecute);
            ItemClickCommand = new RelayCommand<ItemClickEventArgs>(ItemClickCommandExecute);

            CheckListButtonClickCommand = new RelayCommand(CheckListButtonClickCommandExecute);
            SynchronizeButtonClickCommand = new RelayCommand(SynchronizeButtonClickCommandExecute);
            ReorderButtonClickCommand = new RelayCommand(ReorderButtonClickCommandExecute);
            BackButtonClickCommand = new RelayCommand(BackButtonClickCommandExecute);
            SelectAllButtonClickCommand = new RelayCommand<ListView>(SelectAllButtonClickCommandExecute);
            ResetPositionClickCommand = new RelayCommand<ListView>(ResetPositionClickCommandExecute);
            RemoveButtonClickCommand = new RelayCommand<ListView>(RemoveButtonClickCommandExecute);
            AcceptButtonClickCommand = new RelayCommand<ListView>(AcceptButtonClickCommandExecute);
        }

        /// <summary>
        /// �ٸ� ��𵨵�� ���� ���ŵ� �޼����� ó���Ѵ�.
        /// </summary>
        private void RegisterMessages()
        {
            //������ �޼��� ����
            MessengerInstance.Register<Message>(this, NAME, (msg) =>
            {
                switch (msg.Key)
                {
                    case "Activated":
                        if (loadingModel != LoadingMode.None)
                        {
                            PlaylistSource.Clear();
                            LoadFiles();
                        }
                        break;
                    case "MoveToSection":
                        //������ �������� �̵�
                        MoveToSection(msg.GetValue<HubSection>());
                        break;
                    case "BackPressed":
                        msg.GetValue<BackPressedEventArgs>().Handled = true;
                        if (SelectionMode != ListViewSelectionMode.None)
                        {
                            //���� ��� ����
                            SelectionMode = ListViewSelectionMode.None;
                            MainButtonGroupVisible = true;
                            CheckListButtonGroupVisible = false;
                            ReorderButtonGroupVisible = false;
                        }
                        else
                        {
                            //���� Ȯ��
                            MessengerInstance.Send<Message>(new Message("ConfirmTermination", null), MainViewModel.NAME);
                        }
                        break;
                    case "FolderDeleted":
                        //��꼽���� �ݴ� �������� ������ ���, ��� ���� DB�� �ʱ�ȭ �Ǿ� ���� ������ �ʱ�ȭ�� ��Ų��.
                        MessengerInstance.Send<Message>(new Message("CheckFolderSyncForPlaylist", null), AllVideoViewModel.NAME);
                        //Ž���⿡�� ������ ���� ���� Trigger
                        //��� ��� ������ ��� ���� ���ϰ� ��� ����� ����ȭ�Ͽ� �������� �ʴ� ������ �����Ͽ��� ����
                        fileDAO.CleanPlayList();
                        //�ε� ��û ���� ����
                        loadingModel = LoadingMode.Caching;
                        break;
                    case "PlayItem":
                        PlayItem(msg.GetValue<MediaInfo>());
                        break;
                    case "PlayList":
                        loadingModel = LoadingMode.Caching;
                        var list = msg.GetValue<IEnumerable<MediaInfo>>();
                        MessengerInstance.Send<Message>(new Message("MoveToPlaylistSection", list.Count() > 1), CCPlayerViewModel.NAME);
                        //��� ��� ������ ��� ���� ���ϰ� ��� ����� ����ȭ�Ͽ� �������� �ʴ� ������ �����Ͽ��� ����
                        fileDAO.CleanPlayList();
                        //��� ��� ����
                        MakePlaylist(list);
                        break;
                    case "UpdatePausedTime":
                        var source = msg.GetValue<MediaInfo>();
                        fileDAO.UpdatePlayList(new MediaInfo[] { source });
                        //ȭ�� ������Ʈ ó��
                        var item = PlaylistSource.FirstOrDefault(x => x.Path == source.Path);
                        if (item != null)
                        {
                            item.RunningTime = source.RunningTime;
                            item.PausedTime = source.PausedTime;
                        }
//                        System.Diagnostics.Debug.WriteLine(string.Format("����ð� ������Ʈ : {0}", TimeSpan.FromSeconds(source.PausedTime)));
                        break;
                    case "FileAssociation":
                        var value = msg.GetValue<FileActivatedEventArgs>();
                        if (value.Files != null && value.Files.Count > 0)
                        {
                            var file = value.Files.FirstOrDefault();

                            if (file != null && file.IsOfType(Windows.Storage.StorageItemTypes.File))
                            {
                                var mi = new MediaInfo((StorageFile)file);

                                //�ε� �г� ǥ��
                                var loader = ResourceLoader.GetForCurrentView();
                                var loadingMsg = string.Format(loader.GetString("Loading"), loader.GetString("Video"));
                                MessengerInstance.Send(new Message("ShowLoadingPanel", new KeyValuePair<string, bool>(loadingMsg, true)), MainViewModel.NAME);
                                
                                DispatcherHelper.CheckBeginInvokeOnUI(async () =>
                                {
                                    try
                                    {
                                        StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(mi.ParentFolderPath);
                                        IReadOnlyList<StorageFile> fileStorageList = await folder.GetFilesAsync();
                                        List<StorageFile> subtitleList = fileStorageList.Where(x => x.IsSubtitleFile()).ToList();

                                        var pathName = mi.Path.Remove(mi.Path.Length - Path.GetExtension(mi.Path).Length);
                                        //�ڸ� �˻�
                                        foreach (var ext in CCPlayerConstant.SUBTITLE_FILE_SUFFIX)
                                        {
                                            StorageFile subtitleFile = null;
                                            try
                                            {
                                                subtitleFile = new List<StorageFile>(subtitleList).FirstOrDefault(x => Path.GetExtension(x.Path).ToUpper() == ext.ToUpper()
                                                    && x.Path.Length > ext.Length && x.Path.Remove(x.Path.Length - ext.Length).ToUpper().Contains(pathName.ToUpper()));
                                            }
                                            catch (Exception) { }

                                            if (subtitleFile != null)
                                            {
                                                subtitleList.Remove(subtitleFile);

                                                //�ڸ��� �̵�� ���Ͽ� ����
                                                mi.AddSubtitle(new SubtitleInfo(subtitleFile));
                                            }
                                        }
                                    }
                                    catch (System.UnauthorizedAccessException)
                                    { }

                                    //��� ó��
                                    MessengerInstance.Send<Message>(new Message("Play", mi), CCPlayerViewModel.NAME);
                                });
                            }
                        }
                        break;
                    case "ShowErrorFile":
                        if (PlaylistSource.Any())
                        {
                            var kv = msg.GetValue<KeyValuePair<string, MediaInfo>>();
                            var mi = PlaylistSource.FirstOrDefault(f => f.Path == kv.Value.Path);
                            if (mi != null)
                            {
                                mi.OccuredError = kv.Key + "\n";
                            }
                        }
                        break;
                    case "RemovePlayList":
                        OnRemovePlayList(msg.GetValue<MediaInfo>());
                        break;
                }
            });
        }

        /// <summary>
        /// ȭ�鿡 �����͸� �ε��Ѵ�.
        /// </summary>
        async void LoadFiles()
        {
            Stopwatch st = null;
            if (Debugger.IsAttached)
            {
                st = new Stopwatch();
                st.Start();
            }

            await ThreadPool.RunAsync(async handler =>
            {
                //�Ϸ� ��ǥ
                loadingModel = LoadingMode.None;
                //������ DB���� (1 ~ 100��, �ڸ��� �ε�)
                var miList = new List<MediaInfo>();
                fileDAO.LoadPlayList(miList, 100, 0, true);
                //ȭ�鿡 �ݿ�
                foreach (var mi in miList)
                {
                    await DispatcherHelper.RunAsync(() => { PlaylistSource.Add(mi); });
                }

                await DispatcherHelper.RunAsync(() => 
                {
                    CheckListButtonEnable = miList.Count > 0;
                    ReorderButtonEnable = miList.Count > 1;
                });
            });

            if (Debugger.IsAttached)
            {
                System.Diagnostics.Debug.WriteLine("������ �ε� : " + st.Elapsed);
            }
        }

        /// <summary>
        /// ȭ���� �����͸� �ٽ� �����Ѵ�.
        /// </summary>
        async void SaveFiles()
        {
            await ThreadPool.RunAsync(handler =>
            {
                //������ DB�� ���� �߰�
                fileDAO.InsertPlayList(PlaylistSource.Reverse());
            });
        }

        /// <summary>
        /// ���� ����� ��û�ϰ�, �ܰǿ� ���� �����Ͽ� �߰��Ѵ�.
        /// </summary>
        /// <param name="fileInfo"></param>
        void PlayItem(MediaInfo fileInfo)
        {
            //������ ��尡 �ƴҶ��� ó��
            if (ReorderMode == ListViewReorderMode.Disabled
                && string.IsNullOrEmpty(fileInfo.OccuredError))
            {
                //UI�� �ε��� �����̸� ������Ʈ�� ���� ������ ���
                var mi = PlaylistSource.FirstOrDefault(x => x.Path == fileInfo.Path);
                //ȭ�鿡 �ε����� ���� �����̸� DB���� �ε�
                if (mi == null)
                {
                    mi = fileDAO.GetPlayList(fileInfo.Path);
                }
                
                //��� ó��
                MessengerInstance.Send<Message>(new Message("Play", mi), CCPlayerViewModel.NAME);
            }
        }

        /// <summary>
        /// �������� �����Ѵ�.
        /// </summary>
        /// <param name="mediaInfoList"></param>
        void MakePlaylist(IEnumerable<MediaInfo> mediaInfoList)
        {
            if (mediaInfoList != null && mediaInfoList.Count() > 0)
            {
                //������� ��� ����
                var list = mediaInfoList.Where(x => string.IsNullOrEmpty(x.OccuredError));
                if (list != null && list.Any())
                {
                    //���� ������
                    list = list.Reverse<MediaInfo>();
                    InsertPlayList(list);

                    //��� ó��
                    var playItem = list.LastOrDefault();
                    PlayItem(playItem);
                }
            }
        }

        /// <summary>
        /// �������� DB�� �ݿ��Ѵ�.
        /// </summary>
        /// <param name="mediaInfoList"></param>
        private void InsertPlayList(IEnumerable<MediaInfo> mediaInfoList)
        {
            //������ DB�� ���� �߰�
            fileDAO.InsertPlayList(mediaInfoList);
            //101���� 100��, �ڸ��� �ε����� �ʴ´�.
            var miList = new List<MediaInfo>();
            fileDAO.LoadPlayList(miList, 100, 100, false);
            if (miList.Count > 0)
            {
                //101���ʹ� ����
                fileDAO.DeletePlayList(miList);     
            }
        }

        private void MoveToSection(HubSection section)
        {
            //��긦 ���������� �̵�
            var hub = Lime.Xaml.Helpers.ElementHelper.FindVisualParent<Hub>(section);
            var playlistSection = hub.Sections.FirstOrDefault(x => x.ViewModelName() == PlaylistViewModel.NAME);

            if (playlistSection != null)
            {
                hub.ScrollToSection(playlistSection);
            }
        }

        private void OnRemovePlayList(MediaInfo mediaInfo)
        {
            for (int i = PlaylistSource.Count - 1; i >= 0; i--)
            {
                if (mediaInfo.Path == PlaylistSource[i].Path)
                {
                    PlaylistSource.RemoveAt(i);
                    fileDAO.DeletePlayList(new MediaInfo[] { mediaInfo });
                    break;
                }
            }

            CheckPlaylistCount();
        }

        private void CheckPlaylistCount()
        {
            CheckListButtonEnable = PlaylistSource.Count > 0;
            ReorderButtonEnable = PlaylistSource.Count > 1;

            if (PlaylistSource.Count == 0)
            {
                SelectionMode = ListViewSelectionMode.None;
                MainButtonGroupVisible = true;
                CheckListButtonGroupVisible = false;
                ReorderButtonGroupVisible = false;
            }
        }
    }
}
