using SAGESharp;
using SAGESharp.IO.Binary;
using SharpDX;
using ShoefitterDX.Renderer;
using ShoefitterDX.ToolWindows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ShoefitterDX.Editors
{
    public class CharacterModel : INotifyPropertyChanged
    {
        public string Path { get; }

        private bool _showPreview = true;
        public bool ShowPreview
        {
            get => this._showPreview;
            set
            {
                this._showPreview = value;
                this.RaisePropertyChanged(nameof(ShowPreview));
            }
        }

        public XPreview Preview { get; set; }

        public CharacterModel(string path)
        {
            this.Path = path;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    /// <summary>
    /// Interaction logic for CharacterEditor.xaml
    /// </summary>
    public partial class CharacterEditor : EditorBase
    {
        private string CylinderFilePath => Path.Combine(this.Item.FullPath, "Cylinder.slb");
        public SAGESharp.SLB.Cylinder CylinderFile { get; }

        private string AIInfoFilePath => Path.Combine(this.Item.FullPath, "AIInfo.slb");
        public SAGESharp.SLB.AIInfo AIInfoFile { get; }

        public ObservableCollection<CharacterModel> Models { get; } = new ObservableCollection<CharacterModel>();

        private D3D11Mesh CylinderMesh;

        private static T ReadSLBFile<T>(string filename)
        {
            IBinarySerializer<T> serializer = BinarySerializer.ForType<T>();
            using (FileStream stream = new FileStream(filename, FileMode.Open))
            {
                IBinaryReader reader = Reader.ForStream(stream);
                return serializer.Read(reader);
            }
        }

        private static void WriteSLBFile<T>(T slb, string filename)
        {
            IBinarySerializer<T> serializer = BinarySerializer.ForType<T>();
            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                IBinaryWriter writer = Writer.ForStream(stream);
                serializer.Write(writer, slb);
            }
        }

        public CharacterEditor(DataBrowserItem item) : base(item)
        {
            if (File.Exists(this.CylinderFilePath))
            {
                this.CylinderFile = ReadSLBFile<SAGESharp.SLB.Cylinder>(this.CylinderFilePath);
                this.CylinderFile.PropertyChanged += OnSLBPropertyChanged;
                this.CylinderFile.Bounds.PropertyChanged += OnSLBPropertyChanged;
            }

            if (File.Exists(this.AIInfoFilePath))
            {
                this.AIInfoFile = ReadSLBFile<SAGESharp.SLB.AIInfo>(this.AIInfoFilePath);
                this.AIInfoFile.PropertyChanged += OnSLBPropertyChanged;
                this.AIInfoFile.TimerValuesIdle.PropertyChanged += OnSLBPropertyChanged;
                this.AIInfoFile.TimerValuesPatrol.PropertyChanged += OnSLBPropertyChanged;
            }

            foreach (string childFile in Directory.EnumerateFiles(item.FullPath, "*.x", SearchOption.AllDirectories))
            {
                this.Models.Add(new CharacterModel(childFile));
            }

            InitializeComponent();

            PreviewRenderer.CreateResources += PreviewRenderer_CreateResources;
            PreviewRenderer.RenderContent += PreviewRenderer_RenderContent;
            PreviewRenderer.DisposeResources += PreviewRenderer_DisposeResources;

            this.Unloaded += CharacterEditor_Unloaded;
        }

        private void CharacterEditor_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.CylinderFile != null)
            {
                this.CylinderFile.PropertyChanged -= OnSLBPropertyChanged;
                this.CylinderFile.Bounds.PropertyChanged -= OnSLBPropertyChanged;
            }

            if (this.AIInfoFile != null)
            {
                this.AIInfoFile.PropertyChanged -= OnSLBPropertyChanged;
                this.AIInfoFile.TimerValuesIdle.PropertyChanged -= OnSLBPropertyChanged;
                this.AIInfoFile.TimerValuesPatrol.PropertyChanged -= OnSLBPropertyChanged;
            }
        }

        #region Preview Renderer
        private void PreviewRenderer_CreateResources(object sender, EventArgs e)
        {
            this.CylinderMesh = D3D11Mesh.CreateCylinder(PreviewRenderer.Device, Vector3.UnitX * 0.5f, Vector3.UnitZ * 0.5f, Vector3.UnitY * 0.5f, new Vector4(0.1f, 0.8f, 0.9f, 1.0f));
            foreach (CharacterModel model in this.Models)
            {
                using (FileStream stream = new FileStream(model.Path, FileMode.Open))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    XFile file = new XFile(reader);
                    model.Preview = XPreview.FromXFile(PreviewRenderer.Device, file, Path.Combine(Path.GetDirectoryName(this.Item.FullPath), "Textures"), PreviewRenderer);
                }
            }
        }

        private void PreviewRenderer_RenderContent(object sender, EventArgs e)
        {
            if (this.CylinderFile != null)
            {
                Vector3 center = ((Vector3)this.CylinderFile.Bounds.Max + (Vector3)this.CylinderFile.Bounds.Min) * 0.5f;
                Vector3 size = (Vector3)this.CylinderFile.Bounds.Max - (Vector3)this.CylinderFile.Bounds.Min;
                PreviewRenderer.RenderSolidMesh(this.CylinderMesh, new SolidInstanceConstants(Matrix.Scaling(size) * Matrix.Translation(center)));
            }

            foreach (CharacterModel model in this.Models)
            {
                if (model.ShowPreview)
                {
                    foreach (XPreviewSection section in model.Preview.Sections)
                    {
                        PreviewRenderer.RenderWorldMesh(section.Mesh, new WorldInstanceConstants(Matrix.RotationX(-MathUtil.PiOverTwo), section.Color, section.SpecularColor, section.SpecularExponent, section.EmissiveColor), section.DiffuseTexture); // Matrix.RotationX(-MathUtil.PiOverTwo)
                    }
                }
            }
        }

        private void PreviewRenderer_DisposeResources(object sender, EventArgs e)
        {
            this.CylinderMesh?.Dispose();
            this.CylinderMesh = null;

            foreach (CharacterModel model in this.Models)
            {
                model.Preview?.Dispose();
                model.Preview = null;
            }
        }
        #endregion

        public override void Save()
        {
            WriteSLBFile(this.CylinderFile, this.CylinderFilePath);
            WriteSLBFile(this.AIInfoFile, this.AIInfoFilePath);

            base.Save();
        }

        private void OnSLBPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.NeedsSave = true;
        }
    }
}
