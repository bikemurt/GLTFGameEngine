using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using System.Diagnostics;

namespace GLTFGameEngine
{
    internal class Node
    {
        // these are in degrees
        public float Yaw
        {
            get
            {
                return MathHelper.RadiansToDegrees(yawRad);
            }
            set
            {
                yawRad = MathHelper.DegreesToRadians(value);
            }
        }
        private float yawRad;

        public float Pitch
        {
            get
            {
                return MathHelper.RadiansToDegrees(pitchRad);
            }
            set
            {
                var angle = MathHelper.Clamp(value, -89f, 89f);
                pitchRad = MathHelper.DegreesToRadians(angle);
            }
        }
        public float pitchRad;

        public float Roll
        {
            get
            {
                return MathHelper.RadiansToDegrees(rollRad);
            }
            set
            {
                rollRad = MathHelper.DegreesToRadians(value);
            }
        }
        private float rollRad;

        private Vector3 front = -Vector3.UnitZ;
        private Vector3 up = Vector3.UnitY;
        private Vector3 right = Vector3.UnitX;

        public Vector3 Front => front;
        public Vector3 Up => up;
        public Vector3 Right => right;

        public Vector3 Position { get; set; }

        public List<int> RecurseHistory = new();

        private const int RecurseLimit = 100;
        public Vector3 Translation = Vector3.Zero;
        public Node()
        {

        }
        public Node(glTFLoader.Schema.Node node, bool isCamera = false)
        {
            if (isCamera)
            {
                Position = new(node.Translation[0], node.Translation[1], node.Translation[2]);

                Vector4 q = new(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]);
                float rollRad = MathF.Atan2(2.0f * (q.Z * q.Y + q.W * q.X), 1.0f - 2.0f * (q.X * q.X + q.Y * q.Y));
                float pitchRad = MathF.Asin(2.0f * (q.Y * q.W - q.Z * q.X));
                float yawRad = MathF.Atan2(2.0f * (q.Z * q.W + q.X * q.Y), -1.0f + 2.0f * (q.W * q.W + q.X * q.X));

                Pitch = MathHelper.RadiansToDegrees(pitchRad) - 90;
                Yaw = MathHelper.RadiansToDegrees(yawRad) + 90;

                Quaternion q2 = new(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]);
                Vector3 t1;
                float angle;
                q2.ToAxisAngle(out t1, out angle);
                Vector3 test = q2.ToEulerAngles();

                Console.Write('f');

                Pitch = 0;
                Yaw = 0;
                Roll = 0;

                UpdateVectors();
            }
        }
        public void UpdateVectors()
        {
            front.X = MathF.Cos(pitchRad) * MathF.Cos(yawRad - MathF.PI/2);
            front.Y = MathF.Sin(pitchRad);
            front.Z = MathF.Cos(pitchRad) * MathF.Sin(yawRad - MathF.PI/2);

            front = Vector3.Normalize(front);

            Matrix4 rollMat = Matrix4.CreateFromAxisAngle(front, rollRad);
            Matrix3 rollMat3 = new Matrix3(rollMat);

            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, front));
            //up = rollMat3 * up;
        }

        public void ParseNode(SceneWrapper sceneWrapper, int nodeIndex, int parentIndex = -1)
        {
            // recursion protection
            if (RecurseHistory.Contains(nodeIndex)) throw new Exception("Recursion loop found at node index " + nodeIndex);
            RecurseHistory.Add(nodeIndex);

            if (RecurseHistory.Count > RecurseLimit)
            {
                throw new Exception("Node search recursion limit reached (" + RecurseLimit + ")");
            }

            var node = sceneWrapper.Nodes[nodeIndex];
            var renderNode = sceneWrapper.Render.Nodes[nodeIndex];
            if (node.Translation != null)
            {
                renderNode.Translation += new Vector3(node.Translation[0], node.Translation[1], node.Translation[2]);
            }
            if (parentIndex != -1)
            {
                var parentRenderNode = sceneWrapper.Render.Nodes[parentIndex];
                if (parentRenderNode != null && parentRenderNode.Translation != null)
                {
                    renderNode.Translation += parentRenderNode.Translation;
                }
            }    

            if (node.Mesh != null)
            {
                OnRenderFrameMesh(sceneWrapper, nodeIndex);
                return;
            }
            if (node.Children != null)
            {
                foreach (var childIndex in node.Children)
                {
                    sceneWrapper.Render.Nodes[childIndex] = new();
                    ParseNode(sceneWrapper, childIndex, nodeIndex);
                }
            }
        }

        public static void OnRenderFrameMesh(SceneWrapper sceneWrapper, int nodeIndex)
        {
            int meshIndex = sceneWrapper.Nodes[nodeIndex].Mesh.Value;
            var mesh = sceneWrapper.Meshes[meshIndex];

            // INIT
            if (sceneWrapper.Render.Meshes[meshIndex] == null)
            {
                sceneWrapper.Render.Meshes[meshIndex] = new();
                sceneWrapper.Render.Meshes[meshIndex].Primitives = new Primitive[mesh.Primitives.Length];

                for (int i = 0; i < mesh.Primitives.Length; i++)
                {
                    var primitive = mesh.Primitives[i];

                    // send vertex data to GPU, set material
                    sceneWrapper.Render.Meshes[meshIndex].Primitives[i] = new(sceneWrapper, primitive);
                }
            }
            else
            {
                // RENDER
                var node = sceneWrapper.Nodes[nodeIndex];
                var renderNode = sceneWrapper.Render.Nodes[nodeIndex];
                var s = sceneWrapper.Render.ActiveShader;
                for (int i = 0; i < mesh.Primitives.Length; i++)
                {
                    var renderPrimitive = sceneWrapper.Render.Meshes[meshIndex].Primitives[i];

                    // bind textures
                    for (int j = 0; j < renderPrimitive.Textures.Count; j++)
                    {
                        renderPrimitive.Textures[j].Use(TextureUnit.Texture0 + j);
                    }

                    // set projection and view matrices
                    if (s.RenderType == RenderType.PBR || s.RenderType == RenderType.Light)
                    {
                        //Vector3 translation = new(node.Translation[0], node.Translation[1], node.Translation[2]);
                        //Vector3 scale = new(node.Scale[0], node.Scale[1], node.Scale[2]);

                        //Quaternion rotation = new(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]);

                        //Matrix4 model = Matrix4.CreateTranslation(translation) * Matrix4.CreateFromQuaternion(rotation) * Matrix4.CreateScale(scale);
                        // assume row-major order inverts this order

                        Matrix4 model = Matrix4.CreateTranslation(renderNode.Translation);

                        s.SetMatrix4("model", model);
                        s.SetMatrix4("view", sceneWrapper.Render.View);
                        s.SetMatrix4("projection", sceneWrapper.Render.Projection);
                    }

                    if (s.RenderType == RenderType.PBR)
                    {
                        s.SetInt("pointLightSize", 1);
                        s.SetVector3("pointLightPositions[0]", new Vector3(1.75863f, 3.0822f, -1.00545f));
                        s.SetVector3("pointLightColors[0]", new Vector3(100.0f, 100.0f, 100.0f));
                    }

                    s.SetVector3("camPos", sceneWrapper.Render.Nodes[sceneWrapper.Render.ActiveCamNode].Position);

                    // draw mesh
                    GL.BindVertexArray(renderPrimitive.VertexArrayObject);
                    GL.DrawElements(PrimitiveType.Triangles, renderPrimitive.DrawCount, DrawElementsType.UnsignedShort, 0);
                }
            }
        }
    }
}
