using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Media.Control;
using Windows.Foundation;
using System.Diagnostics;
using System.IO;


namespace ZuneDesk
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GlobalSystemMediaTransportControlsSession session;
        private GlobalSystemMediaTransportControlsSessionManager sessionManager;

        public MainWindow()
        {
            SetupSession();
            InitializeComponent();
        }

        void SetupSession()
        {
            sessionManager = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().GetAwaiter().GetResult();

            session = sessionManager.GetCurrentSession();
            session.MediaPropertiesChanged       += Session_MediaPropertiesChanged;
            session.PlaybackInfoChanged          += Session_PlaybackInfoChanged;
            session.TimelinePropertiesChanged    += Session_TimelinePropertiesChanged;

            sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;
        }

        private void SessionManager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            session = sender.GetCurrentSession();
            session.MediaPropertiesChanged      += Session_MediaPropertiesChanged;
            session.PlaybackInfoChanged         += Session_PlaybackInfoChanged;
            session.TimelinePropertiesChanged   += Session_TimelinePropertiesChanged;
        }

        private void Session_TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
        {
            var time = sender.GetTimelineProperties();
            var endTime = sender.GetTimelineProperties().EndTime;
            var curTime = sender.GetTimelineProperties().Position;

            var percentage = curTime.TotalSeconds / endTime.TotalSeconds;

            this.Dispatcher.Invoke(() =>
            {
                TimeTotal.Content = String.Format("{0:D2}:{1:D2}", endTime.Minutes, endTime.Seconds);
                TimeElapsed.Content = String.Format("{0:D2}:{1:D2}", curTime.Minutes, curTime.Seconds);

                TimeProgress.Width = percentage * TimeProgress.MaxWidth;
            });
        }

        private void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        {
            var info = sender.GetPlaybackInfo();
            this.Dispatcher.Invoke(() =>
            {
                switch (info.PlaybackStatus)
                {
                    case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing:
                        PlaybackStatus.Content = "now playing";
                        break;
                    case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused:
                        PlaybackStatus.Content = "paused";
                        break;
                    case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped:
                        PlaybackStatus.Content = "not playing";
                        break;
                }
            });
        }

        private void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            var info = sender.TryGetMediaPropertiesAsync().GetAwaiter().GetResult();

            this.Dispatcher.Invoke(() =>
            {
                AlbumName.Content = info.AlbumTitle.ToUpper();
                ArtistName.Content = info.Artist.ToUpper();
                SongName.Content = info.Title;

                //No Album Art Provided :/
                if(info.Thumbnail == null)
                {
                    return;
                }

                var thumbnailStream = info.Thumbnail.OpenReadAsync().GetAwaiter().GetResult().AsStream();
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = thumbnailStream;
                bitmap.EndInit();

                AlbumArt.Source = bitmap;
            });
        }
    }
}
