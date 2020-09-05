using ShoefitterDX.ToolWindows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;

namespace ShoefitterDX.Editors
{
    /// <summary>
    /// Implementes the base functionality of all editors, including managing save state and tracking that tab icon, title, and tooltip.
    /// </summary>
    public abstract class EditorBase : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public DataBrowserItem Item { get; }

        private bool _needsSave = false;
        public bool NeedsSave
        {
            get => this._needsSave;
            protected set
            {
                this._needsSave = value;
                this.RaisePropertyChanged(nameof(NeedsSave));
            }
        }

        private ImageSource _tabIconSource = null;
        public ImageSource TabIconSource
        {
            get => this._tabIconSource;
            protected set
            {
                this._tabIconSource = value;
                this.RaisePropertyChanged(nameof(TabIconSource));
            }
        }

        private string _tabTitle = "";
        public string TabTitle
        {
            get => this._tabTitle;
            protected set
            {
                this._tabTitle = value;
                this.RaisePropertyChanged(nameof(TabTitle));
            }
        }

        private string _tabToolTip = "";
        public string TabToolTip
        {
            get => this._tabToolTip;
            protected set
            {
                this._tabToolTip = value;
                this.RaisePropertyChanged(nameof(TabToolTip));
            }
        }

        public EditorBase(DataBrowserItem item)
        {
            this.Item = item;
            this.TabTitle = item.Name;
            this.TabToolTip = item.FullPath;
        }

        public virtual void Save()
        {
            this.NeedsSave = false;
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
