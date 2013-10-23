using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework.Media;

namespace RingtoneManager
{
    public partial class MainPage : PhoneApplicationPage
    {
        private string _previousSong = "";

        private MainViewModel viewModel
        {
            get
            {
                return this.DataContext as MainViewModel;
            }
        }

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            this.DataContext = App.GlobalViewModel;

            if (InteropSvc.InteropLib.HasRootAccess() == false)
            {
                // double check
                System.Threading.Thread.Sleep(1000);
                if (InteropSvc.InteropLib.HasRootAccess() == false)
                {
                    MessageBox.Show(LocalizedResources.NoRootAccess, LocalizedResources.Error, MessageBoxButton.OK);
                    throw new Exception("Quit");
                }
            }
            
            viewModel.BusyStateChanged += new EventHandler(BusyStateChanged);
            viewModel.SelectedArtistChanged += new EventHandler(viewModel_SelectedArtistChanged);
            viewModel.SelectedAlbumChanged += new EventHandler(viewModel_SelectedAlbumChanged);
            viewModel.AlbumsLoaded += new EventHandler(viewModel_AlbumsLoaded);
            viewModel.SongsLoaded += new EventHandler(viewModel_SongsLoaded);
            var files = MediaAttach.EnumerateFiles("\\MediaAttach");
            VisualStateManager.GoToState(this, "MediaAttach_Hidden", false);
            if (files.Length > 0)
            {
                VisualStateManager.GoToState(this, "MediaAttach_Visible", true);
            }
            VisualStateManager.GoToState(this, "ShowArtists", true);
            if (viewModel.StateRestored)
            {
                if (viewModel.SelectedArtist != null)
                    viewModel_SelectedArtistChanged(null, new EventArgs());
                if (viewModel.SelectedAlbum != null)
                    viewModel_SelectedAlbumChanged(null, new EventArgs());
            }
            viewModel.StateRestored = false;
        }

        void viewModel_SongsLoaded(object sender, EventArgs e)
        {
            VisualStateManager.GoToState(this, "ShowSongs", !viewModel.StateRestored);
        }

        void viewModel_AlbumsLoaded(object sender, EventArgs e)
        {
            VisualStateManager.GoToState(this, "ShowAlbums", !viewModel.StateRestored);
        }

        void viewModel_SelectedAlbumChanged(object sender, EventArgs e)
        {
            if (viewModel.SelectedAlbum != null)
            {
                viewModel.PreloadSongs(filterBox.Text, viewModel.StateRestored);
            }
            else
            {
                if (BasicStates.CurrentState.Name == "ShowSongs")
                    VisualStateManager.GoToState(this, "ShowAlbums", true);
            }
        }

        void viewModel_SelectedArtistChanged(object sender, EventArgs e)
        {
            if (viewModel.SelectedArtist != null)
            {
                viewModel.PreloadAlbums(viewModel.StateRestored);
            }
            else
            {
                if (BasicStates.CurrentState.Name == "ShowAlbums")
                {
                    VisualStateManager.GoToState(this, "ShowArtists", true);
                }
            }
        }

        private void BusyStateChanged(object sender, EventArgs e)
        {
            if (SystemTray.ProgressIndicator == null)
                SystemTray.ProgressIndicator = new ProgressIndicator();

            SystemTray.ProgressIndicator.IsVisible = viewModel.IsLoadingList;
            SystemTray.ProgressIndicator.IsIndeterminate = true;
            if (viewModel.IsLoadingList)
            {
                SystemTray.ProgressIndicator.Text = LocalizedResources.LoadingList;
            }
            else
            {
                SystemTray.ProgressIndicator.Text = "";
                if (viewModel.SelectedArtist != null && viewModel.SelectedAlbum != null)
                    lstSongs.Visibility = Visibility.Visible;
            }
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            IDictionary<String, String> qs = this.NavigationContext.QueryString;
            
            if (qs.ContainsKey("fromsettings"))
            {
                _previousSong = qs["fromsettings"];
            }
            var mi = ApplicationBar.MenuItems[0] as ApplicationBarMenuItem;
            if (mi != null)
                mi.Text = LocalizedResources.About;
        }

        private void reloadListBox()
        {
            string newText = filterBox.Text;
            viewModel.PreloadSongs(newText);
        }

        private void filterBox_LostFocus(object sender, RoutedEventArgs e)
        {
            reloadListBox();
        }

        private void filterBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                reloadListBox();
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (viewModel.SelectedAlbum != null)
            {
                viewModel.SelectedAlbum = null;
                e.Cancel = true;
            }
            else if (viewModel.SelectedArtist != null)
            {
                viewModel.SelectedArtist = null;
                e.Cancel = true;
            }
            else if (!viewModel.HasInstalledAnyRingtone && _previousSong != "")
            {
                if (MessageBox.Show(LocalizedResources.FromSettingsWarning, LocalizedResources.Warning, MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                InteropSvc.InteropLib.Instance.RegistrySetString7(InteropSvc.InteropLib.HKEY_CURRENT_USER, "ControlPanel\\Sounds\\RingTone0", "Sound", _previousSong);
                InteropSvc.InteropLib.Instance.RegistrySetString7(InteropSvc.InteropLib.HKEY_CURRENT_USER, "ControlPanel\\Sounds\\RingTone1", "Sound", _previousSong);
                InteropSvc.InteropLib.Instance.RegistrySetString7(InteropSvc.InteropLib.HKEY_CURRENT_USER, "ControlPanel\\Sounds\\RingTone2", "Sound", _previousSong);
                InteropSvc.InteropLib.Instance.RegistrySetString7(InteropSvc.InteropLib.HKEY_CURRENT_USER, "ControlPanel\\Sounds\\RingTone3", "Sound", _previousSong);
            }
        }

        private void stkMediaAttach_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var files = MediaAttach.EnumerateFiles("\\MediaAttach");
            if (files.Length == 0)
            {
                MessageBox.Show(LocalizedResources.NoFilesAvailable, LocalizedResources.AppTitle, MessageBoxButton.OK);
            }
            else
            {
                if (MessageBox.Show(LocalizedResources.MediaAttachWarning, LocalizedResources.Warning, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    MediaAttach.AttachImagesToArtists();
                    MessageBox.Show(LocalizedResources.MediaAttachDone, LocalizedResources.Done, MessageBoxButton.OK);
                }
            }
        }

        private void lstArtists_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var listbox = sender as LongListSelector;
            if (listbox.SelectedItem != null)
            {
                var artist = listbox.SelectedItem as Artist;
                viewModel.SelectedArtist = artist;
            }
        }

        private void lstSongs_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var listbox = sender as LongListSelector;
            if (listbox.SelectedItem != null)
            {
                var song = listbox.SelectedItem as SongEx;
                viewModel.CurrentSong = song;
                NavigationService.Navigate(new Uri("/pageCutRingtone.xaml", UriKind.Relative));
            }
        }

        private void lstAlbums_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var listbox = sender as LongListSelector;
            if (listbox.SelectedItem != null)
            {
                var album = listbox.SelectedItem as AlbumEx;
                viewModel.SelectedAlbum = album;
            }
        }

        private void mbAbout_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/About.xaml", UriKind.Relative));
        }

        private void menu_StateChanged(object sender, ApplicationBarStateChangedEventArgs e)
        {
            var m = sender as ApplicationBar;
            if (e.IsMenuVisible)
                m.BackgroundColor = (System.Windows.Media.Color)App.Current.Resources["PhoneChromeColor"];
            else
                m.BackgroundColor = Colors.Transparent;
        }


    }
}