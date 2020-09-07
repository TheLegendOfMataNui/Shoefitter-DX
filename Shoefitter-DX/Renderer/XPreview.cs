using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SAGESharp;
using SharpDX;
using SharpDX.Direct3D11;

namespace ShoefitterDX.Renderer
{
    public class XPreviewSection
    {
        public D3D11Mesh Mesh { get; private set; }
        public Vector4 Color { get; }
        public float SpecularExponent { get; }
        public Vector3 SpecularColor { get; }
        public Vector3 EmissiveColor { get; }
        public ShaderResourceView DiffuseTexture { get; }

        public XPreviewSection(D3D11Mesh mesh, Vector4 color, float specularExponent, Vector3 specularColor, Vector3 emissiveColor, ShaderResourceView diffuseTexture)
        {
            this.Mesh = mesh;
            this.Color = color;
            this.SpecularExponent = specularExponent;
            this.SpecularColor = specularColor;
            this.EmissiveColor = emissiveColor;
            this.DiffuseTexture = diffuseTexture;
        }

        public void Dispose()
        {
            this.Mesh?.Dispose();
            this.Mesh = null;
        }
    }

    public class XPreview
    {
        public List<XPreviewSection> Sections { get; }

        public XPreview(IEnumerable<XPreviewSection> sections)
        {
            this.Sections = new List<XPreviewSection>(sections);
        }

        public void Dispose()
        {
            foreach (XPreviewSection section in this.Sections)
            {
                section.Dispose();
            }
            this.Sections.Clear();
        }

        public static XPreview FromXFile(Device device, SAGESharp.XFile xFile, string textureDirectory, PreviewRenderer renderer)
        {
            List<XPreviewSection> sections = new List<XPreviewSection>();
            List<List<PreviewVertex>> sectionVertices = new List<List<PreviewVertex>>();
            List<List<uint>> sectionIndices = new List<List<uint>>();

            XObject mesh = xFile.Objects[0][1].Object;

            int vertexCount = (int)mesh["nVertices"].Values[0];
            int faceCount = (int)mesh["nFaces"].Values[0];

            XObject meshNormals = null;
            XObject meshTextureCoords = null;
            XObject meshMaterialList = null;
            XObject meshVertexColors = null;

            int normalCount = 0;
            int uvCount = 0;
            int colorCount = 0;

            Vector3[] positions = null;
            Vector3[] normals = null;
            Vector2[] uvs = null;
            Vector4[] colors = null;

            foreach (XChildObject child in mesh.Children)
            {
                if (child.Object.DataType.NameData == "MeshNormals")
                {
                    meshNormals = child.Object;
                    normalCount = (int)meshNormals["nNormals"].Values[0];
                    normals = new Vector3[normalCount];
                    for (int i = 0; i < normalCount; i++)
                    {
                        normals[i] = XUtils.Vector((XObjectStructure)meshNormals["normals"].Values[i]);
                    }
                }
                else if (child.Object.DataType.NameData == "MeshTextureCoords")
                {
                    meshTextureCoords = child.Object;
                    uvCount = (int)meshTextureCoords["nTextureCoords"].Values[0];
                    if (uvCount != vertexCount) throw new NotSupportedException("Oh no!");
                    uvs = new Vector2[uvCount];
                    for (int i = 0; i < uvCount; i++)
                    {
                        uvs[i] = XUtils.TexCoord((XObjectStructure)meshTextureCoords["textureCoords"].Values[i]);
                    }
                }
                else if (child.Object.DataType.NameData == "MeshMaterialList")
                {
                    meshMaterialList = child.Object;
                }
                else if (child.Object.DataType.NameData == "MeshVertexColors")
                {
                    meshVertexColors = child.Object;
                    colorCount = (int)meshVertexColors["nVertexColors"].Values[0];
                    if (colorCount != vertexCount) throw new NotSupportedException("Oh no!");
                    colors = new Vector4[colorCount];
                    for (int i = 0; i < colorCount; i++)
                    {
                        int index = (int)((XObjectStructure)meshVertexColors["vertexColors"].Values[i])["index"].Values[0];
                        colors[index] = XUtils.ColorRGBA((XObjectStructure)((XObjectStructure)meshVertexColors["vertexColors"].Values[i]).Members[1].Values[0]);
                    }
                }
            }

            int sectionCount = (int)meshMaterialList["nMaterials"].Values[0];
            for (int i = 0; i < sectionCount; i++)
            {
                sectionVertices.Add(new List<PreviewVertex>());
                sectionIndices.Add(new List<uint>());
            }

            positions = new Vector3[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                positions[i] = XUtils.Vector((XObjectStructure)mesh["vertices"].Values[i]);
            }

            for (int i = 0; i < faceCount; i++)
            {
                XObjectStructure face = (XObjectStructure)mesh["faces"].Values[i];
                XObjectStructure faceNormals = (XObjectStructure)meshNormals["faceNormals"].Values[i];
                int materialIndex = (int)meshMaterialList["faceIndexes"].Values[i];
                int indexCount = (int)face["nFaceVertexIndices"].Values[0];

                if (indexCount != 3) throw new NotSupportedException("Only 3 vertices per polygon supported!");

                for (int v = 0; v < indexCount; v++)
                {
                    int pIndex = (int)face["faceVertexIndices"].Values[v];
                    int nIndex = (int)faceNormals["faceVertexIndices"].Values[v];
                    sectionIndices[materialIndex].Add((uint)sectionVertices[materialIndex].Count);
                    sectionVertices[materialIndex].Add(new PreviewVertex(positions[pIndex], normals?[nIndex] ?? Vector3.Zero, uvs?[pIndex] ?? Vector2.One, colors?[pIndex] ?? Vector4.One));
                }

            }

            for (int i = 0; i < sectionCount; i++)
            {
                if (sectionIndices[i].Count > 0)
                {
                    XObject material = meshMaterialList[i].Object;
                    XObjectStructure faceColor = (XObjectStructure)material["faceColor"].Values[0];
                    float specExponent = (float)(double)material["power"].Values[0];
                    XObjectStructure specularColor = (XObjectStructure)material["specularColor"].Values[0];
                    XObjectStructure emissiveColor = (XObjectStructure)material["emissiveColor"].Values[0];

                    ShaderResourceView texture = null;
                    foreach (XChildObject materialChild in material.Children)
                    {
                        if (materialChild.Object.DataType.NameData == "TextureFilename")
                        {
                            string textureFilename = Path.Combine(textureDirectory, (string)materialChild.Object["filename"].Values[0]);
                            if (System.IO.File.Exists(textureFilename + ".dds"))
                            {
                                texture = new ShaderResourceView(device, renderer.LoadTextureFromDDS(textureFilename + ".dds"));
                            }
                            else if (System.IO.File.Exists(textureFilename = ".tga"))
                            {
                                texture = new ShaderResourceView(device, renderer.LoadTextureFromTGA(textureFilename + ".tga"));
                            }
                            else
                            {
                                Console.WriteLine($"[WARNING]: Couldn't find file for texture {textureFilename}!");
                            }
                        }
                    }

                    sections.Add(new XPreviewSection(D3D11Mesh.Create(device, sectionVertices[i].ToArray(), sectionIndices[i].ToArray(), SharpDX.Direct3D.PrimitiveTopology.TriangleList), XUtils.ColorRGBA(faceColor), specExponent, XUtils.ColorRGB(specularColor), XUtils.ColorRGB(emissiveColor), texture));
                }
            }

            return new XPreview(sections);
        }
    }
}
