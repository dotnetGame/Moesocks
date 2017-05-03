using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Moesocks.Client.Services.Configuration
{
    public abstract class ConfigurationBase : INotifyPropertyChanged
    {
        [JsonIgnore]
        public bool AutoSave { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler Saving;

        protected bool SetProperty<T>(ref T property, T value, [CallerMemberName]string propertyName = null)
        {
            if(!EqualityComparer<T>.Default.Equals(property, value))
            {
                property = value;
                OnPropertyChanged(propertyName);
                if (AutoSave)
                    Save();
                return true;
            }
            return false;
        }

        public void Save()
        {
            Saving?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
