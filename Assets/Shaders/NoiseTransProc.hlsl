#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    StructuredBuffer<float> _Noise;
    StructuredBuffer<float3> _Positions;
    StructuredBuffer<float3> _Normals;
#endif

float4 _Config;

void ConfigureProcedural () {
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
        unity_ObjectToWorld = 0.0;
        unity_ObjectToWorld._m03_m13_m23_m33 = float4(
			_Positions[unity_InstanceID],
			1.0
		);
        unity_ObjectToWorld._m03_m13_m23 +=  _Config.z * _Noise[unity_InstanceID] * _Normals[unity_InstanceID];
        unity_ObjectToWorld._m00_m11_m22 = _Config.y;
    #endif
}

// Dont be an idito and set return type to unit and be surprised when you see all cubes have a black colour
float4 GetNoiseColor (bool EnableAlpha) {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		float noise = _Noise[unity_InstanceID];
        if (EnableAlpha)
        {
		    return noise < 0.0 ? float4(0.0, 0.0, 0.0, 0.0f) : float4(noise, noise, noise, 1.0);
        }
        else
        {
            return noise < 0.0 ? float4(0.0, 0.0, 0.0f, 1.0f) : float4(noise, noise, noise, 1.0);
        }
	#else
		return 1.0;
	#endif
}

// Standard InOut function to be able to put it in a shader graph.
void ShaderGraphFunction_float (float3 In, bool EnableAlpha, out float3 Out, out float4 Color) {
	Out = In;
	Color = GetNoiseColor(EnableAlpha);
}

void ShaderGraphFunction_half (half3 In, bool EnableAlpha, out half3 Out, out half4 Color) {
	Out = In;
	Color = GetNoiseColor(EnableAlpha);
}