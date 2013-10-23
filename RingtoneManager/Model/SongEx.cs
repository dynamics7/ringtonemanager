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
using Microsoft.Xna.Framework.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;
using System.Threading;
using NAudio.Wave;
using System.IO.IsolatedStorage;
using System.IO;

namespace RingtoneManager
{
    public class SongEx : BaseViewModel
    {
        private Song _song;
        private string _filePath;

        public static event EventHandler OnInstallStateChanged;
        public static event EventHandler OnInstalled;

        private string _nameLow = null, _albumLow = null, _artistLow = null, _genreLow = null;

        public SongEx(Song song)
        {
            Base = song;
        }

        /// <summary>
        /// Song base class
        /// </summary>
        public Song Base
        {
            get
            {
                return _song;
            }
            set
            {
                _song = value;
                StartPosition = 0;
                EndPosition = MaxStartPosition;
                OnChange("Song");
            }
        }

        /// <summary>
        /// Song file path
        /// </summary>
        public string FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    OnChange("FilePath");
                }
            }
        }

        private BitmapImage _thumbnail = null;

        /// <summary>
        /// Album art thumbnail
        /// </summary>
        public BitmapImage Thumbnail
        {
            get
            {
                if (_thumbnail == null)
                {
                    _thumbnail = new BitmapImage();
                    if (Base.Album != null)
                    {
                        var stream = Base.Album.GetAlbumArt();
                        if (stream != null)
                        {
                            _thumbnail.SetSource(Base.Album.GetAlbumArt());
                        }
                    }
                }
                return _thumbnail;
            }
        }

        Visibility _visible = Visibility.Visible;

        /// <summary>
        /// List visibility option
        /// </summary>
        public Visibility Visible
        {
            get
            {
                return _visible;
            }
            set
            {
                if (_visible != value)
                {
                    _visible = value;
                    OnChange("Visible");
                }
            }
        }

        /// <summary>
        /// Checks if any field of Song base class contains certain substring
        /// </summary>
        /// <param name="substr"></param>
        /// <returns></returns>
        public bool Contains(string substr)
        {
            if (substr == null || substr.Length == 0)
                return true;
            substr = substr.ToLower();

            if (_nameLow == null)
            {
                _nameLow = _song.Name.ToLower();
                _albumLow = _song.Album.Name.ToLower();
                _artistLow = _song.Artist.Name.ToLower();
                _genreLow = _song.Genre.Name.ToLower();
            }
            if (_nameLow.Contains(substr) ||
                _artistLow.Contains(substr) ||
                _albumLow.Contains(substr) ||
                _genreLow.Contains(substr))
                return true;
            return false;
        }

        public static void SplitMp3(string inputFilename, string outputFilename, int start, int end)
        {
            Mp3Frame mp3Frame;

            using (IsolatedStorageFile isolatedStorageFile = IsolatedStorageFile.GetUserStoreForApplication())
            using (IsolatedStorageFileStream isolatedStorageFileStream1 = new IsolatedStorageFileStream(inputFilename, FileMode.Open, isolatedStorageFile))
            using (IsolatedStorageFileStream isolatedStorageFileStream2 = new IsolatedStorageFileStream(outputFilename, FileMode.Create, isolatedStorageFile))
            {
                int i = Math.Abs(end - start);
                double currentTime = 0.0;
                var endTimeSpan = TimeSpan.FromSeconds((double)end);
                using (Mp3FileReader mp3FileReader = new Mp3FileReader(isolatedStorageFileStream1))
                {
                    while ((mp3Frame = mp3FileReader.ReadNextFrame()) != null)
                    {
                        if (TimeSpan.FromMilliseconds(currentTime) >= TimeSpan.FromSeconds((double)start))
                        {
                            isolatedStorageFileStream2.Write(mp3Frame.RawData, 0, mp3Frame.RawData.Length);
                        }
                        currentTime += mp3Frame.FrameLengthMs();
                        if (TimeSpan.FromMilliseconds(currentTime) >= endTimeSpan)
                            break;
                    }
                }
            }
        }

        string CutTheSong(SongEx song, int start, int end)
        {
            if (song.FilePath.ToLower().EndsWith(".mp3"))
            {
                if (InteropSvc.InteropLib.Instance.GetFileAttributes7("\\Applications\\Data\\9cefc0bf-7060-45b0-ba66-2d1dcad8dc3c\\Data\\IsolatedStore\\Song.mp3") != 0xFFFFFFFF)
                {
                    InteropSvc.InteropLib.Instance.MoveFile7("\\Applications\\Data\\9cefc0bf-7060-45b0-ba66-2d1dcad8dc3c\\Data\\IsolatedStore\\Song.mp3",
                        "\\Applications\\Data\\9cefc0bf-7060-45b0-ba66-2d1dcad8dc3c\\Data\\IsolatedStore\\Song2.mp3");
                    InteropSvc.InteropLib.Instance.DeleteFile7("\\Applications\\Data\\9cefc0bf-7060-45b0-ba66-2d1dcad8dc3c\\Data\\IsolatedStore\\Song2.mp3");
                }
                bool res = InteropSvc.InteropLib.Instance.CopyFile7(song.FilePath, "\\Applications\\Data\\9cefc0bf-7060-45b0-ba66-2d1dcad8dc3c\\Data\\IsolatedStore\\Song.mp3", false);
                if (res)
                {
                    SplitMp3("Song.mp3", "SongNew.mp3", start, end);
                    return "\\Applications\\Data\\9cefc0bf-7060-45b0-ba66-2d1dcad8dc3c\\Data\\IsolatedStore\\SongNew.mp3";
                }
                return null;
            }
            else
            {
                return song.FilePath;
            }
        }

        private static Object InstallThreadObject = new Object();
        internal class InstallThreadParam
        {
            public bool onlyAdd;
            public int start;
            public int end;
        }
        private void InstallThread(object ip)
        {
            if (Monitor.TryEnter(InstallThreadObject))
            {
                var param = ip as InstallThreadParam;
                bool onlyAdd = param.onlyAdd;
                var dispatcher = System.Windows.Deployment.Current.Dispatcher;
                if (OnInstallStateChanged != null)
                {
                    dispatcher.BeginInvoke(new Action(() =>
                    {
                        InstallEventArgs e = new InstallEventArgs();
                        e.onlyAdd = onlyAdd;
                        e.busyState = true;
                        e.song = this;
                        OnInstallStateChanged(this, e);
                    }));
                }
                //InteropSvc.InteropLib.Instance.AddRingtoneFile(FilePath, "CustomRingtone2", _song.IsProtected ? 1 : 0, 0);

                if (FilePath.Contains("."))
                {
                    string newFilePath = CutTheSong(this, param.start, param.end);
                    if (newFilePath != null)
                    {
                        string extension = newFilePath.Substring(newFilePath.LastIndexOf(".") + 1);
                        string hash = _song.ToString().GetHashCode().ToString("X");
                        InteropSvc.InteropLib.Instance.CopyFile7(newFilePath, "\\My Documents\\Ringtones\\CustomRingtone" + hash + "." + extension, false);
                        InteropSvc.InteropLib.Instance.RegistrySetString7(InteropSvc.InteropLib.HKEY_CURRENT_USER, "ControlPanel\\Sounds\\CustomRingtones", "CustomRingtone" + hash + "." + extension, Base.Name);
                        if (!onlyAdd)
                        {
                            InteropSvc.InteropLib.Instance.RegistrySetString7(InteropSvc.InteropLib.HKEY_CURRENT_USER, "ControlPanel\\Sounds\\RingTone0", "Sound", "\\My Documents\\Ringtones\\CustomRingtone" + hash + "." + extension);
                            InteropSvc.InteropLib.Instance.RegistrySetString7(InteropSvc.InteropLib.HKEY_CURRENT_USER, "ControlPanel\\Sounds\\RingTone1", "Sound", "\\My Documents\\Ringtones\\CustomRingtone" + hash + "." + extension);
                            InteropSvc.InteropLib.Instance.RegistrySetString7(InteropSvc.InteropLib.HKEY_CURRENT_USER, "ControlPanel\\Sounds\\RingTone2", "Sound", "\\My Documents\\Ringtones\\CustomRingtone" + hash + "." + extension);
                            InteropSvc.InteropLib.Instance.RegistrySetString7(InteropSvc.InteropLib.HKEY_CURRENT_USER, "ControlPanel\\Sounds\\RingTone3", "Sound", "\\My Documents\\Ringtones\\CustomRingtone" + hash + "." + extension);
                        }
                    }
                }

                if (OnInstallStateChanged != null)
                {
                    dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (OnInstallStateChanged != null)
                        {
                            InstallEventArgs e = new InstallEventArgs();
                            e.busyState = false;
                            e.song = null;
                            e.onlyAdd = onlyAdd;
                            OnInstallStateChanged(this, e);
                        }

                    }));
                }
                if (OnInstalled != null)
                {
                    dispatcher.BeginInvoke(new Action(() =>
                    {
                        OnInstalled(this, new EventArgs());
                    }));
                }

                Monitor.Exit(InstallThreadObject);
            }
        }

        /// <summary>
        /// Set song as ringtone permanently.
        /// </summary>
        /// <returns>true on success, false on failure</returns>
        public bool SetAsRingtone(int start, int end)
        {
            try
            {
                var itp = new InstallThreadParam();
                itp.onlyAdd = false;
                itp.start = start;
                itp.end = end;
                var thread = new Thread(InstallThread);
                thread.Start(itp);
                return true;
            }
            catch (Exception ex)
            {
            }
            return false;
        }

        /// <summary>
        /// Add to ringtone list
        /// </summary>
        /// <returns>true on success, false on failure</returns>
        public bool AddToRingtoneList(int start, int end)
        {
            try
            {
                var itp = new InstallThreadParam();
                itp.onlyAdd = true;
                itp.start = start;
                itp.end = end;
                var thread = new Thread(InstallThread);
                thread.Start(itp);
                return true;
            }
            catch (Exception ex)
            {
            }
            return false;
        }


        public int MaxStartPosition
        {
            get
            {
                if ((int)Base.Duration.TotalSeconds > 1)
                    return (int)Base.Duration.TotalSeconds - 1;
                return 0;
            }
        }

        public int MaxLength
        {
            get
            {
                int n = (int)Base.Duration.TotalSeconds - _startPosition;
                if (n >= 0)
                    return n;
                return 0;
            }
        }

        private int _startPosition = 0;
        public int StartPosition
        {
            get
            {
                return _startPosition;
            }
            set
            {
                _startPosition = value;
                OnChange("StartPosition");
                OnChange("MaxLength");
            }
        }

        private int _endPosition = 0;
        public int EndPosition
        {
            get
            {
                return _endPosition;
            }
            set
            {
                _endPosition = value;
                OnChange("EndPosition");
            }
        }

        public bool SupportedFormat
        {
            get
            {
                if (FilePath.ToLower().EndsWith(".mp3"))
                    return true;
                
                return false;
            }
        }

    }


    public class SongExComparer : IComparer<SongEx>
    {
        public int Compare(SongEx x, SongEx y)
        {
            try
            {
                int r1 = x.Base.Artist.Name.CompareTo(y.Base.Artist.Name);
                if (r1 != 0)
                    return r1;
                int r2 = x.Base.Album.Name.CompareTo(y.Base.Album.Name);
                if (r2 != 0)
                    return r2;
                if (x.Base.TrackNumber == y.Base.TrackNumber)
                {
                    int r3 = x.Base.Name.CompareTo(y.Base.Name);
                    if (r3 != 0)
                        return r3;
                }
                else
                {
                    if (x.Base.TrackNumber < y.Base.TrackNumber)
                        return -1;
                    return 1;
                }
                return 0;
            }
            catch (Exception ex)
            {
            }
            return 0;
        }
    }
}
