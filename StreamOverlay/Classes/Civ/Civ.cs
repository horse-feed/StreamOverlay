using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamOverlay.Classes.Civ
{
    public class Civ: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

    public void NotifyPropertyChanged(string propName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

    }
        public int Tag { get; set; }
        public int Status { get; set; }


        private bool veto = false;
        public bool Veto
        {
            get { return veto; }
            set { veto = value;
                NotifyPropertyChanged("Veto");
            }
        }

        public string Name
        {
            get
            {
                return Path.GetFileNameWithoutExtension(Icon).ToUpper();
            }
        }


        private string icon = "data/civs/Age of Empires III/random.png";

        public string Icon
        {
            get { return icon; }
            set
            {
                icon = value;
                NotifyPropertyChanged("Icon");
                NotifyPropertyChanged("Name");
            }
        }

        public string StatusIcon
        {
            get
            {
                return Status switch
                {
                    1 => "/resources/win.png",
                    2 => "/resources/loss.png",
                    3 => "/resources/Veto.png",
                    _ => "",
                };
            }
        }


        public void NextStatus()
        {
            if (Status == 3)
            {
                Status = 0;
            }
            else
            {
                Status += 1;
            }
            NotifyPropertyChanged("Status");
            NotifyPropertyChanged("StatusIcon");
        }

    }
}
