using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using NAudio.Wave;
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Phone.Shell;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Expression.Interactivity.Core;
using System.Diagnostics;

namespace RingtoneManager
{
    public partial class pageCutRingtone : PhoneApplicationPage
    {

        private MainViewModel viewModel
        {
            get
            {
                return this.DataContext as MainViewModel;
            }
        }

        private Timer _timer = null;

        void _timer_Tick(object state)
        {
            this.Dispatcher.BeginInvoke(delegate()
            {
                var endSecond = (int)slider1.Value + (int)slider2.Value;
                var sc = new SecondsToFormattedTimeConverter();
                int n = (int)MyMediaElement.Position.TotalSeconds;
                tbCurrentPosition.Text = (n / 60).ToString().PadLeft(2, '0') + ":" + (n % 60).ToString().PadLeft(2, '0');
                if (MyMediaElement.Position.TotalSeconds >= endSecond)
                    MyMediaElement.Stop();
            });
        }


        public pageCutRingtone()
        {
            InitializeComponent();
            this.DataContext = App.GlobalViewModel;
            VisualStateManager.GoToState(this, "CutRingtonePage_Normal", true);
        }

        void SongEx_OnInstalled(object sender, EventArgs e)
        {
            VisualStateManager.GoToState(this, "CutRingtonePage_Normal", true);
            if (viewModel.IsOnlyAddingRingtone)
                MessageBox.Show(LocalizedResources.SongAdded, LocalizedResources.Done, MessageBoxButton.OK);
            else
                MessageBox.Show(LocalizedResources.SongSetAsRingtone, LocalizedResources.Done, MessageBoxButton.OK);
        }

        void MyMediaElement_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            if (MyMediaElement.CurrentState == MediaElementState.Playing)
            {
            }
            else if (MyMediaElement.CurrentState == MediaElementState.Stopped)
            {
                if (BasicStates.CurrentState.Name != "CutRingtonePage_Normal")
                    VisualStateManager.GoToState(this, "CutRingtonePage_Normal", true);
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }
            else if (MyMediaElement.CurrentState == MediaElementState.Closed)
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }

        void MyMediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            int totalSeconds = (int)slider1.Value;
            var tspan = new TimeSpan(totalSeconds / 3600, (totalSeconds / 60) - (totalSeconds / 3600), totalSeconds % 60);
            MyMediaElement.Play();
            MyMediaElement.Position = tspan;

            _timer = new Timer(_timer_Tick, null, 1000, 1000);
        }

        private void BusyStateChanged(object sender, EventArgs e)
        {
            if (SystemTray.ProgressIndicator == null)
                SystemTray.ProgressIndicator = new ProgressIndicator();

            SystemTray.ProgressIndicator.IsVisible = viewModel.IsInstallingRingtone;
            SystemTray.ProgressIndicator.IsIndeterminate = true;
            if (viewModel.IsInstallingRingtone && viewModel.InstallingSong != null && viewModel.InstallingSong.Base != null)
            {
                SystemTray.ProgressIndicator.Text = String.Format(viewModel.IsOnlyAddingRingtone ? LocalizedResources.BusyStateOnlyAdd : LocalizedResources.BusyState,
                                                              viewModel.InstallingSong.Base.Name);
            }
            else
            {
                SystemTray.ProgressIndicator.Text = "";
            }
        }


        private void btnSlider1Dec_Click(object sender, RoutedEventArgs e)
        {
            slider1.Value -= 1;
        }

        private void btnSlider1Inc_Click(object sender, RoutedEventArgs e)
        {
            slider1.Value += 1;
        }

        private void btnSlider2Dec_Click(object sender, RoutedEventArgs e)
        {
            slider2.Value--;
        }

        private void btnSlider2Inc_Click(object sender, RoutedEventArgs e)
        {
            slider2.Value++;
        }

        private void btnAddToList_Click(object sender, RoutedEventArgs e)
        {
            var song = viewModel.CurrentSong;
            song.AddToRingtoneList((int)slider1.Value, (int)slider1.Value + (int)slider2.Value);
            if (BasicStates.CurrentState.Name != "CutRingtonePage_Installing")
                VisualStateManager.GoToState(this, "CutRingtonePage_Installing", true);
        }

        private void btnSetAsRingtone_Click(object sender, RoutedEventArgs e)
        {
            var song = viewModel.CurrentSong;
            song.SetAsRingtone((int)slider1.Value, (int)slider1.Value + (int)slider2.Value);
            if (BasicStates.CurrentState.Name != "CutRingtonePage_Installing")
                VisualStateManager.GoToState(this, "CutRingtonePage_Installing", true);
        }


        private void PrePlayThread(object param)
        {
            var song = param as SongEx;
            string ext = song.FilePath.Substring(song.FilePath.LastIndexOf(".") + 1);
            if (InteropSvc.InteropLib.Instance.GetFileAttributes7("\\Applications\\Data\\9cefc0bf-7060-45b0-ba66-2d1dcad8dc3c\\Data\\IsolatedStore\\PlayingSong." + ext) != 0xFFFFFFFF)
            {
                InteropSvc.InteropLib.Instance.MoveFile7("\\Applications\\Data\\9cefc0bf-7060-45b0-ba66-2d1dcad8dc3c\\Data\\IsolatedStore\\PlayingSong." + ext,
                    "\\Applications\\Data\\9cefc0bf-7060-45b0-ba66-2d1dcad8dc3c\\Data\\IsolatedStore\\PlayingSong2." + ext);
                InteropSvc.InteropLib.Instance.DeleteFile7("\\Applications\\Data\\9cefc0bf-7060-45b0-ba66-2d1dcad8dc3c\\Data\\IsolatedStore\\PlayingSong2." + ext);
            }
            bool res = InteropSvc.InteropLib.Instance.CopyFile7(song.FilePath, "\\Applications\\Data\\9cefc0bf-7060-45b0-ba66-2d1dcad8dc3c\\Data\\IsolatedStore\\PlayingSong." + ext, false);

            Dispatcher.BeginInvoke(delegate()
            {
                int retries = 0;
                L_tryAgain:
                try
                {
                    
                    using (IsolatedStorageFile isolatedStorageFile = IsolatedStorageFile.GetUserStoreForApplication())
                    using (IsolatedStorageFileStream isolatedStorageFileStream = new IsolatedStorageFileStream("PlayingSong." + ext, FileMode.Open, FileAccess.Read, FileShare.Read, isolatedStorageFile))
                    {
                        MyMediaElement.SetSource(isolatedStorageFileStream);
                    }
                }
                catch (Exception ex)
                {
                    if (retries < 5)
                    {
                        retries++;
                        Thread.Sleep(1000);
                        goto L_tryAgain;
                    }
                    if (BasicStates.CurrentState.Name != "CutRingtonePage_Normal")
                        VisualStateManager.GoToState(this, "CutRingtonePage_Normal", true);
                }
            });
        }
        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            
            if (MyMediaElement.CurrentState == MediaElementState.Playing)
            {
                MyMediaElement.Stop();
                return;
            }
            int n = (int)slider1.Value;
            tbCurrentPosition.Text = (n / 60).ToString().PadLeft(2, '0') + ":" + (n % 60).ToString().PadLeft(2, '0');
            if (BasicStates.CurrentState.Name != "CutRingtonePage_Playing")
                VisualStateManager.GoToState(this, "CutRingtonePage_Playing", true);
            var thread = new Thread(PrePlayThread);
            thread.Start(viewModel.CurrentSong);
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (viewModel.IsInstallingRingtone)
                e.Cancel = true;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SongEx.OnInstalled -= SongEx_OnInstalled;
            viewModel.BusyStateChanged -= BusyStateChanged;
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            viewModel.BusyStateChanged += new EventHandler(BusyStateChanged);
            SongEx.OnInstalled += new EventHandler(SongEx_OnInstalled);
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
                if (BasicStates.CurrentState.Name != "CutRingtonePage_Normal")
                VisualStateManager.GoToState(this, "CutRingtonePage_Normal", true);
            }
        }

      
    }
}
