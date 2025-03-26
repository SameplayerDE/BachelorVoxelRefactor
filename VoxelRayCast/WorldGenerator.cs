using System;
using System.Collections.Generic;

namespace VoxelRayCast;

public class WorldGenerator
{

    private static Dictionary<(int, int, int), Chunk> _cache = new();
    public static FastNoiseLite NoiseGenerator;
    
    public static Chunk GetChunk(int x, int y, int z)
    {
        var key = (x, y, z);
        if (_cache.TryGetValue(key, out var chunk))
        {
            return chunk;
        }

        var generatedChunk = GenerateChunk(x, y, z);
        _cache.Add((x, y,  z), generatedChunk);
        return generatedChunk;
    }

    private static Chunk GenerateChunk(int x, int y, int z)
    {
        var chunk = new Chunk
        {
            X = x,
            Y = y,
            Z = z
        };
        const float scale = 1f;
        for (int curY = 0; curY < Chunk.Size; curY++)
        {
            for (int curZ = 0; curZ < Chunk.Size; curZ++)
            {
                for (int curX = 0; curX < Chunk.Size; curX++)
                {
                    var nValue = Math.Max(NoiseGenerator.GetNoise(curX * scale, curY * scale, curZ * scale), 0);
                    chunk.Set(curX, curY, curZ, nValue > 0.0f ? 1 : 0);
                }
            }
        }
        return chunk;
    }
}