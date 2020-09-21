#pragma pack_matrix(row_major);

struct PS_IN
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
	float4 Color;
	float3 SpecularColor;
	float SpecularExponent;
	float3 EmissiveColor;
	float _Padding2;
};

SamplerState DefaultSampler
{

};

Texture2D DiffuseTexture : register(t0);

float4 main(PS_IN input) : SV_Target
{
	float3 normal = normalize(input.Normal);
	float3 toLight = float3(0.0f, 1.0f, 0.0f);
	//float lambert = saturate(dot(toLight, normal)); // Real version
	float lambert = dot(toLight, normal) * 0.5f + 0.5f; // Faked version to add some ambient illumination
	float specular = 0.0f;
	float3 specularColor = SpecularColor;
	float exponent = SpecularExponent;
	float4 diffuseColor = DiffuseTexture.Sample(DefaultSampler, input.UV);
	
	if (diffuseColor.a <= 0.01f)
	{
		discard;
	}
	
	if (lambert > 0.0f && exponent > 0.0f)
	{
		float3 toCamera = normalize(CameraPosition - input.WorldPosition);
		float3 halfDir = normalize(toLight + toCamera);
		float angle = max(dot(halfDir, normal), 0.0f);
		specular = pow(angle, exponent);
	}

	return float4(lambert * diffuseColor.xyz * input.Color.xyz + specular * specularColor + EmissiveColor, diffuseColor.a * Color.a);
}