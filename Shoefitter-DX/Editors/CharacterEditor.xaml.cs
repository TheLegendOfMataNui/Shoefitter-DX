using SAGESharp.IO.Binary;
using SharpDX;
using ShoefitterDX.Renderer;
using ShoefitterDX.ToolWindows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ShoefitterDX.Editors
{
    /// <summary>
    /// Interaction logic for CharacterEditor.xaml
    /// </summary>
    public partial class CharacterEditor : EditorBase
    {
        private string CylinderFilePath => Path.Combine(this.Item.FullPath, "Cylinder.slb");
        public SAGESharp.SLB.Cylinder CylinderFile { get; }

        private string AIInfoFilePath => Path.Combine(this.Item.FullPath, "AIInfo.slb");
        public SAGESharp.SLB.AIInfo AIInfoFile { get; }

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

            InitializeComponent();

            PreviewRenderer.CreateResources += PreviewRenderer_CreateResources;
            PreviewRenderer.RenderContent += PreviewRenderer_RenderContent;
            PreviewRenderer.DisposeResources += PreviewRenderer_DisposeResources;
        }

        #region Preview Renderer
        private void PreviewRenderer_CreateResources(object sender, EventArgs e)
        {
            this.CylinderMesh = D3D11Mesh.CreateCylinder(PreviewRenderer.Device, Vector3.UnitX * 0.5f, Vector3.UnitZ * 0.5f, Vector3.UnitY * 0.5f, new Vector4(0.1f, 0.8f, 0.9f, 1.0f));
        }

        private void PreviewRenderer_RenderContent(object sender, EventArgs e)
        {
            if (this.CylinderFile != null)
            {
                Vector3 center = ((Vector3)this.CylinderFile.Bounds.Max + (Vector3)this.CylinderFile.Bounds.Min) * 0.5f;
                Vector3 size = (Vector3)this.CylinderFile.Bounds.Max - (Vector3)this.CylinderFile.Bounds.Min;
                PreviewRenderer.RenderWorldMesh(this.CylinderMesh, new WorldInstanceConstants(Matrix.Scaling(size) * Matrix.Translation(center)));
            }
        }

        private void PreviewRenderer_DisposeResources(object sender, EventArgs e)
        {
            this.CylinderMesh?.Dispose();
            this.CylinderMesh = null;
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
