using System;
using System.Diagnostics;

namespace ShoefitterDX
{
    public static class Program
    {
        [STAThread]
        [DebuggerNonUserCode]
        public static void Main()
        {
            Renderer.ResourceCache.LoadResources();

            var application = new App();
            application.InitializeComponent();
            application.Run();
        }
    }
}