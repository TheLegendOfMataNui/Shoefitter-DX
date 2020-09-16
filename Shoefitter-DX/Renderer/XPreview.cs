using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
        public SharpDX.Direct3D11.Buffer BindPoseBuffer { get; private set; }

        public XPreview(IEnumerable<XPreviewSection> sections, SharpDX.Direct3D11.Buffer bindPoseBuffer)
        {
            this.Sections = new List<XPreviewSection>(sections);
            this.BindPoseBuffer = bindPoseBuffer;
        }

        public void Dispose()
        {
            foreach (XPreviewSection section in this.Sections)
            {
                section.Dispose();
            }
            this.Sections.Clear();
            this.BindPoseBuffer?.Dispose();
            this.BindPoseBuffer = null;
        }

        public static XPreview FromXFile(Device device, SAGESharp.XFile xFile, string textureDirectory, PreviewRenderer renderer, out bool isBiped)
        {
            List<XPreviewSection> sections = new List<XPreviewSection>();
            List<List<SkinnedPreviewVertex>> sectionVertices = new List<List<SkinnedPreviewVertex>>();
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
            List<float>[] boneWeights = null;
            List<byte>[] boneIndices = null;
            Matrix[] boneBindPoses = null;
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
                sectionVertices.Add(new List<SkinnedPreviewVertex>());
                sectionIndices.Add(new List<uint>());
            }

            positions = new Vector3[vertexCount];
            boneWeights = new List<float>[vertexCount];
            boneIndices = new List<byte>[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                positions[i] = XUtils.Vector((XObjectStructure)mesh["vertices"].Values[i]);
                boneWeights[i] = new List<float>();
                boneIndices[i] = new List<byte>();
            }

            // Load bone weights, bone indices and bone bind poses
            XObject firstBoneInfo = mesh.Children.FirstOrDefault(ch => ch.Object.DataType.NameData == "SkinWeights")?.Object;
            bool isSkinned = firstBoneInfo != null;
            isBiped = false;
            if (firstBoneInfo != null)
            {
                if (BHDFile.BipedBoneNames.Contains(firstBoneInfo["transformNodeName"].Values[0] as string))
                    isBiped = true;

                string[] boneNames = isBiped ? BHDFile.BipedBoneNames : BHDFile.NonBipedBoneNames;

                boneBindPoses = new Matrix[boneNames.Length];

                foreach (XChildObject meshChild in mesh.Children.Where(ch => ch.Object.DataType.NameData == "SkinWeights"))
                {
                    string boneName = meshChild.Object["transformNodeName"].Values[0] as string;
                    byte boneIndex = (byte)Array.IndexOf(boneNames, boneName);
                    boneBindPoses[boneIndex] = new Matrix(Array.ConvertAll((meshChild.Object["matrixOffset"].Values[0] as XObjectStructure)["matrix"].Values.OfType<double>().ToArray(), dbl => (float)dbl));
                    int weightCount = (int)meshChild.Object["nWeights"].Values[0];

                    for (int i = 0; i < weightCount; i++)
                    {
                        int positionIndex = (int)meshChild.Object["vertexIndices"].Values[i];
                        boneWeights[positionIndex].Add((float)(double)meshChild.Object["weights"].Values[i]);
                        boneIndices[positionIndex].Add(boneIndex);
                    }
                }
            }

            // Copy data into sections, creating naive indices along the way
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

                    Vector4 localBoneWeights = Vector4.Zero;
                    uint localBoneIndices = 0;
                    int weightCount = boneIndices[pIndex].Count;
                    if (weightCount > 4)
                        throw new NotSupportedException("Too many weights on vertex!");
                    for (int weight = 0; weight < weightCount; weight++)
                    {
                        localBoneWeights[weight] = boneWeights[pIndex][weight];
                        localBoneIndices |= (uint)boneIndices[pIndex][weight] << (weight * 8);
                    }

                    sectionVertices[materialIndex].Add(new SkinnedPreviewVertex(positions[pIndex], normals?[nIndex] ?? Vector3.Zero, uvs?[pIndex] ?? Vector2.One, colors?[pIndex] ?? Vector4.One, localBoneWeights, localBoneIndices));
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

            SharpDX.Direct3D11.Buffer bindPoseBuffer = null;
            if (isSkinned)
            {
                bindPoseBuffer = new SharpDX.Direct3D11.Buffer(device, Utilities.SizeOf<Matrix>() * 255, ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
                device.ImmediateContext.MapSubresource(bindPoseBuffer, MapMode.WriteDiscard, MapFlags.None, out DataStream stream);
                for (int i = 0; i < boneBindPoses.Length; i++)
                {
                    stream.Write(boneBindPoses[i]);
                }
                device.ImmediateContext.UnmapSubresource(bindPoseBuffer, 0);
            }

            return new XPreview(sections, bindPoseBuffer);
        }
    }
}
