using GlmSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{

    public struct VertexMeshData{
        public float[] positions;
        public float[] texcords;
        public int[] boneIDs;
        public float[] boneWeights;
    }

    public struct BoneInfo
    {
        public int id;
        public mat4 offset;
    }

    public class Mesh
    {
        public String Name { get; set; }
        public List<Vertex> Vertices { get; set; }
        public Dictionary<String, BoneInfo> BoneMap { get; set; }
        public int[] Indices { get; set; }
        public Material Material { get; set; }
        public int vbo { get; set; }
        public int tbo { get; set; }
        public int nbo { get; set; }
        public int bibo { get; set; }
        public int bwbo { get; set; }

        public Mesh()
        {
            this.Vertices = new List<Vertex>();
            this.BoneMap = new Dictionary<string, BoneInfo>();
        }

        public float[] GetVertices()
        {
            return Vertices.SelectMany(v => new float[] { v.position.x, v.position.y, v.position.z }).ToArray();
        }

        public VertexMeshData GetIndicedVertices()
        {
            var meshData = new VertexMeshData();
            meshData.positions = new float[Indices.Length * 3];
            meshData.texcords = new float[Indices.Length * 2];
            meshData.boneIDs = new int[Indices.Length * 4];
            meshData.boneWeights = new float[Indices.Length * 4];

            for (int i = 0; i < Indices.Length; i++)
            {
                var index = Indices[i];
                var vertex = Vertices[index];

                var positionIndex = i * 3;
                meshData.positions[positionIndex] = vertex.position.x;
                meshData.positions[positionIndex + 1] = vertex.position.y;
                meshData.positions[positionIndex + 2] = vertex.position.z;

                var texCoordIndex = i * 2;
                meshData.texcords[texCoordIndex] = vertex.texcords.x;
                meshData.texcords[texCoordIndex + 1] = vertex.texcords.y;

                var boneIdIndex = i * 4;
                meshData.boneIDs[boneIdIndex] = vertex.boneids[0];
                meshData.boneIDs[boneIdIndex + 1] = vertex.boneids[1];
                meshData.boneIDs[boneIdIndex + 2] = vertex.boneids[2];
                meshData.boneIDs[boneIdIndex + 3] = vertex.boneids[3];

                var boneWeightIndex = i * 4;
                meshData.boneWeights[boneWeightIndex] = vertex.boneweights[0];
                meshData.boneWeights[boneWeightIndex + 1] = vertex.boneweights[1];
                meshData.boneWeights[boneWeightIndex + 2] = vertex.boneweights[2];
                meshData.boneWeights[boneWeightIndex + 3] = vertex.boneweights[3];
            }

            return meshData;
        }

    }
}
