using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ShoefitterDX
{
    public class Context : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _projectDirectory = "";
        public string ProjectDirectory
        {
            get => this._projectDirectory;
            set
            {
                this._projectDirectory = value;
                this.RaisePropertyChanged(nameof(ProjectDirectory));
            }
        }

        public Context()
        {

        }

        public Context(string filename)
        {
            // TODO: Implement!
            throw new NotImplementedException();
        }

        public void Save(string filename)
        {
            // TODO: Implement!
            throw new NotImplementedException();
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
