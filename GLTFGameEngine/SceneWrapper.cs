using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GLTFGameEngine
{
    internal class SceneWrapper
    {
        public string FilePath;

        public Render Render;
        public glTFLoader.Schema.Gltf Data;
        public void UseShader(Shader s)
        {
            Render.ActiveShader = s;
            s.Use();
        }

        public void RenderScene()
        {
            // new design pattern 
            // 1. INIT = initialize the Engine data for the node, mesh, etc.
            // 2. RENDER = render using the gltf + engine data
            // why?
            // this should allow for swapping out between scenes/resources
            // future work with texture streaming and modern techniques

            var scene = Data.Scenes[Data.Scene.Value];

            foreach (var shader in Render.Shaders)
            {
                UseShader(shader);

                // iterate through nodes in the scene graph
                foreach (var nodeIndex in scene.Nodes)
                {
                    var node = Data.Nodes[nodeIndex];

                    // INIT RENDER NODES
                    if (Render.Nodes[nodeIndex] == null)
                    {
                        // this only generates the top level node.
                        Render.Nodes[nodeIndex] = new(this, nodeIndex);
                    }

                    // INIT LIGHTS
                    if (node.Extensions != null)
                    {
                        if (node.Extensions.ContainsKey("KHR_lights_punctual"))
                        {

                        }
                    }

                    // RENDER CAMERA
                    // assumption: only one camera node exists

                    var renderNode = Render.Nodes[nodeIndex];
                    if (node.Camera != null && renderNode.Camera != null)
                    {
                        var camera = Data.Cameras[node.Camera.Value].Perspective;

                        Render.Projection = Matrix4.CreatePerspectiveFieldOfView(camera.Yfov, camera.AspectRatio.Value,
                            camera.Znear, camera.Zfar.Value);

                        // retain this quaternion code, it's interesting
                        //Matrix4 rot = Matrix4.CreateFromQuaternion(renderNode.Camera.Rotation);
                        //Matrix4 trans = Matrix4.CreateTranslation(-renderNode.Camera.Position);
                        //Render.View = trans * rot;

                        Render.View = Matrix4.LookAt(renderNode.Camera.Position,
                            renderNode.Camera.Position + renderNode.Camera.Front, renderNode.Camera.Up);

                        Render.ActiveCamNode = nodeIndex;
                    }

                    // INIT and RENDER PRIMITIVES
                    renderNode.ParseNode(this, nodeIndex);

                }

                // clear recurse history for nodes
                foreach (var nodeIndex in scene.Nodes)
                {
                    var renderNode = Render.Nodes[nodeIndex];
                    if (renderNode == null) continue;
                    renderNode.RecurseHistory.Clear();
                }

                // does this means textures don't properly render on the first frame?
                // consequences?
                if (!shader.TextureIntsSet)
                {
                    for (int i = 0; i < shader.ShaderTextureNames.Count; i++)
                    {
                        shader.SetInt(shader.ShaderTextureNames[i], i);
                    }
                    shader.TextureIntsSet = true;
                }

                // animation
                for (int i = 0; i < Data.Animations.Length; i++)
                {
                    if (Render.Animations[i] == null)
                    {
                        Render.Animations[i] = new(this, i);
                    }

                    var renderAnimation = Render.Animations[i];
                    var animation = Data.Animations[i];

                    // loop through each node which exists, and apply transforms
                    foreach (var nodeAnimationDataPair in renderAnimation.NodeAnimationData)
                    {
                        var nodeIndex = nodeAnimationDataPair.Key;
                        var nodeAnimationData = nodeAnimationDataPair.Value;

                        //Data.Nodes[nodeIndex].Translation =
                        //    nodeAnimationData.Translations[renderAnimation.CurrentKeyframe];
                        //Data.Nodes[nodeIndex].Scale =
                        //    nodeAnimationData.Scales[renderAnimation.CurrentKeyframe];
                        //Data.Nodes[nodeIndex].Rotation =
                        //    nodeAnimationData.Rotations[renderAnimation.CurrentKeyframe];
                        
                        Data.Nodes[nodeIndex].Translation = renderAnimation.GetTranslation(nodeIndex, "translation");
                        Data.Nodes[nodeIndex].Scale = renderAnimation.GetTranslation(nodeIndex, "scale");
                        Data.Nodes[nodeIndex].Rotation = renderAnimation.GetTranslation(nodeIndex, "rotation");
                    }

                    renderAnimation.UpdateTime(60);
                }

            }
        }

        public void Release()
        {
            foreach (var shader in Render.Shaders)
            {
                GL.DeleteProgram(shader.Handle);

                foreach (var mesh in Render.Meshes)
                {
                    foreach (var primitive in mesh.Primitives)
                    {
                        GL.DeleteBuffer(primitive.VertexArrayObject);
                        GL.DeleteBuffer(primitive.VertexBufferObject);
                        GL.DeleteBuffer(primitive.ElementBufferObject);
                    }
                }
            }
        }
    }
}
