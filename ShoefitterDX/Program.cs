using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShoefitterDX
{
    static class Program
    {
        private const string INIFilename = "ShoefitterDX.ini";

        public static SAGESharp.INIConfig Config { get; private set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Config = new SAGESharp.INIConfig(INIFilename);

            SAGESharp.OSI.OSIFile osi;

            string osiFilename = "D:\\Program Files\\LEGO Bionicle\\Data\\Base.osi";

            if (!System.IO.File.Exists(osiFilename))
            {
                OpenFileDialog osiBrowser = new OpenFileDialog();
                osiBrowser.Filter = "SAGE OSI File (*.osi)|*.osi";
                if (osiBrowser.ShowDialog() == DialogResult.Cancel)
                    return;
                osiFilename = osiBrowser.FileName;
            }

            using (System.IO.FileStream stream = new System.IO.FileStream(osiFilename, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
            using (System.IO.BinaryReader reader = new System.IO.BinaryReader(stream))
            {
                osi = new SAGESharp.OSI.OSIFile(reader);

            }

            // Test OSI writing
            /*using (System.IO.FileStream stream = new System.IO.FileStream(osiFilename, System.IO.FileMode.Open, System.IO.FileAccess.Write, System.IO.FileShare.None))
            using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream))
            {
                osi.Write(writer);
            }*/

            // Shoefitter-DX Entrypoint
            //Application.Run(new Window());

            // Test control flow analysis
            /*foreach (SAGESharp.OSI.OSIFile.FunctionInfo func in osi.Functions)
            {
                //System.Diagnostics.Debug.WriteLine("Graphing Function '" + func.Name + "'...");
                try
                {
                    SAGESharp.OSI.ControlFlow.SubroutineGraph graph = new SAGESharp.OSI.ControlFlow.SubroutineGraph(func.Instructions, func.BytecodeOffset);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to graph function '" + func.Name + "'!");
                }
            }

            foreach (SAGESharp.OSI.OSIFile.ClassInfo cls in osi.Classes)
            {
                foreach (SAGESharp.OSI.OSIFile.MethodInfo method in cls.Methods)
                {
                    //System.Diagnostics.Debug.WriteLine("Graphing Method '" + cls.Name + "." + osi.Symbols[method.NameSymbol] + "'...");
                    try
                    {
                        SAGESharp.OSI.ControlFlow.SubroutineGraph graph = new SAGESharp.OSI.ControlFlow.SubroutineGraph(method.Instructions, method.BytecodeOffset);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Failed to graph method '" + cls.Name + "." + osi.Symbols[method.NameSymbol] + "'!");
                    }
                }
            }*/

            /*OSIBrowser browser = new OSIBrowser();
            browser.LoadOSI(osi);
            Application.Run(browser);*/

            LSSInteractive IDE = new LSSInteractive();
            Application.Run(IDE);

            Config.Write(INIFilename);
        }
    }
}
