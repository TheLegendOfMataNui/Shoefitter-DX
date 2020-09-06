using System;

namespace ShoefitterDX
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Renderer.ResourceCache.LoadResources();

            var application = new App();
            application.InitializeComponent();
            application.Run();
        }
    }
}