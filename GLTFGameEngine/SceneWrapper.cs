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
    internal class SceneWrapper : glTFLoader.Schema.Gltf
    {
        public string FilePath;

        public Render Render;

        public bool FirstMove = true;
        public Vector2 LastPos;

        public Shader ActiveShader;
        public Matrix4 Projection;
        public Matrix4 View;
        public int ActiveCamNode;
        public List<Shader> Shaders;

        public SceneWrapper()
        {
        }
        public void UseShader(Shader s)
        {
            ActiveShader = s;
            s.Use();
        }

        public void RenderScene()
        {
            // new design pattern 
            // 1. INIT = initialize the Engine data for the node, mesh, etc.
            // 2. RENDER = render using the gltf + engine data
            // why?
            // this should allow for swapping out between scenes/resources

            var scene = Scenes[Scene.Value];

            foreach (var shader in Shaders)
            {
                UseShader(shader);

                // iterate through nodes in the scene
                foreach (var nodeIndex in scene.Nodes)
                {
                    var node = Nodes[nodeIndex];
                    var renderNode = Render.Nodes[nodeIndex];

                    // INIT NODES
                    if (Render.Nodes[nodeIndex] == null)
                    {
                        Render.Nodes[nodeIndex] = new(node, node.Camera != null);
                        renderNode = Render.Nodes[nodeIndex];
                    }

                    // RENDER CAMERA
                    // assumption: only one camera node exists
                    if (node.Camera != null)
                    {
                        
                        var camera = Cameras[node.Camera.Value].Perspective;

                        Projection = Matrix4.CreatePerspectiveFieldOfView(camera.Yfov, camera.AspectRatio.Value,
                            camera.Znear, camera.Zfar.Value);
                        View = Matrix4.LookAt(renderNode.Position, renderNode.Position + renderNode.Front, renderNode.Up);

                        ActiveCamNode = nodeIndex;
                    }

                    // INIT and RENDER PRIMITIVES
                    if (renderNode != null)
                    {
                        renderNode.ParseNode(this, nodeIndex);
                    }

                }

                // clear recurse history for nodes
                foreach (var nodeIndex in scene.Nodes)
                {
                    var renderNode = Render.Nodes[nodeIndex];
                    if (renderNode == null) continue;
                    renderNode.RecurseHistory.Clear();
                }

                // INIT
                if (!shader.TextureIntsSet)
                {
                    for (int i = 0; i < shader.ShaderTextureNames.Count; i++)
                    {
                        shader.SetInt(shader.ShaderTextureNames[i], i);
                    }
                    shader.TextureIntsSet = true;
                }

            }
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

    internal class Mesh
    {
        public Primitive[] Primitives;
    }

    internal class Camera
    {

    }
}
