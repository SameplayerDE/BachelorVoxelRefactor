namespace VoxelRayCast;

public struct Chunk
{
    public static Chunk Empty = new Chunk
    {
        Data = new int[Size * Size * Size] // automatisch alles 0
    };
    
    public const int Size = 16;
    public int X;
    public int Y;
    public int Z;
    public int[] Data = new int[Size * Size * Size];

    public Chunk()
    {
        X = 0;
        Y = 0;
        Z = 0;
    }

    public void Set(int x, int y, int z, int value)
    {
        var index = x + Size * z + Size * Size * y;
        Data[index] = value;
    }
}