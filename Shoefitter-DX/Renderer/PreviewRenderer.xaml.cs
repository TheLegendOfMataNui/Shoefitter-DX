using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SharpDX;
using SharpDX.Direct3D11;

namespace ShoefitterDX.Renderer
{
    public struct PreviewVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 UV;
        public Vector4 Color;

        public PreviewVertex(Vector3 position, Vector3 normal, Vector2 uv, Vector4 color)
        {
            Position = position;
            Normal = normal;
            UV = uv;
            Color = color;
        }

        public static readonly InputElement[] InputElements =
        {
            // Members of PreviewVertex
            new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0),
            new InputElement("NORMAL", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0),
            new InputElement("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32_Float, 0),
            new InputElement("COLOR", 0, SharpDX.DXGI.Format.R32G32B32A32_Float, 0),
        };
    }

    public struct SkinnedPreviewVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 UV;
        public Vector4 Color;
        public Vector4 BoneWeights;
        public uint BoneIndices;

        public SkinnedPreviewVertex(Vector3 position, Vector3 normal, Vector2 uv, Vector4 color, Vector4 boneWeights, uint boneIndices)
        {
            Position = position;
            Normal = normal;
            UV = uv;
            Color = color;
            BoneWeights = boneWeights;
            BoneIndices = boneIndices;
        }

        public static readonly InputElement[] InputElements =
        {
            // Members of SkinnedPreviewVertex
            new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0),
            new InputElement("NORMAL", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0),
            new InputElement("TEXCOORD", 0, SharpDX.DXGI.Format.R32G32_Float, 0),
            new InputElement("COLOR", 0, SharpDX.DXGI.Format.R32G32B32A32_Float, 0),
            new InputElement("BLENDWEIGHTS", 0, SharpDX.DXGI.Format.R32G32B32A32_Float, 0),
            new InputElement("BLENDINDICES", 0, SharpDX.DXGI.Format.R8G8B8A8_UInt, 0),
        };
    }

    public struct FrameConstants
    {
        public Matrix ViewMatrix;
        public Matrix ProjectionMatrix;
        public Vector2 ViewportPosition;
        public Vector2 ViewportSize;
        public Vector3 CameraPosition;
        private float _Padding;
    }

    public struct SolidInstanceConstants
    {
        public Matrix ModelMatrix;
        public Vector4 Color;

        public SolidInstanceConstants(Matrix modelMatrix, Vector4 color)
        {
            this.ModelMatrix = modelMatrix;
            this.Color = color;
        }
    }

    public struct WorldInstanceConstants
    {
        public Matrix ModelMatrix;
        public Vector4 Color;
        public Vector3 SpecularColor;
        public float SpecularExponent;
        public Vector3 EmissiveColor;
        private float _Padding;

        public WorldInstanceConstants(Matrix modelMatrix, Vector4 color, Vector3 specularColor, float specularExponent, Vector3 emissiveColor)
        {
            this.ModelMatrix = modelMatrix;
            this.Color = color;
            this.SpecularColor = specularColor;
            this.SpecularExponent = specularExponent;
            this.EmissiveColor = emissiveColor;
            this._Padding = 0.0f;
        }
    }

    /// <summary>
    /// Interaction logic for PreviewRenderer.xaml
    /// </summary>
    public partial class PreviewRenderer : UserControl
    {
        public Device Device => D3D11.D3D11Device;
        public DeviceContext ImmediateContext => D3D11.D3D11Context;
        public Texture2D Backbuffer => D3D11.D3D11Backbuffer;
        public RenderTargetView BackbufferRTV => D3D11.D3D11BackbufferRTV;
        public Texture2D BackbufferMSAA { get; private set; }
        public RenderTargetView BackbufferMSAARTV { get; private set; }
        public Texture2D Depthbuffer { get; private set; }
        public DepthStencilView DepthbufferDSV { get; private set; }
        public float ViewportWidth => (float)D3D11.RenderSize.Width;
        public float ViewportHeight => (float)D3D11.RenderSize.Height;
        public int MSAACount { get; private set; }
        public int MSAAQuality { get; private set; }
        public bool RenderDocCapture { get; set; }

        public VertexShader WorldVertexShader { get; private set; }
        public PixelShader WorldPixelShader { get; private set; }
        public PixelShader SolidPixelShader { get; private set; }
        public VertexShader SkinnedWorldVertexShader { get; private set; }
        public InputLayout WorldInputLayout { get; private set; }
        public InputLayout SkinnedWorldInputLayout { get; private set; }
        public RasterizerState DefaultRasterizerState { get; private set; }
        public BlendState AlphaBlendState { get; private set; }
        public DepthStencilState DefaultDepthStencilState { get; private set; }
        public DepthStencilState AlwaysDepthStencilState { get; private set; }
        private FrameConstants FrameConstants;
        public SharpDX.Direct3D11.Buffer FrameConstantBuffer { get; private set; }
        public SharpDX.Direct3D11.Buffer SolidInstanceConstantBuffer { get; private set; }
        public SharpDX.Direct3D11.Buffer WorldInstanceConstantBuffer { get; private set; }
        public SharpDX.Direct3D11.Buffer SkeletonPoseBuffer { get; private set; }

        private bool _isFirstPerson = false;
        public bool IsFirstPerson
        {
            get => _isFirstPerson;
            set
            {
                _isFirstPerson = value;
                Camera.FirstPerson = value;
                //ViewModeObritImage.Visibility = _isFirstPerson ? Visibility.Collapsed : Visibility.Visible;
                //ViewModeWASDImage.Visibility = _isFirstPerson ? Visibility.Visible : Visibility.Collapsed;
                this.Controller = value ? (CameraController)new FirstPersonCameraController(Camera) : new OrbitCameraController(Camera);
            }
        }
        public bool ShowGrid { get; set; } = true;
        public D3D11Mesh GridMesh { get; private set; }

        public PreviewCamera Camera { get; } = new PreviewCamera(new Vector3(0.0f, 0.0f, 0.0f));
        private CameraController Controller;

        public PreviewRenderer()
        {
            InitializeComponent();

            this.IsFirstPerson = false;
            this.RenderDocCaptureButton.Visibility = RenderDoc.IsRenderDocAttached() ? Visibility.Visible : Visibility.Collapsed;

            D3D11.CreateResources += D3D11_CreateResources;
            D3D11.CreateBuffers += D3D11_CreateBuffers;
            D3D11.RenderContent += D3D11_RenderContent;
            D3D11.UpdateContent += D3D11_UpdateContent;
            D3D11.DisposeBuffers += D3D11_DisposeBuffers;
            D3D11.DisposeResources += D3D11_DisposeResources;
            D3D11.PreviewKeyDown += D3D11_PreviewKeyDown;
            D3D11.PreviewKeyUp += D3D11_PreviewKeyUp;
            D3D11.PreviewMouseDown += D3D11_PreviewMouseDown;
            D3D11.PreviewMouseUp += D3D11_PreviewMouseUp;
            D3D11.PreviewMouseMove += D3D11_PreviewMouseMove;
            D3D11.PreviewMouseWheel += D3D11_PreviewMouseWheel;
        }

        public event EventHandler CreateResources;
        private void D3D11_CreateResources(object sender, EventArgs e)
        {
            this.MSAACount = 4; // 4 is required to be supported for all formats
            this.MSAAQuality = Device.CheckMultisampleQualityLevels(SharpDX.DXGI.Format.B8G8R8A8_UNorm, this.MSAACount) - 1;

            WorldVertexShader = new VertexShader(Device, ResourceCache.Resources["WorldVertexShader.hlsl"]);
            WorldPixelShader = new PixelShader(Device, ResourceCache.Resources["WorldPixelShader.hlsl"]);
            SolidPixelShader = new PixelShader(Device, ResourceCache.Resources["SolidPixelShader.hlsl"]);
            SkinnedWorldVertexShader = new VertexShader(Device, ResourceCache.Resources["SkinnedWorldVertexShader.hlsl"]);
            WorldInputLayout = new InputLayout(Device, ResourceCache.Resources["WorldVertexShader.hlsl"], PreviewVertex.InputElements);
            SkinnedWorldInputLayout = new InputLayout(Device, ResourceCache.Resources["SkinnedWorldVertexShader.hlsl"], SkinnedPreviewVertex.InputElements);

            FrameConstantBuffer = new SharpDX.Direct3D11.Buffer(Device, SharpDX.Utilities.SizeOf<FrameConstants>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            SolidInstanceConstantBuffer = new SharpDX.Direct3D11.Buffer(Device, SharpDX.Utilities.SizeOf<SolidInstanceConstants>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            WorldInstanceConstantBuffer = new SharpDX.Direct3D11.Buffer(Device, Utilities.SizeOf<WorldInstanceConstants>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            SkeletonPoseBuffer = new SharpDX.Direct3D11.Buffer(Device, Utilities.SizeOf<Matrix>() * 255, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);

            DefaultRasterizerState = new RasterizerState(Device, new RasterizerStateDescription()
            {
                CullMode = CullMode.Back,
                IsDepthClipEnabled = false,
                FillMode = FillMode.Solid,
                IsFrontCounterClockwise = false,
            });
            ImmediateContext.Rasterizer.State = DefaultRasterizerState;

            BlendStateDescription bsdesc = BlendStateDescription.Default();
            bsdesc.RenderTarget[0].IsBlendEnabled = true;
            bsdesc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            bsdesc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            bsdesc.RenderTarget[0].SourceAlphaBlend = BlendOption.InverseDestinationAlpha;
            bsdesc.RenderTarget[0].DestinationAlphaBlend = BlendOption.One;
            bsdesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            bsdesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
            bsdesc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            AlphaBlendState = new BlendState(Device, bsdesc);
            ImmediateContext.OutputMerger.BlendState = AlphaBlendState;

            DefaultDepthStencilState = new DepthStencilState(Device, new DepthStencilStateDescription() { DepthComparison = Comparison.Less, DepthWriteMask = DepthWriteMask.All, IsDepthEnabled = true });
            ImmediateContext.OutputMerger.DepthStencilState = DefaultDepthStencilState;
            AlwaysDepthStencilState = new DepthStencilState(Device, new DepthStencilStateDescription() { DepthComparison = Comparison.Always, DepthWriteMask = DepthWriteMask.All, IsDepthEnabled = false });

            GridMesh = D3D11Mesh.CreateGrid(Device, 10, new Vector4(0.7f, 0.7f, 0.7f, 0.5f));

            D3D11.Focus();

            CreateResources?.Invoke(sender, e);
        }

        public event EventHandler CreateBuffers;
        private void D3D11_CreateBuffers(object sender, EventArgs e)
        {
            ImmediateContext.Rasterizer.SetViewport(0, 0, ViewportWidth, ViewportHeight);
            Camera.AspectRatio = ViewportWidth / ViewportHeight;

            BackbufferMSAA = new Texture2D(Device, new Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Height = (int)ViewportHeight,
                Width = (int)ViewportWidth,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(MSAACount, MSAAQuality),
                Usage = ResourceUsage.Default
            });
            BackbufferMSAARTV = new RenderTargetView(Device, BackbufferMSAA);

            Depthbuffer = new Texture2D(Device, new Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = SharpDX.DXGI.Format.R24G8_Typeless,
                Width = (int)ViewportWidth,
                Height = (int)ViewportHeight,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(MSAACount, MSAAQuality),
                Usage = ResourceUsage.Default
            });
            DepthbufferDSV = new DepthStencilView(Device, Depthbuffer, new DepthStencilViewDescription()
            {
                Dimension = DepthStencilViewDimension.Texture2DMultisampled,
                Format = SharpDX.DXGI.Format.D24_UNorm_S8_UInt,
                Texture2D = new DepthStencilViewDescription.Texture2DResource()
                {
                    MipSlice = 0
                }
            });

            CreateBuffers?.Invoke(sender, e);
        }

        public event EventHandler RenderContent;
        private void D3D11_RenderContent(object sender, EventArgs e)
        {
            bool isCapturing = false;
            if (RenderDocCapture)
            {
                isCapturing = true;
                RenderDocCapture = false;

                RenderDoc.StartCapture();
            }

            // Update the frame constants buffer
            FrameConstants.ViewMatrix = Camera.ViewMatrix;
            FrameConstants.ProjectionMatrix = Camera.ProjectionMatrix;
            FrameConstants.ViewportPosition = Vector2.Zero;
            FrameConstants.ViewportSize = Vector2.One;
            Matrix invertedView = Camera.ViewMatrix;
            invertedView.Invert();
            FrameConstants.CameraPosition = invertedView.TranslationVector;
            ImmediateContext.UpdateSubresource(ref FrameConstants, FrameConstantBuffer);

            ImmediateContext.OutputMerger.SetRenderTargets(DepthbufferDSV, BackbufferMSAARTV);

            // Clear the backbuffer
            ImmediateContext.ClearRenderTargetView(BackbufferMSAARTV, new SharpDX.Mathematics.Interop.RawColor4(0.1f, 0.1f, 0.1f, 1.0f));
            ImmediateContext.ClearDepthStencilView(DepthbufferDSV, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0x00);

            // Render the content!
            RenderContent?.Invoke(sender, e);

            // Render the grid, if enabled
            if (ShowGrid)
            {
                RenderSolidMesh(GridMesh, new SolidInstanceConstants(Matrix.Identity, Vector4.One));
            }

            // Copy the antialiased image to the non-antialiased backbuffer
            ImmediateContext.ResolveSubresource(BackbufferMSAA, 0, Backbuffer, 0, SharpDX.DXGI.Format.B8G8R8A8_UNorm);

            if (isCapturing)
            {
                RenderDoc.EndCapture();
            }
        }

        public event EventHandler<UpdateContentEventArgs> UpdateContent;
        private void D3D11_UpdateContent(object sender, UpdateContentEventArgs e)
        {
            Controller?.Update(e.TimeStep);
            UpdateContent?.Invoke(sender, e);
        }

        public event EventHandler DisposeBuffers;
        private void D3D11_DisposeBuffers(object sender, EventArgs e)
        {
            DepthbufferDSV?.Dispose();
            DepthbufferDSV = null;

            Depthbuffer?.Dispose();
            Depthbuffer = null;

            BackbufferMSAARTV?.Dispose();
            BackbufferMSAARTV = null;

            BackbufferMSAA?.Dispose();
            BackbufferMSAA = null;

            DisposeBuffers?.Invoke(sender, e);
        }

        public event EventHandler DisposeResources;
        private void D3D11_DisposeResources(object sender, EventArgs e)
        {
            DisposeResources?.Invoke(sender, e);

            // TODO: Figure out why we are double-disposing sometimes here!
            GridMesh?.Dispose();
            GridMesh = null;

            AlwaysDepthStencilState?.Dispose();
            AlwaysDepthStencilState = null;
            DefaultDepthStencilState?.Dispose();
            DefaultDepthStencilState = null;
            AlphaBlendState?.Dispose();
            AlphaBlendState = null;
            DefaultRasterizerState?.Dispose();
            DefaultRasterizerState = null;

            SkeletonPoseBuffer?.Dispose();
            SkeletonPoseBuffer = null;
            WorldInstanceConstantBuffer?.Dispose();
            WorldInstanceConstantBuffer = null;
            SolidInstanceConstantBuffer?.Dispose();
            SolidInstanceConstantBuffer = null;
            FrameConstantBuffer?.Dispose();
            FrameConstantBuffer = null;

            SkinnedWorldInputLayout?.Dispose();
            SkinnedWorldInputLayout = null;
            WorldInputLayout?.Dispose();
            WorldInputLayout = null;
            SkinnedWorldVertexShader?.Dispose();
            SkinnedWorldVertexShader = null;
            SolidPixelShader?.Dispose();
            SolidPixelShader = null;
            WorldPixelShader?.Dispose();
            WorldPixelShader = null;
            WorldVertexShader?.Dispose();
            WorldVertexShader = null;
        }

        public void RenderSolidMesh(D3D11Mesh mesh, SolidInstanceConstants instanceConstants)
        {
            ImmediateContext.MapSubresource(SolidInstanceConstantBuffer, MapMode.WriteDiscard, MapFlags.None, out DataStream stream);
            stream.Write(instanceConstants);
            ImmediateContext.UnmapSubresource(SolidInstanceConstantBuffer, 0);

            this.RenderMesh(mesh, SolidInstanceConstantBuffer, WorldVertexShader, SolidPixelShader, WorldInputLayout);
        }

        public void RenderWorldMesh(D3D11Mesh mesh, WorldInstanceConstants instanceConstants, ShaderResourceView diffuseTexture)
        {
            ImmediateContext.MapSubresource(WorldInstanceConstantBuffer, MapMode.WriteDiscard, MapFlags.None, out DataStream stream);
            stream.Write(instanceConstants);
            ImmediateContext.UnmapSubresource(WorldInstanceConstantBuffer, 0);

            ImmediateContext.PixelShader.SetShaderResource(0, diffuseTexture);

            this.RenderMesh(mesh, WorldInstanceConstantBuffer, WorldVertexShader, WorldPixelShader, WorldInputLayout);
        }

        public void RenderSkinnedWorldMesh(D3D11Mesh mesh, WorldInstanceConstants instanceConstants, ShaderResourceView diffuseTexture, SharpDX.Direct3D11.Buffer bindPoseBufer, Matrix[] skeletonPose)
        {
            ImmediateContext.MapSubresource(SkeletonPoseBuffer, MapMode.WriteDiscard, MapFlags.None, out DataStream poseStream);
            //stream.Write(skeletonPose);
            foreach (Matrix m in skeletonPose)
            {
                poseStream.Write(m);
            }
            ImmediateContext.UnmapSubresource(SkeletonPoseBuffer, 0);
            ImmediateContext.VertexShader.SetConstantBuffer(2, bindPoseBufer);
            ImmediateContext.VertexShader.SetConstantBuffer(3, SkeletonPoseBuffer);

            ImmediateContext.MapSubresource(WorldInstanceConstantBuffer, MapMode.WriteDiscard, MapFlags.None, out DataStream instanceStream);
            instanceStream.Write(instanceConstants);
            ImmediateContext.UnmapSubresource(WorldInstanceConstantBuffer, 0);

            ImmediateContext.PixelShader.SetShaderResource(0, diffuseTexture);

            this.RenderMesh(mesh, WorldInstanceConstantBuffer, SkinnedWorldVertexShader, WorldPixelShader, SkinnedWorldInputLayout);
        }

        public void RenderMesh(D3D11Mesh mesh, SharpDX.Direct3D11.Buffer constantBuffer, VertexShader vs, PixelShader ps, InputLayout inputLayout)
        {
            ImmediateContext.VertexShader.Set(vs);
            ImmediateContext.PixelShader.Set(ps);
            ImmediateContext.InputAssembler.InputLayout = inputLayout;
            ImmediateContext.VertexShader.SetConstantBuffer(0, FrameConstantBuffer);
            ImmediateContext.VertexShader.SetConstantBuffer(1, constantBuffer);
            ImmediateContext.PixelShader.SetConstantBuffer(0, FrameConstantBuffer);
            ImmediateContext.PixelShader.SetConstantBuffer(1, constantBuffer);

            ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding[]
            {
                    new VertexBufferBinding(mesh.VertexBuffer, (int)mesh.VertexSize, 0),
            });
            SharpDX.DXGI.Format indexFormat = SharpDX.DXGI.Format.Unknown;
            if (mesh.IndexSize == 1)
                indexFormat = SharpDX.DXGI.Format.R8_UInt;
            else if (mesh.IndexSize == 2)
                indexFormat = SharpDX.DXGI.Format.R16_UInt;
            else if (mesh.IndexSize == 4)
                indexFormat = SharpDX.DXGI.Format.R32_UInt;
            else
                throw new Exception("Invalid mesh index size!");
            ImmediateContext.InputAssembler.SetIndexBuffer(mesh.IndexBuffer, indexFormat, 0);
            ImmediateContext.InputAssembler.PrimitiveTopology = mesh.PrimitiveTopology;

            ImmediateContext.DrawIndexed((int)mesh.IndexCount, 0, 0);
        }

        [Flags()]
        private enum DDSPixelFormatFlags : uint
        {
            AlphaPixels = 0x1,
            Alpha = 0x2,
            FourCC = 0x4,
            RGB = 0x40,
            YUV = 0x200,
            Luminance = 0x20000,
        }

        private const uint FourCCDXT1 = 0x31545844;
        private const uint FourCCDXT3 = 0x33545844;
        private const uint FourCCDXT5 = 0x35545844;
        private const uint FourCCDX10 = 0x30315844;

        public Texture2D LoadTextureFromDDS(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                // Magic
                uint magic = reader.ReadUInt32();
                if (magic != 0x20534444)
                    throw new FileFormatException("Incorrect DDS file magic!");

                // DDS Header
                uint headerSize = reader.ReadUInt32();
                uint flags = reader.ReadUInt32();
                uint height = reader.ReadUInt32();
                uint width = reader.ReadUInt32();
                uint pitchOrLinearSize = reader.ReadUInt32();
                uint depth = reader.ReadUInt32();
                uint mipmapCount = reader.ReadUInt32();
                stream.Seek(sizeof(uint) * 11, SeekOrigin.Current); // Reserved1

                // Pixel Format
                uint pixelFormatSize = reader.ReadUInt32();
                DDSPixelFormatFlags pixelFormatFlags = (DDSPixelFormatFlags)reader.ReadUInt32();
                uint pixelFormatFourCC = reader.ReadUInt32();
                uint pixelFormatRGBBitCount = reader.ReadUInt32();
                uint pixelFormatRBitmask = reader.ReadUInt32();
                uint pixelFormatGBitmask = reader.ReadUInt32();
                uint pixelFormatBBitmask = reader.ReadUInt32();
                uint pixelFormatABitmask = reader.ReadUInt32();

                // Caps
                uint caps = reader.ReadUInt32();
                uint caps2 = reader.ReadUInt32();
                uint caps3 = reader.ReadUInt32();
                uint caps4 = reader.ReadUInt32();
                stream.Seek(sizeof(uint), SeekOrigin.Current); // Reserved2

                // We only support legacy compressed DDS (BC1 (DXT1), BC2 (DXT3), BC3 (DXT5)) or new DX10+ dds
                if (!pixelFormatFlags.HasFlag(DDSPixelFormatFlags.FourCC))
                    throw new FileFormatException("Only DDS files with a FourCC format specified are supported.");

                SharpDX.DXGI.Format pixelFormat = SharpDX.DXGI.Format.Unknown;
                int pixelDataLength = 0;
                if (pixelFormatFourCC == FourCCDXT1)
                {
                    pixelFormat = SharpDX.DXGI.Format.BC1_UNorm;
                    pixelDataLength = (int)(width * height / 2);
                }
                else if (pixelFormatFourCC == FourCCDXT3)
                {
                    pixelFormat = SharpDX.DXGI.Format.BC2_UNorm;
                    pixelDataLength = (int)(width * height);
                }
                else if (pixelFormatFourCC == FourCCDXT5)
                {
                    pixelFormat = SharpDX.DXGI.Format.BC3_UNorm;
                    pixelDataLength = (int)(width * height);
                }
                else if (pixelFormatFourCC == FourCCDX10)
                {
                    // DDS Extended Header (DX10+)
                    pixelFormat = (SharpDX.DXGI.Format)reader.ReadUInt32();
                    pixelDataLength = (int)(SharpDX.DXGI.FormatHelper.SizeOfInBits(pixelFormat) * width * height / 8);
                    ResourceDimension dimension = (ResourceDimension)reader.ReadUInt32();
                    uint miscFlags = reader.ReadUInt32();
                    uint arraySize = reader.ReadUInt32();
                    uint miscFalgs2 = reader.ReadUInt32();
                }
                else
                {
                    throw new FileFormatException("Only DDS files with a FourCC of DXT1, DXT3, DXT5, or DX10 are supported.");
                }
                byte[] pixelData = reader.ReadBytes(pixelDataLength);
                System.IntPtr texdata = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(pixelData, 0);
                return new Texture2D(Device, new Texture2DDescription() { ArraySize = 1, BindFlags = BindFlags.ShaderResource, CpuAccessFlags = CpuAccessFlags.None, Format = pixelFormat, Width = (int)width, Height = (int)height, MipLevels = 1, Usage = ResourceUsage.Immutable, SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0) },
                    new DataBox[] { new DataBox(texdata, (int)(width * SharpDX.DXGI.FormatHelper.SizeOfInBits(pixelFormat) / 8 * (SharpDX.DXGI.FormatHelper.IsCompressed(pixelFormat) ? 4 : 1)), 0) });
            }
        }

        public Texture2D LoadTextureFromTGA(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                // Read TGA header
                byte idLength = reader.ReadByte();
                byte colorMapType = reader.ReadByte();
                byte dataTypeCode = reader.ReadByte();
                ushort colorMapOrigin = reader.ReadUInt16();
                ushort colorMapLength = reader.ReadUInt16();
                byte colorMapBitDepth = reader.ReadByte();
                ushort xOrigin = reader.ReadUInt16();
                ushort yOrigin = reader.ReadUInt16();
                ushort width = reader.ReadUInt16();
                ushort height = reader.ReadUInt16();
                byte bitDepth = reader.ReadByte();
                byte imageDescriptor = reader.ReadByte();
                string id = "";
                if (idLength > 0)
                    id = Encoding.ASCII.GetString(reader.ReadBytes(idLength));

                if (bitDepth != 32)
                    throw new System.BadImageFormatException("TGAs need to be 32bit!");

                if (dataTypeCode == 1)
                {
                    throw new System.BadImageFormatException("No paletted TGAs!");
                }
                else if (dataTypeCode == 2)
                {
                    if (colorMapLength > 0)
                        stream.Seek(colorMapLength, SeekOrigin.Current);

                    byte[] pixelData = reader.ReadBytes(width * height * 4);
                    System.IntPtr texdata = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(pixelData, 0);
                    return new Texture2D(Device, new Texture2DDescription() { ArraySize = 1, BindFlags = BindFlags.ShaderResource, CpuAccessFlags = CpuAccessFlags.None, Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm, Width = width, Height = height, MipLevels = 1, Usage = ResourceUsage.Immutable, SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0) },
                        new DataBox[] { new DataBox(texdata, 4 * width, 0) });
                }
                else
                {
                    throw new System.BadImageFormatException("Bad TGA format!");
                }
            }
        }

        #region Camera Control
        private void D3D11_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = Controller?.MouseWheel(e.Delta) ?? false;
        }

        private void D3D11_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = Controller?.MouseMove((float)e.GetPosition(this).X / ViewportWidth, (float)e.GetPosition(this).Y / ViewportHeight) ?? false;
        }

        private void D3D11_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = Controller?.MouseUp(e.ChangedButton) ?? false;
            if (e.Handled)
            {
                D3D11.ReleaseMouseCapture();
            }
        }

        private void D3D11_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = Controller?.MouseDown(e.ChangedButton) ?? false;
            if (e.Handled)
            {
                D3D11.CaptureMouse();
            }
        }

        private void D3D11_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = Controller?.KeyUp(e.Key == Key.System ? e.SystemKey : e.Key) ?? false;
        }

        private void D3D11_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = Controller?.KeyDown(e.Key == Key.System ? e.SystemKey : e.Key) ?? false;
        }
        #endregion

        private void RenderDocCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            RenderDocCapture = true;
        }
    }
}
