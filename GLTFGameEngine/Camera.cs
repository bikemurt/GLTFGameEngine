using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace GLTFGameEngine
{
    internal class Camera
    {
        // these are in degrees
        public float Yaw
        {
            get
            {
                return MathHelper.RadiansToDegrees(yawRad);
            }
            set
            {
                yawRad = MathHelper.DegreesToRadians(value);
            }
        }
        private float yawRad;

        public float Pitch
        {
            get
            {
                return MathHelper.RadiansToDegrees(pitchRad);
            }
            set
            {
                var angle = MathHelper.Clamp(value, -89f, 89f);
                pitchRad = MathHelper.DegreesToRadians(angle);
            }
        }
        public float pitchRad = 0;

        private Vector3 front = -Vector3.UnitZ;
        private Vector3 up = Vector3.UnitY;
        private Vector3 right = Vector3.UnitX;
        private Vector3 worldUp = Vector3.UnitY;

        public Vector3 Front => front;
        public Vector3 Up => up;
        public Vector3 Right => right;

        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }

        public Camera(SceneWrapper sceneWrapper, int nodeIndex)
        {
            var node = sceneWrapper.Nodes[nodeIndex];
            if (node.Translation != null)
            {
                Position = new Vector3(node.Translation[0], node.Translation[1], node.Translation[2]);
            }
            /*
            if (node.Rotation != null)
            {
                Rotation = new Quaternion(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]);
                Rotation = Quaternion.Conjugate(Rotation);
            }
            worldUp = worldUp * Matrix3.CreateRotationX(rollRad);
            */

            Pitch = -30;
            Yaw = 45;

            UpdateVectors();
        }
        public void UpdateVectors()
        {
            front.X = MathF.Cos(pitchRad) * MathF.Cos(yawRad - MathF.PI / 2);
            front.Y = MathF.Sin(pitchRad);
            front.Z = MathF.Cos(pitchRad) * MathF.Sin(yawRad - MathF.PI / 2);

            front = Vector3.Normalize(front);

            // Quaternion logic
            //Matrix4 camRot = Matrix4.CreateFromQuaternion(Rotation);
            //front = -Vector3.Normalize(camRot.Column2.Xyz);

            // Roll logic
            // Matrix4 rollMat = Matrix4.CreateFromAxisAngle(front, rollRad);
            // Matrix3 rollMat3 = new Matrix3(rollMat);

            right = Vector3.Normalize(Vector3.Cross(front, worldUp));
            up = Vector3.Normalize(Vector3.Cross(right, front));

            // up = up * rollMat3;
        }
    }

}
