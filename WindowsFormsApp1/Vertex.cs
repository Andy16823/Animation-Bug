﻿using GlmSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public struct Vertex
    {
        public vec3 position;
        public vec3 normal;
        public vec2 texcords;
        public int[] boneids;
        public float[] boneweights;
    }
}
