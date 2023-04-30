using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using glTFLoader.Schema;

namespace GLTFGameEngine
{
    internal class Primitive
    {
        public int VertexArrayObject;
        public int VertexBufferObject;
        public int ElementBufferObject;
        public int DrawCount;
        public List<Texture> Textures = new();
        public Primitive(SceneWrapper scene, glTFLoader.Schema.MeshPrimitive primitive)
        {
            LoadVertexData(scene, primitive);
            LoadMaterial(scene, primitive);
        }

        public void LoadVertexData(SceneWrapper sceneWrapper, glTFLoader.Schema.MeshPrimitive primitive)
        {
            // VAO
            VertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(VertexArrayObject);

            // VBO
            VertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBufferObject);

            var posBufferView = sceneWrapper.BufferViews[primitive.Attributes["POSITION"]];
            var uvBufferView = sceneWrapper.BufferViews[primitive.Attributes["TEXCOORD_0"]];
            var normalBufferView = sceneWrapper.BufferViews[primitive.Attributes["NORMAL"]];
            var tangentBufferView = sceneWrapper.BufferViews[primitive.Attributes["TANGENT"]];

            bool isAnimated = primitive.Attributes.ContainsKey("JOINTS_0") && primitive.Attributes.ContainsKey("WEIGHTS_0");
            BufferView jointBufferView = new();
            BufferView weightBufferView = new();
            if (isAnimated)
            {
                jointBufferView = sceneWrapper.BufferViews[primitive.Attributes["JOINTS_0"]];
                weightBufferView = sceneWrapper.BufferViews[primitive.Attributes["WEIGHTS_0"]];
            }

            var indicesBufferView = sceneWrapper.BufferViews[primitive.Indices.Value];


            var buffer = sceneWrapper.Buffers[posBufferView.Buffer];

            byte[] bufferBytes;
            if (buffer.Uri.Contains(".bin"))
            {
                string folder = Path.GetDirectoryName(sceneWrapper.FilePath);
                bufferBytes = File.ReadAllBytes(folder + "\\" + buffer.Uri);
            }
            else
            {
                bufferBytes = Convert.FromBase64String(buffer.Uri.Substring(37));
            }

            // populate vertex data buffer
            // we can take advantage of the fact that Blender packs all vertex data prior to the index
            int vertexBufferLength = indicesBufferView.ByteOffset;

            float[] bufferFloats = new float[vertexBufferLength / 4];
            System.Buffer.BlockCopy(bufferBytes, 0, bufferFloats, 0, vertexBufferLength);
            GL.BufferData(BufferTarget.ArrayBuffer, bufferBytes.Length, bufferFloats, BufferUsageHint.StaticDraw);

            // set vertex attrib pointers
            // position
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), posBufferView.ByteOffset);
            GL.EnableVertexAttribArray(0);
            // UV
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), uvBufferView.ByteOffset);
            GL.EnableVertexAttribArray(1);
            // normal
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), normalBufferView.ByteOffset);
            GL.EnableVertexAttribArray(2);
            // tangent
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), tangentBufferView.ByteOffset);
            GL.EnableVertexAttribArray(3);

            // joints
            GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), jointBufferView.ByteOffset);
            GL.EnableVertexAttribArray(4);
            // weights
            GL.VertexAttribPointer(5, 4, VertexAttribPointerType.Float, false, 4 * sizeof(float), weightBufferView.ByteOffset);
            GL.EnableVertexAttribArray(5);

            // indices
            ushort[] indices = new ushort[indicesBufferView.ByteLength / 2];
            System.Buffer.BlockCopy(bufferBytes, indicesBufferView.ByteOffset, indices, 0, indicesBufferView.ByteLength);

            ElementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ElementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(ushort), indices, BufferUsageHint.StaticDraw);

            DrawCount = indices.Length;

        }

        public void LoadMaterial(SceneWrapper sceneWrapper, glTFLoader.Schema.MeshPrimitive primitive)
        {
            int materialIndex = primitive.Material.Value;
            var material = sceneWrapper.Materials[materialIndex];

            string folder = Path.GetDirectoryName(sceneWrapper.FilePath);

            var albedoPath = folder + "\\" + sceneWrapper.Images[sceneWrapper.Textures[material.PbrMetallicRoughness.BaseColorTexture.Index].Source.Value].Uri;
            var metallicRoughnessPath = folder + "\\" + sceneWrapper.Images[sceneWrapper.Textures[material.PbrMetallicRoughness.MetallicRoughnessTexture.Index].Source.Value].Uri;
            var normalPath = folder + "\\" + sceneWrapper.Images[sceneWrapper.Textures[material.NormalTexture.Index].Source.Value].Uri;

            Textures.Add(new(albedoPath));
            Textures.Add(new(metallicRoughnessPath));
            Textures.Add(new(normalPath));

            if (sceneWrapper.Render.ActiveShader.ShaderTextureNames.Count == 0)
            {
                sceneWrapper.Render.ActiveShader.ShaderTextureNames.Add("albedoMap");
                sceneWrapper.Render.ActiveShader.ShaderTextureNames.Add("metallicRoughnessMap");
                sceneWrapper.Render.ActiveShader.ShaderTextureNames.Add("normalMap");
            }
        }

    }
}
