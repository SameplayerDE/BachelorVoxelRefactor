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

float3 FogColor = float3(0, 0, 0);
float FogStart = 25;
float FogEnd = 100;

float3 AmbientLightColor = float3(0.4, 0.4, 0.4);
float3 LightColor = float3(1, 0.9f, 1);
float ConeAngle = 2;
float3 LightPosition = float3(12, 2, 12);
float3 LightDirection = float3(0, -1, 0);
float LightFalloff = 1;

RWStructuredBuffer<RayResult3D> Results;
RWStructuredBuffer<int> CPUMap;

int MapMaxX;
int MapMaxY;
int MapMaxZ;

RWTexture2D<float4> Output;

Texture3D<float4> Input;
int InputW;
int InputH;

float3 Position;
float3 Rotation;
matrix RotationMatrix;
float iTime;

int Width;
int Height;

float2 rotate2d(float2 v, float a) {
    float sinA = sin(a);
    float cosA = cos(a);
    return float2(v.x * cosA - v.y * sinA, v.y * cosA + v.x * sinA);
}

int getSolid(int x, int y, int z) {

    int gridX = (int)x;
    int gridY = (int)y;
    int gridZ = (int)z;

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

int isBlocked(float3 a, float3 b) {

    float3 dir = b - a;
    float len = length(dir);
    dir = normalize(dir);

    float posX = a.x;
    float posY = a.y;
    float posZ = a.z;

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

    float3 rayPosition = a;
    float3 rayDir = dir;
    float rayDirLength = length(rayDir);

    float3 deltaDist = float3(rayDirLength, rayDirLength, rayDirLength) / rayDir;
    deltaDist = float3(abs(deltaDist.x), abs(deltaDist.y), abs(deltaDist.z));

    float deltaDistX = deltaDist.x;
    float deltaDistZ = deltaDist.z;
    float deltaDistY = deltaDist.y;

    float3 raySign = float3(sign(rayDir.x), sign(rayDir.y), sign(rayDir.z));

    float3 sideDist = (raySign * (mapPosition - rayPosition) + (raySign * 0.5f) + float3(0.5f, 0.5f, 0.5f)) * deltaDist;
    float3 step = raySign;

    float sideDistX = sideDist.x;
    float sideDistY = sideDist.y;
    float sideDistZ = sideDist.z;

    int stepX = (int)step.x;
    int stepY = (int)step.y;
    int stepZ = (int)step.z;


    int side = 0;
    float rayLength = 0.0f;
    int hit = 0;
    float maxDist = len;
    int dist;

    while (len - rayLength > 1.0f)
    {
        if (sideDistX < sideDistZ)
        {
            if (sideDistX < sideDistY)
            {
                sideDistX += deltaDistX;
                mapX += stepX;
                side = 0;
            }
            else
            {
                sideDistY += deltaDistY;
                mapY += stepY;
                side = 1;
            }
        }
        else
        {
            if (sideDistZ < sideDistY)
            {
                sideDistZ += deltaDistZ;
                mapZ += stepZ;
                side = 2;
            }
            else
            {
                sideDistY += deltaDistY;
                mapY += stepY;
                side = 1;
            }
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

        if (getSolid(mapX, mapY, mapZ) != 0)
        {
            hit = 1;
        }
        dist++;
    }

    return hit;
};


float3x3 rotX(float t) {
    float3x3 m = float3x3(1, 0, 0, 0, cos(t), -sin(t), 0, sin(t), cos(t));
    return m;
}

float3x3 rotY(float t) {
    float3x3 m = float3x3(cos(t), 0, sin(t), 0, 1, 0, -sin(t), 0, cos(t));
    return m;
}

float3x3 rotZ(float t) {
    float3x3 m = float3x3(1, 0, 0, 0, cos(t), -sin(t), 0, sin(t), cos(t));
    return m;
}

[numthreads(GroupSize, 1, 1)]
void CS(uint3 localID : SV_GroupThreadID, uint3 groupID : SV_GroupID, uint  localIndex : SV_GroupIndex, uint3 globalID : SV_DispatchThreadID)
{
    RayResult3D result = Results[globalID.x];

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

    //float fovHalf = radians(FOV / 2);
    //float step = radians(FOV) / Width;
    //
    //ry = -fovHalf + Rotation.y + step * globalID.x;

    /*if (ry >= radians(FOV)) {
        rx += radians(45) / Height;
    }*/








    //var rayDir = Vector3.Transform(Vector3.Backward, n_rotation);

    //float3 deltaDist = float3(rayDir.length) / rayDir;
    //deltaDist = new Vector3(Math.Abs(deltaDist.X), Math.Abs(deltaDist.Y), Math.Abs(deltaDist.Z));

    /*float2 pos = float2(X, Y);
    float2 res = float2(Width, Height);
    float2 screenPos = (pos.xy / res.xy) * 2.0f - 1.0f;
    float3 cameraDir = float3(0, 0, 1);
    float3 cameraPlaneU = float3(1.0, 0.0, 0.0);
    float3 cameraPlaneV = float3(0.0, 1.0, 0.0) * Height / Width;
    float3 rayDir = cameraDir + screenPos.x * cameraPlaneU + screenPos.y * cameraPlaneV;*/

    //float fovHalfX = radians(45 / 2);
    //float fovHalfY = radians(10 / 2);
    //float stepValueX = radians(45) / Width;
    //float stepValueY = radians(10) / Height;
    ////
    //float rotationY = -fovHalfX + Rotation.y + stepValueX * (x);
    //float rotationX = -fovHalfY + Rotation.x + stepValueY * (y);
    ////
    //float rayDirX = sin(rotationY);
    //float rayDirY = sin(rotationX);
    //float rayDirZ = cos(rotationY);

     //float3 cameraDir = Rotation;
     //float3 cameraPlaneU = float3(0.0f, 0.0f, 1.0f);
     //float3 cameraPlaneV = float3(1.0f, 0.0f, 0.0f) * Width / Height;
     //float3 rayDir = cameraDir + x * cameraPlaneU + y * cameraPlaneV;
     //
     ////float3 rayDir = float3(rayDirX, rayDirY, rayDirZ);
     ////float3 rayDir = float3(0, 0, 1);
     ////rayDir.xz = rotate2d(rayDir.xz, rotationY);
     //
     //float t = radians(90);
     //float3 rayDir = Rotation;

     //rayDir
     //rayDir.x =  cos(t) * rayDir.x + 0 * rayDir.y + sin(t) * rayDir.z;
     //rayDir.y =       0 * rayDir.x + 1 * rayDir.y +      0 * rayDir.z;
     //rayDir.z = -sin(t) * rayDir.x + 0 * rayDir.y + cos(t) * rayDir.z;
     //rayDir *= ;
     //rayDir.x += x / Width;
     //rayDir.z -= x / Width;



     //float rayDirLength = length(rayDir);
     //
     //float3 deltaDist = float3(rayDirLength, rayDirLength, rayDirLength) / rayDir;
     //deltaDist = float3(abs(deltaDist.x), abs(deltaDist.y), abs(deltaDist.z));
     //
     //float deltaDistX = deltaDist.x;
     //float deltaDistY = deltaDist.y;
     //float deltaDistZ = deltaDist.z;
     //
     //float3 raySign = float3(sign(rayDir.x), sign(rayDir.y), sign(rayDir.z));
     //float3 sideDist = (raySign * (mapPosition - Position) + (raySign * 0.5f) + float3(0.5f, 0.5f, 0.5f)) * deltaDist;
     //float3 step = raySign;
     //
     //float sideDistX = sideDist.x;
     //float sideDistY = sideDist.y;
     //float sideDistZ = sideDist.z;
     //
     //int stepX = (int)step.x;
     //int stepY = (int)step.y;
     //int stepZ = (int)step.z;
     //
     //step = float3(stepX, stepY, stepZ);

     //var n_rotation =
     //    Matrix.CreateRotationX(_rotation.X) *
     //    Matrix.CreateRotationY(_rotation.Y) *
     //    Matrix.CreateRotationZ(_rotation.Z);

     //float t = radians(iTime * 10);
     //float3x3 n_rotation = rotY(t);
     //float3x3 n_rot = AngleAxis3x3(rx, float3(0, 0, 1)) * AngleAxis3x3(ry, float3(0, 0, 1)) * AngleAxis3x3(rz, float3(0, 0, 1));


    int stepSize = 1;

    float3 rayPosition = Position;
    float2 screenPos = (float2(x, y) / float2(Width, Height)) * 2.0f - float2(1, 1);
    float3 cameraDir = float3(0, 0, 1);
    float3 cameraPlaneU = cross(cameraDir, float3(0, 1, 0));
    float3 cameraPlaneV = cross(cross(cameraDir, float3(0, 1, 0)), cameraDir) * Height / Width;
    float3 rayDir = cameraDir + screenPos.x * cameraPlaneU + screenPos.y * cameraPlaneV;

    rayDir = mul(rayDir, RotationMatrix);

    //rayPosition.xz = rotate2d(rayPosition.xz, iTime);
    //rayDir.xz = rotate2d(rayDir.xz, -Rotation.y);
    //rayDir.yz = rotate2d(rayDir.yz, Rotation.x);

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

        if (getSolid(mapX, mapY, mapZ) != 0)
        {
            //if (getSolid(mapX, mapY, mapZ) != 1) {
            //    id = getSolid(mapX, mapY, mapZ);
            hit = 1;
            //}
            
            //id++;
            id = getSolid(mapX, mapY, mapZ);
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

    Results[globalID.x] = result;

    float4 c = float4(0, 0, 0, 0);
    switch (result.Id)
    {
        case 1: c = float4(1, 0, 0, 1); break; //red
        case 2: c = float4(0, 1, 0, 1); break; //green
        case 3: c = float4(0, 0, 1, 1); break; //blue
        case 4: c = float4(1, 1, 1, 1); break; //white
        default: c = float4(1, 1, 1, 1); break; //yellow
    }
    //c = saturate(rayLength) * c;
    //c = (result.Id / 64.0f);
    //c /= saturate((result.Id / 64.0f));
    //c *= 1 - (rayLength / 64.0f);
    //while (id > 0) {
    //    c += float4(0.01f, 0.01f, 0.01f, 1);
    //    id--;
    //}
    //c = float4(1, 1, 1, 1);
    //c = c / float4(1 - (rayLength / 128.0f), 1 - (rayLength / 128.0f), 1 - (rayLength / 128.0f), 1);
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
        //c = Input[uint3(tX, tY, result.Id - 1)];
        //c = Input[uint3(tX, tY, result.Id - 1)];

       //float3 normal;
       //
       //if (result.Side == 0) {
       //    if (result.SideOrientation == 0) {
       //        normal = float3(1, 0, 0);
       //    }
       //    if (result.SideOrientation == 1) {
       //        normal = float3(-1, 0, 0);
       //    }
       //}
       //if (result.Side == 1) {
       //    if (result.SideOrientation == 0) {
       //        normal = float3(0, 1, 0);
       //    }
       //    if (result.SideOrientation == 1) {
       //        normal = float3(0, -1, 0);
       //    }
       //}
       //if (result.Side == 2) {
       //    if (result.SideOrientation == 0) {
       //        normal = float3(0, 0, 1);
       //    }
       //    if (result.SideOrientation == 1) {
       //        normal = float3(0, 0, -1);
       //    }
       //}


        //float3 totalLight = float3(0, 0, 0);
        //totalLight += AmbientLightColor;
        //
        //float3 lightDir = normalize(LightPosition - result.To);
        //float diffuse = saturate(dot(lightDir, normal));
        //
        //if (isBlocked(result.To, LightPosition) == 1) {
        //    diffuse = saturate(dot(-normal, normal));
        //}
        //
        //totalLight += diffuse * LightColor;
        //
        //float3 output = saturate(totalLight) * c;
        //float fog = clamp((result.Length - FogStart) / (FogEnd - FogStart), 0, 1);
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