using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoxelRayCast
{
    public struct RayResult3D
    {
        public int Id;
        public int Hit;
        public int Side;
        public int SideOrientation;
        public float Length;
        public Vector3 Direction;
        public Vector3 From;
        public Vector3 To;
        public Vector3 Rotation;
        public Vector3 Angle;
        public Vector3 MapPosition;
    }
}
