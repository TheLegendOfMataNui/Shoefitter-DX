#pragma pack_matrix(row_major);

struct VS_IN
{
	float3 Position : POSITION0;
	float3 Normal : NORMAL0;
	float2 UV : TEXCOORD0;
	float4 Color : COLOR0;
	float4 BoneWeights : BLENDWEIGHTS0;
	uint4 BoneIndices : BLENDINDICES0;
};

struct VS_OUT
{
	float4 Position : SV_Position;
	float3 WorldPosition : TEXCOORD1;
	float3 Normal : NORMAL0;
	float4 Color : COLOR0;
	float2 UV : TEXCOORD0;
};

cbuffer FrameConstants : register(b0)
{
	float4x4 ViewMatrix;
	float4x4 ProjectionMatrix;
	float2 ViewportPosition;
	float2 ViewportSize;
	float3 CameraPosition;
	float _Padding;
};

cbuffer WorldInstanceConstants : register(b1)
{
	float4x4 Model;
};

cbuffer BindPoseBuffer : register(b2)
{
	float4x4 BindPoses[255];
};

cbuffer PoseBuffer : register(b3)
{
	float4x4 JointPoses[255];
};

VS_OUT main(VS_IN input)
{
	VS_OUT result;
	
	float4 skinnedPosition = float4(0.0f, 0.0f, 0.0f, 0.0f);
	float4 skinnedNormal = float4(0.0f, 0.0f, 0.0f, 0.0f);
	for (int i = 0; i < 4; i++)
	{
		int boneIndex = input.BoneIndices[i];
		skinnedPosition += input.BoneWeights[i] * mul(JointPoses[boneIndex], mul(BindPoses[boneIndex], float4(input.Position, 1.0f)));
		skinnedNormal += input.BoneWeights[i] * mul(JointPoses[boneIndex], mul(BindPoses[boneIndex], float4(input.Normal, 0.0f))); // HACK: Assumes uniform scale.
	}
	
	result.Position = mul(Model, float4(skinnedPosition.xyz, 1.0f));
	result.Position = mul(ProjectionMatrix, mul(ViewMatrix, result.Position));
	result.Normal = mul(Model, float4(normalize(skinnedNormal.xyz), 0.0f)); // Assumes uniform scale
	result.UV = input.UV;
	result.Color = input.Color;
	
	return result;
}