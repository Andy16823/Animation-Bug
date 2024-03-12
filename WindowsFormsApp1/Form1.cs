using NetGL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GenesisMath.Math;
using GlmSharp;
using OpenObjectLoader;
using System.IO;
using GlmSharp.Swizzle;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using Assimp;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private NetGL.OpenGL gl;
        private float rotate;
        private AnimatedModel animatedModel;
        private long lastFrame = 0;
        private Animator animator;

        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            DoubleBuffered = false;
        }       

        /// <summary>
        /// Converts an assimp matrix into an glm matrix
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static mat4 ConvertToGlmMat4(Assimp.Matrix4x4 matrix)
        {
            var mat = new mat4();
            mat.m00 = matrix.A1; // col 0, row 0
            mat.m01 = matrix.B1; // col 0, row 1
            mat.m02 = matrix.C1; // col 0, row 2
            mat.m03 = matrix.D1; // col 0, row 3

            mat.m10 = matrix.A2; // col 1, row 0
            mat.m11 = matrix.B2; // col 1, row 1
            mat.m12 = matrix.C2; // col 1, row 2
            mat.m13 = matrix.D2; // col 1, row 3

            mat.m20 = matrix.A3; // col 2, row 0
            mat.m21 = matrix.B3; // col 2, row 1
            mat.m22 = matrix.C3; // col 2, row 2
            mat.m23 = matrix.D3; // col 2, row 3

            mat.m30 = matrix.A4; // col 3, row 0
            mat.m31 = matrix.B4; // col 3, row 1
            mat.m32 = matrix.C4; // col 3, row 2
            mat.m33 = matrix.D4; // col 3, row 3

            return mat;
        }


        /// <summary>
        /// Intial the animated model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="gL"></param>
        private void InitialModel(AnimatedModel model, OpenGL gL)
        {
            foreach (var material in model.Materials)
            {
                Console.WriteLine("Loading Texture " + material.DiffuseMap);
                material.DiffuseMapRenderID = this.LoadTexture(material.DiffuseMap, gL);
                Console.WriteLine("Loaded Texture " + material.DiffuseMap + " with error " + gl.GetError());
            }

            foreach (var mesh in model.Meshes)
            {
                var meshData = mesh.GetIndicedVertices();

                Console.WriteLine("Loading Mesh " + mesh.Name);

                Console.WriteLine("Loading Verticies from " + mesh.Name);
                mesh.vbo = this.LoadBuffer(meshData.positions, gl, OpenGL.DynamicDraw);
                Console.WriteLine("Loaded Verticies from " + mesh.Name + " with error " + gl.GetError());

                Console.WriteLine("Loading Texture cords from " + mesh.Name);
                mesh.tbo = this.LoadBuffer(meshData.texcords, gl, OpenGL.DynamicDraw);
                Console.WriteLine("Loaded Texture cords from " + mesh.Name + " with error " + gl.GetError());

                Console.WriteLine("Loading Bone ID's from " + mesh.Name);
                mesh.bibo = this.LoadBuffer(meshData.boneIDs, gl, OpenGL.DynamicDraw);
                Console.WriteLine("Loaded Bone ID's from " + mesh.Name + " with error " + gl.GetError());

                Console.WriteLine("Loading Bone weights from " + mesh.Name);
                mesh.bwbo = this.LoadBuffer(meshData.boneWeights, gl, OpenGL.DynamicDraw);
                Console.WriteLine("Loaded Bone weights from " + mesh.Name + " with error " + gl.GetError());

                Console.WriteLine("Loaded Mesh " + mesh.Name + " with error " + gl.GetError());
            }

        }

        /// <summary>
        /// Renders the model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="program"></param>
        /// <param name="p_mat"></param>
        /// <param name="v_mat"></param>
        /// <param name="m_mat"></param>
        private void RenderModel(AnimatedModel model, int program, mat4 p_mat, mat4 v_mat, mat4 m_mat)
        {
            foreach(var mesh in model.Meshes)
            {
                gl.UseProgram(program);
                gl.UniformMatrix4fv(gl.GetUniformLocation(program, "projection"), 1, false, p_mat.ToArray());
                gl.UniformMatrix4fv(gl.GetUniformLocation(program, "view"), 1, false, v_mat.ToArray());
                gl.UniformMatrix4fv(gl.GetUniformLocation(program, "model"), 1, false, m_mat.ToArray());

                for(int i = 0; i < animator.Transformations.Count; i++)
                {
                    var bmat = animator.Transformations[i];
                    gl.UniformMatrix4fv(gl.GetUniformLocation(program, "finalBonesMatrices[" + i.ToString() + "]"), 1, false, bmat.ToArray());
                }

                gl.BindTexture(NetGL.OpenGL.Texture2D, mesh.Material.DiffuseMapRenderID);
                gl.Uniform1I(gl.GetUniformLocation(program, "textureSampler"), 0);

                gl.EnableVertexAttribArray(0);
                gl.BindBuffer(OpenGL.ArrayBuffer, mesh.vbo);
                gl.VertexAttribPointer(0, 3, OpenGL.Float, false, 0, 0);

                gl.EnableVertexAttribArray(2);
                gl.BindBuffer(OpenGL.ArrayBuffer, mesh.tbo);
                gl.VertexAttribPointer(2, 2, OpenGL.Float, false, 0, 0);

                gl.EnableVertexAttribArray(3);
                gl.BindBuffer(OpenGL.ArrayBuffer, mesh.bibo);
                gl.VertexAttribPointer(3, 4, OpenGL.Int, false, 0, 0);

                gl.EnableVertexAttribArray(4);
                gl.BindBuffer(OpenGL.ArrayBuffer, mesh.bwbo);
                gl.VertexAttribPointer(4, 4, OpenGL.Float, false, 0, 0);

                gl.DrawArrays(OpenGL.Triangles, 0, (mesh.Indices.Length));
            }
        }

        /// <summary>
        /// Loads an texture 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="gl"></param>
        /// <returns></returns>
        private int LoadTexture(Bitmap bitmap, OpenGL gl)
        {
            int texid = gl.GenTextures(1);
            gl.BindTexture(NetGL.OpenGL.Texture2D, texid);
            gl.TexParameteri(NetGL.OpenGL.Texture2D, NetGL.OpenGL.TextureMinFilter, NetGL.OpenGL.Nearest);
            gl.TexParameteri(NetGL.OpenGL.Texture2D, NetGL.OpenGL.TextureMagFilter, NetGL.OpenGL.Linear);
            gl.TexParameteri(NetGL.OpenGL.Texture2D, NetGL.OpenGL.TextureWrapS, NetGL.OpenGL.Repeate);
            gl.TexParameteri(NetGL.OpenGL.Texture2D, NetGL.OpenGL.TextureWrapT, NetGL.OpenGL.Repeate);
            gl.TexImage2D(NetGL.OpenGL.Texture2D, 0, NetGL.OpenGL.RGBA, bitmap.Width, bitmap.Height, 0, NetGL.OpenGL.BGRAExt, NetGL.OpenGL.UnsignedByte, bitmap);
            
            return texid;
        }

        /// <summary>
        /// Loads an float buffer
        /// </summary>
        /// <param name="flaots"></param>
        /// <param name="gl"></param>
        /// <param name="renderMode"></param>
        /// <returns></returns>
        private int LoadBuffer(float[] flaots, OpenGL gl, int renderMode)
        {
            int buffer = gl.GenBuffer(1);
            gl.BindBuffer(OpenGL.ArrayBuffer, buffer);
            gl.BufferData(OpenGL.ArrayBuffer, flaots.Length * sizeof(float), flaots, renderMode);
            return buffer;
        }

        /// <summary>
        /// Loads an int buffer
        /// </summary>
        /// <param name="ints"></param>
        /// <param name="gl"></param>
        /// <param name="renderMode"></param>
        /// <returns></returns>
        private int LoadBuffer(int[] ints, OpenGL gl, int renderMode)
        {
            int buffer = gl.GenBuffer(1);
            gl.BindBuffer(OpenGL.ArrayBuffer, buffer);
            gl.BufferData(OpenGL.ArrayBuffer, ints.Length * sizeof(int), ints, renderMode);
            return buffer;
        }

        /// <summary>
        /// Rendering thread
        /// </summary>
        private void loop()
        {
            //loading the model
            String modelspath = new System.IO.FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).Directory + "\\Models";
            this.animatedModel = AnimatedModel.LoadModel(modelspath + "\\Animation\\Human.fbx");

            //create an animator and load an animation
            animator = new Animator(animatedModel.Animations[5]);

            //Creating the shaders
            string vertexShaderCode = @"
                #version 430 core

                layout(location = 0) in vec3 pos;
                layout(location = 1) in vec3 norm;
                layout(location = 2) in vec2 tex;
                layout(location = 3) in ivec4 boneIds; 
                layout(location = 4) in vec4 weights;
	
                uniform mat4 projection;
                uniform mat4 view;
                uniform mat4 model;
	
                const int MAX_BONES = 100;
                const int MAX_BONE_INFLUENCE = 4;
                uniform mat4 finalBonesMatrices[MAX_BONES];
	
                out vec2 texCoord;
	
                void main()
                {
                    vec4 totalPosition = vec4(0.0f);
                    for(int i = 0 ; i < MAX_BONE_INFLUENCE ; i++)
                    {
                        if(boneIds[i] == -1) 
                            continue;
                        if(boneIds[i] >=MAX_BONES) 
                        {
                            totalPosition = vec4(pos,1.0f);
                            break;
                        }
                        vec4 localPosition = finalBonesMatrices[boneIds[i]] * vec4(pos,1.0f);
                        totalPosition += localPosition * weights[i];
                        vec3 localNormal = mat3(finalBonesMatrices[boneIds[i]]) * norm;
                    }
		
                    mat4 viewModel = view * model;
                    gl_Position =  projection * viewModel * totalPosition;
                    texCoord = tex;
                }


            ";

            string fragmentShaderCode = @"
                #version 430 core

                in vec2 texCoord;

                out vec4 fragColor;
                uniform sampler2D textureSampler;
                uniform sampler2D normalMap;

                void main()
                {
                    vec2 flippedTexCoord = vec2(texCoord.x, 1.0 - texCoord.y);
                    vec4 texColor = texture(textureSampler, flippedTexCoord);

                    fragColor = texColor * vec4(1.0, 1.0, 1.0, 1.0);
                }
            ";

            //Create a new instance from netgl
            gl = new NetGL.OpenGL();
            gl.modernGL = true;
            gl.Initial(this.panel1.Handle);
            gl.SwapIntervalEXT(0);
            gl.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            
            //Enable depthtest
            gl.Enable(OpenGL.DepthTest);
            gl.DepthFunc(OpenGL.Less);
            
            //Compile the vertex shader
            int vertexShader = gl.CreateShader(OpenGL.VertexShader);
            gl.SetShaderSource(vertexShader, 1, vertexShaderCode);
            gl.CompileShader(vertexShader);

            //Compile the fragment shader
            int fragmentShader = gl.CreateShader(OpenGL.FragmentShader);
            gl.SetShaderSource(fragmentShader, 1, fragmentShaderCode);
            gl.CompileShader(fragmentShader);

            //Create the shader program
            int program = gl.CreateProgram();
            gl.AttachShader(program, vertexShader);
            gl.AttachShader(program, fragmentShader);
            gl.LinkProgram(program);

            gl.DeleteShader(vertexShader);
            gl.DeleteShader(fragmentShader);

            //Initial the model
            InitialModel(this.animatedModel, gl);

            //Set the last fram to the current time
            lastFrame = Genesis.Core.Utils.GetCurrentTimeMillis(); 

            while (true) {
                Thread.Sleep(100);

                long lnow = Genesis.Core.Utils.GetCurrentTimeMillis();
                long ldt = lnow - lastFrame;
                lastFrame = lnow;

                //Update the animation
                animator.UpdateAnimation(0.5f);

                gl.Clear(NetGL.OpenGL.ColorBufferBit | NetGL.OpenGL.DepthBufferBit);

                mat4 p_mat = mat4.Perspective(GenesisMath.Math.Matrix4x4.DegreesToRadians(45.0f), (float)this.ClientSize.Width / (float)this.ClientSize.Height, 0.1f, 100f);
                mat4 v_mat = mat4.LookAt(new vec3(0f, 0f, 1f), new vec3(0f, 0f, 0f), new vec3(0f, 1f, 0f));

                mat4 mt_mat = mat4.Translate(new vec3(0f, -15.0f, -50f));
                mat4 mr_mat = mat4.RotateX(-1.5f) * mat4.RotateY(0f) * mat4.RotateZ(3f);
                mat4 ms_mat = mat4.Scale(new vec3(0.2f, 0.2f, 0.2f));
                mat4 m_mat = mt_mat * mr_mat * ms_mat;

                //Render the model
                this.RenderModel(animatedModel, program, p_mat, v_mat, m_mat);

                gl.Flush();
                gl.SwapLayerBuffers(NetGL.OpenGL.SwapMainPlane);
                Console.WriteLine(gl.GetError());
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            Thread renderThread = new Thread(new ThreadStart(loop));
            renderThread.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
