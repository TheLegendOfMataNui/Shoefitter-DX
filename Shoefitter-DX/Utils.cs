using SAGESharp.Animations;
using SAGESharp.IO.Binary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ShoefitterDX
{
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
