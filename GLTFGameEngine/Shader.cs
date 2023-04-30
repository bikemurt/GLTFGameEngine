using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace GLTFGameEngine
{
    internal enum RenderType
    {
        PBR, Light
    }

    internal class Shader
    {
        // config data
        public List<string> ShaderTextureNames = new();
        public bool TextureIntsSet = false;
        public string VertexPath = "";
        public string FragmentPath = "";
        public RenderType RenderType = RenderType.PBR;

        // populated data
        public int Handle;
        private readonly Dictionary<string, int> _uniformLocations = new();

        public Shader(string vertPath, string fragPath, RenderType renderType = RenderType.PBR)
        {
            LoadShaders(vertPath, fragPath);
            RenderType = renderType;
        }

        private void LoadShaders(string vertPath, string fragPath)
        {
            var shaderSource = File.ReadAllText(vertPath);
            var vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, shaderSource);
            CompileShader(vertexShader);

            shaderSource = File.ReadAllText(fragPath);
            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, shaderSource);
            CompileShader(fragmentShader);

            Handle = GL.CreateProgram();

            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);

            LinkProgram(Handle);

            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);

            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

            for (var i = 0; i < numberOfUniforms; i++)
            {
                int size = 0;
                var key = GL.GetActiveUniform(Handle, i, out size, out _);

                if (size == 1)
                {
                    var location = GL.GetUniformLocation(Handle, key);
                    _uniformLocations.Add(key, location);
                }
                if (size > 1)
                {
                    var keyOrig = key;
                    for (int j = 0; j < size; j++)
                    {
                        key = keyOrig.Replace("[0]", "[" + j + "]");
                        var location = GL.GetUniformLocation(Handle, key);
                        _uniformLocations.Add(key, location);
                    }
                }
            }
        }

        private static void CompileShader(int shader)
        {
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);
            if (code != (int)All.True)
            {
                var infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"Error occurred whilst compiling Shader({shader}).\n\n{infoLog}");
            }
        }

        private static void LinkProgram(int program)
        {
            GL.LinkProgram(program);

            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
            if (code != (int)All.True)
            {
                throw new Exception($"Error occurred whilst linking Program({program})");
            }
        }

        public void Use()
        {
            GL.UseProgram(Handle);
        }

        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }

        public void SetInt(string name, int data)
        {
            GL.Uniform1(_uniformLocations[name], data);
        }

        public void SetFloat(string name, float data)
        {
            GL.Uniform1(_uniformLocations[name], data);
        }

        public void SetMatrix4(string name, Matrix4 data)
        {
            GL.UniformMatrix4(_uniformLocations[name], true, ref data);
        }

        public void SetVector3(string name, Vector3 data)
        {
            GL.Uniform3(_uniformLocations[name], data);
        }
    }
}