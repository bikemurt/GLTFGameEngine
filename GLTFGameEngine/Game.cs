using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using glTFLoader;
using OpenTK.Mathematics;
using glTFLoader.Schema;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GLTFGameEngine
{

    internal class Game : GameWindow
    {
        private SceneWrapper sceneWrapper;
        private List<Shader> shaders = new List<Shader>();
        public Game(int width, int height, string title) :
            base(GameWindowSettings.Default, new NativeWindowSettings() { Size = (width, height), Title = title })
        {
            // start by loading the gltf file with scene information
            string sceneFile = "C:\\Projects\\OpenGL\\ModelsGLTF\\Cube\\cube.gltf";
            
            sceneWrapper = Interface.LoadModel<SceneWrapper>(sceneFile);
            sceneWrapper.FilePath = sceneFile;
            sceneWrapper.Render = new(sceneWrapper);

            shaders.Add(new("Shaders/pbr.vert", "Shaders/pbr.frag", RenderType.PBR));

            sceneWrapper.Render.Shaders = shaders;
        }
        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0f, 0f, 0f, 1f);
            GL.Enable(EnableCap.DepthTest);

            CursorState = CursorState.Grabbed;
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            sceneWrapper.RenderScene();

            SwapBuffers();
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, Size.X, Size.Y);
            //_camera.AspectRatio = (float)Size.X / (float)Size.Y;
        }
        protected override void OnUnload()
        {
            base.OnUnload();
        }
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (!IsFocused) return;

            var input = KeyboardState;
            if (input.IsKeyDown(Keys.Escape)) Close();

            var cam = sceneWrapper.Render.Nodes[sceneWrapper.Render.ActiveCamNode];
            if (cam == null) return;

            const float cameraSpeed = 1.5f;
            const float sensitivity = 0.1f;
            if (input.IsKeyReleased(Keys.Enter))
            {
                Console.WriteLine("Pos" + cam.Position.ToString());
                Console.WriteLine("Yaw" + cam.Yaw.ToString());
                Console.WriteLine("Pitch" + cam.Pitch.ToString());
            }

            if (input.IsKeyDown(Keys.W))
            {
                cam.Position += cam.Front * cameraSpeed * (float)e.Time; // Forward
            }

            if (input.IsKeyDown(Keys.S))
            {
                cam.Position -= cam.Front * cameraSpeed * (float)e.Time; // Backwards
            }
            if (input.IsKeyDown(Keys.A))
            {
                cam.Position -= cam.Right * cameraSpeed * (float)e.Time; // Left
            }
            if (input.IsKeyDown(Keys.D))
            {
                cam.Position += cam.Right * cameraSpeed * (float)e.Time; // Right
            }
            if (input.IsKeyDown(Keys.Space))
            {
                cam.Position += cam.Up * cameraSpeed * (float)e.Time; // Up
            }
            if (input.IsKeyDown(Keys.LeftShift))
            {
                cam.Position -= cam.Up * cameraSpeed * (float)e.Time; // Down
            }

            // Get the mouse state
            var mouse = MouseState;

            if (sceneWrapper.Render.FirstMove)
            {
                sceneWrapper.Render.LastPos = new Vector2(mouse.X, mouse.Y);
                sceneWrapper.Render.FirstMove = false;
            }
            else
            {
                // Calculate the offset of the mouse position
                var deltaX = mouse.X - sceneWrapper.Render.LastPos.X;
                var deltaY = mouse.Y - sceneWrapper.Render.LastPos.Y;
                sceneWrapper.Render.LastPos = new Vector2(mouse.X, mouse.Y);

                // Apply the camera pitch and yaw (we clamp the pitch in the camera class)
                cam.Yaw += deltaX * sensitivity;
                cam.Pitch -= deltaY * sensitivity; // Reversed since y-coordinates range from bottom to top
            }
            cam.UpdateVectors();
        }
    }
}
