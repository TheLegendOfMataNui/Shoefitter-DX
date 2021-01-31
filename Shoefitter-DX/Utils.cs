using SAGESharp.Animations;
using SAGESharp.IO.Binary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace ShoefitterDX
{
    public class VectorModel : INotifyPropertyChanged
    {
        private float x;
        public float X
        {
            get => x;
            set
            {
                x = value;
                RaisePropertyChanged(nameof(X));
            }
        }

        private float y;
        public float Y
        {
            get => y;
            set
            {
                y = value;
                RaisePropertyChanged(nameof(Y));
            }
        }

        private float z;
        public float Z
        {
            get => z;
            set
            {
                z = value;
                RaisePropertyChanged(nameof(Z));
            }
        }

        public VectorModel()
        {

        }

        public VectorModel(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public void Load(SharpDX.Vector3 vector)
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
        }

        public static implicit operator SharpDX.Vector3(VectorModel vector)
        {
            return new SharpDX.Vector3(vector.X, vector.Y, vector.Z);
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

    public static class Utils
    {
        public static T ReadSLBFile<T>(string filename)
        {
            IBinarySerializer<T> serializer = BinarySerializer.ForType<T>();
            using (FileStream stream = new FileStream(filename, FileMode.Open))
            {
                IBinaryReader reader = Reader.ForStream(stream);
                return serializer.Read(reader);
            }
        }

        public static BKD ReadBKDFile(string filename)
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

        public static void WriteSLBFile<T>(T slb, string filename)
        {
            IBinarySerializer<T> serializer = BinarySerializer.ForType<T>();
            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                IBinaryWriter writer = Writer.ForStream(stream);
                serializer.Write(writer, slb);
            }
        }

        public static void WriteBKDFile(BKD bkd, string filename)
        {
            IBinarySerializer<BKD> serializer = BinarySerializer.ForBKDFiles;
            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                IBinaryWriter writer = Writer.ForStream(stream);
                serializer.Write(writer, bkd);
            }
        }
    }
}
