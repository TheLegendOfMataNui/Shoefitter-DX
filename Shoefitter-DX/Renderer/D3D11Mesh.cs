using System.Collections.Generic;
using System.Text;
using SharpDX;
using SharpDX.Direct3D11;

namespace ShoefitterDX.Renderer
{
    public sealed class D3D11Mesh : System.IDisposable
    {
        public SharpDX.Direct3D.PrimitiveTopology PrimitiveTopology { get; }
        public uint VertexSize { get; }
        public uint IndexSize { get; }
        public uint VertexCount { get; }
        public uint IndexCount { get; }
        public Buffer VertexBuffer { get; private set; }
        public Buffer IndexBuffer { get; private set; }

        public D3D11Mesh(Device device, uint vertexSize, uint indexSize, uint vertexCount, uint indexCount, byte[] vertexData, byte[] indexData, SharpDX.Direct3D.PrimitiveTopology primitiveTopology)
        {
            PrimitiveTopology = primitiveTopology;
            VertexSize = vertexSize;
            IndexSize = indexSize;
            VertexCount = vertexCount;
            IndexCount = indexCount;
            VertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertexData);
            IndexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, indexData);
        }

        public D3D11Mesh(uint vertexSize, uint indexSize, uint vertexCount, uint indexCount, Buffer vertexBuffer, Buffer indexBuffer, SharpDX.Direct3D.PrimitiveTopology primitiveTopology)
        {
            VertexSize = vertexSize;
            IndexSize = indexSize;
            VertexCount = vertexCount;
            IndexCount = indexCount;
            VertexBuffer = vertexBuffer;
            IndexBuffer = indexBuffer;
            PrimitiveTopology = primitiveTopology;
        }

        public static D3D11Mesh Create<V, I>(Device device, V[] vertices, I[] indices, SharpDX.Direct3D.PrimitiveTopology primitiveTopology) where V : struct where I : struct
        {
            return new D3D11Mesh((uint)Utilities.SizeOf<V>(), (uint)Utilities.SizeOf<I>(), (uint)vertices.Length, (uint)indices.Length, Buffer.Create<V>(device, BindFlags.VertexBuffer, vertices), Buffer.Create<I>(device, BindFlags.IndexBuffer, indices), primitiveTopology);
        }

        public void Dispose()
        {
            VertexBuffer.Dispose();
            VertexBuffer = null;
            IndexBuffer.Dispose();
            IndexBuffer = null;
        }

        public static D3D11Mesh CreateGrid(Device device, int size, Vector4 color)
        {
            List<PreviewVertex> vertices = new List<PreviewVertex>();
            List<uint> indices = new List<uint>();

            for (int k = -size; k <= size; k++)
            {
                indices.Add((uint)vertices.Count);
                indices.Add((uint)vertices.Count + 1);
                indices.Add((uint)vertices.Count + 2);
                indices.Add((uint)vertices.Count + 3);
                vertices.Add(new PreviewVertex(new Vector3(-size, 0, k), Vector3.Up, Vector2.Zero, k == 0 ? new Vector4(0.5f, 0.0f, 0.0f, 1.0f) : color));
                vertices.Add(new PreviewVertex(new Vector3(size, 0, k), Vector3.Up, Vector2.Zero, k == 0 ? new Vector4(0.5f, 0.0f, 0.0f, 1.0f) : color));
                vertices.Add(new PreviewVertex(new Vector3(k, 0, -size), Vector3.Up, Vector2.Zero, k == 0 ? new Vector4(0.0f, 0.0f, 0.5f, 1.0f) : color));
                vertices.Add(new PreviewVertex(new Vector3(k, 0, size), Vector3.Up, Vector2.Zero, k == 0 ? new Vector4(0.0f, 0.0f, 0.5f, 1.0f) : color));
            }

            return D3D11Mesh.Create(device, vertices.ToArray(), indices.ToArray(), SharpDX.Direct3D.PrimitiveTopology.LineList);
        }

        private static readonly Vector4 Red = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        private static readonly Vector4 Green = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        private static readonly Vector4 Blue = new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
        public static D3D11Mesh CreateAxes(Device device)
        {
            
            return D3D11Mesh.Create(device, new PreviewVertex[]
            {
                new PreviewVertex(Vector3.Zero, Vector3.Zero, Vector2.Zero, Red),
                new PreviewVertex(Vector3.UnitX, Vector3.Zero, Vector2.Zero, Red),
                new PreviewVertex(Vector3.Zero, Vector3.Zero, Vector2.Zero, Green),
                new PreviewVertex(Vector3.UnitY, Vector3.Zero, Vector2.Zero, Green),
                new PreviewVertex(Vector3.Zero, Vector3.Zero, Vector2.Zero, Blue),
                new PreviewVertex(Vector3.UnitZ, Vector3.Zero, Vector2.Zero, Blue)
            }, new uint[] {
                0, 1, 2, 3, 4, 5
            }, SharpDX.Direct3D.PrimitiveTopology.LineList);
        }

        public static D3D11Mesh CreateBone(Device device, Vector4 color)
        {
            return D3D11Mesh.Create(device, new PreviewVertex[]
            {
                new PreviewVertex(Vector3.Zero, Vector3.Zero, Vector2.Zero, color),
                new PreviewVertex(new Vector3(0.5f, 0.0f, 0.25f), Vector3.Zero, Vector2.Zero, color),
                new PreviewVertex(Vector3.UnitZ, Vector3.Zero, Vector2.Zero, color),
                new PreviewVertex(new Vector3(-0.5f, 0.0f, 0.25f), Vector3.Zero, Vector2.Zero, color),
                new PreviewVertex(new Vector3(0.0f, 0.5f, 0.25f), Vector3.Zero, Vector2.Zero, color),
                new PreviewVertex(new Vector3(0.0f, -0.5f, 0.25f), Vector3.Zero, Vector2.Zero, color)
            }, new uint[] {
                0, 1, 2, 3, 0, 4, 2, 5, 0
            }, SharpDX.Direct3D.PrimitiveTopology.LineStrip);
        }

        public static D3D11Mesh CreateFullscreenQuad(Device device)
        {
            List<PreviewVertex> vertices = new List<PreviewVertex>
            {
                new PreviewVertex(new Vector3(-1.0f, 1.0f, 0), Vector3.Zero, new Vector2(0.0f, 0.0f), Vector4.One),
                new PreviewVertex(new Vector3(1.0f, 1.0f, 0), Vector3.Zero, new Vector2(1.0f, 0.0f), Vector4.One),
                new PreviewVertex(new Vector3(-1.0f, -1.0f, 0), Vector3.Zero, new Vector2(0.0f, 1.0f), Vector4.One),
                new PreviewVertex(new Vector3(1.0f, -1.0f, 0), Vector3.Zero, new Vector2(1.0f, 1.0f), Vector4.One)
            };
            List<uint> indices = new List<uint> { 0, 1, 2, 3 };

            return D3D11Mesh.Create(device, vertices.ToArray(), indices.ToArray(), SharpDX.Direct3D.PrimitiveTopology.TriangleStrip);
        }

        public static D3D11Mesh CreateCone(Device device)
        {
            // Start with the center of the cap and the point
            List<PreviewVertex> vertices = new List<PreviewVertex>
            {
                new PreviewVertex(Vector3.Zero, -Vector3.UnitZ, Vector2.Zero, Vector4.One),
                new PreviewVertex(Vector3.UnitZ, Vector3.UnitZ, Vector2.Zero, Vector4.One),
            };
            List<uint> indices = new List<uint>();

            for (int i = 0; i <= CIRCLE_DIVISIONS; i++)
            {
                float angle = i * 2.0f * MathUtil.Pi / CIRCLE_DIVISIONS;
                vertices.Add(new PreviewVertex(new Vector3((float)System.Math.Cos(angle), (float)System.Math.Sin(angle), 0.0f), Vector3.Zero, Vector2.Zero, Vector4.One));
                indices.Add(0);
                indices.Add((uint)vertices.Count - 1);
                indices.Add((uint)vertices.Count - 2);
                indices.Add(1);
                indices.Add((uint)vertices.Count - 2);
                indices.Add((uint)vertices.Count - 1);
            }

            return D3D11Mesh.Create(device, vertices.ToArray(), indices.ToArray(), SharpDX.Direct3D.PrimitiveTopology.TriangleList);
        }

        private const int CIRCLE_DIVISIONS = 16;
        public static D3D11Mesh CreateCircle(Device device, Vector3 right, Vector3 up, Vector4 color)
        {
            List<PreviewVertex> vertices = new List<PreviewVertex>();
            List<uint> indices = new List<uint>();
            for (int i = 0; i < CIRCLE_DIVISIONS; i++)
            {
                float angle = i * 2.0f * MathUtil.Pi / CIRCLE_DIVISIONS;
                vertices.Add(new PreviewVertex(right * (float)System.Math.Cos(angle) + up * (float)System.Math.Sin(angle), Vector3.Zero, Vector2.Zero, color));
                indices.Add((uint)indices.Count);
            }
            indices.Add(0);
            return D3D11Mesh.Create(device, vertices.ToArray(), indices.ToArray(), SharpDX.Direct3D.PrimitiveTopology.LineStrip);
        }

        public static D3D11Mesh CreateCylinder(Device device, Vector3 circleX, Vector3 circleY, Vector3 halfHeight, Vector4 color)
        {
            List<PreviewVertex> vertices = new List<PreviewVertex>();
            List<uint> indices = new List<uint>();
            for (int i = 0; i < CIRCLE_DIVISIONS; i++)
            {
                float angle = i * 2.0f * MathUtil.Pi / CIRCLE_DIVISIONS;
                vertices.Add(new PreviewVertex(circleX * (float)System.Math.Cos(angle) + circleY * (float)System.Math.Sin(angle) + halfHeight * -1.0f, Vector3.Zero, Vector2.Zero, color));
                indices.Add((uint)vertices.Count - 1);
            }
            indices.Add(0);
            for (int i = 0; i < CIRCLE_DIVISIONS; i++)
            {
                float angle = i * 2.0f * MathUtil.Pi / CIRCLE_DIVISIONS;
                vertices.Add(new PreviewVertex(circleX * (float)System.Math.Cos(angle) + circleY * (float)System.Math.Sin(angle) + halfHeight, Vector3.Zero, Vector2.Zero, color));
                indices.Add((uint)vertices.Count - 1);
            }
            indices.Add(CIRCLE_DIVISIONS);
            for (int i = 0; i < CIRCLE_DIVISIONS; i++)
            {
                indices.Add((uint)i);
                indices.Add((uint)(i + CIRCLE_DIVISIONS));
                indices.Add((uint)(CIRCLE_DIVISIONS + (i + 1) % CIRCLE_DIVISIONS));
            }
            return D3D11Mesh.Create(device, vertices.ToArray(), indices.ToArray(), SharpDX.Direct3D.PrimitiveTopology.LineStrip);
        }
    }
}
