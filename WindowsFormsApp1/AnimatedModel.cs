using GlmSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public class AnimatedModel
    {
        public List<Material> Materials { get; set; }
        public List<Mesh> Meshes { get; set; }
        public List<Animation> Animations { get; set; }

        public AnimatedModel()
        {
            Animations = new List<Animation>();
            Materials = new List<Material>();
            Meshes = new List<Mesh>();
        }

        public bool HasBoneMap()
        {
            foreach (var mesh in Meshes)
            {
                if(mesh.BoneMap.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public Dictionary<String, BoneInfo> GetBoneMap()
        {
            foreach (var mesh in Meshes)
            {
                if (mesh.BoneMap.Count > 0)
                {
                    return mesh.BoneMap;
                }
            }
            return null;
        }

        public static AnimatedModel LoadModel(String filename)
        {
            FileInfo fileInfo = new FileInfo(filename);
            var directory = fileInfo.DirectoryName;

            Assimp.Scene model;
            Assimp.AssimpContext importer = new Assimp.AssimpContext();
            importer.SetConfig(new Assimp.Configs.NormalSmoothingAngleConfig(66.0f));
            model = importer.ImportFile(filename, Assimp.PostProcessPreset.TargetRealTimeMaximumQuality | Assimp.PostProcessSteps.Triangulate);

            var animatedModel = new AnimatedModel();
            //Load the materials
            foreach (var aMaterial in model.Materials)
            {
                var material = new Material();
                if (aMaterial.HasTextureDiffuse)
                {
                    material.DiffuseMap = (Bitmap)Bitmap.FromFile(directory + "\\" + aMaterial.TextureDiffuse.FilePath);
                }
                else
                {
                    material.DiffuseMap = Genesis.Core.Utils.CreateEmptyTexture(1, 1);
                }
                animatedModel.Materials.Add(material);
            }
            //Load the meshs
            foreach (var aMesh in model.Meshes)
            {
                var gMesh = new Mesh();
                gMesh.Name = aMesh.Name;
                gMesh.Material = animatedModel.Materials[aMesh.MaterialIndex];
                gMesh.Indices = aMesh.GetIndices();

                for (int i = 0; i < aMesh.VertexCount; i++)
                {
                    Vertex vertex = new Vertex();
                    vertex.position = new vec3(aMesh.Vertices[i].X, aMesh.Vertices[i].Z, aMesh.Vertices[i].Y);
                    if (aMesh.TextureCoordinateChannels[0] != null)
                    {
                        vertex.texcords = new vec2(aMesh.TextureCoordinateChannels[0][i].X, aMesh.TextureCoordinateChannels[0][i].Y);
                    }
                    else
                    {
                        vertex.texcords = new vec2(0f);
                    }
                    SetDefaultBoneData(ref vertex);
                    gMesh.Vertices.Add(vertex);
                }

                ExtractBoneWeights(gMesh, aMesh, model);
                animatedModel.Meshes.Add(gMesh);
            }

            Console.WriteLine("BoneMap" + animatedModel.GetBoneMap().Count);

            for (int i = 0; i < model.AnimationCount; i++)
            {
                var animation = new Animation(model, i, animatedModel);
                animatedModel.Animations.Add(animation);
            }
            return animatedModel;
        }

        private static void SetDefaultBoneData(ref Vertex vertex)
        {
            vertex.boneids = new int[4];
            vertex.boneweights = new float[4];
            for (int i = 0; i < 4; i++)
            {
                vertex.boneids[i] = -1;
                vertex.boneweights[i] = 0.0f;
            }
        }

        private static void SetVertexBoneData(ref Vertex vertex, int boneId, float weight)
        {
            for (int i = 0; i < 4; i++)
            {
                if (vertex.boneids[i] < 0)
                {
                    vertex.boneweights[i] = weight;
                    vertex.boneids[i] = boneId;
                    break;
                }
            }
        }

        public static void ExtractBoneWeights(Mesh mesh, Assimp.Mesh aimesh, Assimp.Scene model)
        {
            int boneCounter = 0;

            for (int i = 0; i < aimesh.BoneCount; i++)
            {
                var bone = aimesh.Bones[i];
                var boneID = -1;
                var boneName = bone.Name;
                if (!mesh.BoneMap.ContainsKey(boneName))
                {
                    BoneInfo boneInfo = new BoneInfo();
                    boneInfo.id = boneCounter;
                    boneInfo.offset = Form1.ConvertToGlmMat4(bone.OffsetMatrix);
                    mesh.BoneMap.Add(boneName, boneInfo);
                    boneID = boneCounter;
                    boneCounter++;
                }
                else
                {
                    boneID = mesh.BoneMap[boneName].id;
                }
                Debug.Assert(boneID != -1);
                var weights = bone.VertexWeights;
                for (int iw = 0; iw < bone.VertexWeightCount; iw++)
                {
                    var vertexId = bone.VertexWeights[iw].VertexID;
                    var weight = bone.VertexWeights[iw].Weight;
                    Debug.Assert(vertexId <= mesh.Vertices.Count);
                    var vertex = mesh.Vertices[vertexId];
                    SetVertexBoneData(ref vertex, boneID, weight);
                }
            }
        }

    }
}
