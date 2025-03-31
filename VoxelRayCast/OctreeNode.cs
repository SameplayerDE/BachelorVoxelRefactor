using Microsoft.Xna.Framework;

namespace VoxelRayCast;

public class OctreeNode {
    public Vector3 Min;      // Minimum des AABB
    public Vector3 Max;      // Maximum des AABB
    // public int ChildStart;   // Index im Buffer, ab dem die 8 Kindknoten liegen (-1, wenn Blatt)
    // public int ChildCount;   // Anzahl der Kindknoten (0 oder 8)
    public int Value;        // 0 = leer, 1 = voll, -1 = gemischt
    public OctreeNode[] Children;
}