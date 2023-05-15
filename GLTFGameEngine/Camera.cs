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

        public Camera(Vector3 position)
        {
            Position = position;

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
            
            right = Vector3.Normalize(Vector3.Cross(front, worldUp));
            up = Vector3.Normalize(Vector3.Cross(right, front));
        }
    }

}
