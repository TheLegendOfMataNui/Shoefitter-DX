using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AvalonDock;
using AvalonDock.Layout;

namespace ShoefitterDX.Editors
{
    /// <summary>
    /// Displays an editor in a layout document.
    /// </summary>
    public class EditorDocument : LayoutDocument
    {
        public EditorBase Editor { get; }

        public EditorDocument(EditorBase editor)
        {
            this.Editor = editor;
            this.Content = editor;
            this.IconSource = editor.TabIconSource;
            this.Title = editor.TabTitle;
            this.ToolTip = editor.TabToolTip;

            Editor.PropertyChanged += Editor_PropertyChanged;
            Closed += (sender, args) => Editor.PropertyChanged -= Editor_PropertyChanged;
        }

        private void Editor_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditorBase.TabIconSource))
                this.IconSource = this.Editor.TabIconSource;
            else if (e.PropertyName == nameof(EditorBase.TabTitle))
                this.Title = this.Editor.TabTitle;
            else if (e.PropertyName == nameof(EditorBase.TabToolTip))
                this.ToolTip = this.Editor.TabToolTip;
        }
    }
}
