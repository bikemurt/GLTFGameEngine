using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace GLTFGameEngine
{
    internal class DataStore
    {
        public static Dictionary<string, byte[]> BufferBytes = new();

        public static Quaternion GetQuat(float[] input)
        {
            if (input.Length != 4) throw new Exception("Bad input for quaternion");
            Quaternion result = new(input[0], input[1], input[2], input[3]);
            return result;
        }
        public static Matrix4 GetMat4FromQuat(float[] input)
        {
            return Matrix4.CreateFromQuaternion(GetQuat(input));
        }

        public static Matrix4 GetMat4FromTranslation(float[] input)
        {
            if (input.Length != 3) throw new Exception("Bad input for Mat4");
            return Matrix4.CreateTranslation(input[0], input[1], input[2]);
        }
        public static Matrix4 GetMat4FromScale(float[] input)
        {
            if (input.Length != 3) throw new Exception("Bad input for Mat4");
            return Matrix4.CreateScale(input[0], input[1], input[2]);
        }
        public static Matrix4 GetMat4(float[] input)
        {
            if (input.Length != 16) throw new Exception("Bad input for Mat4");
            return new Matrix4(
                input[0], input[4], input[8], input[12],
                input[1], input[5], input[9], input[13],
                input[2], input[6], input[7], input[14],
                input[3], input[7], input[8], input[15]
                );
        }

        public static byte[] GetBin(SceneWrapper sceneWrapper, glTFLoader.Schema.Buffer buffer)
        {
            byte[] bufferBytes = new byte[10];
            if (buffer.Uri != null)
            {
                if (buffer.Uri.Contains(".bin"))
                {
                    string binPath = Path.GetDirectoryName(sceneWrapper.FilePath) + "\\" + buffer.Uri;
                    if (!BufferBytes.ContainsKey(binPath))
                    {
                        bufferBytes = File.ReadAllBytes(binPath);
                    }
                    else
                    {
                        bufferBytes = BufferBytes[binPath];
                    }
                }
                else
                {
                    // this is not really optimized but i don't intend on using this version of GLTF
                    bufferBytes = Convert.FromBase64String(buffer.Uri.Substring(37));
                }
            }
            return bufferBytes;
        }
    }
}
