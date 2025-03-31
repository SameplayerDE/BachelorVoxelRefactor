#define GroupSize 64
static const float PI = 3.14159265f;

struct RayResult3D
{
    int Id;
    int Hit;
    int Side;
    int SideOrientation;
    float Length;
    float3 Direction;
    float3 From;
    float3 To;
    float3 Rotation;
    float3 Angle;
};

RWStructuredBuffer<int> CPUMap; // map data
RWTexture2D<float4> Output; // render target

Texture3D<float4> Input; // texture atlas
int InputW; // texture width
int InputH; // texture height

int MapMaxX;
int MapMaxY;
int MapMaxZ;

float3 Position; // camera position
float3 Offset;
float3 Rotation; // camera rotation
matrix RotationMatrix;
float iTime; // IDK

int Width;
int Height;

int getSolid(int x, int y, int z) {

    int gridX = (int)x - Offset.x;
    int gridY = (int)y - Offset.y;
    int gridZ = (int)z - Offset.z;

    if (gridY >= MapMaxY || gridY < 0)
    {
        return 0;
    }
    if (gridZ >= MapMaxZ || gridZ < 0)
    {
        return 0;
    }
    if (gridX >= MapMaxX || gridX < 0)
    {
        return 0;
    }
    int index = gridX + MapMaxX * gridZ + MapMaxX * MapMaxZ * gridY;
    int solid = CPUMap[index];
    return solid;
};

/*bool RayIntersectsAABB(float3 origin, float3 dir, float3 min, float3 max, out float tmin, out float tmax) {
    float3 invDir = 1.0 / dir;

    float3 t0 = (min - origin) * invDir;
    float3 t1 = (max - origin) * invDir;

    float3 tmin3 = min(t0, t1);
    float3 tmax3 = max(t0, t1);

    tmin = max(max(tmin3.x, tmin3.y), tmin3.z);
    tmax = min(min(tmax3.x, tmax3.y), tmax3.z);

    return tmax >= max(tmin, 0.0);
}*/

[numthreads(GroupSize, 1, 1)]
void CS(uint3 localID : SV_GroupThreadID, uint3 groupID : SV_GroupID, uint  localIndex : SV_GroupIndex, uint3 globalID : SV_DispatchThreadID)
{
    RayResult3D result;

    int x = globalID.x % Width;
    int y = globalID.x / Width;

    float posX = Position.x;
    float posY = Position.y;
    float posZ = Position.z;

    int mapX = (int)posX;
    int mapY = (int)posY;
    int mapZ = (int)posZ;

    if (posX < 0)
    {
        mapX -= 1;
    }
    if (posY < 0)
    {
        mapY -= 1;
    }
    if (posZ < 0)
    {
        mapZ -= 1;
    }

    float3 mapPosition = float3(mapX, mapY, mapZ);
    int stepSize = 1;

    float3 rayPosition = Position;
    float2 screenPos = (float2(x, y) / float2(Width, Height)) * 2.0f - float2(1, 1);
    float3 cameraDir = float3(0, 0, 1);
    float3 cameraPlaneU = cross(cameraDir, float3(0, 1, 0));
    float3 cameraPlaneV = cross(cross(cameraDir, float3(0, 1, 0)), cameraDir) * Height / Width;
    float3 rayDir = cameraDir + screenPos.x * cameraPlaneU + screenPos.y * cameraPlaneV;

    rayDir = mul(rayDir, RotationMatrix);
    float rayDirLength = length(rayDir);

    float3 deltaDist = float3(rayDirLength, rayDirLength, rayDirLength) / rayDir;
    deltaDist = float3(abs(deltaDist.x), abs(deltaDist.y), abs(deltaDist.z));

    float deltaDistX = deltaDist.x;
    float deltaDistY = deltaDist.y;
    float deltaDistZ = deltaDist.z;

    float3 raySign = float3(sign(rayDir.x), sign(rayDir.y), sign(rayDir.z));

    float3 sideDist = (raySign * (mapPosition - rayPosition) + (raySign * 0.5f) + float3(0.5f, 0.5f, 0.5f)) * deltaDist;
    float3 step = raySign;

    float sideDistX = sideDist.x;
    float sideDistY = sideDist.y;
    float sideDistZ = sideDist.z;

    int stepX = (int)step.x;
    int stepY = (int)step.y;
    int stepZ = (int)step.z;

    int distance = 0;
    int maxDistance = 512;
    int minDistance = 32;
    int hit = 0;
    int side = 0;
    int id = 0;
    int orientation = 0;
    float rayLength = 0.0f;

    while (hit == 0 && distance < maxDistance)
    {
        if (sideDistX < sideDistZ)
        {
            if (sideDistX < sideDistY)
            {
                sideDistX += deltaDistX;
                mapX += stepX;
                side = 0;
                orientation = stepX < 0 ? 0 : 1;
            }
            else
            {
                sideDistY += deltaDistY;
                mapY += stepY;
                side = 1;
                orientation = stepY < 0 ? 0 : 1;
            }
        }
        else
        {
            if (sideDistZ < sideDistY)
            {
                sideDistZ += deltaDistZ;
                mapZ += stepZ;
                side = 2;
                orientation = stepZ < 0 ? 0 : 1;
            }
            else
            {
                sideDistY += deltaDistY;
                mapY += stepY;
                side = 1;
                orientation = stepY < 0 ? 0 : 1;
            }
        }

		int voxelValue = getSolid(mapX, mapY, mapZ);
		if (voxelValue != 0)
		{
			hit = 1;
			id = voxelValue;
			break;
		}
        distance++;
    }

    if (side == 0)
    {
        rayLength = sideDistX - deltaDistX;
    }
    else if (side == 1)
    {
        rayLength = sideDistY - deltaDistY;
    }
    else if (side == 2)
    {
        rayLength = sideDistZ - deltaDistZ;
    }

    result.Id = id;
    result.Hit = hit;
    result.Side = side;
    result.SideOrientation = orientation;
    result.Length = rayLength;

    result.Direction = normalize(rayDir);
    result.Angle = float3(0, 0, 0);
    result.Rotation = float3(0, 0, 0);
    result.From = float3(posX, posY, posZ);
    result.To = result.From + result.Direction * result.Length;

    //Results[globalID.x] = result;

    float4 c = float4(0, 0, 0, 0);
    int tX = 0;
    int tY = 0;

    float xRatio = result.To.x - (int)result.To.x;
    float yRatio = 1 - (result.To.y - (int)result.To.y);
    float zRatio = result.To.z - (int)result.To.z;

    if (result.Side == 0)
    {
        tX = (InputW * zRatio);
        tY = (InputH * yRatio);
    }
    if (result.Side == 1)
    {
        tX = (InputW * zRatio);
        tY = (InputH * xRatio);
    }
    if (result.Side == 2)
    {
        tX = (InputW * xRatio);
        tY = (InputH * yRatio);
    }

    uint2 idL = uint2(x, Height - y);

    if (result.Hit == 1 || result.Id != 0)
    {
        c = Input[uint3(tX, tY, result.Id - 1)];
       	float3 normal = float3(0, 1, 0);
       
       	if (result.Side == 0) {
       	    if (result.SideOrientation == 0) {
       	        normal = float3(1, 0, 0);
       	    }
       	    if (result.SideOrientation == 1) {
       	        normal = float3(-1, 0, 0);
       	    }
       	}
       	if (result.Side == 1) {
       	    if (result.SideOrientation == 0) {
       	        normal = float3(0, 1, 0);
       	    }
       	    if (result.SideOrientation == 1) {
       	        normal = float3(0, -1, 0);
       	    }
       	}
       	if (result.Side == 2) {
       	    if (result.SideOrientation == 0) {
       	        normal = float3(0, 0, 1);
       	    }
       	    if (result.SideOrientation == 1) {
       	        normal = float3(0, 0, -1);
       	    }
       	}

        Output[idL] = c;
    }
    else
    {
        Output[idL] = float4(0, 0, 0, 1);
    }
}



technique T0
{
    pass P0
    {
        ComputeShader = compile cs_5_0 CS();
    }
};