using GlmSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public struct NodeData
    {
        public mat4 transformation;
        public string name;
        public int count;
        public List<NodeData> childs;
    }

    public class Animation
    {
        public String Name { get; set; }
        public float Duration { get; set; }
        public double Ticks { get; set; }
        public List<Bone> Bones { get; set; }
        public NodeData RootNode { get; set; }

        public Dictionary<String, BoneInfo> BoneMap { get; set; }

        public Animation(Assimp.Scene model, int index, AnimatedModel animatedModel) 
        { 
            this.Bones = new List<Bone>();  
            var animation = model.Animations[index];
            this.Name = animation.Name;
            this.Ticks = animation.TicksPerSecond;
            this.Duration = (float) animation.DurationInTicks;
            var rootNode = new NodeData();
            this.ReadHeirarchyData(ref rootNode, model.RootNode.Children[0]);
            this.RootNode = rootNode;
            this.ReadMissingBones(animation, animatedModel.Meshes[0]);
            BoneMap = animatedModel.Meshes[0].BoneMap;
        }

        public void ReadHeirarchyData(ref NodeData dest, Assimp.Node src)
        {
            if (src == null)
                Debug.Assert(false);
            dest.name = src.Name;
            dest.transformation = Form1.ConvertToGlmMat4(src.Transform);
            dest.count = src.ChildCount;
            dest.childs = new List<NodeData>();
            foreach (var child in src.Children)
            {
                NodeData nodeData = new NodeData();
                ReadHeirarchyData(ref nodeData, child);
                dest.childs.Add(nodeData);
            }
        }

        public void ReadMissingBones(Assimp.Animation animation, Mesh mesh)
        {
            var size = animation.NodeAnimationChannelCount;
            var boneInfos = mesh.BoneMap;
            var boneCount = boneInfos.Count;

            for ( var i = 0; i < size; i++ )
            {
                var channel = animation.NodeAnimationChannels[i];
                var boneName = channel.NodeName;

                if (!boneInfos.ContainsKey(boneName)) {
                    BoneInfo info = new BoneInfo();
                    info.id = boneCount;
                    boneInfos.Add(boneName, info);
                    boneCount++;
                }
                Bones.Add(new Bone(boneName, boneInfos[boneName].id, channel));
            }
        }

        public Bone FindBone(String name)
        {
            foreach (var bone in this.Bones)
            {
                if(bone.Name.Equals(name)) 
                    return bone;
            }
            return null;
        }

    }
}
