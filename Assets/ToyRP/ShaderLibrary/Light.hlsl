#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED



struct Light {
	float3 color;
	float3 direction;
	float attenuation;
};

#define MAX_DIRECTIONAL_LIGHT_COUNT 4

CBUFFER_START(_ToyRPLight)
	int _DirectionalLightCount;
	float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
	// 每个光照的方向
	float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
	// 每个光照的信息
	float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

int GetDirectionalLightCount () {
	return _DirectionalLightCount;
}



DirectionalShadowData GetDirectionalShadowData (int lightIndex, ShadowData shadowData) {
	DirectionalShadowData data;
	data.strength = _DirectionalLightShadowData[lightIndex].x *  shadowData.strength;;
	data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
	data.normalBias = _DirectionalLightShadowData[lightIndex].z;
	return data;
}

Light GetDirectionalLight (int index, Surface surfaceWS, ShadowData shadowData) {
	Light light;
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirections[index].xyz;

	// 光照的 阴影强度，阴影集所以，法线偏移，光照衰减
	DirectionalShadowData dirShadowData  = GetDirectionalShadowData(index,shadowData);
	//
	light.attenuation = GetDirectionalShadowAttenuation(dirShadowData,shadowData, surfaceWS);
	//light.attenuation = shadowData.cascadeIndex * 0.25;
	//light.attenuation = 0.1;
	
	return light;
}

#endif