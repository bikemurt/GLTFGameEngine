using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using System.Diagnostics;
using glTFLoader.Schema;

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
        public List<Matrix4> InverseBindMatrices = new();

        public int NodeIndex = -1;
        public int ParentNodeIndex = -1;
        public Node(SceneWrapper sceneWrapper, int nodeIndex)
        {
            var node = sceneWrapper.Data.Nodes[nodeIndex];
            if (node.Translation != null)
            {
                Translation = new Vector3(node.Translation[0], node.Translation[1], node.Translation[2]);
            }
            if (node.Rotation != null)
            {
                Rotation = new Quaternion(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]);
            }
            if (node.Scale != null)
            {
                Scale = new Vector3(node.Scale[0], node.Scale[1], node.Scale[2]);
            }
            if (node.Camera != null)
            {
                Camera = new(sceneWrapper, nodeIndex);
            }

            NodeIndex = nodeIndex;
        }

        public void ParseNode(SceneWrapper sceneWrapper, int nodeIndex)
        {
            // recursion protection
            // how much overhead does this cause? profile later
            if (RecurseHistory.Contains(nodeIndex)) throw new Exception("Recursion loop found at node index " + nodeIndex);
            RecurseHistory.Add(nodeIndex);

            if (RecurseHistory.Count > RecurseLimit)
            {
                throw new Exception("Node search recursion limit reached (" + RecurseLimit + ")");
            }

            var node = sceneWrapper.Data.Nodes[nodeIndex];
            var renderNode = sceneWrapper.Render.Nodes[nodeIndex];

            if (node.Children != null)
            {
                foreach (var childIndex in node.Children)
                {
                    if (sceneWrapper.Render.Nodes[childIndex] == null)
                    {
                        // init render nodes in the node graph
                        sceneWrapper.Render.Nodes[childIndex] = new(sceneWrapper, childIndex)
                        {
                            ParentNodeIndex = nodeIndex
                        };
                    }
                    ParseNode(sceneWrapper, childIndex);
                }
            }

            if (node.Skin != null)
            {
                // if there's a skin entry in the gltf data, when expect
                // inverse bind matrices too
                if (renderNode.InverseBindMatrices.Count == 0)
                {
                    var invBindIndex = sceneWrapper.Data.Skins[node.Skin.Value].InverseBindMatrices.Value;
                    var bufferView = sceneWrapper.Data.BufferViews[invBindIndex];
                    var buffer = sceneWrapper.Data.Buffers[bufferView.Buffer];

                    float[] bufferFloats = DataStore.GetFloats(sceneWrapper, buffer, bufferView);

                    for (int i = 0; i < bufferFloats.Length; i += 16)
                    {
                        Matrix4 invBindMatrix = DataStore.GetMat4(
                            new float[]
                            {
                                bufferFloats[i + 0], bufferFloats[i + 1], bufferFloats[i + 2], bufferFloats[i + 3],
                                bufferFloats[i + 4], bufferFloats[i + 5], bufferFloats[i + 6], bufferFloats[i + 7],
                                bufferFloats[i + 8], bufferFloats[i + 9], bufferFloats[i + 10], bufferFloats[i + 11],
                                bufferFloats[i + 12], bufferFloats[i + 13], bufferFloats[i + 14], bufferFloats[i + 15]
                            }
                            );
                        renderNode.InverseBindMatrices.Add(invBindMatrix);
                    }

                }
            }

            // Render should be the last operation
            // at this point all child nodes should be processed, and they should know what their parents are
            if (node.Mesh != null)
            {
                // a draw call occurs for each mesh
                // deferred rendering?
                RenderMesh(sceneWrapper, nodeIndex);
            }
        }

        public static void RenderMesh(SceneWrapper sceneWrapper, int nodeIndex)
        {
            int meshIndex = sceneWrapper.Data.Nodes[nodeIndex].Mesh.Value;
            var mesh = sceneWrapper.Data.Meshes[meshIndex];

            // INIT the mesh
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
                // RENDER the mesh
                var node = sceneWrapper.Data.Nodes[nodeIndex];
                var renderNode = sceneWrapper.Render.Nodes[nodeIndex];

                var shader = sceneWrapper.Render.ActiveShader;

                
                for (int i = 0; i < mesh.Primitives.Length; i++)
                {
                    var renderPrimitive = sceneWrapper.Render.Meshes[meshIndex].Primitives[i];

                    // bind textures
                    for (int j = 0; j < renderPrimitive.Textures.Count; j++)
                    {
                        renderPrimitive.Textures[j].Use(TextureUnit.Texture0 + j);
                    }

                    Matrix4 model = DataStore.GetMat4FromTRS(node);

                    shader.SetMatrix4("model", model);
                    shader.SetMatrix4("view", sceneWrapper.Render.View);
                    shader.SetMatrix4("projection", sceneWrapper.Render.Projection);

                    // set uniforms for animation
                    if (node.Skin != null)
                    {
                        var joints = sceneWrapper.Data.Skins[node.Skin.Value].Joints;
                        for (int j = 0; j < joints.Length; j++)
                        {
                            var jointIndex = j;
                            var jointNodeIndex = joints[j];

                            Matrix4 globalTransform = JointGlobalTransform(sceneWrapper, jointNodeIndex);

                            Matrix4 jointMatrix = globalTransform * renderNode.InverseBindMatrices[jointIndex];

                            shader.SetMatrix4("jointMatrix[" + jointIndex.ToString() + "]", jointMatrix);
                        }
                    }
                    
                    if (shader.RenderType == RenderType.PBR)
                    {
                        shader.SetInt("pointLightSize", 1);
                        shader.SetVector3("pointLightPositions[0]", new Vector3(1.75863f, 3.0822f, -1.00545f));
                        shader.SetVector3("pointLightColors[0]", new Vector3(100.0f, 100.0f, 100.0f));
                    }

                    shader.SetVector3("camPos", sceneWrapper.Render.Nodes[sceneWrapper.Render.ActiveCamNode].Camera.Position);

                    // draw mesh
                    GL.BindVertexArray(renderPrimitive.VertexArrayObject);
                    GL.DrawElements(PrimitiveType.Triangles, renderPrimitive.DrawCount, DrawElementsType.UnsignedShort, 0);
                }
            }
        }

        public static Matrix4 JointGlobalTransform(SceneWrapper sceneWrapper, int jointNodeIndex)
        {
            var jointNode = sceneWrapper.Data.Nodes[jointNodeIndex];
            var jointRenderNode = sceneWrapper.Render.Nodes[jointNodeIndex];

            // calculate deepest node transform first
            var globalTransform = Matrix4.Identity;
            var localTransform = DataStore.GetMat4FromTRS(jointNode);
            globalTransform *= localTransform;

            // traverse to the root looking for parent nodes
            while (jointRenderNode.ParentNodeIndex != -1)
            {
                var parentRenderNode = sceneWrapper.Render.Nodes[jointRenderNode.ParentNodeIndex];

                var parentNode = sceneWrapper.Data.Nodes[jointRenderNode.ParentNodeIndex];
                localTransform = DataStore.GetMat4FromTRS(parentNode);
                globalTransform = localTransform * globalTransform;

                jointRenderNode = parentRenderNode;
            }
            
            return globalTransform;
        }
    }
}
