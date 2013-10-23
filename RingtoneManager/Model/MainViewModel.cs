using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Media;
using System.Threading;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;

namespace RingtoneManager
{
    public class MainViewModel : BaseViewModel
    {

        public MainViewModel()
        {
            SongEx.OnInstallStateChanged += new EventHandler(InstallStateChanged);
        }

        

        public void InstallStateChanged(object sender, EventArgs e)
        {
            InstallEventArgs pe = e as InstallEventArgs;
            IsOnlyAddingRingtone = pe.onlyAdd;
            InstallingSong = pe.song;
            IsInstallingRingtone = pe.busyState;
        }

        public event EventHandler BusyStateChanged;

        public event EventHandler AlbumsLoaded;
        public event EventHandler SongsLoaded;

        private bool _isOnlyAddingRingtone = false;
        public bool IsOnlyAddingRingtone
        {
            get
            {
                return _isOnlyAddingRingtone;
            }
            set
            {
                _isOnlyAddingRingtone = value;
                OnChange("IsOnlyAddingRingtone");
            }
        }
        private bool _isInstallingRingtone = false;
        /// <summary>
        /// returns true if any song is being installed.
        /// </summary>
        public bool IsInstallingRingtone
        {
            get
            {
                return _isInstallingRingtone;
            }
            private set
            {
                if (_isInstallingRingtone == true && value == false)
                    HasInstalledAnyRingtone = true;
                _isInstallingRingtone = value;
                OnChange("IsInstallingRingtone");
                if (BusyStateChanged != null)
                    BusyStateChanged(this, new EventArgs());
            }
        }

        private bool _hasInstalledAnyRingtone = false;
        public bool HasInstalledAnyRingtone
        {
            get
            {
                return _hasInstalledAnyRingtone;
            }
            set
            {
                if (_hasInstalledAnyRingtone != value)
                {
                    _hasInstalledAnyRingtone = value;
                    OnChange("HasInstalledAnyRingtone");
                }
            }
        }

        private bool _isLoadingList = false;
        public bool IsLoadingList
        {
            get
            {
                return _isLoadingList;
            }
            private set
            {
                _isLoadingList = value;
                OnChange("IsLoadingList");
            }
        }

        private SongEx _installingSong;
        /// <summary>
        /// Song that is currently being installed.
        /// </summary>
        public SongEx InstallingSong
        {
            get
            {
                return _installingSong;
            }
            private set
            {
                _installingSong = value;
                OnChange("InstallingSong");
            }
        }

       
        private SongEx _currentSong;
        /// <summary>
        /// Song that is currently being cutted
        /// </summary>
        public SongEx CurrentSong
        {
            get
            {
                return _currentSong;
            }
            set
            {
                _currentSong = value;
                OnChange("CurrentSong");
            }
        }

        private List<Thread> _activeThreads = new List<Thread>();
        private object _activeThreadsLock = new object();

#region "Song list"

        private static List<SongEx> _allSongCache = null;
        private List<SongEx> _currentSongCache = null;


        private static List<SongEx> GetActualSongList()
        {
            var list = new List<SongEx>();
            MediaLibrary lib = new MediaLibrary();

            if (lib != null)
            {
                uint hMediaList = InteropSvc.InteropLib.Instance.ZMediaLibrary_GetSongs();
                int count = lib.Songs.Count;
                for (int x = 0; x < count; x++)
                {
                    var song = lib.Songs[x];
                    if (!song.IsProtected)
                    {
                        var songEx = new SongEx(song);

                        uint hash = InteropSvc.InteropLib.Instance.ZMediaList_GetHash(hMediaList, x);

                        string hashStr = String.Format("{0:X}", hash).PadLeft(8, '0');

                        string path = "\\My Documents\\Zune\\Content\\" + hashStr.Substring(0, 4) + "\\" + hashStr.Substring(4, 2) + "\\" + hashStr.Substring(6, 2);
                        if (InteropSvc.InteropLib.Instance.GetFileAttributes7(path + ".mp3") != 0xFFFFFFFFU)
                        {
                            path += ".mp3";
                        }
                        else
                        {
                            path += ".wma";
                        }
                        songEx.FilePath = path;

                        list.Add(songEx);
                    }
                }
                InteropSvc.InteropLib.Instance.ZMediaList_Release(hMediaList);
                lib.Dispose();
                list.Sort(new SongExComparer());
            }
            return list;
        }

        private void LoadSongsThread(object param)
        {
            var filter = param as string;

            var dispatcher = System.Windows.Deployment.Current.Dispatcher;
            dispatcher.BeginInvoke(new Action(() =>
            {
                IsLoadingList = true;
                if (BusyStateChanged != null)
                    BusyStateChanged(this, new EventArgs());
            }));
            if (_allSongCache == null)
            {
                var fullList = GetActualSongList();
                _allSongCache = fullList;
            }
            if (SelectedAlbum != null && SelectedArtist != null)
            {
                filter = filter.ToLower();
                var list = new List<SongEx>();
                foreach (var song in SelectedAlbum.Base.Songs)
                {
                    foreach (var song2 in _allSongCache)
                    {
                        if (song == song2.Base)
                        {
                            if (song.Name.ToLower().Contains(filter))
                                list.Add(song2);
                            break;
                        }
                    }
                }
                _currentSongCache = list;
            }
            else
            {
                _currentSongCache = _allSongCache;
            }
            dispatcher.BeginInvoke(new Action(() =>
            {
                IsLoadingList = false;
                OnChange("Songs");
                if (BusyStateChanged != null)
                    BusyStateChanged(this, new EventArgs());

                if (SongsLoaded != null)
                    SongsLoaded(this, new EventArgs());
            }));
            var thread = Thread.CurrentThread;
            lock (_activeThreadsLock)
            {
                if (_activeThreads.Contains(thread))
                    _activeThreads.Remove(thread);
            }
        }

        private string _currentFilter = null;
        public string CurrentFilter
        {
            get
            {
                return _currentFilter;
            }
            set
            {
                _currentFilter = value;
                OnChange("CurrentFilter");
            }
        }

        /// <summary>
        /// Loads songs asynchronously or synchronously.
        /// </summary>
        public void PreloadSongs(string filter, bool sync = false)
        {
            CurrentFilter = filter;
            if (sync)
            {
                LoadSongsThread(filter);
            }
            else
            {
                var thread = new Thread(LoadSongsThread);
                lock (_activeThreadsLock)
                {
                    _activeThreads.Add(thread);
                }
                thread.Start(filter);
            }
        }

        /// <summary>
        /// Song list
        /// </summary>
        public List<SongEx> Songs
        {
            get
            {
                return _currentSongCache;
            }
            set
            {
                _currentSongCache = value;
                OnChange("Songs");
            }
        }

#endregion

#region "Artists list"

        private List<Artist> _artistCache = null;
        public List<Artist> Artists
        {
            get
            {
                if (_artistCache == null)
                {
                    var list = new List<Artist>();
                    MediaLibrary lib = new MediaLibrary();
                    foreach (var artist in lib.Artists)
                    {
                        if (artist.Albums.Count > 0)
                        {
                            list.Add(artist);
                        }
                    }
                    lib.Dispose();
                    _artistCache = list;
                }
                return _artistCache;
            }
        }

#endregion

#region "Albums list"

        private ObservableCollection<AlbumEx> _albumsCache = null;
        public ObservableCollection<AlbumEx> Albums
        {
            get
            {
                return _albumsCache;
            }
        }

        private void PreloadAlbumsThread()
        {
            var dispatcher = System.Windows.Deployment.Current.Dispatcher;

            dispatcher.BeginInvoke(new Action(() =>
            {
                IsLoadingList = true;
                if (BusyStateChanged != null)
                    BusyStateChanged(this, new EventArgs());
            }));

            _albumsCache = null;
            if (_selectedArtist != null)
            {
                _albumsCache = new ObservableCollection<AlbumEx>();
                foreach (var album in _selectedArtist.Albums)
                {
                    var albumEx = new AlbumEx();
                    albumEx.Base = album;
                    if (albumEx.Base.Songs.Count > 0)
                    {
                        _albumsCache.Add(albumEx);
                    }
                }
            }
            if (AlbumsLoaded != null)
            {
                dispatcher.BeginInvoke(new Action(() =>
                {

                    IsLoadingList = false;

                    if (BusyStateChanged != null)
                        BusyStateChanged(this, new EventArgs());

                    if (AlbumsLoaded != null)
                        AlbumsLoaded(this, new EventArgs());
                    OnChange("Albums");
                }));
            }
            var thread = Thread.CurrentThread;
            lock (_activeThreadsLock)
            {
                if (_activeThreads.Contains(thread))
                    _activeThreads.Remove(thread);
            }
        }

        public void PreloadAlbums(bool sync = false)
        {
            if (sync)
            {
                PreloadAlbumsThread();
            }
            else
            {
                var thread = new Thread(PreloadAlbumsThread);
                lock (_activeThreadsLock)
                {
                    _activeThreads.Add(thread);
                }
                thread.Start();
            }
        }

#endregion

        private Artist _selectedArtist;
        public Artist SelectedArtist
        {
            get
            {
                return _selectedArtist;
            }
            set
            {
                _selectedArtist = value;
                OnChange("SelectedArtist");

                OnChange("Title");
                var dispatcher = System.Windows.Deployment.Current.Dispatcher;
                dispatcher.BeginInvoke(delegate()
                {
                    if (SelectedArtistChanged != null)
                        SelectedArtistChanged(this, new EventArgs());
                });
            }
        }

        public event EventHandler SelectedArtistChanged;


        private AlbumEx _selectedAlbum;
        public AlbumEx SelectedAlbum
        {
            get
            {
                return _selectedAlbum;
            }
            set
            {
                Songs = null;
                _selectedAlbum = value;
                OnChange("SelectedAlbum");
                OnChange("Title");
                var dispatcher = System.Windows.Deployment.Current.Dispatcher;
                dispatcher.BeginInvoke(delegate()
                {
                    if (SelectedAlbumChanged != null)
                        SelectedAlbumChanged(this, new EventArgs());
                });
            }
        }

        public event EventHandler SelectedAlbumChanged;

        public string Title
        {
            get
            {
                string result = LocalizedResources.Title;
                if (SelectedArtist != null)
                {
                    result = SelectedArtist.Name;
                    if (SelectedAlbum != null)
                        result += " / " + SelectedAlbum.Base.Name;
                }
                return result;
            }
        }

        private bool _stateRestored;
        public bool StateRestored
        {
            get
            {
                return _stateRestored;
            }
            set
            {
                _stateRestored = value;
            }
        }

        public object LoadObject(IsolatedStorageSettings settings, string name)
        {
            if (settings.Contains(name))
                return settings[name];
            return null;
        }

        public bool LoadBool(IsolatedStorageSettings settings, string name)
        {
            var obj = LoadObject(settings, name);
            if (obj == null)
                return false;
            return (bool)obj;
        }

        public int LoadInt(IsolatedStorageSettings settings, string name)
        {
            var obj = LoadObject(settings, name);
            if (obj == null)
                return 0;
            return (int)obj;
        }

        public void LoadSettings()
        {
            _stateRestored = true;
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            string savedArtist = LoadObject(settings, "SelectedArtist") as string;
            string savedAlbum = LoadObject(settings, "SelectedAlbum") as string;
            string savedSong = LoadObject(settings, "CurrentSong") as string;
            bool WasTombstoned = LoadBool(settings, "WasTombstoned");
            int startPos = LoadInt(settings, "CurrentSongStartPosition");
            int endPos = LoadInt(settings, "CurrentSongEndPosition");
            if (savedArtist != null)
            {
                foreach (var artist in Artists)
                {
                    if (artist.Name == savedArtist)
                    {
                        SelectedArtist = artist;
                        break;
                    }
                }
                if (savedAlbum != null)
                {
                    PreloadAlbums(true);
                    foreach (var album in Albums)
                    {
                        if (album.Base.Name == savedAlbum)
                        {
                            SelectedAlbum = album;
                            break;
                        }
                    }
                    if (savedSong != null)
                    {
                        PreloadSongs("", true);
                        foreach (var song in Songs)
                        {
                            if (song.Base.Name == savedSong)
                            {
                                CurrentSong = song;
                                CurrentSong.StartPosition = startPos;
                                CurrentSong.EndPosition = endPos;
                                break;
                            }
                        }
                    }
                }
                
            }
        }

        public void SaveSettings(bool tombStone)
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            settings["SelectedArtist"] = (SelectedArtist != null) ? SelectedArtist.Name : null;
            settings["SelectedAlbum"] = (SelectedAlbum != null) ? SelectedAlbum.Base.Name : null;
            settings["CurrentSong"] = (CurrentSong != null) ? CurrentSong.Base.Name : null;
            settings["WasTombstoned"] = tombStone;
            if (CurrentSong != null)
            {
                settings["CurrentSongStartPosition"] = CurrentSong.StartPosition;
                settings["CurrentSongEndPosition"] = CurrentSong.EndPosition;
            }
            lock (_activeThreadsLock)
            {
                foreach (var thread in _activeThreads)
                {
                    if (thread.IsAlive)
                    {
                        thread.Join(10000);
                    }
                }
            }
        }


    }
}
