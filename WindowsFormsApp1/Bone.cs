using GlmSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public struct KeyPosition
    {
        public vec3 position;
        public float timestamp;
    }

    public struct KeyRotation { 
        public quat orientation;
        public float timestamp;
    }

    public struct KeyScale
    {
        public vec3 scale;
        public float timestamp;
    }

    public class Bone
    {
        private List<KeyPosition> m_Positions;
        private List<KeyRotation> m_Rotations;
        private List<KeyScale> m_Scales;

        public mat4 Transform { get; set; }
        public String Name { get; set; }
        public int ID { get; set; }

        public Bone(String name, int ID, Assimp.NodeAnimationChannel nodeAnimation)
        {
            Name = name;
            this.ID = ID;

            m_Positions = new List<KeyPosition>();
            foreach (var aiPositionKey in nodeAnimation.PositionKeys)
            {
                var keyPosition = new KeyPosition();
                keyPosition.position = new vec3(aiPositionKey.Value.X, aiPositionKey.Value.Y, aiPositionKey.Value.Z);
                keyPosition.timestamp = (float)aiPositionKey.Time;
                m_Positions.Add(keyPosition);
            }

            m_Rotations = new List<KeyRotation>();
            foreach (var aiRotationKey in nodeAnimation.RotationKeys)
            {
                var keyRotation = new KeyRotation();
                keyRotation.orientation = new quat(aiRotationKey.Value.X, aiRotationKey.Value.Y, aiRotationKey.Value.Z, aiRotationKey.Value.W);
                keyRotation.timestamp = (float)aiRotationKey.Time;
                m_Rotations.Add(keyRotation);
            }

            m_Scales = new List<KeyScale>();
            foreach (var aiScaleKey in nodeAnimation.ScalingKeys)
            {
                var keyScale = new KeyScale();
                keyScale.scale = new vec3(aiScaleKey.Value.X, aiScaleKey.Value.Y, aiScaleKey.Value.Z);
                keyScale.timestamp = (float)aiScaleKey.Time;
                m_Scales.Add(keyScale);
            }
        }

        public int GetPositionIndex(float animationTime)
        {
            for(int i = 0; i < m_Positions.Count -1; i++)
            {
                var position = m_Positions[i + 1];
                if(animationTime < position.timestamp)
                {
                    return i;
                }
            }
            return -1;
        }

        public int GetRotationIndex(float animationTime)
        {
            for (int i = 0; i < m_Rotations.Count -1; i++)
            {
                var rotation = m_Rotations[i + 1];
                if (animationTime < rotation.timestamp)
                {
                    return i;
                }
            }
            return -1;
        }

        public int GetScaleIndex(float animationTime)
        {
            for (int i = 0; i < m_Scales.Count -1; i++)
            {
                var scale = m_Scales[i + 1];
                if (animationTime < scale.timestamp)
                {
                    return i;
                }
            }
            Debug.Assert(false);
            return -1;
        }

        public float GetScaleFactor(float lastTimestamp, float nextTimestamp, float animationTime)
        {
            var midWayLength = animationTime - lastTimestamp;
            var frameDiff = nextTimestamp - lastTimestamp;
            return midWayLength / frameDiff;
        }

        public mat4 InterpolatePosition(float animationTime)
        {
            if(m_Positions.Count == 1)
            {
                return mat4.Identity * mat4.Translate(m_Positions[0].position);
            }
            var p0 = GetPositionIndex(animationTime);
            var p1 = p0 + 1;

            var scaleFactor = this.GetScaleFactor(m_Positions[p0].timestamp, m_Positions[p1].timestamp, animationTime);
            var finalPosition = vec3.Mix(m_Positions[p0].position, m_Positions[p1].position, scaleFactor);
            return mat4.Identity * mat4.Translate(finalPosition);
        }

        public mat4 InterpolateRotation(float animationTime)
        {
            if(m_Rotations.Count == 1)
            {
                var nRotation = m_Rotations[0].orientation.Normalized;
                return nRotation.ToMat4;
            }
            var p0 = GetRotationIndex(animationTime);
            var p1 = p0 + 1;

            var scaleFactor = this.GetScaleFactor(m_Rotations[p0].timestamp, m_Rotations[p1].timestamp, animationTime);
            quat finalRotation = quat.SLerp(m_Rotations[p0].orientation, m_Rotations[p1].orientation, scaleFactor);
            return finalRotation.Normalized.ToMat4;
        }

        public mat4 InterpolateScale(float animationTime)
        {
            if(m_Scales.Count == 1)
            {
                return mat4.Identity * mat4.Scale(m_Scales[0].scale);
            }
            var p0 = GetScaleIndex(animationTime);
            var p1 = p0 + 1;

            var scaleFactor = this.GetScaleFactor(m_Scales[p0].timestamp, m_Scales[p1].timestamp, animationTime);
            var finalScale = vec3.Mix(m_Scales[p0].scale, m_Scales[p1].scale, scaleFactor);
            return mat4.Identity * mat4.Scale(finalScale);
        }

        public void Update(float animationTime)
        {
            var location = this.InterpolatePosition(animationTime);
            var rotation = this.InterpolateRotation(animationTime);
            var scale = this.InterpolateScale(animationTime);

            Transform = location * rotation * scale;
            Console.WriteLine("Update Bone " + this.Name);
        }
    }
}
