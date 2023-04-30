using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using glTFLoader;
using OpenTK.Mathematics;
using glTFLoader.Schema;

namespace GLTFGameEngine
{

    internal class Game : GameWindow
    {
        private Scene Scene;
        public Game(int width, int height, string title) :
            base(GameWindowSettings.Default, new NativeWindowSettings() { Size = (width, height), Title = title })
        {
            // start by loading the gltf file with scene information
            string sceneFile = "C:\\Projects\\OpenGL\\ModelsGLTF\\Cube\\cube_test.gltf";
            
            Scene = Interface.LoadModel<Scene>(sceneFile);
            Scene.FilePath = sceneFile;
            Scene.Render = new(Scene);
        }
        protected override void OnLoad()
        {
            base.OnLoad();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            // new design pattern 
            // 1. INIT = initialize the Engine data for the node, mesh, etc.
            // 2. RENDER = render using the gltf + engine data
            // why?
            // this should allow for swapping out between scenes/resources

            var scene = Scene.Scenes[Scene.Scene.Value];

            Matrix4 view = new();
            Matrix4 projection = new();

            // iterate through nodes in the scene
            foreach (var nodeIndex in scene.Nodes)
            {
                var node = Scene.Nodes[nodeIndex];
                var renderNode = Scene.Render.Nodes[nodeIndex];

                // update view and projection matrices from camera
                if (node.Camera != null)
                {
                    // INIT
                    if (Scene.Render.Nodes[nodeIndex] == null)
                    {
                        Scene.Render.Nodes[nodeIndex] = new(node);
                    }

                    // RENDER
                    var camera = Scene.Cameras[node.Camera.Value].Perspective;

                    projection = Matrix4.CreatePerspectiveFieldOfView(camera.Yfov, camera.AspectRatio.Value,
                        camera.Znear, camera.Zfar.Value);

                    view = Matrix4.LookAt(renderNode.Position, renderNode.Position + renderNode.Front, renderNode.Up);
                }

                renderNode.InitNode(Scene, nodeIndex);
            }
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
        }
        protected override void OnUnload()
        {
            base.OnUnload();
        }
        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
        }
    }
}
