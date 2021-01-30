using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ShoefitterDX.Editors
{
    /// <summary>
    /// Interaction logic for AreaEditor.xaml
    /// </summary>
    public partial class AreaEditor : EditorBase
    {
        private string AreaID { get; }
        private string LevelID { get; }

        private string ObjectFilePath => Path.Combine(this.Item.FullPath, this.AreaID + "_OBJ.slb");
        public SAGESharp.SLB.Level.ObjectTable ObjectFile;

        public List<Renderer.XPreview> ObjectPreviews = new List<Renderer.XPreview>();

        public AreaEditor(ToolWindows.DataBrowserItem item) : base(item)
        {
            string[] pathParts = item.FullPath.Split('\\');
            AreaID = pathParts[pathParts.Length - 1];
            LevelID = pathParts[pathParts.Length - 2];

            InitializeComponent();

            if (File.Exists(ObjectFilePath))
            {
                ObjectFile = Utils.ReadSLBFile<SAGESharp.SLB.Level.ObjectTable>(ObjectFilePath);
                // TODO: Add PropertyChanged handlers???
            }

            PreviewRenderer.CreateResources += PreviewRenderer_CreateResources;
            PreviewRenderer.RenderContent += PreviewRenderer_RenderContent;
            PreviewRenderer.DisposeResources += PreviewRenderer_DisposeResources;
        }

        private static SharpDX.Matrix BuildObjectTransform(SAGESharp.SLB.Level.Object obj)
        {
            return SharpDX.Matrix.Translation(obj.Location.X, obj.Location.Y, obj.Location.Z); // TODO: Rotation!
        }

        private void PreviewRenderer_CreateResources(object sender, EventArgs e)
        {
            foreach (SAGESharp.SLB.Level.Object obj in ObjectFile.Objects)
            {
                using (FileStream stream = new FileStream(Path.Combine(this.Item.FullPath, obj.ID.ToString() + ".x"), FileMode.Open))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    SAGESharp.XFile xFile = new SAGESharp.XFile(reader);
                    ObjectPreviews.Add(Renderer.XPreview.FromXFile(PreviewRenderer.Device, xFile, Path.Combine(this.Item.FullPath, "..\\" + LevelID), PreviewRenderer, out bool _));
                }
            }
        }

        private void PreviewRenderer_RenderContent(object sender, EventArgs e)
        {
            int i = 0;
            foreach (Renderer.XPreview objectPreview in ObjectPreviews)
            {
                foreach (Renderer.XPreviewSection section in objectPreview.Sections)
                {
                    PreviewRenderer.RenderWorldMesh(section.Mesh, new Renderer.WorldInstanceConstants(SharpDX.Matrix.RotationX(-SharpDX.MathUtil.PiOverTwo) * BuildObjectTransform(ObjectFile.Objects[i]), section.Color, section.SpecularColor, section.SpecularExponent, section.EmissiveColor), section.DiffuseTexture);
                }
                i++;
            }
        }

        private void PreviewRenderer_DisposeResources(object sender, EventArgs e)
        {
            foreach (Renderer.XPreview preview in ObjectPreviews)
            {
                preview.Dispose();
            }
        }
    }
}
