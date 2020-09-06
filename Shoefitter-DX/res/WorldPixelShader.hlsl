#pragma pack_matrix(row_major);

struct PS_IN
{
	float4 Position : SV_Position;
	float3 Normal : NORMAL0;
	float2 UV : TEXCOORD0;
	float4 Color : COLOR0;
};

float4 main(PS_IN input) : SV_Target
{
	return input.Color;
}