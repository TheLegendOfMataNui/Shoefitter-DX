using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace ShoefitterDX.Renderer
{
    public class UpdateContentEventArgs : EventArgs
    {
        public float TimeStep { get; }

        public UpdateContentEventArgs(float timeStep)
        {
            this.TimeStep = timeStep;
        }
    }

    public class D3D11Element : FrameworkElement
    {
        private D3D11Image Image;

        private SharpDX.Direct3D11.Query queryForCompletion;

        private Direct3DEx D3D9;
        private DeviceEx D3D9Device;

        public SharpDX.Direct3D11.Device D3D11Device { get; private set; }
        public DeviceContext D3D11Context { get; private set; }
        public Texture2D D3D11Backbuffer { get; private set; }
        public RenderTargetView D3D11BackbufferRTV { get; private set; }

        public bool AreBuffersLoaded { get; private set; } = false;
        private System.Diagnostics.Stopwatch UpdateStopwatch = null;

        private bool IsInDesignMode => DesignerProperties.GetIsInDesignMode(this);

        public D3D11Element()
        {
            if (!IsInDesignMode)
            {
                this.Focusable = true;
                this.Loaded += PreviewElement_Loaded;
                this.Unloaded += PreviewElement_Unloaded;
                this.MouseDown += (sender, e) => Focus();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (!IsInDesignMode)
            {
                if (Image != null && Image.IsFrontBufferAvailable)
                {
                    drawingContext.DrawImage(Image, new Rect(new System.Windows.Point(), RenderSize));
                }
            }
        }

        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr GetDesktopWindow();

        private void PreviewElement_Loaded(object sender, RoutedEventArgs e)
        {
            // Create the D3D9 device
            PresentParameters presentparams = new SharpDX.Direct3D9.PresentParameters
            {
                Windowed = true,
                SwapEffect = SharpDX.Direct3D9.SwapEffect.Discard,
                DeviceWindowHandle = GetDesktopWindow(),
                PresentationInterval = SharpDX.Direct3D9.PresentInterval.Default
            };
            const SharpDX.Direct3D9.CreateFlags deviceFlags = SharpDX.Direct3D9.CreateFlags.HardwareVertexProcessing | SharpDX.Direct3D9.CreateFlags.Multithreaded | SharpDX.Direct3D9.CreateFlags.FpuPreserve;
            this.D3D9 = new Direct3DEx();
            this.D3D9Device = new DeviceEx(this.D3D9, 0, DeviceType.Hardware, IntPtr.Zero, deviceFlags, presentparams);

            // Create D3D11
            DeviceCreationFlags flags = DeviceCreationFlags.BgraSupport;
#if DEBUG
            flags |= DeviceCreationFlags.Debug;
#endif
            D3D11Device = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware, flags);
            this.D3D11Context = D3D11Device.ImmediateContext;
            this.queryForCompletion = new SharpDX.Direct3D11.Query(D3D11Device, new QueryDescription { Type = SharpDX.Direct3D11.QueryType.Event, Flags = QueryFlags.None });

            this.OnCreateResources(this, new EventArgs());

            this.AreBuffersLoaded = CreateInternalBuffers();
        }

        private void PreviewElement_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.AreBuffersLoaded)
            {
                DisposeInternalBuffers();
            }

            this.OnDisposeResources(this, new EventArgs());

            this.D3D9Device.Dispose();
            this.D3D9.Dispose();
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            float timeStep = 0.0f;
            if (UpdateStopwatch == null)
            {
                UpdateStopwatch = new System.Diagnostics.Stopwatch();
            }
            else
            {
                timeStep = (float)UpdateStopwatch.Elapsed.TotalSeconds;
            }
            UpdateStopwatch.Restart();
            this.OnUpdateContent(this, new UpdateContentEventArgs(timeStep));

            this.OnRenderContent(this, new EventArgs());

            this.D3D11Context.Flush();
            this.D3D11Context.End(queryForCompletion);

            SharpDX.Mathematics.Interop.RawBool completed;
            while (!(D3D11Context.GetData(queryForCompletion, out completed)
                   && completed)) System.Threading.Thread.Yield();

            Image.InvalidateRendering();
        }

        private bool CreateInternalBuffers()
        {
            if (this.RenderSize.Width <= 0 || this.RenderSize.Height <= 0)
            {
                return false;
            }

            this.D3D11Backbuffer = new Texture2D(this.D3D11Device, new Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Height = (int)this.RenderSize.Height,
                Width = (int)this.RenderSize.Width,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.Shared,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            });
            this.D3D11BackbufferRTV = new RenderTargetView(this.D3D11Device, this.D3D11Backbuffer);
            this.D3D11Context.ClearRenderTargetView(this.D3D11BackbufferRTV, new SharpDX.Mathematics.Interop.RawColor4(0.0f, 0.0f, 0.0f, 1.0f));
            this.Image = new D3D11Image(this.D3D9Device, this.D3D11Backbuffer);

            this.OnCreateBuffers(this, new EventArgs());

            this.InvalidateVisual();
            CompositionTarget.Rendering += CompositionTarget_Rendering;
            this.SizeChanged += PreviewElement_SizeChanged;

            return true;
        }

        private void DisposeInternalBuffers()
        {
            this.SizeChanged -= PreviewElement_SizeChanged;
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            UpdateStopwatch = null;

            this.OnDisposeBuffers(this, new EventArgs());

            this.Image.Dispose();
            this.D3D11BackbufferRTV.Dispose();
            this.D3D11Backbuffer.Dispose();
        }

        private void PreviewElement_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                if (this.AreBuffersLoaded)
                {
                    this.DisposeInternalBuffers();
                }

                this.AreBuffersLoaded = this.CreateInternalBuffers();

            }
        }

        public event EventHandler CreateResources;
        protected virtual void OnCreateResources(object sender, EventArgs e)
        {
            this.CreateResources?.Invoke(sender, e);
        }

        public event EventHandler CreateBuffers;
        protected virtual void OnCreateBuffers(object sender, EventArgs e)
        {
            this.CreateBuffers?.Invoke(sender, e);
        }

        public event EventHandler RenderContent;
        protected virtual void OnRenderContent(object sender, EventArgs e)
        {
            this.RenderContent?.Invoke(sender, e);
        }

        public event EventHandler<UpdateContentEventArgs> UpdateContent;
        protected virtual void OnUpdateContent(object sender, UpdateContentEventArgs e)
        {
            this.UpdateContent?.Invoke(sender, e);
        }

        public event EventHandler DisposeBuffers;
        protected virtual void OnDisposeBuffers(object sender, EventArgs e)
        {
            this.DisposeBuffers?.Invoke(sender, e);
        }

        public event EventHandler DisposeResources;
        protected virtual void OnDisposeResources(object sender, EventArgs e)
        {
            this.DisposeResources?.Invoke(sender, e);
        }
    }
}
