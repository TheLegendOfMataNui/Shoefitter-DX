using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AvalonDock.Layout;
using ShoefitterDX.Editors;
using ShoefitterDX.ToolWindows;

namespace ShoefitterDX
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<object, EditorDocument> OpenDocuments { get; } = new Dictionary<object, EditorDocument>();
        private SAGESharp.INIConfig Config { get; }
        private const string ConfigFilename = "Shoeffiter-DX.ini";

        public Context Context { get; } = new Context();
        public DataBrowser DataBrowser { get; }

        public MainWindow()
        {
            InitializeComponent();

            Config = new SAGESharp.INIConfig(ConfigFilename);
            Context.ProjectDirectory = Config.GetValueOrDefault("Context", "ProjectDirectory", @"E:\Projects\Modding\Bionicle\My-Code\LOMN-Beta\");

            this.DataBrowser = new DataBrowser(this.Context);
            this.DataBrowser.ItemDoubleClicked += DataBrowser_ItemDoubleClicked;

            LayoutAnchorablePane rightPane = new LayoutAnchorablePane();
            rightPane.DockWidth = new GridLength(200);
            rightPane.Children.Add(new LayoutAnchorable() { Content = this.DataBrowser, Title = "Data Browser" });
            DockingManager.Layout.RootPanel.Children.Add(rightPane);

            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Config.Write(ConfigFilename);
        }

        private void DataBrowser_ItemDoubleClicked(object sender, DataBrowserItem e)
        {
            if (e.Type != null)
            {
                ShowOrCreateDocument(e.FullPath, () => new EditorDocument(Activator.CreateInstance(e.Type.EditorType, e) as EditorBase));
            }
        }

        private void ShowOrCreateDocument(object key, Func<EditorDocument> constructor)
        {
            EditorDocument doc;
            if (OpenDocuments.ContainsKey(key))
            {
                doc = OpenDocuments[key];
            }
            else
            {
                doc = constructor();
                DockingManager.Layout.Descendents().OfType<LayoutDocumentPane>().First().Children.Add(doc);
                OpenDocuments.Add(key, doc);
                doc.Closed += (sender, args) => OpenDocuments.Remove(key);
            }

            doc.IsActive = true;
            doc.IsSelected = true;
        }
    }
}
