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
            Config = new SAGESharp.INIConfig(INIFilename);

            SAGESharp.OSI.OSIFile osi;

            using (System.IO.FileStream stream = new System.IO.FileStream("D:\\Program Files\\LEGO Bionicle\\Data\\Base.osi", System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
            using (System.IO.BinaryReader reader = new System.IO.BinaryReader(stream))
            {
                osi = new SAGESharp.OSI.OSIFile(reader);

            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Window());

            foreach (SAGESharp.OSI.OSIFile.FunctionInfo func in osi.Functions)
            {
                System.Diagnostics.Debug.WriteLine("Graphing Function '" + func.Name + "'...");
                SAGESharp.OSI.ControlFlow.SubroutineGraph graph = new SAGESharp.OSI.ControlFlow.SubroutineGraph(func.Instructions, func.BytecodeOffset);
            }

            /*foreach (SAGESharp.OSI.OSIFile.ClassInfo cls in osi.Classes)
            {
                foreach (SAGESharp.OSI.OSIFile.MethodInfo method in cls.Methods)
                {
                    System.Diagnostics.Debug.WriteLine("Graphing Method '" + cls.Name + "." + osi.Symbols[method.NameSymbol] + "'...");
                    SAGESharp.OSI.ControlFlow.SubroutineGraph graph = new SAGESharp.OSI.ControlFlow.SubroutineGraph(method.Instructions, method.BytecodeOffset);
                }
            }*/

            OSIBrowser browser = new OSIBrowser();
            browser.LoadOSI(osi);
            Application.Run(browser);

            Config.Write(INIFilename);
        }
    }
}
