using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace VoxelRayCast;

public class WorldGenerator
{

    private static ConcurrentDictionary<(int, int, int), Chunk> _cache = new();
    public static FastNoiseLite NoiseGenerator;
    
    public static Chunk GetChunk(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0)
        {
            return Chunk.Empty;
        }
        
        var key = (x, y, z);
        
        if (_cache.TryGetValue(key, out var chunk))
        {
            return chunk;
        }
        var generatedChunk = GenerateChunk(x, y, z);
        _cache.TryAdd((x, y,  z), generatedChunk);
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
                    var nValue = Math.Max(NoiseGenerator.GetNoise(x * Chunk.Size + curX, z * Chunk.Size + curZ, y * Chunk.Size + curY), 0);
                    chunk.Set(curX, curY, curZ, nValue > 0.0f ? 1 : 0);
                }
            }
        }
        return chunk;
    }

    public static OctreeNode BuildOctree(Chunk chunk)
    {
        return BuildOctree(chunk.Data, 0, 0, 0, Chunk.Size);
    }

    private static OctreeNode BuildOctree(int[] data, int x, int y, int z, int size)
    {
        var result = new OctreeNode
        {
            Min = new Vector3(x, y, z),
            Max = new Vector3(x + size, y + size, z + size)
        };

        if (IsHomogeneous(data, x, y, z, size, out var value))
        {
            result.Value = value;
            result.Children = null;
        }
        else
        {
            result.Value = -1;
            if (size > 1)
            {
                var half = size / 2;
                result.Children = new OctreeNode[8]; // has always 8 childs if not homo (haha, homo)
                
                int index = 0;
                for (int dx = 0; dx < 2; dx++)
                {
                    for (int dy = 0; dy < 2; dy++)
                    {
                        for (int dz = 0; dz < 2; dz++)
                        {
                            result.Children[index++] = BuildOctree(
                                data,
                                x + dx * half,
                                y + dy * half,
                                z + dz * half,
                                half);
                        }
                    }
                }
            }
            else
            {
                result.Children = null;
            }
        }
        
        return result;
    }
    
    private static int GetVoxel(int[] data, int x, int y, int z)
    {
        return data[x + Chunk.Size * (z + Chunk.Size * y)];
    }
    
    private static bool IsHomogeneous(int[] data, int x0, int y0, int z0, int size, out int value)
    {
        value = GetVoxel(data, x0, y0, z0);
        for (int y = 0; y < size; y++)
        {
            for (int z = 0; z < size; z++)
            {
                for (int x = 0; x < size; x++)
                {
                    int current = GetVoxel(data, x0 + x, y0 + y, z0 + z);
                    if (current != value)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
}