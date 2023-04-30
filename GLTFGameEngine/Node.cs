using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace GLTFGameEngine
{
    internal class Node
    {
        public float Yaw = 0.0f;
        public float Pitch = 0.0f;
        public float Roll = 0.0f;
        public Vector3 Front = -Vector3.UnitZ;
        public Vector3 Up = Vector3.UnitY;
        public Vector3 Right = Vector3.UnitX;
        public Vector3 Position;
        public List<int> RecurseHistory = new();

        private const int RecurseLimit = 100;
        public Node()
        {

        }
        public Node(glTFLoader.Schema.Node node, bool rotationCalc = false)
        {
            if (rotationCalc)
            {
                Vector4 q = new(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]);
                Roll = MathF.Atan2(2.0f * (q.Z * q.Y + q.W * q.X), 1.0f - 2.0f * (q.X * q.X + q.Y * q.Y));
                Pitch = MathF.Asin(2.0f * (q.Y * q.W - q.Z * q.X));
                Yaw = MathF.Atan2(2.0f * (q.Z * q.W + q.X * q.Y), -1.0f + 2.0f * (q.W * q.W + q.X * q.X));

                Quaternion q2 = new(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]);
                Vector4 t = q2.ToAxisAngle();
                Vector3 test = q2.ToEulerAngles();

                Console.Write('f');
            }

            Position = new(node.Translation[0], node.Translation[1], node.Translation[2]);
        }
        public void UpdateVectors()
        {
            Front.X = MathF.Cos(Pitch) * MathF.Cos(Yaw);
            Front.Y = MathF.Sin(Pitch);
            Front.Z = MathF.Cos(Pitch) * MathF.Sin(Yaw);

            Vector3 front = Vector3.Normalize(Front);

            Right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            Up = Vector3.Normalize(Vector3.Cross(Right, front));
        }

        public void ParseNode(SceneWrapper sceneWrapper, int nodeIndex)
        {
            // recursion protection
            if (RecurseHistory.Contains(nodeIndex)) throw new Exception("Recursion loop found at node index " + nodeIndex);
            RecurseHistory.Add(nodeIndex);

            if (RecurseHistory.Count > RecurseLimit)
            {
                throw new Exception("Node search recursion limit reached (" + RecurseLimit + ")");
            }

            var node = sceneWrapper.Nodes[nodeIndex];
            if (node.Mesh != null)
            {
                OnRenderFrameMesh(sceneWrapper, nodeIndex);
                return;
            }
            if (node.Children != null)
            {
                foreach (var childIndex in node.Children)
                {
                    ParseNode(sceneWrapper, childIndex);
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
                var s = sceneWrapper.ActiveShader;
                for (int i = 0; i < mesh.Primitives.Length; i++)
                {
                    var primitive = mesh.Primitives[i];
                    var renderPrimitive = sceneWrapper.Render.Meshes[meshIndex].Primitives[i];

                    // bind textures
                    for (int j = 0; j < renderPrimitive.Textures.Count; j++) renderPrimitive.Textures[j].Use(TextureUnit.Texture0 + j);

                    // set projection and view matrices
                    if (s.RenderType == RenderType.PBR || s.RenderType == RenderType.Light)
                    {
                        Vector3 translation = new(node.Translation[0], node.Translation[1], node.Translation[2]);
                        Vector3 scale = new(node.Scale[0], node.Scale[1], node.Scale[2]);

                        Quaternion rotation = new(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]);

                        Matrix4 model = Matrix4.CreateTranslation(translation) * Matrix4.CreateFromQuaternion(rotation) * Matrix4.CreateScale(scale);
                        // assume row-major order inverts this order

                        //Matrix4 model = Matrix4.CreateScale(scale) * Matrix4.CreateFromQuaternion(rotation) * Matrix4.CreateTranslation(translation);

                        s.SetMatrix4("model", model);
                        s.SetMatrix4("view", sceneWrapper.View);
                        s.SetMatrix4("projection", sceneWrapper.Projection);
                    }

                    if (s.RenderType == RenderType.PBR)
                    {
                        s.SetInt("pointLightSize", 1);
                        s.SetVector3("pointLightPositions[0]", new Vector3(1.75863f, 1.00545f, 3.0822f));
                        s.SetVector3("pointLightColors[0]", new Vector3(10.0f, 10.0f, 10.0f));
                    }

                    s.SetVector3("camPos", sceneWrapper.Render.Nodes[sceneWrapper.ActiveCamNode].Position);

                    // draw mesh
                    GL.BindVertexArray(renderPrimitive.VertexArrayObject);
                    GL.DrawElements(PrimitiveType.Triangles, renderPrimitive.DrawCount, DrawElementsType.UnsignedShort, 0);
                }
            }
        }
    }
}
