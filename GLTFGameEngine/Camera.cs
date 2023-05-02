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
        public float pitchRad;

        public float Roll
        {
            get
            {
                return MathHelper.RadiansToDegrees(rollRad);
            }
            set
            {
                rollRad = MathHelper.DegreesToRadians(value);
            }
        }
        private float rollRad;

        private Vector3 front = -Vector3.UnitZ;
        private Vector3 up = Vector3.UnitY;
        private Vector3 right = Vector3.UnitX;

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
            if (node.Rotation != null)
            {
                Rotation = new Quaternion(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]);
                Rotation = Quaternion.Conjugate(Rotation);
            }

            Pitch = 0;
            Yaw = 0;
            Roll = 0;

            UpdateVectors();
        }
        public void UpdateVectors()
        {
            front.X = MathF.Cos(pitchRad) * MathF.Cos(yawRad - MathF.PI / 2);
            front.Y = MathF.Sin(pitchRad);
            front.Z = MathF.Cos(pitchRad) * MathF.Sin(yawRad - MathF.PI / 2);

            front = Vector3.Normalize(front);

            Matrix4 rollMat = Matrix4.CreateFromAxisAngle(front, rollRad);
            Matrix3 rollMat3 = new Matrix3(rollMat);

            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, front));
            up = rollMat3 * up;
        }
    }

}
