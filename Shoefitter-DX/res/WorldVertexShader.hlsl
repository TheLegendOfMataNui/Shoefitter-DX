#pragma pack_matrix(row_major);

struct VS_IN
{
	float3 Position : POSITION0;
	float3 Normal : NORMAL0;
	float2 UV : TEXCOORD0;
	float4 Color : COLOR0;
};

struct VS_OUT
{
	float4 Position : SV_Position;
	float3 Normal : NORMAL0;
	float2 UV : TEXCOORD0;
	float4 Color : COLOR0;
};

cbuffer FrameConstants : register(b0)
{
	float4x4 ViewMatrix;
	float4x4 ProjectionMatrix;
	float2 ViewportPosition;
	float2 ViewportSize;
};

cbuffer WorldInstanceConstants : register(b1)
{
	float4x4 Model;
};

VS_OUT main(VS_IN input)
{
	VS_OUT result;
	
	result.Position = mul(Model, float4(input.Position, 1.0f));
	result.Position = mul(ProjectionMatrix, mul(ViewMatrix, result.Position));
	result.Normal = mul(Model, float4(input.Normal, 0.0f)); // Assumes uniform scale
	result.UV = input.UV;
	result.Color = input.Color;
	
	return result;
}