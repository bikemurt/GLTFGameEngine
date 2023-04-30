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
    internal class Scene : glTFLoader.Schema.Gltf
    {
        public string FilePath;
        public Render Render;
        public Scene()
        {
        }
    }
    internal class Render
    {
        public Camera[] Cameras;
        public Mesh[] Meshes;
        public Node[] Nodes;
        public Render(glTFLoader.Schema.Gltf sceneData)
        {
            Cameras = new Camera[sceneData.Cameras.Length];
            Meshes = new Mesh[sceneData.Meshes.Length];
            Nodes = new Node[sceneData.Nodes.Length];
        }
    }
    internal class Node
    {
        public float Yaw = 0.0f;
        public float Pitch = 0.0f;
        public float Roll = 0.0f;
        public Vector3 Front = -Vector3.UnitZ;
        public Vector3 Up = Vector3.UnitY;
        public Vector3 Right = Vector3.UnitX;
        public Vector3 Position;

        private List<int> RecurseHistory = new();
        private const int RecurseLimit = 100;
        public Node()
        {

        }
        public Node(glTFLoader.Schema.Node node)
        {
            Vector4 q = new(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]);
            Roll = MathF.Atan2(2.0f * (q.Z * q.Y + q.W * q.X), 1.0f - 2.0f * (q.X * q.X + q.Y * q.Y));
            Pitch = MathF.Asin(2.0f * (q.Y * q.W - q.Z * q.X));
            Yaw = MathF.Atan2(2.0f * (q.Z * q.W + q.X * q.Y), -1.0f + 2.0f * (q.W * q.W + q.X * q.X));

            Position = new(node.Translation[0], node.Translation[1], node.Translation[2]);
        }
        public void UpdateVectors()
        {
            Front.X = MathF.Cos(Pitch) * MathF.Cos(Yaw);
            Front.Y = MathF.Sin(Pitch);
            Front.Z = MathF.Cos(Pitch) * MathF.Sin(Yaw);

            Front = Vector3.Normalize(Front);

            Right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
            Up = Vector3.Normalize(Vector3.Cross(Right, Front));
        }

        public void InitNode(Scene scene, int nodeIndex)
        {
            // recursion protection
            if (RecurseHistory.Contains(nodeIndex)) return;
            RecurseHistory.Add(nodeIndex);

            if (RecurseHistory.Count > RecurseLimit)
            {
                throw new Exception("Node search recursion limit reached (" + RecurseLimit + ")");
            }

            var node = scene.Nodes[nodeIndex];
            if (node.Mesh != null)
            {
                OnRenderFrameMesh(scene, node.Mesh.Value);
                return;
            }
            if (node.Children != null)
            {
                foreach (var childIndex in node.Children)
                {
                    InitNode(scene, childIndex);
                }
            }
        }

        public static void OnRenderFrameMesh(Scene scene, int meshIndex)
        {
            var mesh = scene.Meshes[meshIndex];

            // INIT
            if (scene.Render.Meshes[meshIndex] == null)
            {
                scene.Render.Meshes[meshIndex] = new();
                scene.Render.Meshes[meshIndex].Primitives = new Primitive[mesh.Primitives.Length];

                for (int i = 0; i < mesh.Primitives.Length; i++)
                {
                    var primitive = mesh.Primitives[i];

                    scene.Render.Meshes[meshIndex].Primitives[i] = new(scene, primitive);
                }
            }

        }
    }
    internal class Mesh
    {
        public Primitive[] Primitives;
    }
    internal class Primitive
    {
        public int VertexArrayObject;
        public int VertexBufferObject;
        public int ElementBufferObject;
        public int DrawCount;
        public Primitive(Scene scene, glTFLoader.Schema.MeshPrimitive primitive)
        {
            // VAO
            VertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(VertexArrayObject);

            // VBO
            VertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);

            var posBufferView = scene.BufferViews[primitive.Attributes["POSITION"]];
            var uvBufferView = scene.BufferViews[primitive.Attributes["TEXCOORD_0"]];
            var nrmBufferView = scene.BufferViews[primitive.Attributes["NORMAL"]];

            // don't need this apparently
            //var tngBufferView = ModelData.BufferViews[gltfPrimitive.Attributes["TANGENT"]];

            var buffer = scene.Buffers[posBufferView.Buffer];

            byte[] bufferBytes;

            if (buffer.Uri.Contains(".bin"))
            {
                string folder = Path.GetDirectoryName(scene.FilePath);
                bufferBytes = File.ReadAllBytes(folder + "\\" + buffer.Uri);
            }
            else
            {
                bufferBytes = Convert.FromBase64String(buffer.Uri.Substring(37));
            }

            // add main vertex data (no index data) to buffer
            int vertexBufferLength = buffer.ByteLength -
                scene.BufferViews[primitive.Indices.Value].ByteLength;

            float[] bufferFloats = new float[vertexBufferLength / 4];
            System.Buffer.BlockCopy(bufferBytes, 0, bufferFloats, 0, vertexBufferLength);
            GL.BufferData(BufferTarget.ArrayBuffer, bufferBytes.Length, bufferFloats, BufferUsageHint.StaticDraw);

            // set vertex attrib pointers
            int uvOffset = posBufferView.ByteLength;
            int nrmOffset = uvOffset + uvBufferView.ByteLength;
            int tngOffset = nrmOffset + nrmBufferView.ByteLength;

            // position
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            // UV
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), uvOffset);
            GL.EnableVertexAttribArray(1);
            // normal
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), nrmOffset);
            GL.EnableVertexAttribArray(2);
            // tangent
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), tngOffset);
            GL.EnableVertexAttribArray(3);

            // indices
            var indicesBufferView = scene.BufferViews[primitive.Indices.Value];
            int indicesBytesOffset = indicesBufferView.ByteOffset;
            int indicesBytesLength = indicesBufferView.ByteLength;
            ushort[] indices = new ushort[indicesBytesLength / 2];
            System.Buffer.BlockCopy(bufferBytes, indicesBytesOffset, indices, 0, indicesBytesLength);

            ElementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(ushort), indices, BufferUsageHint.StaticDraw);

            DrawCount = indices.Length;
        }
    }
    internal class Camera
    {

    }
}
