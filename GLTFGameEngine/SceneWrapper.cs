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
using System.Reflection.Metadata.Ecma335;

namespace GLTFGameEngine
{
    internal class SceneWrapper
    {
        public string FilePath;

        public Render Render;
        public glTFLoader.Schema.Gltf Data;

        public bool AllParentNodesSet = false;
        public void UseShader(Shader s)
        {
            Render.ActiveShader = s;
            s.Use();
        }

        public void RenderScene()
        {
            foreach (var shader in Render.Shaders)
            {
                UseShader(shader);

                for (int i = 0; i < Data.Nodes.Length; i++)
                {
                    if (Render.Nodes[i] == null) Render.Nodes[i] = new(this, i);

                    var node = Data.Nodes[i];
                    var renderNode = Render.Nodes[i];

                    if (node.Camera != null) Node.UpdateCamera(this, i);
                    if (node.Mesh != null) Node.RenderMesh(this, i);
                    if (node.Skin != null) Node.SetInverseBindMatrices(this, i);
                    if (node.Extensions != null) Node.SetSceneLight(this, i);
                }

                // set texture uniforms
                if (!shader.TextureIntsSet)
                {
                    for (int i = 0; i < shader.ShaderTextureNames.Count; i++)
                    {
                        shader.SetInt(shader.ShaderTextureNames[i], i);
                    }
                    shader.TextureIntsSet = true;
                }
                
                // second iteration through glTF nodes required to establish
                // each node's parent node. will be -1 if they are top level
                if (!AllParentNodesSet)
                {
                    for (int i = 0; i < Data.Nodes.Length; i++)
                    {
                        var node = Data.Nodes[i];
                        if (node.Children == null) continue;

                        foreach (var childIndex in node.Children)
                        {
                            var childRenderNode = Render.Nodes[childIndex];
                            childRenderNode.ParentNodeIndex = i;
                        }
                    }
                    AllParentNodesSet = true;
                }
                
                // for verbosity
                if (AllParentNodesSet)
                {
                    // ready to animate once all parent nodes are set
                    for (int i = 0; i < Data.Animations.Length; i++)
                    {
                        if (Render.Animations[i] == null)
                        {
                            Render.Animations[i] = new(this, i);
                        }

                        var renderAnimation = Render.Animations[i];

                        // loop through each node which exists, and apply transforms
                        foreach (var nodeAnimationDataPair in renderAnimation.NodeAnimationData)
                        {
                            var nodeIndex = nodeAnimationDataPair.Key;
                            var nodeAnimationData = nodeAnimationDataPair.Value;

                            Data.Nodes[nodeIndex].Translation = renderAnimation.GetTranslation(nodeIndex, "translation");
                            Data.Nodes[nodeIndex].Scale = renderAnimation.GetTranslation(nodeIndex, "scale");
                            Data.Nodes[nodeIndex].Rotation = renderAnimation.GetTranslation(nodeIndex, "rotation");
                        }

                        renderAnimation.UpdateTime(60);
                    }
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
