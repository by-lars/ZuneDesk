using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Windows.Media.Control;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Timers;

namespace ZuneDesk.Controls.MediaControls {
    public partial class MediaControls : UserControl {
        private GlobalSystemMediaTransportControlsSessionManager m_SessionManager;
        private GlobalSystemMediaTransportControlsSession m_Session;

        private TimeSpan m_TimeCurrentPos;
        private TimeSpan m_TimeLength;
        private Timer m_TimelineTimer;

        private Int32Rect m_ImageCropRect;
        private bool      m_ImageNeedsCropping;

        public MediaControls() {
            InitializeComponent();

            m_ImageNeedsCropping = false;

            m_TimelineTimer = new Timer(1000);
            m_TimelineTimer.Elapsed += TimelineTimer_Tick;

            m_SessionManager = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().GetAwaiter().GetResult();
            m_SessionManager.CurrentSessionChanged += CurrentSessionChanged;
            CurrentSessionChanged(m_SessionManager, null);
        }

        private void SetAppSpecificCropRegions(string id) {
            switch (id) {
                case "Spotify.exe":
                    m_ImageCropRect = new Int32Rect(33, 0, 234, 234);
                    m_ImageNeedsCropping = true;
                    break;
                default:
                    m_ImageNeedsCropping = false;
                    break;
            }
        }

        private void TimelineTimer_Tick(Object sender, ElapsedEventArgs e) {
            this.Dispatcher.Invoke(() => {
                m_TimeCurrentPos = m_TimeCurrentPos.Add(TimeSpan.FromSeconds(1));
                Position.Content = String.Format("{0:D2}:{1:D2}", m_TimeCurrentPos.Minutes, m_TimeCurrentPos.Seconds);
                ProgressBar.Width = (m_TimeCurrentPos.TotalSeconds / m_TimeLength.TotalSeconds) * ProgressBar.MaxWidth;
            });
        }

        private void CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args) {
            m_Session = sender.GetCurrentSession();
            if (m_Session != null) {
                this.Dispatcher.Invoke(() => {
                    SetAppSpecificCropRegions(m_Session.SourceAppUserModelId);
                    MediaPropertiesChanged(m_Session, null);
                    PlaybackInfoChanged(m_Session, null);
                    TimelinePropertiesChanged(m_Session, null);
                });

                m_Session.MediaPropertiesChanged += MediaPropertiesChanged;
                m_Session.PlaybackInfoChanged += PlaybackInfoChanged;
                m_Session.TimelinePropertiesChanged += TimelinePropertiesChanged;
            } else {
                this.Dispatcher.Invoke(() => {
                    Status.Content = "not playing";
                });
            }
        }

        private void TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args) {
            this.Dispatcher.Invoke(() => {
                var timeline = sender.GetTimelineProperties();
                m_TimeCurrentPos = timeline.Position;
                m_TimeLength     = timeline.EndTime;

                Position.Content = String.Format("{0:D2}:{1:D2}", m_TimeCurrentPos.Minutes, m_TimeCurrentPos.Seconds);
                EndTime.Content = String.Format("{0:D2}:{1:D2}", m_TimeLength.Minutes, m_TimeLength.Seconds);

                ProgressBar.Width = (m_TimeCurrentPos.TotalSeconds / m_TimeLength.TotalSeconds) * ProgressBar.MaxWidth;
            });
        }

        private void PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args) {
            this.Dispatcher.Invoke(() => {
                var info = sender.GetPlaybackInfo();         
                switch (info.PlaybackStatus) {
                    case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing:
                        Status.Content = "now playing";
                        if (!m_TimelineTimer.Enabled) {
                            m_TimelineTimer.Start();
                        }
                        break;
                    case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused:
                        Status.Content = "paused";
                        m_TimelineTimer.Stop();
                        break;
                    case GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped:
                        Status.Content = "not playing";
                        m_TimelineTimer.Stop();
                        break;
                }
            });
        }

        private void MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args) {
            this.Dispatcher.Invoke(async () => {
                var media = await sender.TryGetMediaPropertiesAsync();

                AlbumTitle.Content = media.AlbumTitle;
                Title.Content = media.Title;
                Artist.Content = media.Artist;

                if (media.Thumbnail == null) {
                    return;
                }

                var stream = await media.Thumbnail.OpenReadAsync();
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream.AsStream();
                image.EndInit();

                if (m_ImageNeedsCropping) {
                    CroppedBitmap cropped = new CroppedBitmap(image, m_ImageCropRect);
                    Thumbnail.Source = cropped;
                } else {
                    Thumbnail.Source = image;
                }
            });
        }

        private void StartStop_Click(object sender, RoutedEventArgs e) {
            m_Session.TryTogglePlayPauseAsync();
        }

        private void Back_Click(object sender, RoutedEventArgs e) {
            m_Session.TrySkipPreviousAsync();
        }

        private void Skip_Click(object sender, RoutedEventArgs e) {
            m_Session.TrySkipNextAsync();
        }
    }
}
