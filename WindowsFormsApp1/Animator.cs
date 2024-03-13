using GlmSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public class Animator
    {
        public Animation CurrentAnimation { get; set; }
        public List<mat4> Transformations { get; set; }
        public float CurrentTime { get; set; }
        public float DeltaTime { get; set; }

        public Animator(Animation animation) 
        { 
            this.CurrentAnimation = animation;
            this.Transformations = new List<mat4>();
            for (int i = 0; i < 100; i++)
            {
                this.Transformations.Add(mat4.Identity);
            }

        }

        public void UpdateAnimation(float dt)
        {
            this.DeltaTime = dt;
            if (this.CurrentAnimation != null)
            {
                CurrentTime += (float)CurrentAnimation.Ticks * dt;
                CurrentTime = CurrentTime % CurrentAnimation.Duration;
                CalculateBoneTransformation(CurrentAnimation.RootNode,mat4.Identity);
            }
        }

        public void CalculateBoneTransformation(NodeData node, mat4 parentTransform) 
        {
            var nodeName = node.name;
            var transform = node.transformation;
            Bone bone = CurrentAnimation.FindBone(nodeName);

            if (bone != null)
            {
                bone.Update(CurrentTime);
                transform = bone.Transform;
            }

            //if (glm.IsNaN(transform.m00))
            //{
            //    Debug.Assert(false);
            //}

            mat4 globalTransformation = parentTransform * transform;
            var boneMap = CurrentAnimation.BoneMap;
            if(boneMap.ContainsKey(node.name))
            {
                int index = boneMap[node.name].id;
                mat4 offset = boneMap[node.name].offset;
                Transformations[index] = globalTransformation * offset;
            }

            foreach (var child in node.childs) 
            {
                CalculateBoneTransformation(child, globalTransformation);
            }
        }

        public void PlayAnimation(Animation pAnimation)
        {
            CurrentAnimation = pAnimation;
            CurrentTime = 0.0f;
        }
    }
}
