using SAGESharp;
using SAGESharp.Animations;
using SAGESharp.IO.Binary;
using SharpDX;
using ShoefitterDX.Models;
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

        private List<string> SkeletonFilePaths { get; } = new List<string>();
        public Models.SkeletonModel Skeleton { get; }
        public BoneModel SelectedBone => SkeletonTreeView.SelectedItem as BoneModel;

        public ObservableCollection<Models.AnimationModel> Animations { get; } = new ObservableCollection<Models.AnimationModel>();
        public AnimationModel SelectedAnimation { get; set; } = null;
        public float AnimationTime { get; set; }

        private Matrix[] DefaultPose = new Matrix[255];
        private Matrix[] Pose = new Matrix[255]; // Each joint's local pose
        private Matrix[] WorldPose = new Matrix[255]; // Cumulative joint transforms in model space

        public ObservableCollection<CharacterModel> Models { get; } = new ObservableCollection<CharacterModel>();

        private D3D11Mesh CylinderMesh;
        private D3D11Mesh AxisMesh;
        private D3D11Mesh BoneMesh;

        private static T ReadSLBFile<T>(string filename)
        {
            IBinarySerializer<T> serializer = BinarySerializer.ForType<T>();
            using (FileStream stream = new FileStream(filename, FileMode.Open))
            {
                IBinaryReader reader = Reader.ForStream(stream);
                return serializer.Read(reader);
            }
        }

        private static BKD ReadBKDFile(string filename)
        {
            IBinarySerializer<BKD> serializer = BinarySerializer.ForBKDFiles;
            using (FileStream stream = new FileStream(filename, FileMode.Open))
            {
                IBinaryReader reader = Reader.ForStream(stream);
                BKD result = new BKD();
                result.Read(reader);
                return result;
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

            foreach (string bkdFile in Directory.EnumerateFiles(item.FullPath, "*.bkd", SearchOption.AllDirectories))
            {
                Animations.Add(AnimationModel.FromBKD(ReadBKDFile(bkdFile), bkdFile));
            }

            using (FileStream bhdStream = new FileStream(Path.Combine(item.FullPath, Path.GetFileName(item.FullPath) + ".bhd"), FileMode.Open))
            using (BinaryReader bhdReader = new BinaryReader(bhdStream))
            {
                Skeleton = new Models.SkeletonModel(new SAGESharp.BHDFile(bhdReader), true);
                Skeleton.WriteTransforms(Pose);
                Skeleton.WriteTransforms(DefaultPose);
            }

            InitializeComponent();

            PreviewRenderer.CreateResources += PreviewRenderer_CreateResources;
            PreviewRenderer.RenderContent += PreviewRenderer_RenderContent;
            PreviewRenderer.UpdateContent += PreviewRenderer_UpdateContent;
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
            this.AxisMesh = D3D11Mesh.CreateAxes(PreviewRenderer.Device);
            this.BoneMesh = D3D11Mesh.CreateBone(PreviewRenderer.Device, Vector4.One);
            foreach (CharacterModel model in this.Models)
            {
                using (FileStream stream = new FileStream(model.Path, FileMode.Open))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    XFile file = new XFile(reader);
                    model.Preview = XPreview.FromXFile(PreviewRenderer.Device, file, Path.Combine(Path.GetDirectoryName(this.Item.FullPath), "Textures"), PreviewRenderer, out bool isBiped);
                }
            }
        }

        private void RecursiveRenderBone(Models.BoneModel bone, Matrix parentTransform)
        {
            Matrix combined = Pose[bone.ID] * parentTransform;

            PreviewRenderer.RenderSolidMesh(this.AxisMesh, new SolidInstanceConstants(Matrix.Scaling(0.1f) * combined, Vector4.One));

            foreach (Models.BoneModel childBone in bone.Children)
            {
                Matrix look = new Matrix();
                Vector3 right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, childBone.Transform.TranslationVector));
                look.Row1 = new Vector4(right, 0.0f);
                look.Row2 = new Vector4(Vector3.Normalize(Vector3.Cross(right, childBone.Transform.TranslationVector)), 0.0f);
                look.Row3 = new Vector4(Vector3.Normalize(childBone.Transform.TranslationVector), 0.0f);
                look.Row4 = Vector4.UnitW;

                Matrix final = Matrix.Scaling(0.1f, 0.1f, childBone.Transform.TranslationVector.Length()) * look * combined;
                Vector3 endpointParent = Vector3.TransformCoordinate(Vector3.UnitZ, final);
                Vector3 endpointChild = (childBone.Transform * combined).TranslationVector;
                PreviewRenderer.RenderSolidMesh(this.BoneMesh, new SolidInstanceConstants(final, (bone == SelectedBone) ? Vector4.One : new Vector4(0.5f, 0.5f, 0.5f, 1.0f)));

                this.RecursiveRenderBone(childBone, combined);
            }
        }

        private void PreviewRenderer_RenderContent(object sender, EventArgs e)
        {
            if (this.CylinderFile != null)
            {
                Vector3 center = ((Vector3)this.CylinderFile.Bounds.Max + (Vector3)this.CylinderFile.Bounds.Min) * 0.5f;
                Vector3 size = (Vector3)this.CylinderFile.Bounds.Max - (Vector3)this.CylinderFile.Bounds.Min;
                PreviewRenderer.RenderSolidMesh(this.CylinderMesh, new SolidInstanceConstants(Matrix.Scaling(size) * Matrix.Translation(center), Vector4.One));
            }

            Skeleton.BakePose(Pose, WorldPose);
            foreach (CharacterModel model in this.Models)
            {
                if (model.ShowPreview)
                {
                    foreach (XPreviewSection section in model.Preview.Sections)
                    {
                        PreviewRenderer.RenderSkinnedWorldMesh(section.Mesh, new WorldInstanceConstants(Matrix.RotationX(-MathUtil.PiOverTwo), section.Color, section.SpecularColor, section.SpecularExponent, section.EmissiveColor), section.DiffuseTexture, model.Preview.BindPoseBuffer, WorldPose); // Matrix.RotationX(-MathUtil.PiOverTwo)
                    }
                }
            }

            PreviewRenderer.ImmediateContext.OutputMerger.DepthStencilState = PreviewRenderer.AlwaysDepthStencilState;
            foreach (Models.BoneModel bone in Skeleton.RootBones)
            {
                this.RecursiveRenderBone(bone, Matrix.RotationX(-MathUtil.PiOverTwo));
            }
            PreviewRenderer.ImmediateContext.OutputMerger.DepthStencilState = PreviewRenderer.DefaultDepthStencilState;
        }

        private void PreviewRenderer_UpdateContent(object sender, UpdateContentEventArgs e)
        {
            AnimationTime += e.TimeStep;

            if (SelectedAnimation != null)
            {
                if (SelectedAnimation.Duration > 0.0f)
                {
                    while (AnimationTime >= SelectedAnimation.Duration)
                    {
                        AnimationTime -= SelectedAnimation.Duration;
                    }
                }

                SelectedAnimation.Evaluate(AnimationTime, DefaultPose, Pose);
            }
        }

        private void PreviewRenderer_DisposeResources(object sender, EventArgs e)
        {
            this.CylinderMesh?.Dispose();
            this.CylinderMesh = null;
            this.AxisMesh?.Dispose();
            this.AxisMesh = null;
            this.BoneMesh?.Dispose();
            this.BoneMesh = null;

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
