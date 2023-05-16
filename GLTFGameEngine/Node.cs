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
using static OpenTK.Graphics.OpenGL.GL;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace GLTFGameEngine
{
    internal class Node
    {
        public Camera Camera;

        public List<Matrix4> InverseBindMatrices = new();

        public int ParentNodeIndex = -1;
        public Node(SceneWrapper sceneWrapper, int nodeIndex)
        {
            var node = sceneWrapper.Data.Nodes[nodeIndex];

            if (node.Camera != null)
            {
                Vector3 position = new Vector3(0, 0, 3);
                if (node.Translation != null)
                {
                    position.X = node.Translation[0];
                    position.Y = node.Translation[1];
                    position.Z = node.Translation[2];
                }
                Camera = new(position);
            }
        }

        public static void SetSceneLight(SceneWrapper sceneWrapper, int nodeIndex)
        {
            if (sceneWrapper.Render.LightNodeMap.ContainsKey(nodeIndex)) return;

            var node = sceneWrapper.Data.Nodes[nodeIndex];
            var renderNode = sceneWrapper.Render.Nodes[nodeIndex];

            var extKey = "KHR_lights_punctual";
            if (node.Extensions.ContainsKey(extKey))
            {
                var lightIndex = JsonConvert.DeserializeObject<KHRLightsPunctualExt>(node.Extensions[extKey].ToString()).Light;

                if (sceneWrapper.Data.Extensions.ContainsKey(extKey))
                {
                    var khrLights = JsonConvert.DeserializeObject<KHRLightsPunctual>(
                        sceneWrapper.Data.Extensions[extKey].ToString()
                        );

                    var light = khrLights.Lights[lightIndex.Value];

                    sceneWrapper.Render.LightNodeMap.Add(nodeIndex,
                        new Light()
                        {
                            Color = new Vector3(light.Color[0], light.Color[1], light.Color[2]),
                            Position = new Vector3(node.Translation[0],
                                node.Translation[1], node.Translation[2]),
                            LightType = light.Type
                        }
                        );
                }
            }
        }

        public static void SetInverseBindMatrices(SceneWrapper sceneWrapper, int nodeIndex)
        {
            var node = sceneWrapper.Data.Nodes[nodeIndex];
            var renderNode = sceneWrapper.Render.Nodes[nodeIndex];

            // if there's a skin entry in the gltf data, when expect
            // inverse bind matrices too
            if (renderNode.InverseBindMatrices.Count == 0)
            {
                var invBindIndex = sceneWrapper.Data.Skins[node.Skin.Value].InverseBindMatrices.Value;
                var bufferView = sceneWrapper.Data.BufferViews[invBindIndex];
                var buffer = sceneWrapper.Data.Buffers[bufferView.Buffer];

                float[] bufferFloats = DataStore.GetFloats(sceneWrapper, buffer, bufferView);

                for (int j = 0; j < bufferFloats.Length; j += 16)
                {
                    Matrix4 invBindMatrix = DataStore.GetMat4(
                        new float[]
                        {
                            bufferFloats[j + 0], bufferFloats[j + 1], bufferFloats[j + 2], bufferFloats[j + 3],
                            bufferFloats[j + 4], bufferFloats[j + 5], bufferFloats[j + 6], bufferFloats[j + 7],
                            bufferFloats[j + 8], bufferFloats[j + 9], bufferFloats[j + 10], bufferFloats[j + 11],
                            bufferFloats[j + 12], bufferFloats[j + 13], bufferFloats[j + 14], bufferFloats[j + 15]
                        }
                        );
                    renderNode.InverseBindMatrices.Add(invBindMatrix);
                }

            }
        }

        public static void UpdateCamera(SceneWrapper sceneWrapper, int nodeIndex)
        {
            var node = sceneWrapper.Data.Nodes[nodeIndex];
            var renderNode = sceneWrapper.Render.Nodes[nodeIndex];

            var camera = sceneWrapper.Data.Cameras[node.Camera.Value].Perspective;

            sceneWrapper.Render.Projection = Matrix4.CreatePerspectiveFieldOfView(camera.Yfov, camera.AspectRatio.Value,
                camera.Znear, camera.Zfar.Value);

            sceneWrapper.Render.View = Matrix4.LookAt(renderNode.Camera.Position,
                renderNode.Camera.Position + renderNode.Camera.Front, renderNode.Camera.Up);

            sceneWrapper.Render.ActiveCamNode = nodeIndex;
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
                        shader.SetInt("animate", 1);
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
                    else
                    {
                        shader.SetInt("animate", 0);
                    }

                    if (shader.RenderType == RenderType.PBR)
                    {
                        shader.SetInt("pointLightSize", sceneWrapper.Render.LightNodeMap.Count);
                        int j = 0;
                        foreach (var light in sceneWrapper.Render.LightNodeMap)
                        {
                            shader.SetVector3("pointLightPositions[" + j + "]", light.Value.Position);
                            shader.SetVector3("pointLightColors[" + j + "]", light.Value.Intensity * light.Value.Color);
                            j++;
                        }
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
