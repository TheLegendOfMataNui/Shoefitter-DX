using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SharpDX.D3DCompiler;

namespace ShoefitterDX.Renderer
{
    public static class ResourceCache
    {
        private static readonly string ResourceDirectory = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "res");

        public static Dictionary<string, byte[]> Resources { get; } = new Dictionary<string, byte[]>();

        public static void LoadResources()
        {
            Resources.Clear();
            foreach (string filename in Directory.EnumerateFiles(ResourceDirectory))
            {
                byte[] data = System.IO.File.ReadAllBytes(filename);
                if (filename.EndsWith(".hlsl", StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine("Compiling shader '" + filename + "'...");
                    string profile = null;
                    if (filename.ToLower().Contains("pixel"))
                        profile = "ps_4_0";
                    else if (filename.ToLower().Contains("vertex"))
                        profile = "vs_4_0";
                    else
                        throw new FormatException("Could not determine the type of shader in file '" + filename + "'!");

                    CompilationResult result = ShaderBytecode.Compile(Encoding.ASCII.GetString(data), "main", profile, ShaderFlags.Debug);

                    if (result.HasErrors)
                    {
                        throw new FormatException("Could not compile the shader in file '" + filename + "': \n\n" + result.ResultCode.ToString() + " " + result.Message);
                    }
                    else
                    {
                        data = result.Bytecode;
                    }
                }
                
                Resources.Add(Path.GetFileName(filename), data);
            }
        }
    }
}
