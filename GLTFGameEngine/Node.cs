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
        public Camera Camera;
        public List<int> RecurseHistory = new();
        private const int RecurseLimit = 100;
        public Vector3 Translation = Vector3.Zero;
        public Vector3 Scale = Vector3.One;
        public Quaternion Rotation = Quaternion.Identity;
        public Node()
        {

        }
        public Node(glTFLoader.Schema.Node node, bool isCamera = false)
        {
            if (node.Camera != null)
            {
                Camera = new(node);
            }
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
            if (node.Rotation != null)
            {
                renderNode.Rotation = new Quaternion(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]);
            }
            if (node.Scale != null)
            {
                renderNode.Scale = new Vector3(node.Scale[0], node.Scale[1], node.Scale[2]);
            }
            /*
            if (parentIndex != -1)
            {
                var parentRenderNode = sceneWrapper.Render.Nodes[parentIndex];
                if (parentRenderNode != null && parentRenderNode.Translation != null)
                {
                    renderNode.Translation += parentRenderNode.Translation;
                    renderNode.Scale *= parentRenderNode.Scale;
                }
            }    
            */

            if (node.Mesh != null)
            {
                RenderMesh(sceneWrapper, nodeIndex);
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

        public static void RenderMesh(SceneWrapper sceneWrapper, int nodeIndex)
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
                        Matrix4 model = Matrix4.CreateFromQuaternion(renderNode.Rotation)
                            * Matrix4.CreateScale(renderNode.Scale) * Matrix4.CreateTranslation(renderNode.Translation);

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

                    s.SetVector3("camPos", sceneWrapper.Render.Nodes[sceneWrapper.Render.ActiveCamNode].Camera.Position);

                    // draw mesh
                    GL.BindVertexArray(renderPrimitive.VertexArrayObject);
                    GL.DrawElements(PrimitiveType.Triangles, renderPrimitive.DrawCount, DrawElementsType.UnsignedShort, 0);
                }
            }
        }
    }
}
