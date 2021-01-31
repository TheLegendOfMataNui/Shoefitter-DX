using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Linq;

namespace ShoefitterDX.Editors
{
    public enum ObjectPhysicsType
    {
        Static,
        StaticDoubleSided,
        StaticTransparent,
        Dynamic,
    }

    public enum ObjectCollisionShape
    {
        None,
        Box,
        Cylinder,
        Sphere,
    }

    public class AreaObject : System.ComponentModel.INotifyPropertyChanged, IDisposable
    {
        
        private string objectID;
        public string ObjectID
        {
            get => objectID;
            set
            {
                objectID = value;
                if (ModelPreview != null)
                {
                    UpdatePreview();
                }
                RaisePropertyChanged(nameof(ObjectID));
            }
        }

        public VectorModel Position { get; } = new VectorModel();

        public VectorModel Rotation { get; } = new VectorModel();

        public VectorModel BoundsMin { get; } = new VectorModel();

        public VectorModel BoundsMax { get; } = new VectorModel();

        private ObjectPhysicsType physicsType;
        public ObjectPhysicsType PhysicsType
        {
            get => physicsType;
            set
            {
                physicsType = value;
                RaisePropertyChanged(nameof(PhysicsType));
            }
        }

        private ObjectCollisionShape collisionShape;
        public ObjectCollisionShape CollisionShape
        {
            get => collisionShape;
            set
            {
                collisionShape = value;
                RaisePropertyChanged(nameof(CollisionShape));
            }
        }

        public SharpDX.Matrix Transform => SharpDX.Matrix.Translation(Position);

        public Renderer.XPreview ModelPreview { get; private set; }
        private string AreaDirectory { get; }
        private string LevelID { get; }
        private Renderer.PreviewRenderer PreviewRenderer { get; }

        public AreaObject(SAGESharp.SLB.Level.Object obj, string areaDirectory, string levelID, Renderer.PreviewRenderer previewRenderer)
        {
            this.AreaDirectory = areaDirectory;
            this.LevelID = levelID;
            this.PreviewRenderer = previewRenderer;
            this.ObjectID = obj.ID.ToString();
            this.Position.Load(obj.Location);
            this.Rotation.Load(obj.Orientation);
            this.BoundsMin.Load(obj.CollisionPoint1);
            this.BoundsMax.Load(obj.CollisionPoint2);

            if ((obj.Flags & 0b110000) == 0b110000)
            {
                PhysicsType = ObjectPhysicsType.Dynamic;
            }
            else if ((obj.Flags & 0b100110000) == 0b100010000)
            {
                PhysicsType = ObjectPhysicsType.StaticTransparent;
            }
            else if ((obj.Flags & 0b100110000) == 0b000010000)
            {
                PhysicsType = ObjectPhysicsType.Static;
            }
            else if ((obj.Flags & 0b111000) == 0b001000)
            {
                PhysicsType = ObjectPhysicsType.StaticDoubleSided;
            }
            else
            {
                throw new Exception("Unknown object type for object " + obj.ID.ToString() + " in _OBJ.slb!");
            }

            if ((obj.Flags & 0b10) == 0b10)
            {
                CollisionShape = ObjectCollisionShape.Box;
            }
            else if ((obj.Flags & 0b1) == 0b1)
            {
                CollisionShape = ObjectCollisionShape.Cylinder;
            }
            else if ((obj.Flags & 0b10000000000) == 0b10000000000)
            {
                CollisionShape = ObjectCollisionShape.Sphere;
            }
            else
            {
                CollisionShape = ObjectCollisionShape.None;
            }
        }

        public void UpdatePreview()
        {
            if (ModelPreview != null)
            {
                ModelPreview.Dispose();
                ModelPreview = null;
            }
            string modelFilename = Path.Combine(this.AreaDirectory, ObjectID + ".x");
            if (PreviewRenderer != null && File.Exists(modelFilename))
            {
                using (FileStream stream = new FileStream(modelFilename, FileMode.Open))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    SAGESharp.XFile xFile = new SAGESharp.XFile(reader);
                    ModelPreview = Renderer.XPreview.FromXFile(PreviewRenderer.Device, xFile, Path.Combine(AreaDirectory, "..\\" + LevelID), PreviewRenderer, out bool _);
                }
            }
        }

        public SAGESharp.SLB.Level.Object ToObject()
        {
            int flags = 0;

            if (PhysicsType == ObjectPhysicsType.Static)
            {
                flags |= 0b10000;
            }
            else if (PhysicsType == ObjectPhysicsType.StaticDoubleSided)
            {
                flags |= 0b1000;
            }
            else if (PhysicsType == ObjectPhysicsType.StaticTransparent)
            {
                flags |= 0b100010000;
            }
            else if (PhysicsType == ObjectPhysicsType.Dynamic)
            {
                flags |= 0b110000;
            }

            if (CollisionShape == ObjectCollisionShape.Box)
            {
                flags |= 0b10;
            }
            else if (CollisionShape == ObjectCollisionShape.Cylinder)
            {
                flags |= 0b1;
            }
            else if (CollisionShape == ObjectCollisionShape.Sphere)
            {
                flags |= 0b10000000000;
            }

            return new SAGESharp.SLB.Level.Object() { CollisionPoint1 = (SharpDX.Vector3)this.BoundsMin, CollisionPoint2 = (SharpDX.Vector3)this.BoundsMax, ID = SAGESharp.SLB.Identifier.From(this.ObjectID), Location = (SharpDX.Vector3)this.Position, Orientation = (SharpDX.Vector3)this.Rotation, Unknown = -1, Flags = flags };
        }

        public void Dispose()
        {
            if (ModelPreview != null)
            {
                ModelPreview.Dispose();
                ModelPreview = null;
            }
        }

        #region INotifyPropertyChanged
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    /// <summary>
    /// Interaction logic for AreaEditor.xaml
    /// </summary>
    public partial class AreaEditor : EditorBase
    {
        private string AreaID { get; }
        private string LevelID { get; }

        private string ObjectFilePath => Path.Combine(this.Item.FullPath, this.AreaID + "_OBJ.slb");
        public SAGESharp.SLB.Level.ObjectTable ObjectFile;

        public ObservableCollection<AreaObject> Objects { get; } = new ObservableCollection<AreaObject>();

        //private AreaObject selectedObject;
        public static DependencyProperty SelectedObjectProperty = DependencyProperty.Register(nameof(SelectedObject), typeof(AreaObject), typeof(AreaEditor));
        public AreaObject SelectedObject
        {
            get => (AreaObject)GetValue(SelectedObjectProperty);
            set => SetValue(SelectedObjectProperty, value);
        }

        private Renderer.D3D11Mesh CubeMesh;

        public AreaEditor(ToolWindows.DataBrowserItem item) : base(item)
        {
            string[] pathParts = item.FullPath.Split('\\');
            AreaID = pathParts[pathParts.Length - 1];
            LevelID = pathParts[pathParts.Length - 2];

            InitializeComponent();

            if (File.Exists(ObjectFilePath))
            {
                ObjectFile = Utils.ReadSLBFile<SAGESharp.SLB.Level.ObjectTable>(ObjectFilePath);
                foreach (SAGESharp.SLB.Level.Object obj in ObjectFile.Objects)
                {
                    AreaObject objectPreview = new AreaObject(obj, Item.FullPath, LevelID, PreviewRenderer);
                    objectPreview.PropertyChanged += Object_PropertyChanged;
                    objectPreview.Position.PropertyChanged += Object_PropertyChanged;
                    objectPreview.Rotation.PropertyChanged += Object_PropertyChanged;
                    objectPreview.BoundsMin.PropertyChanged += Object_PropertyChanged;
                    objectPreview.BoundsMax.PropertyChanged += Object_PropertyChanged;
                    Objects.Add(objectPreview);
                }
            }

            PreviewRenderer.CreateResources += PreviewRenderer_CreateResources;
            PreviewRenderer.RenderContent += PreviewRenderer_RenderContent;
            PreviewRenderer.DisposeResources += PreviewRenderer_DisposeResources;
        }

        private void Object_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.NeedsSave = true;
        }

        private void PreviewRenderer_CreateResources(object sender, EventArgs e)
        {
            CubeMesh = Renderer.D3D11Mesh.CreateCube(PreviewRenderer.Device, SharpDX.Vector4.One);
            foreach (AreaObject obj in Objects)
            {
                obj.UpdatePreview();
            }
        }

        private void PreviewRenderer_RenderContent(object sender, EventArgs e)
        {
            foreach (AreaObject obj in Objects)
            {
                foreach (Renderer.XPreviewSection section in obj.ModelPreview.Sections)
                {
                    PreviewRenderer.RenderWorldMesh(section.Mesh, new Renderer.WorldInstanceConstants(SharpDX.Matrix.RotationX(-SharpDX.MathUtil.PiOverTwo) * obj.Transform, section.Color, section.SpecularColor, section.SpecularExponent, section.EmissiveColor), section.DiffuseTexture);
                }
                if (obj.BoundsMin != SharpDX.Vector3.Zero || obj.BoundsMax != SharpDX.Vector3.Zero)
                {
                    PreviewRenderer.RenderSolidMesh(CubeMesh, new Renderer.SolidInstanceConstants(SharpDX.Matrix.Scaling(obj.BoundsMax.X - obj.BoundsMin.X, obj.BoundsMax.Y - obj.BoundsMin.Y, obj.BoundsMax.Z - obj.BoundsMin.Z) * SharpDX.Matrix.Translation(((SharpDX.Vector3)obj.BoundsMax + (SharpDX.Vector3)obj.BoundsMin) * 0.5f + (SharpDX.Vector3)obj.Position), SharpDX.Vector4.One));
                }
            }
        }

        private void PreviewRenderer_DisposeResources(object sender, EventArgs e)
        {
            foreach (AreaObject obj in Objects)
            {
                obj.Dispose();
            }
            CubeMesh.Dispose();
        }

        public override void Save()
        {
            Utils.WriteSLBFile(new SAGESharp.SLB.Level.ObjectTable() { Id = SAGESharp.SLB.Identifier.Zero, Objects = new List<SAGESharp.SLB.Level.Object>(this.Objects.Select(obj => obj.ToObject())) }, this.ObjectFilePath);

            base.Save();
        }
    }
}
