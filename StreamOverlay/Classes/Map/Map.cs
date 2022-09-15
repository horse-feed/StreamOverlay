using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamOverlay.Classes.Map
{
    public class Map : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        }
        public string title { get; set; }
        public string icon { get; set; }

        private int order { get; set; } = 0;

        private int veto { get; set; } = 0;
        private int home { get; set; } = 0;

        public string VetoIcon
        {
            get
            {
                if (veto > 0)
                {
                    return "pack://application:,,,/resources/Veto.png";
                }
                else
                {
                    return "";
                }
            }
        }

        public string HomeIcon
        {
            get
            {
                if (home > 0)
                {
                    return "pack://application:,,,/resources/Home.png";
                }
                else
                {
                    return "";
                }
            }
        }

        public int Veto
        {
            get { return veto; }
            set
            {

                if (veto != value)
                {
                    veto = value;
                    NotifyPropertyChanged("Veto");
                    NotifyPropertyChanged("VetoIcon");
                    if (home != 0)
                    {
                        home = 0;
                        NotifyPropertyChanged("Home");
                        NotifyPropertyChanged("HomeIcon");
                    }
                }
            }
        }

        public int Home
        {
            get { return home; }
            set
            {

                if (home != value)
                {
                    home = value;
                    NotifyPropertyChanged("Home");
                    NotifyPropertyChanged("HomeIcon");
                    if (veto != 0)
                    {
                        veto = 0;
                        NotifyPropertyChanged("Veto");
                        NotifyPropertyChanged("VetoIcon");
                    }
                }

            }
        }

        public int Order
        {
            get { return order; }
            set
            {
                order = value;
                NotifyPropertyChanged("Order");
            }
        }
    }
}
