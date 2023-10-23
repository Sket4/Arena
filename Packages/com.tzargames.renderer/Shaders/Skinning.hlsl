#ifndef TG_SKINNING_INCLUDED
#define TG_SKINNING_INCLUDED

void ComputeSkinning_OneBone(uint boneIndex, inout float3 positionOS, inout float3 normalOS, inout float3 tangentOS)
{
#if defined(DOTS_INSTANCING_ON)
    uint4 skinningData = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(uint4, _SkinningData);

    uint offset = skinningData.x + boneIndex * 48;

    float3x4 skinMatrix;

    float4 p1 = asfloat(DOTSInstanceData_Load4(offset + 0 * 16));
    float4 p2 = asfloat(DOTSInstanceData_Load4(offset + 1 * 16));
    float4 p3 = asfloat(DOTSInstanceData_Load4(offset + 2 * 16));

    skinMatrix = float3x4(p1.x, p1.w, p2.z, p3.y, p1.y, p2.x, p2.w, p3.z, p1.z, p2.y, p3.x, p3.w);

    positionOS = mul(skinMatrix, float4(positionOS, 1));
    normalOS = mul(skinMatrix, float4(normalOS, 0));
    tangentOS = mul(skinMatrix, float4(tangentOS, 0));
#endif
}

#endif