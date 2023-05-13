using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLTFGameEngine
{
    internal class Render
    {
        public Mesh[] Meshes;
        public Node[] Nodes;
        public List<Light> Lights = new();

        public bool FirstMove = true;
        public Vector2 LastPos;

        public Shader ActiveShader;
        public Matrix4 Projection;
        public Matrix4 View;
        public int ActiveCamNode;
        public List<Shader> Shaders;
        public Render(SceneWrapper sceneWrapper)
        {
            Meshes = new Mesh[sceneWrapper.Data.Meshes.Length];
            Nodes = new Node[sceneWrapper.Data.Nodes.Length];
        }
    }
}
