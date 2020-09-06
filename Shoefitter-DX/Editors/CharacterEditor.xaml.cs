using SAGESharp.IO.Binary;
using ShoefitterDX.ToolWindows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

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
        }

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
