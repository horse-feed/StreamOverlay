using LibVLCSharp.Shared;
using StreamOverlay.Classes.Civ;
using StreamOverlay.Classes.Overlays;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace StreamOverlay
{

    public class CIVS : ObservableCollection<Civ>
    {
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class OverlayWindow : Window, INotifyPropertyChanged
    {
        private bool needClose = true;

        public CIVS CivPool { get; set; } = new CIVS();
        public ObservableCollection<Civ> Team1Player1CivPool = new();
        public ObservableCollection<Civ> Team1Player2CivPool = new();
        public ObservableCollection<Civ> Team1Player3CivPool = new();

        public ObservableCollection<Civ> Team2Player1CivPool = new();
        public ObservableCollection<Civ> Team2Player2CivPool = new();
        public ObservableCollection<Civ> Team2Player3CivPool = new();


        private double minimapSize = 100;
        public double MinimapSize
        {
            get
            {
                return minimapSize;
            }
            set
            {
                minimapSize = value;
                NotifyPropertyChanged("MinimapSize");
            }
        }

        public string Time
        {
            get
            {
                if (_time.TotalSeconds == 0)
                {
                    WaitingAnimation.Visibility = Visibility.Visible;
                    lTimer.Visibility = Visibility.Hidden;
                }
                else
                {
                    WaitingAnimation.Visibility = Visibility.Hidden;
                    lTimer.Visibility = Visibility.Visible;

                }
                return _time.ToString("c");
            }
        }

        public class ActionCommand : ICommand
        {
            private readonly Action _action;

            public ActionCommand(Action action)
            {
                _action = action;
            }

            public void Execute(object parameter)
            {
                _action();
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;
        }

        private readonly DispatcherTimer _timer;
        TimeSpan _time;

        private ICommand timeUp;
        public ICommand TimeUp
        {
            get
            {
                return timeUp ??= new ActionCommand(() =>
                    {
                        _time = _time.Add(TimeSpan.FromMinutes(1));
                        NotifyPropertyChanged("Time");
                    });
            }
        }

        private ICommand timeDown;
        public ICommand TimeDown
        {
            get
            {
                return timeDown ??= new ActionCommand(() =>
                    {
                        if (_time.TotalSeconds > 60)
                        {
                            _time = _time.Add(TimeSpan.FromMinutes(-1));
                            NotifyPropertyChanged("Time");
                        }
                        else if (_time.TotalSeconds > 0)
                        {
                            _time = _time.Add(TimeSpan.FromSeconds(-1));
                            NotifyPropertyChanged("Time");
                        }
                    });
            }
        }


        private ICommand closeOverlay;
        public ICommand CloseOverlay
        {
            get
            {
                return closeOverlay ??= new ActionCommand(() =>
                    {
                        needClose = false;
                        this.Hide();
                        Application.Current.MainWindow.Show();
                    });
            }
        }

        private ICommand mininizeOverlay;
        public ICommand MinimizeOverlay
        {
            get
            {
                return mininizeOverlay ??= new ActionCommand(() =>
                    {
                        this.WindowState = WindowState.Minimized;
                    });
            }
        }

        private ICommand countdownVisibility;
        public ICommand CountdownVisibility
        {
            get
            {
                return countdownVisibility ??= new ActionCommand(() =>
                    {
                        if (gCountdown.Visibility == Visibility.Visible)
                        {
                            gCountdown.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            gCountdown.Visibility = Visibility.Visible;
                        }

                    });
            }
        }

        private ICommand scheduleVisibility;
        public ICommand ScheduleVisibility
        {
            get
            {
                return scheduleVisibility ??= new ActionCommand(() =>
                    {
                        if (gSchedule.Visibility == Visibility.Visible)
                        {
                            gSchedule.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            gSchedule.Visibility = Visibility.Visible;
                        }

                    });
            }
        }

        private ICommand mapPoolVisibility;
        public ICommand MapPoolVisibility
        {
            get
            {
                return mapPoolVisibility ??= new ActionCommand(() =>
                    {
                        if (gMapPool.Visibility == Visibility.Visible)
                        {
                            gMapPool.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            gMapPool.Visibility = Visibility.Visible;
                        }

                    });
            }
        }

        private ICommand eventLogoVisibility;
        public ICommand EventLogoVisibility
        {
            get
            {
                return eventLogoVisibility ??= new ActionCommand(() =>
                    {
                        if (gEventLogo.Visibility == Visibility.Visible)
                        {
                            gEventLogo.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            gEventLogo.Visibility = Visibility.Visible;
                        }

                    });
            }
        }

        private ICommand brandLogoVisibility;
        public ICommand BrandLogoVisibility
        {
            get
            {
                return brandLogoVisibility ??= new ActionCommand(() =>
                    {
                        if (gBrandLogo.Visibility == Visibility.Visible)
                        {
                            gBrandLogo.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            gBrandLogo.Visibility = Visibility.Visible;
                        }

                    });
            }
        }

        private ICommand playersPanelVisibility;
        public ICommand PlayerPanelVisibility
        {
            get
            {
                return playersPanelVisibility ??= new ActionCommand(() =>
                    {
                        if (gPlayersPanel.Visibility == Visibility.Visible)
                        {
                            gPlayersPanel.Visibility = Visibility.Hidden;
                            gPlayers.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            gPlayersPanel.Visibility = Visibility.Visible;
                            gPlayers.Visibility = Visibility.Visible;
                        }

                    });
            }
        }


        private ICommand twitchPanelVisibility;
        public ICommand TwitchPanelVisibility
        {
            get
            {
                return twitchPanelVisibility ??= new ActionCommand(() =>
                    {
                        if (gTwitchInfo.Visibility == Visibility.Visible)
                        {
                            gTwitchInfo.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            gTwitchInfo.Visibility = Visibility.Visible;
                        }

                    });
            }
        }

        private ICommand chromakeyVisibility;
        public ICommand ChromakeyVisibility
        {
            get
            {
                return chromakeyVisibility ??= new ActionCommand(() =>
                    {
                        if (((SolidColorBrush)OverlayCanvas.Background).Color == ((SolidColorBrush)(new BrushConverter().ConvertFromString("#ff00b140"))).Color)
                        {
                            PreviewImage.Opacity = 1;
                            OverlayCanvas.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#01000000");
                            ResourceDictionary newRes = new();
                            var settingsDialog = Application.Current.Windows.Cast<Window>().FirstOrDefault(window => window is SettingsDialog) as SettingsDialog;
                            newRes.Source = settingsDialog.cbTemplates.SelectedIndex switch
                            {
                                2 => new Uri("/StreamOverlay;component/Templates/AOE4.xaml", UriKind.RelativeOrAbsolute),
                                1 => new Uri("/StreamOverlay;component/Templates/KOTOW.xaml", UriKind.RelativeOrAbsolute),
                                _ => new Uri("/StreamOverlay;component/Templates/AOE3DE.xaml", UriKind.RelativeOrAbsolute),
                            };
                            Application.Current.Resources.MergedDictionaries.Remove(Application.Current.Resources.MergedDictionaries.FirstOrDefault(x => x.Source.ToString().Contains("/Templates/")));
                            Application.Current.Resources.MergedDictionaries.Add(newRes);
                        }
                        else
                        {
                            PreviewImage.Opacity = 0;
                            OverlayCanvas.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#ff00b140");

                            /*
                            ResourceDictionary newRes = new();
                            var settingsDialog = Application.Current.Windows.Cast<Window>().FirstOrDefault(window => window is SettingsDialog) as SettingsDialog;

                            newRes.Source = new Uri("/StreamOverlay;component/Templates/AOE3DE.xaml", UriKind.RelativeOrAbsolute);

                            Application.Current.Resources.MergedDictionaries.Remove(Application.Current.Resources.MergedDictionaries.FirstOrDefault(x => x.Source.ToString().Contains("/Templates/")));
                            Application.Current.Resources.MergedDictionaries.Add(newRes);*/
                        }

                    });
            }
        }

        private ICommand scorePanelVisibility;
        public ICommand ScorePanelVisibility => scorePanelVisibility ??= new ActionCommand(() =>
                    {
                        if (gScorePanel.Visibility == Visibility.Visible)
                        {
                            gScorePanel.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            gScorePanel.Visibility = Visibility.Visible;
                        }


                    });


        public Overlay Current { get; set; }
        public List<Overlay> KOTOWSpecialScreen { get; set; }
        int CurrentPlayingOverlayIndex = 0;
        int CurrentPlayingOverlayCount = 0;


        public bool IsKOTOWSpecialScreeanActivated = false;

        private ICommand kotowSpecial;
        public ICommand KotowSpecial
        {
            get
            {
                return kotowSpecial ??= new ActionCommand(async () =>
                    {
                        if (!IsKOTOWSpecialScreeanActivated)
                        {

                            IsKOTOWSpecialScreeanActivated = true;
                            CurrentPlayingOverlayIndex = 0;
                            CurrentPlayingOverlayCount = 0;
                            _time = TimeSpan.FromMinutes(13.5);
                            IsCasting = true;
                            currentAudioIndex = Playlist.IndexOf(Playlist.FirstOrDefault(x => Path.GetFileName(x).StartsWith("001 Casting - ")));
                            await FadeOut(1000);
                            AudioPlayer.Source = new Uri(Playlist[currentAudioIndex]);
                            FadeIn(1000);
                            AudioPlayer.Play();
                            tbAudioName.Text = Path.GetFileName(Playlist[currentAudioIndex]);
                            bPlay.Visibility = Visibility.Collapsed;
                            bPause.Visibility = Visibility.Visible;
                            ResourceDictionary newRes = new()
                            {
                                Source = KOTOWSpecialScreen[CurrentPlayingOverlayIndex].title switch
                                {
                                    "Kings of The Old World" => new Uri("/StreamOverlay;component/Templates/KOTOW.xaml", UriKind.RelativeOrAbsolute),
                                    _ => new Uri("/StreamOverlay;component/Templates/AOE3DE.xaml", UriKind.RelativeOrAbsolute),
                                }
                            };
                            Application.Current.Resources.MergedDictionaries.Remove(Application.Current.Resources.MergedDictionaries.FirstOrDefault(x => x.Source.ToString().Contains("/Templates/")));
                            Application.Current.Resources.MergedDictionaries.Add(newRes);
                            _mediaPlayer.Play(new Media(_libVLC, new Uri(Path.Combine(AppContext.BaseDirectory, KOTOWSpecialScreen[0].video))));


                            _mediaPlayer.EndReached += (sender, e) =>
                            {

                                ThreadPool.QueueUserWorkItem(_ =>
                            {
                                if (IsKOTOWSpecialScreeanActivated)
                                {
                                    CurrentPlayingOverlayCount++;
                                    if (CurrentPlayingOverlayCount >= 3)
                                    {


                                        if (CurrentPlayingOverlayIndex >= KOTOWSpecialScreen.Count - 1)
                                        {
                                            CurrentPlayingOverlayIndex = 0;
                                        }
                                        else
                                        {
                                            CurrentPlayingOverlayIndex++;
                                        }
                                        CurrentPlayingOverlayCount = 0;
                                    }

                                    ResourceDictionary newRes = new()
                                    {
                                        Source = KOTOWSpecialScreen[CurrentPlayingOverlayIndex].title switch
                                        {
                                            "Kings of The Old World" => new Uri("/StreamOverlay;component/Templates/KOTOW.xaml", UriKind.RelativeOrAbsolute),
                                            _ => new Uri("/StreamOverlay;component/Templates/AOE3DE.xaml", UriKind.RelativeOrAbsolute),
                                        }
                                    };
                                    Application.Current.Resources.MergedDictionaries.Remove(Application.Current.Resources.MergedDictionaries.FirstOrDefault(x => x.Source.ToString().Contains("/Templates/")));
                                    Application.Current.Resources.MergedDictionaries.Add(newRes);

                                    _mediaPlayer.Play(new Media(_libVLC, new Uri(Path.Combine(AppContext.BaseDirectory, KOTOWSpecialScreen[CurrentPlayingOverlayIndex].video))));
                                }
                                else
                                {
                                    ResourceDictionary newRes = new()
                                    {
                                        Source = Current.title switch
                                        {
                                            "Kings of The Old World" => new Uri("/StreamOverlay;component/Templates/KOTOW.xaml", UriKind.RelativeOrAbsolute),
                                            _ => new Uri("/StreamOverlay;component/Templates/AOE3DE.xaml", UriKind.RelativeOrAbsolute),
                                        }
                                    };
                                    Application.Current.Resources.MergedDictionaries.Remove(Application.Current.Resources.MergedDictionaries.FirstOrDefault(x => x.Source.ToString().Contains("/Templates/")));
                                    Application.Current.Resources.MergedDictionaries.Add(newRes);
                                    _mediaPlayer.Play(new Media(_libVLC, new Uri(Path.Combine(AppContext.BaseDirectory, Current.video)), new string[] { ":input-repeat=65535" }));

                                }
                            });

                            };
                        }
                        else
                        {
                            IsKOTOWSpecialScreeanActivated = false;
                            CurrentPlayingOverlayIndex = 0;
                            CurrentPlayingOverlayCount = 0;
                            IsCasting = false;
                            ResourceDictionary newRes = new();
                            var settingsDialog = Application.Current.Windows.Cast<Window>().FirstOrDefault(window => window is SettingsDialog) as SettingsDialog;
                            newRes.Source = settingsDialog.cbTemplates.SelectedIndex switch
                            {
                                2 => new Uri("/StreamOverlay;component/Templates/AOE4.xaml", UriKind.RelativeOrAbsolute),
                                1 => new Uri("/StreamOverlay;component/Templates/KOTOW.xaml", UriKind.RelativeOrAbsolute),
                                _ => new Uri("/StreamOverlay;component/Templates/AOE3DE.xaml", UriKind.RelativeOrAbsolute),
                            };
                            Application.Current.Resources.MergedDictionaries.Remove(Application.Current.Resources.MergedDictionaries.FirstOrDefault(x => x.Source.ToString().Contains("/Templates/")));
                            Application.Current.Resources.MergedDictionaries.Add(newRes);
                            Animation.MediaPlayer.Play(new Media(_libVLC, new Uri(Path.Combine(AppContext.BaseDirectory, Current.video)), new string[] { ":input-repeat=65535" }));
                        }
                    });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        }
        public string TwitchIcon { get; set; }
        public DispatcherTimer AnimateMapPool = new();
        public DispatcherTimer TwitchInfo = new();
        public int Team1Score { get; set; }
        public int Team2Score { get; set; }

        private string team1Name;
        public string Team1Name
        {
            get
            {
                return team1Name;
            }
            set
            {
                team1Name = value;
                NotifyPropertyChanged("Team1Name");
            }
        }

        private string team2Name;
        public string Team2Name
        {
            get
            {
                return team2Name;
            }
            set
            {
                team2Name = value;
                NotifyPropertyChanged("Team2Name");
            }
        }

        public LibVLC _libVLC;
        public MediaPlayer _mediaPlayer;


        bool IsCasting = false;

        private readonly List<string> Playlist = new();
        private int currentAudioIndex = 0;
        public HttpClient client = new();

        private double volume;

        public double Volume
        {
            get
            {
                return volume;
            }
            set
            {
                volume = value;
                NotifyPropertyChanged("Volume");
            }
        }

        public static DependencyProperty SoundVolume = DependencyProperty.Register(
    "SoundVolume", typeof(double),
    typeof(OverlayWindow)
    );


        public OverlayWindow()
        {

            client.DefaultRequestHeaders.Add("Client-Id", "kimne78kx3ncx6brgo4mv6wki5h1ko");
            InitializeComponent();
            DataContext = this;
            Storyboard s = (Storyboard)TryFindResource("wait");
            s.Begin();
            var myCur = Application.GetResourceStream(new Uri("pack://application:,,,/resources/Cursor.cur")).Stream;
            Cursor = new Cursor(myCur);
            _time = TimeSpan.FromSeconds(0);
            _timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                NotifyPropertyChanged("Time");
                if (_time != TimeSpan.Zero)
                    _time = _time.Add(TimeSpan.FromSeconds(-1));
            }, Application.Current.Dispatcher);





            var settingsDialog = Application.Current.Windows.Cast<Window>().FirstOrDefault(window => window is SettingsDialog) as SettingsDialog;


            CIVS ss = FindResource("CivPool") as CIVS;

            var civs = settingsDialog.cbGames.SelectedIndex switch
            {
                0 => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "civs", "Age of Empires I")).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Civ { Icon = x }).ToList(),
                1 => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "civs", "Age of Empires II")).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Civ { Icon = x }).ToList(),
                2 => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "civs", "Age of Empires III")).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Civ { Icon = x }).ToList(),
                3 => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "civs", "Age of Empires IV")).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Civ { Icon = x }).ToList()

            };

            if (settingsDialog.cbGames.SelectedIndex == 3)
            {
                MinimapSize = 80;
            }
            else
                MinimapSize = 100;

            foreach (var civ in civs)
            {
                ss.Add(civ);
            }
            ss.Move(civs.FindIndex(c => c.Icon.Contains("random")), ss.Count - 1);
            NotifyPropertyChanged("CivPool");
            _timer.Start();
            Volume = settingsDialog.Setting.SoundVolume;
            sVolume.Value = Volume;

            Binding myBinding = new()
            {
                Source = this,
                Path = new PropertyPath("Volume"),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(this, SoundVolume, myBinding);

            Playlist = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "audio", "Age of Empires")).Where(x => Path.GetExtension(x).ToLower() == ".mp3").ToList();
            Playlist.AddRange(Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "audio", "Age of Empires II")).Where(x => Path.GetExtension(x).ToLower() == ".mp3"));
            Playlist.AddRange(Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "audio", "Age of Empires III")).Where(x => Path.GetExtension(x).ToLower() == ".mp3"));
            Playlist.AddRange(Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "audio", "Age of Empires IV")).Where(x => Path.GetExtension(x).ToLower() == ".mp3"));
            Playlist.AddRange(Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "audio", "Casting")).Where(x => Path.GetExtension(x).ToLower() == ".mp3"));

            AudioPlayer.Source = new Uri(Playlist[0]);
            tbAudioName.Text = Path.GetFileName(Playlist[currentAudioIndex]);


            if (settingsDialog.Setting.AutoplaySound)
            {
                AudioPlayer.Play();
                bPlay.Visibility = Visibility.Collapsed;
            }
            else
            {
                bPause.Visibility = Visibility.Collapsed;
            }

        }
        private void g_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var civ = (sender as Image).Tag as Civ;
            civ.Icon = (sender as Image).ToolTip.ToString();

        }
        public void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                switch (e.Key)
                {
                    case Key.Down:
                        TimeDown.Execute(null);
                        e.Handled = true;
                        return;

                    case Key.Up:
                        TimeUp.Execute(null);
                        e.Handled = true;
                        return;
                }
        }

        public void Player_Unloaded(object sender, RoutedEventArgs e)
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _libVLC.Dispose();
            AudioPlayer.Stop();
            var settingsDialog = Application.Current.Windows.Cast<Window>().FirstOrDefault(window => window is SettingsDialog) as SettingsDialog;
            settingsDialog.tbTeam1.Text = Team1Name;
            settingsDialog.tbTeam2.Text = Team2Name;
            settingsDialog.Setting.Countdown.Source = _time;
            Point CurrentPosition = new(Canvas.GetLeft(gCountdown), Canvas.GetTop(gCountdown));
            settingsDialog.Setting.Countdown.Position = new Position()
            {
                X = CurrentPosition.X,
                Y = CurrentPosition.Y,
                Type = Position.GetTypeFromPosition(CurrentPosition, gCountdown.ActualWidth, gCountdown.ActualHeight)
            };

            settingsDialog.Setting.Countdown.IsVisible = gCountdown.Visibility == Visibility.Visible;

            settingsDialog.Setting.Schedule.Source = Schedule.Text;
            CurrentPosition = new Point(Canvas.GetLeft(gSchedule), Canvas.GetTop(gSchedule));
            settingsDialog.Setting.Schedule.Position = new Position()
            {
                X = CurrentPosition.X,
                Y = CurrentPosition.Y,
                Type = Position.GetTypeFromPosition(CurrentPosition, gSchedule.ActualWidth, gSchedule.ActualHeight)
            };

            settingsDialog.Setting.Schedule.IsVisible = gSchedule.Visibility == Visibility.Visible;


            CurrentPosition = new Point(Canvas.GetLeft(gBrandLogo), Canvas.GetTop(gBrandLogo));
            settingsDialog.Setting.BrandLogo.Position = new Position()
            {
                X = CurrentPosition.X,
                Y = CurrentPosition.Y,
                Type = Position.GetTypeFromPosition(CurrentPosition, gBrandLogo.ActualWidth, gBrandLogo.ActualHeight)
            };

            settingsDialog.Setting.BrandLogo.IsVisible = gBrandLogo.Visibility == Visibility.Visible;
            settingsDialog.Setting.BrandLogo.Zoom = sBrandLogo.Value;


            CurrentPosition = new Point(Canvas.GetLeft(gEventLogo), Canvas.GetTop(gEventLogo));
            settingsDialog.Setting.EventLogo.Position = new Position()
            {
                X = CurrentPosition.X,
                Y = CurrentPosition.Y,
                Type = Position.GetTypeFromPosition(CurrentPosition, gEventLogo.ActualWidth, gEventLogo.ActualHeight)
            };

            settingsDialog.Setting.EventLogo.IsVisible = gEventLogo.Visibility == Visibility.Visible;
            settingsDialog.Setting.EventLogo.Zoom = sEventLogo.Value;
            settingsDialog.Setting.SoundVolume = Volume;



            settingsDialog.Setting.TwitchPanel.Source = TwitchChannel.Text;
            CurrentPosition = new Point(Canvas.GetLeft(gTwitchInfo), Canvas.GetTop(gTwitchInfo));
            settingsDialog.Setting.TwitchPanel.Position = new Position()
            {
                X = CurrentPosition.X,
                Y = CurrentPosition.Y,
                Type = Position.GetTypeFromPosition(CurrentPosition, gTwitchInfo.ActualWidth, gTwitchInfo.ActualHeight)
            };

            settingsDialog.Setting.TwitchPanel.IsVisible = gTwitchInfo.Visibility == Visibility.Visible;

            settingsDialog.Setting.Map.IsVisible = gMapPool.Visibility == Visibility.Visible;


            //settingsDialog.Setting.Map.Source = TwitchChannel.Text;
            CurrentPosition = new Point(Canvas.GetLeft(gMapPool), Canvas.GetTop(gMapPool));
            settingsDialog.Setting.Map.Position = new Position()
            {
                X = CurrentPosition.X,
                Y = CurrentPosition.Y,
                Type = Position.GetTypeFromPosition(CurrentPosition, gMapPool.ActualWidth, gMapPool.ActualHeight)
            };


            settingsDialog.Setting.Chromakey = ((SolidColorBrush)OverlayCanvas.Background).Color == ((SolidColorBrush)(new BrushConverter().ConvertFromString("#ff00b140"))).Color;
            settingsDialog.Setting.PlayersPanel.IsVisible = gPlayersPanel.Visibility == Visibility.Visible;
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            if (needClose)
                Environment.Exit(0);
        }


        double FirstXPos, FirstYPos;
        object MovingObject;

        private void ObjectMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Slider)
            {
                FirstXPos = e.GetPosition(sender as FrameworkElement).X;
                FirstYPos = e.GetPosition(sender as FrameworkElement).Y;
                MovingObject = sender;
            }

        }

        private void ObjectMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Slider)
            {
                MovingObject = null;
            }
            VerticalLine.Visibility = Visibility.Collapsed;
            HorizontalLine.Visibility = Visibility.Collapsed;
        }

        private void OverlayCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBox)
                this.Focus();
        }

        private void Team1_MouseDown(object sender, MouseButtonEventArgs e)
        {

            int Tag = Convert.ToInt32((sender as Grid).Tag);

            var civ1 = Team1Player1CivPool.FirstOrDefault(x => x.Tag == Tag);
            var civ2 = Team1Player2CivPool.FirstOrDefault(x => x.Tag == Tag);
            var civ3 = Team1Player3CivPool.FirstOrDefault(x => x.Tag == Tag);

            if (civ1 != null)
            {
                civ1.NextStatus();
            }
            if (civ2 != null)
            {
                civ2.NextStatus();
            }
            if (civ3 != null)
            {
                civ3.NextStatus();
            }
            Team1Score = Team1Player1CivPool.Where(x => x.Status == 1).Count();

            NotifyPropertyChanged("Team1Score");

        }

        private void Team2_MouseDown(object sender, MouseButtonEventArgs e)
        {

            int Tag = Convert.ToInt32((sender as Grid).Tag);

            var civ1 = Team2Player1CivPool.FirstOrDefault(x => x.Tag == Tag);
            var civ2 = Team2Player2CivPool.FirstOrDefault(x => x.Tag == Tag);
            var civ3 = Team2Player3CivPool.FirstOrDefault(x => x.Tag == Tag);

            if (civ1 != null)
            {
                civ1.NextStatus();
            }
            if (civ2 != null)
            {
                civ2.NextStatus();
            }
            if (civ3 != null)
            {
                civ3.NextStatus();
            }
            Team2Score = Team2Player1CivPool.Where(x => x.Status == 1).Count();
            NotifyPropertyChanged("Team2Score");
        }



        private void Slider_ValueChanged(System.Object sender, RoutedPropertyChangedEventArgs<System.Double> e)
        {
            stBrandLogo.ScaleX = 1 + e.NewValue / 100;
            stBrandLogo.ScaleY = 1 + e.NewValue / 100;
        }

        private void Slider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            stEventLogo.ScaleX = 1 + e.NewValue / 100;
            stEventLogo.ScaleY = 1 + e.NewValue / 100;
        }

        private void AudioPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (cbLoop.IsChecked == false)
            {


            IncreaseIndex:
                {
                    if (currentAudioIndex >= Playlist.Count - 1)
                        currentAudioIndex = 0;
                    else
                        currentAudioIndex++;
                }

                if (IsCasting == false && Path.GetFileName(Playlist[currentAudioIndex]).Contains("Casting - "))
                {
                    goto IncreaseIndex;
                }


            }
            if (!Path.GetFileName(Playlist[currentAudioIndex]).Contains("Casting - "))
            {
                if (IsCasting)
                {
                    IsKOTOWSpecialScreeanActivated = false;
                    IsCasting = false;
                    AudioPlayer.Source = new Uri(Playlist[currentAudioIndex]);
                    //FadeIn(1000);
                    AudioPlayer.Pause();
                    tbAudioName.Text = Path.GetFileName(Playlist[currentAudioIndex]);
                    bPlay.Visibility = Visibility.Visible;
                    bPause.Visibility = Visibility.Collapsed;
                    return;
                }

            }


            AudioPlayer.Source = new Uri(Playlist[currentAudioIndex]);
            FadeIn(1000);
            AudioPlayer.Play();
            tbAudioName.Text = Path.GetFileName(Playlist[currentAudioIndex]);
            bPlay.Visibility = Visibility.Collapsed;
            bPause.Visibility = Visibility.Visible;
        }

        private async void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
        DecreaseIndex:
            {
                if (currentAudioIndex == 0)
                    currentAudioIndex = Playlist.Count - 1;
                else
                    currentAudioIndex--;
            }
            if (IsCasting == false && Path.GetFileName(Playlist[currentAudioIndex]).Contains("Casting - "))
            {
                goto DecreaseIndex;
            }




            if (!Path.GetFileName(Playlist[currentAudioIndex]).Contains("Casting - "))
            {
                IsCasting = false;
                IsKOTOWSpecialScreeanActivated = false;
            }



            await FadeOut(1000);
            AudioPlayer.Source = new Uri(Playlist[currentAudioIndex]);
            FadeIn(1000);
            AudioPlayer.Play();
            tbAudioName.Text = Path.GetFileName(Playlist[currentAudioIndex]);
            bPlay.Visibility = Visibility.Collapsed;
            bPause.Visibility = Visibility.Visible;
        }

        private void Image_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            FadeIn(1000);
            AudioPlayer.Play();
            bPlay.Visibility = Visibility.Collapsed;
            bPause.Visibility = Visibility.Visible;
        }

        private async void Image_MouseDown_2(object sender, MouseButtonEventArgs e)
        {
            await FadeOut(1000);
            AudioPlayer.Pause();
            bPlay.Visibility = Visibility.Visible;
            bPause.Visibility = Visibility.Collapsed;
        }

        public async Task FadeOut(int duration)
        {
            double oldVolume = Volume;
            this.BeginAnimation(SoundVolume, new DoubleAnimation(0, TimeSpan.FromMilliseconds(duration)));
            await Task.Delay(duration);
            Volume = oldVolume;
        }

        public void FadeIn(int duration)
        {
            this.BeginAnimation(SoundVolume, new DoubleAnimation(Volume, TimeSpan.FromMilliseconds(duration)));
        }

        private async void Image_MouseDown_3(object sender, MouseButtonEventArgs e)
        {


        IncreaseIndex:
            {
                if (currentAudioIndex >= Playlist.Count - 1)
                    currentAudioIndex = 0;
                else
                    currentAudioIndex++;
            }

            if (IsCasting == false && Path.GetFileName(Playlist[currentAudioIndex]).Contains("Casting - "))
            {
                goto IncreaseIndex;
            }



            if (!Path.GetFileName(Playlist[currentAudioIndex]).Contains("Casting - "))
            {
                IsKOTOWSpecialScreeanActivated = false;
                IsCasting = false;
            }

            await FadeOut(1000);
            AudioPlayer.Source = new Uri(Playlist[currentAudioIndex]);
            FadeIn(1000);
            AudioPlayer.Play();

            tbAudioName.Text = Path.GetFileName(Playlist[currentAudioIndex]);
            bPlay.Visibility = Visibility.Collapsed;
            bPause.Visibility = Visibility.Visible;
        }

        private async void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IsCasting = false;
            IsKOTOWSpecialScreeanActivated = false;
            currentAudioIndex = Playlist.IndexOf(Playlist.FirstOrDefault(x => Path.GetFileName(x).StartsWith("001 Age of Empires - ")));
            await FadeOut(1000);
            AudioPlayer.Source = new Uri(Playlist[currentAudioIndex]);
            FadeIn(1000);
            AudioPlayer.Play();
            tbAudioName.Text = Path.GetFileName(Playlist[currentAudioIndex]);
            bPlay.Visibility = Visibility.Collapsed;
            bPause.Visibility = Visibility.Visible;
        }

        private async void TextBlock_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            IsCasting = false;
            IsKOTOWSpecialScreeanActivated = false;
            currentAudioIndex = Playlist.IndexOf(Playlist.FirstOrDefault(x => Path.GetFileName(x).StartsWith("001 Age of Empires II - ")));
            await FadeOut(1000);
            AudioPlayer.Source = new Uri(Playlist[currentAudioIndex]);
            FadeIn(1000);
            AudioPlayer.Play();
            tbAudioName.Text = Path.GetFileName(Playlist[currentAudioIndex]);
            bPlay.Visibility = Visibility.Collapsed;
            bPause.Visibility = Visibility.Visible;
        }

        private async void TextBlock_MouseDown_2(object sender, MouseButtonEventArgs e)
        {
            IsCasting = false;
            IsKOTOWSpecialScreeanActivated = false;
            currentAudioIndex = Playlist.IndexOf(Playlist.FirstOrDefault(x => Path.GetFileName(x).StartsWith("001 Age of Empires III - ")));
            await FadeOut(1000);
            AudioPlayer.Source = new Uri(Playlist[currentAudioIndex]);
            FadeIn(1000);
            AudioPlayer.Play();
            tbAudioName.Text = Path.GetFileName(Playlist[currentAudioIndex]);
            bPlay.Visibility = Visibility.Collapsed;
            bPause.Visibility = Visibility.Visible;
        }

        private async void TextBlock_MouseDown_3(object sender, MouseButtonEventArgs e)
        {
            IsCasting = false;
            IsKOTOWSpecialScreeanActivated = false;
            currentAudioIndex = Playlist.IndexOf(Playlist.FirstOrDefault(x => Path.GetFileName(x).StartsWith("001 Age of Empires IV - ")));
            await FadeOut(1000);
            AudioPlayer.Source = new Uri(Playlist[currentAudioIndex]);
            FadeIn(1000);
            AudioPlayer.Play();
            tbAudioName.Text = Path.GetFileName(Playlist[currentAudioIndex]);
            bPlay.Visibility = Visibility.Collapsed;
            bPause.Visibility = Visibility.Visible;
        }

        bool CasterShowed = false;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!CasterShowed)
            {

                CircleEase ease1 = new()
                {
                    EasingMode = EasingMode.EaseInOut
                };
                DoubleAnimation center = new(0, 1, TimeSpan.FromSeconds(0.8))
                {
                    EasingFunction = ease1
                };

                DoubleAnimation center2 = new(0, 1, TimeSpan.FromSeconds(1))
                {
                    EasingFunction = ease1
                };
                CasterTransparentStop.BeginAnimation(GradientStop.OffsetProperty, center);
                CasterBlackStop.BeginAnimation(GradientStop.OffsetProperty, center2);
                CasterShowed = true;
            }
            else
            {
                CircleEase ease1 = new()
                {
                    EasingMode = EasingMode.EaseInOut
                };
                DoubleAnimation center3 = new(1, 0, TimeSpan.FromSeconds(0.8))
                {
                    BeginTime = TimeSpan.FromSeconds(0.4),
                    EasingFunction = ease1
                };

                DoubleAnimation center4 = new(1, 0, TimeSpan.FromSeconds(1))
                {
                    EasingFunction = ease1
                };

                CasterTransparentStop.BeginAnimation(GradientStop.OffsetProperty, center3);
                CasterBlackStop.BeginAnimation(GradientStop.OffsetProperty, center4);
                CasterShowed = false;
            }

        }

        private void sVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.BeginAnimation(SoundVolume, new DoubleAnimation(e.NewValue/100, TimeSpan.FromMilliseconds(0)));
            Volume = e.NewValue;
        }

        private async void TextBlock_MouseDown_4(object sender, MouseButtonEventArgs e)
        {
            _time = TimeSpan.FromMinutes(13.5);
            IsCasting = true;
            currentAudioIndex = Playlist.IndexOf(Playlist.FirstOrDefault(x => Path.GetFileName(x).StartsWith("001 Casting - ")));
            await FadeOut(1000);
            AudioPlayer.Source = new Uri(Playlist[currentAudioIndex]);
            FadeIn(1000);
            AudioPlayer.Play();
            tbAudioName.Text = Path.GetFileName(Playlist[currentAudioIndex]);
            bPlay.Visibility = Visibility.Collapsed;
            bPause.Visibility = Visibility.Visible;
        }

        private void mousemove_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not Slider && MovingObject != null)
            {
                var element = sender as FrameworkElement;
                var point = Mouse.GetPosition(OverlayCanvas);
                
                if (Math.Abs(point.X - (1920 - element.ActualWidth) / 2) < 20)
                {
                    Canvas.SetLeft(element, (1920 - element.ActualWidth) / 2);
                }

                if (Math.Abs(point.Y - (1080 - element.ActualHeight) / 2) < 20)
                {
                    Canvas.SetTop(element, (1080 - element.ActualHeight) / 2);
                }
            }
        }

        private void ObjectMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (MovingObject != null)
                {
                    var element = MovingObject as FrameworkElement;
                    var point = Mouse.GetPosition(OverlayCanvas);
                    

                    double ActualWidth;
                    double ActualHeight;
                    double MouseX;
                    double MouseY;

                    if (element.Name == "iEventLogo")
                    {
                        ActualWidth = element.ActualWidth * stEventLogo.ScaleX;
                        ActualHeight = element.ActualHeight * stEventLogo.ScaleY;

                        MouseX = FirstXPos * stEventLogo.ScaleX;
                        MouseY = FirstYPos * stEventLogo.ScaleY;
                    }
                    else if (element.Name == "iBrandLogo")
                    {
                        ActualWidth = element.ActualWidth * stBrandLogo.ScaleX;
                        ActualHeight = element.ActualHeight * stBrandLogo.ScaleY;

                        MouseX = FirstXPos * stBrandLogo.ScaleX;
                        MouseY = FirstYPos * stBrandLogo.ScaleY;
                    }
                    else if (element.Name == "gTwitchInfo")
                    {
                        ActualWidth = element.ActualWidth * stTwitchInfo.ScaleX;
                        ActualHeight = element.ActualHeight * stTwitchInfo.ScaleY;

                        MouseX = FirstXPos * stTwitchInfo.ScaleX;
                        MouseY = FirstYPos * stTwitchInfo.ScaleY;
                    }
                    else
                    {
                        ActualWidth = element.ActualWidth;
                        ActualHeight = element.ActualHeight;

                        MouseX = FirstXPos;
                        MouseY = FirstYPos;
                    }



                    // Right Edge
                    if (Math.Abs(point.X - MouseX - 1920 + 20 + ActualWidth) < 20)
                    {
                        Canvas.SetLeft(element, 1920 - 20 - ActualWidth);
                        VerticalLine.Visibility = Visibility.Visible;
                        VerticalLine.X1 = 1920 - 20;
                        VerticalLine.X2 = 1920 - 20;
                    }
                    // Left Edge
                    else if (Math.Abs(point.X - MouseX - 20) < 20)
                    {
                        Canvas.SetLeft(element, 20);
                        VerticalLine.Visibility = Visibility.Visible;
                        VerticalLine.X1 = 20;
                        VerticalLine.X2 = 20;
                    }
                    // Center X
                    else if (Math.Abs(point.X - MouseX - (1920 - ActualWidth) / 2) < 20)
                    {
                        Canvas.SetLeft(element, (1920 - ActualWidth) / 2);
                        VerticalLine.Visibility = Visibility.Visible;
                        VerticalLine.X1 = 1920 / 2;
                        VerticalLine.X2 = 1920 / 2;
                    }
                    else
                    {
                        Canvas.SetLeft(element, point.X - MouseX);
                        VerticalLine.Visibility = Visibility.Collapsed;
                    }



                    // Top Edge
                    if (Math.Abs(point.Y - MouseY - 20) < 20)
                    {
                        Canvas.SetTop(element, 20);
                        HorizontalLine.Visibility = Visibility.Visible;
                        HorizontalLine.Y1 = 20;
                        HorizontalLine.Y2 = 20;
                    }
                    // Bottom Edge
                    else if (Math.Abs(point.Y - MouseY - 1080 + 20 + ActualHeight) < 20)
                    {
                        Canvas.SetTop(element, 1080 - 20 - ActualHeight);
                        HorizontalLine.Visibility = Visibility.Visible;
                        HorizontalLine.Y1 = 1080 - 20;
                        HorizontalLine.Y2 = 1080 - 20;
                    }
                    // Center Y
                    else if (Math.Abs(point.Y - MouseY - (1080 - ActualHeight) / 2) < 20)
                    {
                        Canvas.SetTop(element, (1080 - ActualHeight) / 2);
                        HorizontalLine.Visibility = Visibility.Visible;
                        HorizontalLine.Y1 = 1080 / 2;
                        HorizontalLine.Y2 = 1080 / 2;
                    }
                    else
                    {
                        Canvas.SetTop(element, point.Y - MouseY);
                        HorizontalLine.Visibility = Visibility.Collapsed;
                    }
                }
            }

        }
    }
}
