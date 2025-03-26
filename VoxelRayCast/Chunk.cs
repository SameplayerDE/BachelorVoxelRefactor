namespace VoxelRayCast;

public class Chunk
{
    public const int Size = 16;
    public int X;
    public int Y;
    public int Z;
    public int[] Data = new int[Size * Size * Size];

    public void Set(int x, int y, int z, int value)
    {
        var index = x + Size * z + Size * Size * y;
        Data[index] = value;
    }
}