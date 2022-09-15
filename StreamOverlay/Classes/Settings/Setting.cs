using StreamOverlay.Classes.Map;
using StreamOverlay.Classes.Settings;
using System.Collections.Generic;
using System.ComponentModel;

namespace StreamOverlay
{
    public class Setting : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
        private int appVersion;
        public int AppVersion
        {
            get
            {
                return appVersion;
            }
            set
            {
                appVersion = value;
                NotifyPropertyChanged("AppVersion");
            }
        }
        private int appLanguage;
        public int AppLanguage
        {
            get
            {
                return appLanguage;
            }
            set
            {
                appLanguage = value;
                NotifyPropertyChanged("AppLanguage");
            }
        }

        private int selectedGame = 2;
        public int SelectedGame
        {
            get
            {
                return selectedGame;
            }
            set
            {
                selectedGame = value;
                NotifyPropertyChanged("SelectedGame");
            }
        }

        private string selectedOverlay;
        public string SelectedOverlay
        {
            get
            {
                return selectedOverlay;
            }
            set
            {
                selectedOverlay = value;
                NotifyPropertyChanged("SelectedOverlay");
            }
        }
        private int elementsStyle;
        public int ElementsStyle
        {
            get
            {
                return elementsStyle;
            }
            set
            {
                elementsStyle = value;
                NotifyPropertyChanged("ElementStyle");
            }
        }


        private int civVeto = 0;
        public int CivVeto
        {
            get
            {
                return civVeto;
            }
            set
            {
                civVeto = value;
                NotifyPropertyChanged("CivVeto");
            }
        }

        private double hudScale = 100;
        public double HUDScale
        {
            get { return hudScale; }
            set
            {
                hudScale = value;
                NotifyPropertyChanged("HUDScale");
            }
        }
        private bool chromakey;
        public bool Chromakey
        {
            get
            {
                return chromakey;
            }
            set
            {
                chromakey = value;
                NotifyPropertyChanged("Chromakey");
            }
        }
        private bool autoplaySound;
        public bool AutoplaySound
        {
            get { return autoplaySound; }
            set
            {
                autoplaySound = value;
                NotifyPropertyChanged("AutoplaySound");
            }
        }
        private bool loopBackgrounds;
        public bool LoopBackgrounds
        {
            get { return loopBackgrounds; }
            set
            {
                loopBackgrounds = value;
                NotifyPropertyChanged("LoopBackgrounds");
            }
        }

        public double SoundVolume { get; set; } = 0.5;

        public string Team1Persons { get; set; } = "<NOT SET>";
        public string Team2Persons { get; set; } = "<NOT SET>";
        public string Team1Name { get; set; } = "Player1's Team";
        public string Team2Name { get; set; } = "Player2's Team";

        public List<Map> SelectedMaps { get; set; } = new List<Map>();
        public Element Countdown { get; set; } = new Element();
        public Element Schedule { get; set; } = new Element();
        
        public Element EventLogo { get; set; } = new Element();
        public Element BrandLogo { get; set; } = new Element();
        public Element TwitchPanel { get; set; } = new Element();
        public Element ScoreInputPanel { get; set; } = new Element();
        public Element PlayersPanel { get; set; } = new Element();
        public Element Map { get; set; } = new Element();

    }
}
