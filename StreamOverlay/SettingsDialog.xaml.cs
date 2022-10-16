using LibVLCSharp.Shared;
using StreamOverlay.Classes.Civ;
using StreamOverlay.Classes.Map;
using StreamOverlay.Classes.Overlays;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using static StreamOverlay.Classes.Twitch.Twitch;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;


namespace StreamOverlay
{
    public class PathToBrushConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && value is string @string)
            {
                var ib = new ImageBrush()
                {
                    ImageSource = new BitmapImage(new Uri(@string)),
                    Stretch = Stretch.Uniform
                };
                return ib;
            }
            return value;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    /// <summary>
    /// Логика взаимодействия для SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #region Methods

        private static ObservableCollection<Map> BuildMaps(int game)
        {


            List<Map> maps = game switch
            {
                3 => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "maps", "Age of Empires IV")).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Map { title = Path.GetFileNameWithoutExtension(x).ToUpper(), icon = x }).ToList(),
                2 => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "maps", "Age of Empires III")).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Map { title = Path.GetFileNameWithoutExtension(x).ToUpper(), icon = x }).ToList(),
                1 => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "maps", "Age of Empires II")).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Map { title = Path.GetFileNameWithoutExtension(x).ToUpper(), icon = x }).ToList(),
                0 => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "maps", "Age of Empires")).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Map { title = Path.GetFileNameWithoutExtension(x).ToUpper(), icon = x }).ToList(),

            };
            ObservableCollection<Map> Maps = new(maps);
            return Maps;
        }

        #endregion

        #region Properties and Variables

        public ObservableCollection<Civ> CivPool { get; set; } = new();

        public ObservableCollection<Civ> Team1Player1CivPool = new();
        public ObservableCollection<Civ> Team1Player2CivPool = new();
        public ObservableCollection<Civ> Team1Player3CivPool = new();

        public ObservableCollection<Civ> Team2Player1CivPool = new();
        public ObservableCollection<Civ> Team2Player2CivPool = new();
        public ObservableCollection<Civ> Team2Player3CivPool = new();

        private int selectedOverlayIndex;
        public int SelectedOverlayIndex
        {
            get
            {
                return selectedOverlayIndex;
            }
            set
            {
                selectedOverlayIndex = value;
                NotifyPropertyChanged("SelectedOverlayIndex");
                NotifyPropertyChanged("SelectedOverlay");
                NotifyPropertyChanged("isMapPoolEnabled");
                NotifyPropertyChanged("isRoundEnabled");
                NotifyPropertyChanged("isPlayersEnabled");
                NotifyPropertyChanged("isTeamsEnabled");
                NotifyPropertyChanged("isCivsEnabled");
            }
        }
        public Overlay SelectedOverlay
        {
            get
            {
                return Overlays[selectedOverlayIndex];
            }
        }

        private Cursor FAoE;
        public Cursor AoE
        {
            get { return FAoE; }
            set
            {
                FAoE = value;
                NotifyPropertyChanged();
            }
        }


        public ObservableCollection<Map> MapPool = new();
        private readonly ObservableCollection<Overlay> Overlays = new();

        public List<Logo> persons1v1 = new();
        public List<Logo> persons2v2 = new();
        public List<Logo> persons3v3 = new();

        #endregion


        private int CurrentPlayingOverlayIndex = 0;
        private int CurrentPlayingOverlayCount = 0;
        private readonly int Version = 33;

        private double minimapSize = 185;
        public double MinimapSize
        {
            get
            {
                return minimapSize;
            }
            set
            {
                minimapSize = value;
                NotifyPropertyChanged();
            }
        }

        private bool filterChecked = false;
        public bool FilterChecked
        {
            get
            {
                return filterChecked;
            }
            set
            {
                filterChecked = value;
                NotifyPropertyChanged();
                collectionView.Refresh();
            }
        }

        private string mapFilter = "";
        public string MapFilter
        {
            get
            {
                return mapFilter;
            }
            set
            {
                mapFilter = value;
                NotifyPropertyChanged();
                collectionView.Refresh();
            }
        }

        private readonly ICollectionView collectionView;
        bool Filter(object filter)
        {
            if (FilterChecked)
            {
                if (filter == null) return (filter as Map).Order != 0 || (filter as Map).Home != 0 || (filter as Map).Veto != 0;
                return (filter as Map).title.ToLower().Contains(MapFilter) && ((filter as Map).Order != 0 || (filter as Map).Home != 0 || (filter as Map).Veto != 0);
            }
            else
            {
                if (filter == null) return true;
                return (filter as Map).title.ToLower().Contains(MapFilter);
            }

        }

        public Setting Setting { get; set; } = new Setting();

        public SettingsDialog()
        {

            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata { DefaultValue = 15 });
            InitializeComponent();
            DataContext = this;
            try
            {
                Setting = JsonSerializer.Deserialize<Setting>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "setting.json")));
            }
            catch
            {
                Setting = new()
                {
                    AppVersion = Version,
                    AppLanguage = cbLanguages.SelectedIndex
                };

                Setting.PlayersPanel.Source = 0;

                Setting.Countdown.Position = new Position() { Type = PositionType.TopRight, X = 0, Y = 0 };
                Setting.Countdown.Zoom = 0;
                Setting.Countdown.Source = TimeSpan.Zero;
                Setting.Countdown.IsVisible = false;

                Setting.Map.Position = new Position() { Type = PositionType.BottomLeft, X = 0, Y = 0 };
                Setting.Map.Zoom = 0;
                Setting.Map.Source = null;
                Setting.Map.IsVisible = true;

                Setting.Schedule.Position = new Position() { Type = PositionType.BottomRight, X = 0, Y = 0 };
                Setting.Schedule.Zoom = 0;
                Setting.Schedule.Source = "";
                Setting.Schedule.IsVisible = false;

                Setting.EventLogo.Position = new Position() { Type = PositionType.TopLeft, X = 0, Y = 0 };
                Setting.EventLogo.Zoom = 0;
                Setting.EventLogo.Source = "";
                Setting.EventLogo.IsVisible = false;

                Setting.BrandLogo.Position = new Position() { Type = PositionType.TopLeft, X = 0, Y = 0 };
                Setting.BrandLogo.Zoom = 0;
                Setting.BrandLogo.Source = "";
                Setting.BrandLogo.IsVisible = false;

                Setting.TwitchPanel.Position = new Position() { Type = PositionType.TopLeft, X = 0, Y = 0 };
                Setting.TwitchPanel.Zoom = 0;
                Setting.TwitchPanel.Source = "";
                Setting.TwitchPanel.IsVisible = false;

            }
            cbLanguages.SelectedIndex = Setting.AppLanguage;
            AoE = new Cursor(Application.GetResourceStream(new Uri("pack://application:,,,/resources/Cursor.cur")).Stream);
            cbGames.SelectedIndex = Setting.SelectedGame;
            if (cbGames.SelectedIndex == 3)
            {
                minimapSize = 150;
            }
            else
            {
                minimapSize = 185;
            }
            cbGames.SelectionChanged += cbGames_SelectionChanged;
            MapPool = new(BuildMaps(cbGames.SelectedIndex));



            foreach (var m in Setting.SelectedMaps)
            {
                var map = MapPool.FirstOrDefault(x => x.title.ToUpper() == m.title.ToUpper());
                if (map != null)
                {
                    map.Order = m.Order;
                    map.Home = m.Home;
                    map.Veto = m.Veto;

                }
            }



            var civs = cbGames.SelectedIndex switch
            {
                0 => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "civs", "Age of Empires I")).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Civ { Icon = x }).ToList(),
                1 => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "civs", "Age of Empires II")).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Civ { Icon = x }).ToList(),
                2 => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "civs", "Age of Empires III")).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Civ { Icon = x }).ToList(),
                3 => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "civs", "Age of Empires IV")).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Civ { Icon = x }).ToList()

            };

            CivPool = new ObservableCollection<Civ>(civs);
            CivPool.Move(civs.FindIndex(c => c.Icon.Contains("random")), CivPool.Count - 1);
            NotifyPropertyChanged("CivPool");


            cbTemplates.SelectedIndex = Setting.ElementsStyle;
            TeamPanel.SelectedIndex = Convert.ToInt32(Setting.PlayersPanel.Source.ToString());
            cbCivVetoCount.SelectedIndex = Setting.CivVeto;
            tbTeam1.Text = Setting.Team1Name;
            tbTeam2.Text = Setting.Team2Name;
            
            alignBO();
            cbCivVetoCount.SelectionChanged += cbCivVetoCount_SelectionChanged;
            lwTeam1Player1CivPool.ItemsSource = Team1Player1CivPool;
            lwTeam1Player2CivPool.ItemsSource = Team1Player2CivPool;
            lwTeam1Player3CivPool.ItemsSource = Team1Player3CivPool;

            lwTeam2Player1CivPool.ItemsSource = Team2Player1CivPool;
            lwTeam2Player2CivPool.ItemsSource = Team2Player2CivPool;
            lwTeam2Player3CivPool.ItemsSource = Team2Player3CivPool;

            lvMapPool.ItemsSource = MapPool;

            collectionView = CollectionViewSource.GetDefaultView(lvMapPool.ItemsSource);
            collectionView.Filter += Filter;

            collectionView.SortDescriptions.Add(new SortDescription("title", ListSortDirection.Ascending));
            var view = (ICollectionViewLiveShaping)CollectionViewSource.GetDefaultView(lvMapPool.ItemsSource);
            view.IsLiveSorting = true;


            List<Logo> brandLogos = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "logo")).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Logo { Name = Path.GetFileNameWithoutExtension(x), Path = x }).ToList();
            brandLogos.Insert(0, new Logo() { Name = "<NOT SET>", Path = "" });


            persons1v1 = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "persons", "1v1"), "*.*", SearchOption.AllDirectories).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Logo { Name = Path.GetRelativePath(Path.Combine(AppContext.BaseDirectory, "data", "persons", "1v1"), x), Path = x }).ToList();
            persons1v1.Insert(0, new Logo() { Name = "<NOT SET>", Path = "" });

            persons2v2 = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "persons", "2v2"), "*.*", SearchOption.AllDirectories).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Logo { Name = Path.GetRelativePath(Path.Combine(AppContext.BaseDirectory, "data", "persons", "2v2"), x), Path = x }).ToList();
            persons2v2.Insert(0, new Logo() { Name = "<NOT SET>", Path = "" });

            persons3v3 = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "persons", "3v3"), "*.*", SearchOption.AllDirectories).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Logo { Name = Path.GetRelativePath(Path.Combine(AppContext.BaseDirectory, "data", "persons", "3v3"), x), Path = x }).ToList();
            persons3v3.Insert(0, new Logo() { Name = "<NOT SET>", Path = "" });


            if (TeamPanel.SelectedIndex == 0)
            {
                cbTeam1Persons.ItemsSource = persons1v1;
                cbTeam2Persons.ItemsSource = persons1v1;

                var team1 = persons1v1.FirstOrDefault(x => x.Name == Setting.Team1Persons);
                if (team1 != null)
                {
                    cbTeam1Persons.SelectedItem = team1;
                }
                else
                {
                    cbTeam1Persons.SelectedIndex = 0;
                }

                var team2 = persons1v1.FirstOrDefault(x => x.Name == Setting.Team2Persons);
                if (team2 != null)
                {
                    cbTeam2Persons.SelectedItem = team2;
                }
                else
                {
                    cbTeam2Persons.SelectedIndex = 0;
                }
            }
            if (TeamPanel.SelectedIndex == 1)
            {
                cbTeam1Persons.ItemsSource = persons2v2;
                cbTeam2Persons.ItemsSource = persons2v2;

                var team1 = persons2v2.FirstOrDefault(x => x.Name == Setting.Team1Persons);
                if (team1 != null)
                {
                    cbTeam1Persons.SelectedItem = team1;
                }
                else
                {
                    cbTeam1Persons.SelectedIndex = 0;
                }

                var team2 = persons2v2.FirstOrDefault(x => x.Name == Setting.Team2Persons);
                if (team2 != null)
                {
                    cbTeam2Persons.SelectedItem = team2;
                }
                else
                {
                    cbTeam2Persons.SelectedIndex = 0;
                }
            }
            if (TeamPanel.SelectedIndex == 2)
            {
                cbTeam1Persons.ItemsSource = persons3v3;
                cbTeam2Persons.ItemsSource = persons3v3;

                var team1 = persons3v3.FirstOrDefault(x => x.Name == Setting.Team1Persons);
                if (team1 != null)
                {
                    cbTeam1Persons.SelectedItem = team1;
                }
                else
                {
                    cbTeam1Persons.SelectedIndex = 0;
                }

                var team2 = persons3v3.FirstOrDefault(x => x.Name == Setting.Team2Persons);
                if (team2 != null)
                {
                    cbTeam2Persons.SelectedItem = team2;
                }
                else
                {
                    cbTeam2Persons.SelectedIndex = 0;
                }
            }

            ICollectionView brand_view = CollectionViewSource.GetDefaultView(brandLogos);
            brand_view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            BrandLogos.ItemsSource = brand_view;
            var brand = brandLogos.FirstOrDefault(x => x.Name == Setting.BrandLogo.Source.ToString());
            if (brand != null)
            {
                BrandLogos.SelectedItem = brand;
            }

            var animations = Directory.GetDirectories(Path.Combine(AppContext.BaseDirectory, "data", "animations"));
            SelectedOverlayIndex = 0;
            int i = 0;
            foreach (var anim in animations)
            {
                if (File.Exists(Path.Combine(anim, "video.mp4")) && File.Exists(Path.Combine(anim, "icon.png")) && File.Exists(Path.Combine(anim, "preview.png")))
                {
                    Overlays.Add(new Overlay() { isLooped = File.Exists(Path.Combine(anim, "looped")), title = Path.GetFileName(anim), preview = Path.Combine(anim, "preview.png"), icon = Path.Combine(anim, "icon.png"), video = Path.Combine(anim, "video.mp4") });
                    if (Path.GetFileName(anim) == Setting.SelectedOverlay)
                    {
                        SelectedOverlayIndex = i;
                    }
                    i++;
                }

            }



            List<Logo> eventLogos = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "logo")).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Logo { Name = Path.GetFileNameWithoutExtension(x), Path = x }).ToList();
            eventLogos.Insert(0, new Logo() { Name = "<NOT SET>", Path = "" });
            ICollectionView event_view = CollectionViewSource.GetDefaultView(eventLogos);
            brand_view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            EventLogos.ItemsSource = event_view;

            var eve = eventLogos.FirstOrDefault(x => x.Name == Setting.EventLogo.Source.ToString());
            if (eve != null)
            {
                EventLogos.SelectedItem = eve;
            }

            if (File.Exists(Path.Combine(AppContext.BaseDirectory, "UpdateCounter.txt")))
            {
                int counter = Convert.ToInt32(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "UpdateCounter.txt")));
                if (counter < Version)
                {
                    gReleaseNotes.Visibility = Visibility.Visible;
                    BlurControl.Visibility = Visibility.Visible;
                    File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "UpdateCounter.txt"), Version.ToString());
                }
            }
            else
            {
                gReleaseNotes.Visibility = Visibility.Visible;
                BlurControl.Visibility = Visibility.Visible;
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "UpdateCounter.txt"), Version.ToString());
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

            ThumbnailGenerator thumbnailGenerator = new();

            thumbnailGenerator.Background.Source = new BitmapImage(new Uri(SelectedOverlay.preview));
            //thumbnailGenerator.Overlay.Source = new BitmapImage(new Uri(thumbnail.background.overlay));

            if (BrandLogos.SelectedIndex == 0)
            {
                thumbnailGenerator.iBrandLogo.Visibility = Visibility.Collapsed;
            }
            else
            {
                thumbnailGenerator.iBrandLogo.Source = new BitmapImage(new Uri((BrandLogos.SelectedItem as Logo).Path));
            }

            if (EventLogos.SelectedIndex == 0)
            {
                thumbnailGenerator.iEventLogo.Visibility = Visibility.Collapsed;
            }
            else
            {
                thumbnailGenerator.iEventLogo.Source = new BitmapImage(new Uri((EventLogos.SelectedItem as Logo).Path));

            }

            thumbnailGenerator.BorderSlider.Value = 10;
            thumbnailGenerator.BlurSlider.Value = 3;
            thumbnailGenerator.FontColorBorder.SelectedColor = (Color)ColorConverter.ConvertFromString("#ff000001");

            thumbnailGenerator.FontTitle.Text = "/Fonts/#Trajan Pro 3";
            thumbnailGenerator.ShadowColorTitle.SelectedColor = (Color)ColorConverter.ConvertFromString("#ff000000");
            thumbnailGenerator.ShadowTitleSlider.Value = 30;
            thumbnailGenerator.FontColorTitle.SelectedColor = (Color)ColorConverter.ConvertFromString("#ffd1372f");
            thumbnailGenerator.FontSizeSliderTitle.Value = 160;
            thumbnailGenerator.TitleThumbnail.SetValue(Canvas.LeftProperty, 120.0);
            thumbnailGenerator.TitleThumbnail.SetValue(Canvas.TopProperty, 45.0);


            thumbnailGenerator.FontRound.Text = "/Fonts/#Trajan Pro 3";
            thumbnailGenerator.ShadowColorRound.SelectedColor = (Color)ColorConverter.ConvertFromString("#ff000000");
            thumbnailGenerator.ShadowRoundSlider.Value = 20;
            thumbnailGenerator.FontColorRound.SelectedColor = (Color)ColorConverter.ConvertFromString("#ffFFFFFF");
            thumbnailGenerator.FontSizeSliderRound.Value = 120;
            thumbnailGenerator.RoundThumbnail.SetValue(Canvas.LeftProperty, 0.0);
            thumbnailGenerator.RoundThumbnail.SetValue(Canvas.TopProperty, 0.0);

            thumbnailGenerator.FontPlayer.Text = "/Fonts/#Trajan Pro 3";
            thumbnailGenerator.ShadowColorPlayer.SelectedColor = (Color)ColorConverter.ConvertFromString("#ff000000");
            thumbnailGenerator.ShadowPlayerSlider.Value = 20;
            thumbnailGenerator.FontColorPlayer1.SelectedColor = (Color)ColorConverter.ConvertFromString("#ffFFFFFF");
            thumbnailGenerator.FontSizeSliderPlayer.Value = 110;



            thumbnailGenerator.FontPlayer.Text = "/Fonts/#Trajan Pro 3";
            thumbnailGenerator.ShadowColorPlayer.SelectedColor = (Color)ColorConverter.ConvertFromString("#ff000000");
            thumbnailGenerator.ShadowPlayerSlider.Value = 20;
            thumbnailGenerator.FontColorPlayer2.SelectedColor = (Color)ColorConverter.ConvertFromString("#ffFFFFFF");
            thumbnailGenerator.FontSizeSliderPlayer.Value = 110;


            thumbnailGenerator.FontVS.Text = "/Fonts/#Trajan Pro 3";
            thumbnailGenerator.ShadowColorVS.SelectedColor = (Color)ColorConverter.ConvertFromString("#ff000000");
            thumbnailGenerator.ShadowVSSlider.Value = 20;
            thumbnailGenerator.FontColorVS.SelectedColor = (Color)ColorConverter.ConvertFromString("#ffffd700");
            thumbnailGenerator.FontSizeSliderVS.Value = 120;

            thumbnailGenerator.VsThumbnail.SetValue(Canvas.LeftProperty, 650.0);
            thumbnailGenerator.VsThumbnail.SetValue(Canvas.TopProperty, 180.0);

            thumbnailGenerator.Player2Thumbnail.SetValue(Canvas.TopProperty, 310.0);
            thumbnailGenerator.Player1Thumbnail.SetValue(Canvas.TopProperty, 50.0);

            thumbnailGenerator.Player2Thumbnail.SetValue(Canvas.LeftProperty, 500.0);
            thumbnailGenerator.Player1Thumbnail.SetValue(Canvas.LeftProperty, 500.0);

            thumbnailGenerator.ShowDialog();
        }



        private void Window_ContentRendered(object sender, EventArgs e)
        {
            this.Topmost = false;
        }



        private async void Window_Initialized(object sender, EventArgs e)
        {
            this.Topmost = true;

        }

        private void Window_Activated(object sender, EventArgs e)
        {
            foreach (Window window in App.Current.Windows)
            {
                if (window is OverlayWindow || window is ThumbnailGenerator)
                {
                    if (!window.IsVisible)
                    {
                        window.Close();
                    }
                }
            }
        }


        private void Button_Click_11(object sender, RoutedEventArgs e)
        {
            if (SelectedOverlayIndex == 0)
            {
                SelectedOverlayIndex = Overlays.Count - 1;
            }
            else
            {
                SelectedOverlayIndex--;
            }
        }

        private void btn_act_forward_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedOverlayIndex == Overlays.Count - 1)
            {
                SelectedOverlayIndex = 0;
            }
            else
            {
                SelectedOverlayIndex++;
            }
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {



            if (SelectedOverlay == null)
            {
                (sender as Button).IsEnabled = true;
                return;
            }

                (sender as Button).IsEnabled = false;

            if (MapPool.Count(x => x.Veto == 1) != MapPool.Count(x => x.Veto == 2))
            {
                mbText.Text = "You have selected Veto Map not for all teams.";
                BlurControl.Visibility = Visibility.Visible;
                gMessageBox.Visibility = Visibility.Visible;
                (sender as Button).IsEnabled = true;
                return;
            }

            if (MapPool.Count(x => x.Home == 1) !=  MapPool.Count(x => x.Home == 2))
            {
                mbText.Text = "You have selected Home Map not for all teams.";
                BlurControl.Visibility = Visibility.Visible;
                gMessageBox.Visibility = Visibility.Visible;
                (sender as Button).IsEnabled = true;
                return;
            }



            ResourceDictionary newRes = new();

            newRes.Source = cbTemplates.SelectedIndex switch
            {
                2 => new Uri("/StreamOverlay;component/Templates/AOE4.xaml", UriKind.RelativeOrAbsolute),
                1 => new Uri("/StreamOverlay;component/Templates/KOTOW.xaml", UriKind.RelativeOrAbsolute),
                _ => new Uri("/StreamOverlay;component/Templates/AOE3DE.xaml", UriKind.RelativeOrAbsolute),
            };
            Application.Current.Resources.MergedDictionaries.Remove(Application.Current.Resources.MergedDictionaries.FirstOrDefault(x => x.Source.ToString().Contains("/Templates/")));
            Application.Current.Resources.MergedDictionaries.Add(newRes);

            newRes = new()
            {
                Source = cbLanguages.SelectedIndex switch
                {
                    1 => new Uri("/StreamOverlay;component/Languages/ru-RU.xaml", UriKind.RelativeOrAbsolute),
                    2 => new Uri("/StreamOverlay;component/Languages/fr-FR.xaml", UriKind.RelativeOrAbsolute),
                    3 => new Uri("/StreamOverlay;component/Languages/de-DE.xaml", UriKind.RelativeOrAbsolute),
                    4 => new Uri("/StreamOverlay;component/Languages/es-ES.xaml", UriKind.RelativeOrAbsolute),
                    5 => new Uri("/StreamOverlay;component/Languages/zh-CH.xaml", UriKind.RelativeOrAbsolute),
                    _ => new Uri("/StreamOverlay;component/Languages/en-US.xaml", UriKind.RelativeOrAbsolute),
                }
            };
            Application.Current.Resources.MergedDictionaries.Remove(Application.Current.Resources.MergedDictionaries.FirstOrDefault(x => x.Source.ToString().Contains("/Languages/")));
            Application.Current.Resources.MergedDictionaries.Add(newRes);
            if (cbTwitch.IsChecked == true)
            {
                try
                {
                    var data = new StringContent(
                        "[{ \"operationName\":\"ChannelShell\",\"variables\":{ \"login\":\"" + tbTwitchChannel.Text + "\"},\"extensions\":{ \"persistedQuery\":{ \"version\":1,\"sha256Hash\":\"580ab410bcd0c1ad194224957ae2241e5d252b2c5173d8e0cce9d32d5bb14efe\"} } }, {\"operationName\":\"ChannelAvatar\",\"variables\":{\"channelLogin\":\"" + tbTwitchChannel.Text + "\"},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\": \"84ed918aaa9aaf930e58ac81733f552abeef8ac26c0117746865428a7e5c8ab0\"}}},{\"operationName\": \"UseLive\",\"variables\": {\"channelLogin\": \"" + tbTwitchChannel.Text + "\"},\"extensions\": {\"persistedQuery\": {\"version\": 1,\"sha256Hash\": \"639d5f11bfb8bf3053b424d9ef650d04c4ebb7d94711d644afb08fe9a0fad5d9\"}}}]", Encoding.UTF8, "application/json");

                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("Client-Id", "kimne78kx3ncx6brgo4mv6wki5h1ko");
                    HttpResponseMessage response = await client.PostAsync("https://gql.twitch.tv/gql", data);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var twitch = JsonSerializer.Deserialize<List<Root>>(responseBody);
                    twitch[1].data.user.followers.totalCount.ToString();
                }
                catch
                {

                    mbText.Text = "The entered Twitch channel does not exist.";
                    BlurControl.Visibility = Visibility.Visible;
                    gMessageBox.Visibility = Visibility.Visible;
                    (sender as Button).IsEnabled = true;
                    return;

                }

            }

            OverlayWindow mainWindow = new();




            if (tbAnimation.IsChecked == true)
            {
                mainWindow.PreviewImage.Source = new BitmapImage(new Uri(SelectedOverlay.preview));

                mainWindow._libVLC = new LibVLC(); // "--reset-plugins-cache"
                mainWindow._mediaPlayer = new MediaPlayer(mainWindow._libVLC);

                mainWindow.Unloaded += mainWindow.Player_Unloaded;

                List<Overlay> loopedOverlays = Overlays.Where(x => x.isLooped).ToList();

                List<Overlay> KOTOWSpecificScreen = Overlays.Where(x => x.isLooped).ToList();
                var moss = Overlays.First(x => x.title == "Moss");
                var kotow = Overlays.First(x => x.title == "Kings of The Old World");
                KOTOWSpecificScreen.Remove(moss);
                KOTOWSpecificScreen.Insert(0, moss);
                mainWindow.Current = SelectedOverlay;
                mainWindow.KOTOWSpecialScreen = new List<Overlay>();
                foreach (var overlay in KOTOWSpecificScreen)
                {
                    mainWindow.KOTOWSpecialScreen.Add(overlay);
                    mainWindow.KOTOWSpecialScreen.Add(kotow);
                }

                mainWindow.Animation.Loaded += (sender, e) =>
                {
                    mainWindow.Animation.MediaPlayer = mainWindow._mediaPlayer;
                    if (cbOverlayLoop.IsChecked == false)
                        //mainWindow.Animation.MediaPlayer.Play(new Media(mainWindow._libVLC, new Uri(@"dshow://"), new string[] { ":dshow-aspect-ratio=16:9 :dshow-adev=none :dshow-config :live-caching=0" }));
                        mainWindow.Animation.MediaPlayer.Play(new Media(mainWindow._libVLC, new Uri(Path.Combine(AppContext.BaseDirectory, SelectedOverlay.video)), new string[] { ":input-repeat=65535" }));
                    else
                        mainWindow.Animation.MediaPlayer.Play(new Media(mainWindow._libVLC, new Uri(Path.Combine(AppContext.BaseDirectory, loopedOverlays[0].video))));
                    mainWindow.PreviewImage.Visibility = Visibility.Collapsed;
                };





                if (cbOverlayLoop.IsChecked == true)
                {
                    mainWindow._mediaPlayer.EndReached += (sender, e) =>
                    {
                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            CurrentPlayingOverlayCount++;
                            if (CurrentPlayingOverlayCount >= 5)
                            {
                                if (CurrentPlayingOverlayIndex >= loopedOverlays.Count - 1)
                                {
                                    CurrentPlayingOverlayIndex = 0;
                                }
                                else
                                {
                                    CurrentPlayingOverlayIndex++;
                                }
                                CurrentPlayingOverlayCount = 0;
                            }
                            mainWindow._mediaPlayer.Play(new Media(mainWindow._libVLC, new Uri(Path.Combine(AppContext.BaseDirectory, loopedOverlays[CurrentPlayingOverlayIndex].video))));
                        });
                    };
                }
            }
            else
            {
                mainWindow.PreviewImage.Source = new BitmapImage(new Uri(SelectedOverlay.preview));
            }

            if (BrandLogos.SelectedIndex == 0)
            {
                mainWindow.gBrandLogo.Visibility = Visibility.Collapsed;
            }
            else
            {
                mainWindow.iBrandLogo.Source = new BitmapImage(new Uri((BrandLogos.SelectedItem as Logo).Path));
            }

            if (EventLogos.SelectedIndex == 0)
            {
                mainWindow.gEventLogo.Visibility = Visibility.Collapsed;
            }
            else
            {
                mainWindow.iEventLogo.Source = new BitmapImage(new Uri((EventLogos.SelectedItem as Logo).Path));
            }

            if (cbCountdown.IsChecked == false)
            {
                mainWindow.gCountdown.Visibility = Visibility.Hidden;
            }

            mainWindow.TwitchChannel.Text = tbTwitchChannel.Text;
            var myCur = Application.GetResourceStream(new Uri("pack://application:,,,/resources/Cursor.cur")).Stream;
            mainWindow.Schedule.PreviewKeyDown += mainWindow.TextBox_PreviewKeyDown;
            mainWindow.Schedule.Cursor = new Cursor(myCur);
            myCur = Application.GetResourceStream(new Uri("pack://application:,,,/resources/Cursor.cur")).Stream;
            mainWindow.tbScoreText.Cursor = new Cursor(myCur);
            mainWindow.tbScoreText.PreviewKeyDown += mainWindow.TextBox_PreviewKeyDown;
            if (cbSchedule.IsChecked == true)
            {
                mainWindow.Schedule.Focus();
            }
            else
            {
                mainWindow.gSchedule.Visibility = Visibility.Hidden;
            }

            if (cbScorePanel.IsChecked == false)
            {
                mainWindow.gScorePanel.Visibility = Visibility.Hidden;
            }

            if (cbTwitch.IsChecked == false)
            {
                mainWindow.gTwitchInfo.Visibility = Visibility.Hidden;
            }

            if (cbChromakey.IsChecked == true)
            {
                mainWindow.PreviewImage.Opacity = 0;
                mainWindow.OverlayCanvas.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#ff00b140");
            }

            mainWindow.TwitchInfo.Interval = new TimeSpan(0, 0, 0);
            mainWindow.TwitchInfo.Tick += async (object s, EventArgs eventArgs) =>
            {
                mainWindow.TwitchInfo.Stop();
                mainWindow.TwitchInfo.Interval = new TimeSpan(0, 0, 30);
                try
                {
                    var data = new StringContent(
                        "[{ \"operationName\":\"ChannelShell\",\"variables\":{ \"login\":\"" + tbTwitchChannel.Text + "\"},\"extensions\":{ \"persistedQuery\":{ \"version\":1,\"sha256Hash\":\"580ab410bcd0c1ad194224957ae2241e5d252b2c5173d8e0cce9d32d5bb14efe\"} } }, {\"operationName\":\"ChannelAvatar\",\"variables\":{\"channelLogin\":\"" + tbTwitchChannel.Text + "\"},\"extensions\":{\"persistedQuery\":{\"version\":1,\"sha256Hash\": \"84ed918aaa9aaf930e58ac81733f552abeef8ac26c0117746865428a7e5c8ab0\"}}},{\"operationName\": \"UseLive\",\"variables\": {\"channelLogin\": \"" + tbTwitchChannel.Text + "\"},\"extensions\": {\"persistedQuery\": {\"version\": 1,\"sha256Hash\": \"639d5f11bfb8bf3053b424d9ef650d04c4ebb7d94711d644afb08fe9a0fad5d9\"}}}]", Encoding.UTF8, "application/json");


                    HttpResponseMessage response = await mainWindow.client.PostAsync("https://gql.twitch.tv/gql", data);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    //Debug.WriteLine(responseBody);
                    var twitch = JsonSerializer.Deserialize<List<Root>>(responseBody);
                    mainWindow.TwitchChannel.Text = twitch[0].data.userOrError.displayName;
                    mainWindow.twitchFollowers.Text = twitch[1].data.user.followers.totalCount.ToString();
                    if (twitch[0].data.userOrError.stream != null)
                        mainWindow.twitchViewers.Text = twitch[0].data.userOrError.stream.viewersCount.ToString();
                    if (twitch[2].data.user.stream != null)
                    {
                        var streamLength = DateTime.UtcNow - twitch[2].data.user.stream.createdAt;
                        mainWindow.twitchStreamLength.Text = $"{Math.Truncate(streamLength.TotalHours)} hr {streamLength.Minutes} min";
                    }

                    mainWindow.TwitchIcon = twitch[0].data.userOrError.profileImageURL;
                    mainWindow.NotifyPropertyChanged("TwitchIcon");

                }
                catch { }
                mainWindow.TwitchInfo.Start();
            };
            mainWindow.TwitchInfo.Start();

            var a = MapPool.Where(x => x.Home == 1).ToList();
            a.AddRange(MapPool.Where(x => x.Veto == 1).ToList());

            var b = MapPool.Where(x => x.Home == 2).ToList();
            b.AddRange(MapPool.Where(x => x.Veto == 2).ToList());

            mainWindow.lvHomeMapsTeam1.ItemsSource = new ObservableCollection<Map>(a);
            mainWindow.lvHomeMapsTeam2.ItemsSource = new ObservableCollection<Map>(b);

            List<Map> maps = MapPool.Where(x => x.Order != 0).OrderBy(x => x.Order).ToList();
            mainWindow.Team1Name = tbTeam1.Text;
            mainWindow.Team2Name = tbTeam2.Text;
            //mainWindow.tbScoreText.Text = $"{tbTeam1.Text} 0:0 {tbTeam2.Text}";

            if (maps.Count == 0)
            {
                mainWindow.tbTeam1Score.Visibility = Visibility.Hidden;
                mainWindow.tbTeam2Score.Visibility = Visibility.Hidden;
            }

            foreach (var civ in Team1Player1CivPool)
            {
                civ.Status = 0;
            }
            foreach (var civ in Team1Player2CivPool)
            {
                civ.Status = 0;
            }
            foreach (var civ in Team1Player3CivPool)
            {
                civ.Status = 0;
            }


            foreach (var civ in Team2Player1CivPool)
            {
                civ.Status = 0;
            }
            foreach (var civ in Team2Player2CivPool)
            {
                civ.Status = 0;
            }
            foreach (var civ in Team2Player3CivPool)
            {
                civ.Status = 0;
            }

            mainWindow.Team1Player1CivPool = Team1Player1CivPool;
            mainWindow.Team2Player1CivPool = Team2Player1CivPool;


            mainWindow.Team1Player2CivPool = Team1Player2CivPool;
            mainWindow.Team2Player2CivPool = Team2Player2CivPool;


            mainWindow.Team1Player3CivPool = Team1Player3CivPool;
            mainWindow.Team2Player3CivPool = Team2Player3CivPool;


            mainWindow.lwTeam1Player1CivPool.ItemsSource = mainWindow.Team1Player1CivPool;
            mainWindow.lwTeam2Player1CivPool.ItemsSource = mainWindow.Team2Player1CivPool;


            mainWindow.lwTeam1Player2CivPool.ItemsSource = mainWindow.Team1Player2CivPool;
            mainWindow.lwTeam2Player2CivPool.ItemsSource = mainWindow.Team2Player2CivPool;


            mainWindow.lwTeam1Player3CivPool.ItemsSource = mainWindow.Team1Player3CivPool;
            mainWindow.lwTeam2Player3CivPool.ItemsSource = mainWindow.Team2Player3CivPool;


            mainWindow.iTeam1.Source = iTeam1.Source;
            mainWindow.iTeam2.Source = iTeam2.Source;



            if (cbPlayersPanel.IsChecked == false)
            {
                mainWindow.gPlayers.Visibility = Visibility.Hidden;
                mainWindow.gPlayersPanel.Visibility = Visibility.Hidden;
            }

            DoubleAnimation hideMap = new(1, 0, TimeSpan.FromSeconds(0));
            mainWindow.ibMapIcon.BeginAnimation(Image.OpacityProperty, hideMap);

            if (maps.Count > 0)
            {
                CircleEase ease = new()
                {
                    EasingMode = EasingMode.EaseInOut
                };


                DoubleAnimation center = new(0, 1, TimeSpan.FromSeconds(0.8))
                {
                    EasingFunction = ease
                };

                DoubleAnimation center2 = new(0, 1, TimeSpan.FromSeconds(1))
                {
                    EasingFunction = ease
                };

                DoubleAnimation ZoonInAnimation = new(0, 1, TimeSpan.FromSeconds(0.8))
                {
                    EasingFunction = ease
                };

                DoubleAnimation center11 = new(1, 0, TimeSpan.FromSeconds(1))
                {
                    EasingFunction = ease
                };

                DoubleAnimation center12 = new(1, 0, TimeSpan.FromSeconds(0.8))
                {
                    EasingFunction = ease
                };


                DoubleAnimation ZoomOutAnimation = new(1, 0, TimeSpan.FromSeconds(1))
                {
                    EasingFunction = ease,
                    BeginTime = TimeSpan.FromSeconds(6)
                };


                DoubleAnimation center3 = new(1, 0, TimeSpan.FromSeconds(0.8))
                {
                    BeginTime = TimeSpan.FromSeconds(6.4),
                    EasingFunction = ease
                };

                DoubleAnimation center4 = new(1, 0, TimeSpan.FromSeconds(1))
                {
                    BeginTime = TimeSpan.FromSeconds(6),
                    EasingFunction = ease
                };

                DoubleAnimation center13 = new(0, 1, TimeSpan.FromSeconds(0.8))
                {
                    BeginTime = TimeSpan.FromSeconds(6),
                    EasingFunction = ease
                };

                DoubleAnimation center14 = new(0, 1, TimeSpan.FromSeconds(1))
                {
                    BeginTime = TimeSpan.FromSeconds(6.4),
                    EasingFunction = ease
                };


                mainWindow.tbBO.Text = (this.FindResource("BestOf") as string) + " " + MapPool.Count(x => x.Order != 0 || x.Home != 0).ToString();

                mainWindow.AnimateMapPool.Interval = new TimeSpan(0, 0, 0);

                mainWindow.AnimateMapPool.Tick += async (object s, EventArgs eventArgs) =>
                {
                    mainWindow.AnimateMapPool.Stop();

                    int MapIndex = 1;
                    foreach (Map map in maps)
                    {

                        mainWindow.tbMapPool.Text = "#" + MapIndex.ToString() + " " + map.title;
                        mainWindow.ibMapIcon.Source = new BitmapImage(new Uri(map.icon));

                        mainWindow.stMap.BeginAnimation(ScaleTransform.ScaleXProperty, ZoonInAnimation);
                        mainWindow.stMap.BeginAnimation(ScaleTransform.ScaleYProperty, ZoonInAnimation);


                        mainWindow.MapTransparentStop.BeginAnimation(GradientStop.OffsetProperty, center);
                        mainWindow.MapBlackStop.BeginAnimation(GradientStop.OffsetProperty, center2);

                        mainWindow.BOTransparentStop.BeginAnimation(GradientStop.OffsetProperty, center11);
                        mainWindow.BOBlackStop.BeginAnimation(GradientStop.OffsetProperty, center12);

                        mainWindow.ibMapIcon.BeginAnimation(Image.OpacityProperty, ZoonInAnimation);

                        await Task.Delay(1000);


                        if (maps.Count > 1)
                        {
                            mainWindow.stMap.BeginAnimation(ScaleTransform.ScaleXProperty, ZoomOutAnimation, HandoffBehavior.Compose);
                            mainWindow.stMap.BeginAnimation(ScaleTransform.ScaleYProperty, ZoomOutAnimation, HandoffBehavior.Compose);
                            mainWindow.MapTransparentStop.BeginAnimation(GradientStop.OffsetProperty, center3);
                            mainWindow.MapBlackStop.BeginAnimation(GradientStop.OffsetProperty, center4);
                            mainWindow.BOTransparentStop.BeginAnimation(GradientStop.OffsetProperty, center13);
                            mainWindow.BOBlackStop.BeginAnimation(GradientStop.OffsetProperty, center14);
                        }

                        MapIndex++;
                        await Task.Delay(10000);
                    }
                    if (maps.Count > 1)
                        mainWindow.AnimateMapPool.Start();
                };
                mainWindow.AnimateMapPool.Start();
            }
            else
            {
                mainWindow.gMapPool.Visibility = Visibility.Hidden;
            }

            mainWindow.InputBindings.Add(new InputBinding(mainWindow.TimeUp, new KeyGesture(Key.Up, ModifierKeys.Control)));
            mainWindow.InputBindings.Add(new InputBinding(mainWindow.TimeDown, new KeyGesture(Key.Down, ModifierKeys.Control)));
            mainWindow.InputBindings.Add(new InputBinding(mainWindow.CloseOverlay, new KeyGesture(Key.Escape)));
            mainWindow.InputBindings.Add(new InputBinding(mainWindow.MinimizeOverlay, new KeyGesture(Key.Space, ModifierKeys.Control)));

            mainWindow.InputBindings.Add(new InputBinding(mainWindow.CountdownVisibility, new KeyGesture(Key.D1, ModifierKeys.Control)));
            mainWindow.InputBindings.Add(new InputBinding(mainWindow.ScheduleVisibility, new KeyGesture(Key.D2, ModifierKeys.Control)));
            mainWindow.InputBindings.Add(new InputBinding(mainWindow.MapPoolVisibility, new KeyGesture(Key.D3, ModifierKeys.Control)));
            mainWindow.InputBindings.Add(new InputBinding(mainWindow.BrandLogoVisibility, new KeyGesture(Key.D4, ModifierKeys.Control)));
            mainWindow.InputBindings.Add(new InputBinding(mainWindow.EventLogoVisibility, new KeyGesture(Key.D5, ModifierKeys.Control)));
            mainWindow.InputBindings.Add(new InputBinding(mainWindow.PlayerPanelVisibility, new KeyGesture(Key.D6, ModifierKeys.Control)));
            mainWindow.InputBindings.Add(new InputBinding(mainWindow.TwitchPanelVisibility, new KeyGesture(Key.D7, ModifierKeys.Control)));
            mainWindow.InputBindings.Add(new InputBinding(mainWindow.ScorePanelVisibility, new KeyGesture(Key.D8, ModifierKeys.Control)));

            mainWindow.InputBindings.Add(new InputBinding(mainWindow.ChromakeyVisibility, new KeyGesture(Key.D0, ModifierKeys.Control)));
            //mainWindow.InputBindings.Add(new InputBinding(mainWindow.KotowSpecial, new KeyGesture(Key.K, ModifierKeys.Alt)));


            PresentationSource presentationsource = PresentationSource.FromVisual(this);
            Matrix m = presentationsource.CompositionTarget.TransformToDevice;

            double DpiWidthFactor = m.M11;
            double DpiHeightFactor = m.M22;

            mainWindow.stTwitchInfo.ScaleX = ScaleUI.Value / 100;
            mainWindow.stTwitchInfo.ScaleY = ScaleUI.Value / 100;

            mainWindow.stScorePanel.ScaleX = ScaleUI.Value / 100;
            mainWindow.stScorePanel.ScaleY = ScaleUI.Value / 100;


            mainWindow.VideoBox.Width = SystemParameters.PrimaryScreenWidth;// / DpiWidthFactor;
            mainWindow.VideoBox.Height = SystemParameters.PrimaryScreenHeight;// / DpiHeightFactor;


            mainWindow.Animation.Width = SystemParameters.PrimaryScreenWidth;// / DpiWidthFactor;
            mainWindow.Animation.Height = SystemParameters.PrimaryScreenHeight;// / DpiHeightFactor;

            mainWindow.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            mainWindow.Arrange(new Rect(0, 0, mainWindow.DesiredSize.Width, mainWindow.DesiredSize.Height));

            mainWindow.gCountdown.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            mainWindow.gCountdown.Arrange(new Rect(0, 0, mainWindow.gCountdown.DesiredSize.Width, mainWindow.gCountdown.DesiredSize.Height));

            mainWindow.gSchedule.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            mainWindow.gSchedule.Arrange(new Rect(0, 0, mainWindow.gSchedule.DesiredSize.Width, mainWindow.gSchedule.DesiredSize.Height));

            mainWindow.gMapPool.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            mainWindow.gMapPool.Arrange(new Rect(0, 0, mainWindow.gMapPool.DesiredSize.Width, mainWindow.gMapPool.DesiredSize.Height));


            mainWindow.gBrandLogo.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            mainWindow.gBrandLogo.Arrange(new Rect(0, 0, mainWindow.gBrandLogo.DesiredSize.Width, mainWindow.gBrandLogo.DesiredSize.Height));

            mainWindow.gEventLogo.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            mainWindow.gEventLogo.Arrange(new Rect(0, 0, mainWindow.gEventLogo.DesiredSize.Width, mainWindow.gEventLogo.DesiredSize.Height));

            mainWindow.gTwitchInfo.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            mainWindow.gTwitchInfo.Arrange(new Rect(0, 0, mainWindow.gTwitchInfo.DesiredSize.Width, mainWindow.gTwitchInfo.DesiredSize.Height));

            mainWindow.gScorePanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            mainWindow.gScorePanel.Arrange(new Rect(0, 0, mainWindow.gScorePanel.DesiredSize.Width, mainWindow.gScorePanel.DesiredSize.Height));

            mainWindow.gPlayersPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            mainWindow.gPlayersPanel.Arrange(new Rect(0, 0, mainWindow.gPlayersPanel.DesiredSize.Width, mainWindow.gPlayersPanel.DesiredSize.Height));

            mainWindow.gPlayers.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            mainWindow.gPlayers.Arrange(new Rect(0, 0, mainWindow.gPlayers.DesiredSize.Width, mainWindow.gPlayers.DesiredSize.Height));

            mainWindow.gPlaybackControl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            mainWindow.gPlaybackControl.Arrange(new Rect(0, 0, mainWindow.gPlaybackControl.DesiredSize.Width, mainWindow.gPlaybackControl.DesiredSize.Height));




            Setting.Countdown.Position.GetPositionFromType(mainWindow.gCountdown.ActualWidth, mainWindow.gCountdown.ActualHeight);
            mainWindow.gCountdown.SetValue(Canvas.LeftProperty, Setting.Countdown.Position.X);
            mainWindow.gCountdown.SetValue(Canvas.TopProperty, Setting.Countdown.Position.Y);

            Setting.Schedule.Position.GetPositionFromType(mainWindow.gSchedule.ActualWidth, mainWindow.gSchedule.ActualHeight);
            mainWindow.gSchedule.SetValue(Canvas.LeftProperty, Setting.Schedule.Position.X);
            mainWindow.gSchedule.SetValue(Canvas.TopProperty, Setting.Schedule.Position.Y);

            Setting.Map.Position.GetPositionFromType(mainWindow.gMapPool.ActualWidth, mainWindow.gMapPool.ActualHeight);
            mainWindow.gMapPool.SetValue(Canvas.LeftProperty, Setting.Map.Position.X);
            mainWindow.gMapPool.SetValue(Canvas.TopProperty, Setting.Map.Position.Y);

            Setting.BrandLogo.Position.GetPositionFromType(mainWindow.gBrandLogo.ActualWidth, mainWindow.gBrandLogo.ActualHeight);
            mainWindow.gBrandLogo.SetValue(Canvas.LeftProperty, Setting.BrandLogo.Position.X);
            mainWindow.gBrandLogo.SetValue(Canvas.TopProperty, Setting.BrandLogo.Position.Y);
            mainWindow.sBrandLogo.Value = Setting.BrandLogo.Zoom;

            Setting.EventLogo.Position.GetPositionFromType(mainWindow.gEventLogo.ActualWidth, mainWindow.gEventLogo.ActualHeight);
            mainWindow.gEventLogo.SetValue(Canvas.LeftProperty, Setting.EventLogo.Position.X);
            mainWindow.gEventLogo.SetValue(Canvas.TopProperty, Setting.EventLogo.Position.Y);
            mainWindow.sEventLogo.Value = Setting.EventLogo.Zoom;

            Setting.TwitchPanel.Position.GetPositionFromType(mainWindow.gTwitchInfo.ActualWidth, mainWindow.gTwitchInfo.ActualHeight);
            mainWindow.gTwitchInfo.SetValue(Canvas.LeftProperty, Setting.TwitchPanel.Position.X);
            mainWindow.gTwitchInfo.SetValue(Canvas.TopProperty, Setting.TwitchPanel.Position.Y);

            mainWindow.gScorePanel.SetValue(Canvas.LeftProperty, 1920 - 600 * ScaleUI.Value / 100 - 5);
            mainWindow.gScorePanel.SetValue(Canvas.TopProperty, 60 * ScaleUI.Value / 100 + 5);

            mainWindow.Cursor = AoE;
            Mouse.OverrideCursor = AoE;
            mainWindow.Show();
            this.Hide();



            (sender as Button).IsEnabled = true;

        }



        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            BlurControl.Visibility = Visibility.Collapsed;
            gMessageBox.Visibility = Visibility.Collapsed;
        }



        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            gReleaseNotes.Visibility = Visibility.Collapsed;
            BlurControl.Visibility = Visibility.Collapsed;
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            gReleaseNotes.Visibility = Visibility.Visible;
            BlurControl.Visibility = Visibility.Visible;
        }

        private void bTimerAlign_Click(object sender, RoutedEventArgs e)
        {
            switch (Setting.Countdown.Position.Type)
            {
                case PositionType.TopRight:
                    Setting.Countdown.Position.Type = PositionType.BottomRight;
                    break;
                case PositionType.BottomRight:
                    Setting.Countdown.Position.Type = PositionType.BottomLeft;
                    break;
                case PositionType.BottomLeft:
                    Setting.Countdown.Position.Type = PositionType.TopLeft;
                    break;
                case PositionType.TopLeft:
                case PositionType.Custom:
                    Setting.Countdown.Position.Type = PositionType.TopRight;
                    break;
            }
        }

        private void bScheduleAlign_Click(object sender, RoutedEventArgs e)
        {
            switch (Setting.Schedule.Position.Type)
            {
                case PositionType.TopRight:
                    Setting.Schedule.Position.Type = PositionType.BottomRight;
                    break;
                case PositionType.BottomRight:
                    Setting.Schedule.Position.Type = PositionType.BottomLeft;
                    break;
                case PositionType.BottomLeft:
                    Setting.Schedule.Position.Type = PositionType.TopLeft;
                    break;
                case PositionType.TopLeft:
                case PositionType.Custom:
                    Setting.Schedule.Position.Type = PositionType.TopRight;
                    break;
            }
        }

        private void bBrandAlign_Click(object sender, RoutedEventArgs e)
        {
            switch (Setting.BrandLogo.Position.Type)
            {
                case PositionType.TopRight:
                    Setting.BrandLogo.Position.Type = PositionType.BottomRight;
                    break;
                case PositionType.BottomRight:
                    Setting.BrandLogo.Position.Type = PositionType.BottomLeft;
                    break;
                case PositionType.BottomLeft:
                    Setting.BrandLogo.Position.Type = PositionType.TopLeft;
                    break;
                case PositionType.TopLeft:
                case PositionType.Custom:
                    Setting.BrandLogo.Position.Type = PositionType.TopRight;
                    break;
            }
        }

        private void bEventAlign_Click(object sender, RoutedEventArgs e)
        {
            switch (Setting.EventLogo.Position.Type)
            {
                case PositionType.TopRight:
                    Setting.EventLogo.Position.Type = PositionType.BottomRight;
                    break;
                case PositionType.BottomRight:
                    Setting.EventLogo.Position.Type = PositionType.BottomLeft;
                    break;
                case PositionType.BottomLeft:
                    Setting.EventLogo.Position.Type = PositionType.TopLeft;
                    break;
                case PositionType.TopLeft:
                case PositionType.Custom:
                    Setting.EventLogo.Position.Type = PositionType.TopRight;
                    break;
            }
        }

        private void bMapAlign_Click(object sender, RoutedEventArgs e)
        {
            switch (Setting.Map.Position.Type)
            {
                case PositionType.TopRight:
                    Setting.Map.Position.Type = PositionType.BottomRight;
                    break;
                case PositionType.BottomRight:
                    Setting.Map.Position.Type = PositionType.BottomLeft;
                    break;
                case PositionType.BottomLeft:
                    Setting.Map.Position.Type = PositionType.TopLeft;
                    break;
                case PositionType.TopLeft:
                case PositionType.Custom:
                    Setting.Map.Position.Type = PositionType.TopRight;
                    break;
            }
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private async void Window_Closed(object sender, EventArgs e)
        {
            Setting.SelectedGame = cbGames.SelectedIndex;
            var maps = MapPool.Where(x => x.Order != 0 || x.Veto != 0 || x.Home != 0).ToList();
            Setting.SelectedMaps = maps;
            Setting.SelectedOverlay = Overlays[SelectedOverlayIndex].title;
            Setting.BrandLogo.Source = (BrandLogos.SelectedItem as Logo).Name;
            Setting.EventLogo.Source = (EventLogos.SelectedItem as Logo).Name;
            Setting.PlayersPanel.Source = TeamPanel.SelectedIndex;
            Setting.CivVeto = cbCivVetoCount.SelectedIndex;
            Setting.ElementsStyle = cbTemplates.SelectedIndex;
            Setting.Team1Persons = (cbTeam1Persons.SelectedItem as Logo).Name;
            Setting.Team2Persons = (cbTeam2Persons.SelectedItem as Logo).Name;

            Setting.Team1Name = tbTeam1.Text;
            Setting.Team2Name = tbTeam2.Text;

            Setting.AppVersion = Version;
            Setting.AppLanguage = cbLanguages.SelectedIndex;


            await File.WriteAllTextAsync(Path.Combine(AppContext.BaseDirectory, "setting.json"), JsonSerializer.Serialize(Setting, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            string targetURL = "https://forms.gle/kDu5MUfSjsPk2bkz9";
            var psi = new ProcessStartInfo
            {
                FileName = targetURL,
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private void bTwitchAlign_Click(object sender, RoutedEventArgs e)
        {
            switch (Setting.TwitchPanel.Position.Type)
            {
                case PositionType.TopRight:
                    Setting.TwitchPanel.Position.Type = PositionType.BottomRight;
                    break;
                case PositionType.BottomRight:
                    Setting.TwitchPanel.Position.Type = PositionType.BottomLeft;
                    break;
                case PositionType.BottomLeft:
                    Setting.TwitchPanel.Position.Type = PositionType.TopLeft;
                    break;
                case PositionType.TopLeft:
                case PositionType.Custom:
                    Setting.TwitchPanel.Position.Type = PositionType.TopRight;
                    break;
            }
        }

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            foreach (var map in MapPool)
            {
                map.Order = 0;
                map.Home = 0;
                map.Veto = 0;
            }
            alignBO();
        }

        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            FilterChecked = !FilterChecked;
        }

        private void alignBO()
        {
            if (TeamPanel.SelectedIndex == 0)
            {
                Team1Player2CivPool.Clear();
                Team2Player2CivPool.Clear();

                Team1Player3CivPool.Clear();
                Team2Player3CivPool.Clear();
            }
            if (TeamPanel.SelectedIndex == 1)
            {
                Team1Player3CivPool.Clear();
                Team2Player3CivPool.Clear();
            }
            var count = MapPool.Count(x => x.Order != 0) + MapPool.Count(x => x.Home != 0) + cbCivVetoCount.SelectedIndex;
            var c = Team1Player1CivPool.Count != count && (Team1Player2CivPool.Count != count && TeamPanel.SelectedIndex > 0) && (Team1Player3CivPool.Count != count && TeamPanel.SelectedIndex > 1);
            while (Team1Player1CivPool.Count != count)
            {
                if (Team1Player1CivPool.Count > count)
                {
                    Team1Player1CivPool.RemoveAt(Team1Player1CivPool.Count - 1);
                    Team2Player1CivPool.RemoveAt(Team2Player1CivPool.Count - 1);
                }
                if (Team1Player1CivPool.Count < count)
                {
                    Team1Player1CivPool.Add(new Civ() { Tag = Team1Player1CivPool.Count, Icon = CivPool.First(c => c.Icon.Contains("random")).Icon });
                    Team2Player1CivPool.Add(new Civ() { Tag = Team2Player1CivPool.Count, Icon = CivPool.First(c => c.Icon.Contains("random")).Icon });
                }
            }
            if (TeamPanel.SelectedIndex > 0)
            {
                while (Team1Player2CivPool.Count != count)
                {
                    if (Team1Player2CivPool.Count > count)
                    {
                        Team1Player2CivPool.RemoveAt(Team1Player2CivPool.Count - 1);
                        Team2Player2CivPool.RemoveAt(Team2Player2CivPool.Count - 1);
                    }
                    if (Team1Player2CivPool.Count < count)
                    {
                        Team1Player2CivPool.Add(new Civ() { Tag = Team1Player2CivPool.Count, Icon = CivPool.First(c => c.Icon.Contains("random")).Icon });
                        Team2Player2CivPool.Add(new Civ() { Tag = Team2Player2CivPool.Count, Icon = CivPool.First(c => c.Icon.Contains("random")).Icon });
                    }
                }
            }
            if (TeamPanel.SelectedIndex > 1)
            {
                while (Team1Player3CivPool.Count != count)
                {
                    if (Team1Player3CivPool.Count > count)
                    {
                        Team1Player3CivPool.RemoveAt(Team1Player3CivPool.Count - 1);
                        Team2Player3CivPool.RemoveAt(Team2Player3CivPool.Count - 1);
                    }
                    if (Team1Player3CivPool.Count < count)
                    {
                        Team1Player3CivPool.Add(new Civ() { Tag = Team1Player3CivPool.Count, Icon = CivPool.First(c => c.Icon.Contains("random")).Icon });
                        Team2Player3CivPool.Add(new Civ() { Tag = Team2Player3CivPool.Count, Icon = CivPool.First(c => c.Icon.Contains("random")).Icon });
                    }
                }
            }




            NotifyPropertyChanged("Team1Player1CivPool");
            NotifyPropertyChanged("Team1Player2CivPool");
            NotifyPropertyChanged("Team1Player3CivPool");

            NotifyPropertyChanged("Team2Player1CivPool");
            NotifyPropertyChanged("Team2Player2CivPool");
            NotifyPropertyChanged("Team2Player3CivPool");
        }

        private void cb_Checked(object sender, RoutedEventArgs e)
        {
            var map = MapPool.FirstOrDefault(x => x.title == (sender as ToggleButton).Tag.ToString());
            map.Order = MapPool.Count(x => x.Order != 0) + 1;
            map.Veto = 0;
            map.Home = 0;
            alignBO();

        }

        private void cb_Unchecked(object sender, RoutedEventArgs e)
        {
            var map = MapPool.FirstOrDefault(x => x.title == (sender as ToggleButton).Tag.ToString());
            if (map.Order != 0)
            {


                foreach (var m in MapPool)
                {
                    if (m.Order > map.Order)
                    {
                        m.Order--;
                    }
                }
                map.Order = 0;
                alignBO();
            }

        }

        private void TeamPanel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            alignBO();
            if (TeamPanel.SelectedIndex == 0)
            {
                cbTeam1Persons.ItemsSource = persons1v1;
                cbTeam2Persons.ItemsSource = persons1v1;
                cbTeam1Persons.SelectedIndex = 0;
                cbTeam2Persons.SelectedIndex = 0;
            }
            if (TeamPanel.SelectedIndex == 1)
            {
                cbTeam1Persons.ItemsSource = persons2v2;
                cbTeam2Persons.ItemsSource = persons2v2;
                cbTeam1Persons.SelectedIndex = 0;
                cbTeam2Persons.SelectedIndex = 0;
            }
            if (TeamPanel.SelectedIndex == 2)
            {
                cbTeam1Persons.ItemsSource = persons3v3;
                cbTeam2Persons.ItemsSource = persons3v3;
                cbTeam1Persons.SelectedIndex = 0;
                cbTeam2Persons.SelectedIndex = 0;
            }
        }

        private void g_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var civ = (sender as Image).Tag as Civ;
            civ.Icon = (sender as Image).ToolTip.ToString();

        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
            }
        }

        private void cbTeam1Persons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbTeam1Persons.SelectedIndex > 0)
                iTeam1.Source = new BitmapImage(new Uri((cbTeam1Persons.SelectedItem as Logo).Path));
            else
                iTeam1.Source = new BitmapImage();
        }

        private void cbTeam2Persons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbTeam2Persons.SelectedIndex > 0)
                iTeam2.Source = new BitmapImage(new Uri((cbTeam2Persons.SelectedItem as Logo).Path));
            else
                iTeam2.Source = new BitmapImage();
        }

        private void Run_MouseDown(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = e.Uri.ToString(),
                UseShellExecute = true
            };
            Process.Start(psi);

        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "https://www.youtube.com/watch?v=MtK_UWyOC6M",
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private void cbGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MapPool = new(BuildMaps(cbGames.SelectedIndex));
            NotifyPropertyChanged("MapPool");
            lvMapPool.ItemsSource = MapPool;
            Team1Player1CivPool = new();
            Team1Player2CivPool = new();
            Team1Player3CivPool = new();

            Team2Player1CivPool = new();
            Team2Player2CivPool = new();
            Team2Player3CivPool = new();

            var civs = cbGames.SelectedIndex switch
            {
                0 => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "civs", "Age of Empires I")).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Civ { Icon = x }).ToList(),
                1 => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "civs", "Age of Empires II")).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Civ { Icon = x }).ToList(),
                2 => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "civs", "Age of Empires III")).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Civ { Icon = x }).ToList(),
                3 => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "civs", "Age of Empires IV")).Where(x => Path.GetExtension(x).ToLower() == ".png").Select(x => new Civ { Icon = x }).ToList()

            };

            if (cbGames.SelectedIndex == 3)
            {
                minimapSize = 150;
            }
            else
            {
                minimapSize = 185;
            }

            CivPool = new ObservableCollection<Civ>(civs);
            CivPool.Move(civs.FindIndex(c => c.Icon.Contains("random")), CivPool.Count - 1);
            NotifyPropertyChanged("CivPool");

            alignBO();
            lwTeam1Player1CivPool.ItemsSource = Team1Player1CivPool;
            lwTeam1Player2CivPool.ItemsSource = Team1Player2CivPool;
            lwTeam1Player3CivPool.ItemsSource = Team1Player3CivPool;

            lwTeam2Player1CivPool.ItemsSource = Team2Player1CivPool;
            lwTeam2Player2CivPool.ItemsSource = Team2Player2CivPool;
            lwTeam2Player3CivPool.ItemsSource = Team2Player3CivPool;
        }

        private int helpPhase = 1;


        private void PlaceHelpMark(short position, int X, int Y)
        {

            Point helpMarkPosition = gHelpButton.TransformToAncestor(mainGridPanel)
                              .Transform(new Point(0, 0));

            Point PositionOffset = new();

            switch (position)
            {
                case 0: // Right
                    {
                        PositionOffset.X = X - helpMarkPosition.X;
                        PositionOffset.Y = Y - helpMarkPosition.Y - 40;
                        break;
                    }
                case 1: // Left
                    {
                        PositionOffset.X = X - helpMarkPosition.X - 40;
                        PositionOffset.Y = Y - helpMarkPosition.Y - 40;
                        break;
                    }
                case 2: // Bottom
                    {
                        PositionOffset.X = X - helpMarkPosition.X - 40;
                        PositionOffset.Y = Y - helpMarkPosition.Y - 40;
                        break;
                    }
                case 3: // Top
                    {
                        PositionOffset.X = X - helpMarkPosition.X - 40;
                        PositionOffset.Y = Y - helpMarkPosition.Y - 40;
                        break;
                    }
                case 4: // Default
                    {
                        PositionOffset.X = 0;
                        PositionOffset.Y = 0;
                        break;
                    }
            }
            gHelpButton.Visibility = Visibility.Visible;
            t1.X = PositionOffset.X;
            t1.Y = PositionOffset.Y;
        }

        private async void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            gHelp.Visibility = Visibility.Visible;
            gHelpButton.Visibility = Visibility.Collapsed;
            tbHelp.IsEnabled = false;
            tbHelpText.Text = "";
            if (helpPhase == 0)
            {

                bConfirmHelp.Visibility = Visibility.Collapsed;
                bCancelHelp.Visibility = Visibility.Collapsed;
            }
            else
            {

                bConfirmHelp.Visibility = Visibility.Visible;
                bCancelHelp.Visibility = Visibility.Visible;
            }

            Storyboard story = new Storyboard();
            story.FillBehavior = FillBehavior.HoldEnd;



            string tmp = string.Empty;
            string helpText = "";
            switch (helpPhase)
            {
                case 0:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Rage.png"));
                        helpText = "Who said that?\nWho the fuck said that?\nWho just signed his own death warrant?\nNobody, huh?! The fairy fucking godmother said it! Out-fucking-standing! I will P.T. you all until you fuckingdie! I'll P.T. you until your assholes are sucking buttermilk.";
                        break;
                    }
                case 1:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Normal.png"));
                        tiGeneralSettings.IsSelected = true;
                        helpText = "Welcome comrades, I am Commando Merr, your Senior Drill Instructor.\nFrom now on, you will speak only when spoken to, and the first and last words out of your filthy sewers will be \"Sir!\".\nDo you maggots understand that?";
                        break;
                    }
                case 2:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Normal.png"));
                        helpText = "If you ladies leave my island, if you survive recruit training ... you will be a weapon, you will be a minister of Twitch, praying for stream.\nBut until that day you are pukes! You're the lowest form of life on ESOC. You are nothing but unorganized grabasstic pieces of crossbow shit!\nBecause I am hard, you will not like me. But the more you hate me, the more you will learn. I am hard, but I am fair!\nDo you maggots understand that?";
                        break;
                    }
                case 3:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Normal.png"));
                        helpText = "Well, there's one more thing you suckers haven't learned.\nEach preparation item will be displayed with an exclamation mark. Do you maggots understand that?";
                        break;
                    }
                case 4:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Angry.png"));
                        helpText = "The deadliest weapon in stream is an overlay and his animated background. It is your streamer instinct which must be harnessed if you expect to survive in Twitch. Your overlay is only a tool. It is a hard heart that kills. If your streamer instincts are not clean and strong you will hesitate at the moment of truth. You will not stream. You will become dead streamers. And then you will be in a world of shit. Because streamers are not allowed to stream without overlay! So choose a suitable animated background for your stream here.\nDo you maggots understand?";
                        PlaceHelpMark(0, 1289, 131);
                        break;
                    }
                case 5:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Angry.png"));
                        helpText = "After you, like little kids, have admired the animated backgrounds, you need to set up the remaining elements. The first tab concerns the general settings.\nDo you maggots understand?";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(3, 573, 169);
                        break;
                    }
                case 6:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Normal.png"));
                        helpText = "At the beginning, select the game you are going to stream. Map pool and civs will depend on this. It's not that hard, is it?";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(0, 1501, 263);
                        break;
                    }

                case 7:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Normal.png"));
                        helpText = "Next, if you scumbag liked not one animated background, but all at once, fuck it, then enable the option to play all the backgrounds in a loop. Each overlay is played 5 times. Not all overlays will be displayed in this mode, for example, those created for special events or tournaments.\nDo you understand?";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(0, 910, 303);
                        break;
                    }
                case 8:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Funny.png"));
                        helpText = "The next option is as simple as you are dumb. Enable it if you want to automatically start playing soundtracks from the games.\nLet's move on to the next option faster, before I become the same as you blockheads.";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(0, 920, 346);
                        break;
                    }

                case 9:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Suspicious.png"));
                        helpText = "Everything is learned in comparison, because this option will be used only by those who have more than one gyrus in the brain. It replaces animated background with a green chromakey. Required for live-casting mode to display some overlay elements on top of the game UI. Switches by hotkey CTRL+0. I allow you to read more about setting up the chromakey in the wiki or watch the video guide.\nDo you understand?";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(0, 980, 383);
                        break;
                    }

                case 10:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Angry.png"));
                        helpText = "You damned capitalist, finally buy yourself a good PC if you have this option in disabled state. It enables or disables the animated background. In the disabled state, a static picture is shown, which significantly reduces the load on the PC.\nBurn your wooden calculator and move on.";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(0, 870, 427);
                        break;
                    }
                case 11:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Angry.png"));
                        helpText = "Tonight ... you pukes will sleep with your Twitch Channel! You will give your channel a girl's name! Because this is the only pussy you people are going to get! Your days of finger-banging old Mary Jane Rottencrotch through her pretty pink panties are over! You're married to this piece, this weapon of stream and communication! And you will be faithful!\nThis option enables a twitch panel with a profile picture, the number of subscribers, current viewers and the duration of the broadcast. Click on Twitch Channel logo to show/hide Caster Name input box. Switches by hotkey CTRL+7. It is usually used in combination with chromakey.\nEnter the name of the channel, and go ahead bitch tribe!";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(0, 1502, 467);
                        break;
                    }
                case 12:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Normal.png"));
                        helpText = "It's time to move on to serious things. This element displays a countdown during which you, mama's sons, will eat borscht and masturbate to the photos of your classmates. Switches by hotkey CTRL+1. To add or decrease the time, use hotkeys CTRL+Up and CTRL+Down. When the time expires, it displays a dot animation. Do you understand?";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(0, 850, 510);
                        break;
                    }
                case 13:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Surprised.png"));
                        helpText = "A small digression, the positions of the elements in the overlay can be adjusted in advance, using the positional buttons. The position of the elements is also saved if you moved them in the overlay window. Elements in overlay window can be snapped to center or edges with adjustments lines when you drag it. Do you understand?";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(0, 1054, 529);
                        break;
                    }
                case 14:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Normal.png"));
                        helpText = "To let normal people know what and when you fools are going to stream, use the schedule element. Switches by hotkey CTRL+2. To edit the information, click on the area of the element in the running overlay and enter the desired text.\nDo you understand?";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(0, 726, 555);
                        break;
                    }
                case 15:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Angry.png"));
                        helpText = "If you idiots have your own logo, well, you can display it in the overlay. Switches by hotkey CTRL+4 for brand logo and CTRL+5 for event logo. The size of the logo can be edited in the running overlay.\nDo you understand?";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(0, 1500, 623);
                        break;
                    }
                case 16:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Normal.png"));
                        helpText = "To adapt to a specific game, animated background or event, you can choose the style of overlay elements. Nothing complicated, let's move on.";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(0, 1503, 691);
                        break;
                    }
                case 17:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Suspicious.png"));
                        helpText = "The next option is quite specific, not everyone will find a place to use it. It is designed to match the size of some overlay elements like Twitch Panel or Small Score panel to the size of the game UI. This option only for Age of Empires III! Do you understand kids?";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(0, 1502, 733);
                        break;
                    }
                case 18:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Funny.png"));
                        helpText = "Finally the last option in the general settings. Selecting the language of the overlay elements. It doesn't matter if you are showing an overlay for Uncle Sam or for Uncle Vova, select the desired language and go to the next settings tab.";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(0, 1502, 782);
                        break;
                    }
                case 19:
                    {
                        tiMapPool.IsSelected = true;
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Normal.png"));
                        helpText = "So, soldier, if you survived to this section of settings, then everything is not lost yet. Setting up the map pool allows you not only to show maps, but also to display player's сiv picks. If nothing is selected, then this element will not be displayed in the overlay. Element switches by hotkey CTRL+4.";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(3, 820, 174);
                        break;
                    }
                case 20:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Angry.png"));
                        helpText = "If you have a problem with your eyesight or you have forgotten letters from the amount of information you have received, then there is a filter for searching for maps...";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(0, 1489, 288);
                        break;
                    }
                case 21:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Angry.png"));
                        helpText = "...as well as buttons for resetting and displaying only selected maps...";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(2, 514, 368);
                        break;
                    }
                case 22:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Angry.png"));
                        helpText = "...here you can select (from top to bottom): map display order, player's home map pick, map veto.\nAll the maps will be displayed in the selected order, and this is important. The number of selected maps (home and displayed) will also depend on the settings for the selection of civs for teams (number of cards = number of civs).\nDo you understand?";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(0, 524, 473);
                        break;
                    }
                case 23:
                    {
                        tiPlayerSettings.IsSelected = true;
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Normal.png"));
                        helpText = "The last tab of the settings concerns the display of information about players: the names of teams/players, the score panel, pictures.";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(3, 1037, 170);
                        break;
                    }
                case 24:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Normal.png"));
                        helpText = "You can enter names here...";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(1, 1246, 278);
                        break;
                    }
                case 25:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Normal.png"));
                        helpText = "...here you can choose a picture of the player. It can be either a real photo or a game character from the campaign. God damn, please don't select dick pics here...";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(1, 1252, 372);
                        break;
                    }
                case 26:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Normal.png"));
                        helpText = "...this element is intended to display the current score. It cannot be moved in the overlay window, it is overlaid on top of the game UI. It is usually used in combination with chromakey. The content is automatically synchronized with the name of the teams/players and the score. Element switches by hotkey CTRL+8. Attention, the dimensions of this element must match the dimensions of the game UI. This option only for Age of Empires III!";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(0, 843, 440);
                        break;
                    }
                case 27:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Normal.png"));
                        helpText = "Displays the player panel element. Can be configured for 1 vs 1, 2 vs 2 and 3 vs 3. It displays pictures of teams, selected civs, team names and score. The element cannot be moved. Element switches by hotkey CTRL+6. To change player civ in overlay - click Mouse Left Button, to change Win/Loss/Veto status - click Right Mouse Button.";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(1, 1266, 482);
                        break;
                    }
                case 28:
                    {
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Normal.png"));
                        helpText = "Select the number of civs that will be banned by each team. To display veto icon use mouse right click on civ icon in overlay window.";
                        PlaceHelpMark(4, 0, 0);
                        PlaceHelpMark(1, 1261, 533);
                        break;
                    }
                default:
                    {
                        helpPhase = 0;
                        iHelper.Source = new BitmapImage(new Uri("pack://application:,,,/resources/stickers/Funny.png"));
                        bConfirmHelp.Visibility = Visibility.Collapsed;
                        bCancelHelp.Visibility = Visibility.Collapsed;
                        helpText = "Good job ladies, now you know enough to start using the most powerful tool for setting up overlays! See you next time.";
                        gHelpButton.Visibility = Visibility.Collapsed;
                        break;
                    }
            }

            DiscreteStringKeyFrame discreteStringKeyFrame;
            StringAnimationUsingKeyFrames stringAnimationUsingKeyFrames = new StringAnimationUsingKeyFrames();
            stringAnimationUsingKeyFrames.Duration = new Duration(TimeSpan.FromMilliseconds(helpText.Length * 30));
            stringAnimationUsingKeyFrames.BeginTime = TimeSpan.FromMilliseconds(0);

            foreach (char c in helpText)
            {
                discreteStringKeyFrame = new DiscreteStringKeyFrame();
                discreteStringKeyFrame.KeyTime = KeyTime.Uniform;
                tmp += c;
                discreteStringKeyFrame.Value = tmp;
                stringAnimationUsingKeyFrames.KeyFrames.Add(discreteStringKeyFrame);
            }



            Storyboard.SetTargetName(stringAnimationUsingKeyFrames, tbHelpText.Name);
            Storyboard.SetTargetProperty(stringAnimationUsingKeyFrames, new PropertyPath(TextBlock.TextProperty));
            story.Children.Add(stringAnimationUsingKeyFrames);

            story.Begin(tbHelpText);


            if (helpPhase == 0)
            {
                await Task.Delay(helpText.Length * 30 + 1000);
                PlaceHelpMark(4, 0, 0);
                gHelp.Visibility = Visibility.Collapsed;
                gHelpButton.Visibility = Visibility.Visible;
                helpPhase = 1;
                tbHelp.IsEnabled = true;
            }
            else
                helpPhase++;
        }

        private void Button_Click_10(object sender, RoutedEventArgs e)
        {

            helpPhase = 0;
            tbHelp.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));


        }

        private void bConfirmHelp_Click(object sender, RoutedEventArgs e)
        {
            tbHelp.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private void mainGridPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //   var point = Mouse.GetPosition(mainGridPanel);
            //   MessageBox.Show($"{point.X}, {point.Y}");
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            var map = MapPool.FirstOrDefault(x => x.title == (sender as ToggleButton).Tag.ToString());


            if (map.Home == 2)
            {
                map.Home = 0;
            }
            else
            {
                if (map.Order != 0)
                {
                    foreach (var m in MapPool)
                    {
                        if (m.Order > map.Order)
                        {
                            m.Order--;
                        }
                    }
                    map.Order = 0;
                    alignBO();
                }

                /* if (map.Home == 0 && MapPool.Count(x => x.Home == 1) == 0)
                     map.Home = 1;
                 else if ((map.Home == 0 || map.Home == 1) && MapPool.Count(x => x.Home == 2) == 0)
                     map.Home = 2;
                 else
                     map.Home = 0;*/

                map.Home++;

            }
            alignBO();
        }

        private void ToggleButton_Click_1(object sender, RoutedEventArgs e)
        {
            var map = MapPool.FirstOrDefault(x => x.title == (sender as ToggleButton).Tag.ToString());


            if (map.Veto == 2)
            {
                map.Veto = 0;
            }
            else
            {
                if (map.Order != 0)
                {
                    foreach (var m in MapPool)
                    {
                        if (m.Order > map.Order)
                        {
                            m.Order--;
                        }
                    }
                    map.Order = 0;
                    alignBO();
                }

                /*  if (map.Veto == 0 && MapPool.Count(x => x.Veto == 1) == 0)
                      map.Veto = 1;
                  else if ((map.Veto == 0 || map.Veto == 1) && MapPool.Count(x => x.Veto == 2) == 0)
                      map.Veto = 2;
                  else
                      map.Veto = 0;*/
                map.Veto++;
            }
           

        }

        private void cbCivVetoCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            alignBO();
        }
    }

}
