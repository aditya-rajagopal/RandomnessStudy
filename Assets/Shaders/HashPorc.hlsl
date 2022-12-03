#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
    StructuredBuffer<uint> _Hashes;
    StructuredBuffer<float3> _Positions;
    StructuredBuffer<float3> _Normals;
#endif

float4 _Config;

void ConfigureProcedural () {
    #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
        // Split 1 D array locations int a x, y 2D locaiton  using the 1/resolution
        // value stored in the config file.
        // we add a very small number 0.000001 because of floating point limitations when 
        // we do instanceID/resolution we get numbers that are a tiny bit smaller than a whole number
        // Which then causes misalignment. 
        // float v = floor(_Config.y * unity_InstanceID + 0.00001);
        // float u = unity_InstanceID - _Config.x * v;
        // We are now receiving positions from a buffer instead of calculating it from the instance ID

        // Now we define the object to world transformation
        // We start with a 0s 4x4 matrix
        unity_ObjectToWorld = 0.0;
        // We set the last column to the position of the object
        //     // We scale the position to be between 0 and 1 with 1/resolution and center it around the origin.
        //     _Config.y * (u + 0.5) - 0.5,
        //     // We have the 4th byte of the hash function that we dont use in the colour. We can instead use it to define an offset
        //     // in the y direction based on the hash
        //     _Config.z * ((1.0 / 255.0) * (_Hashes[unity_InstanceID] >> 24) - 0.5),
        //     _Config.y * (v + 0.5) - 0.5,
        // Once again we have position already
        unity_ObjectToWorld._m03_m13_m23_m33 = float4(
			_Positions[unity_InstanceID],
			1.0
		);
        // We still want that y offset based on the 4th byte of the hash function
        // With normals we can adjust the displacement in the nromal direction using the normal vector
        unity_ObjectToWorld._m03_m13_m23 +=  _Config.z * ((1.0 / 255.0) * (_Hashes[unity_InstanceID] >> 24) - 0.5) * _Normals[unity_InstanceID];
        // We also scale down the cube so that if we give a uniform coordinate it should tile the plane
        unity_ObjectToWorld._m00_m11_m22 = _Config.y;
    #endif
}

// Dont be an idito and set return type to unit and be surprised when you see all cubes have a black colour
float3 GetHashColor () {
	#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		uint hash = _Hashes[unity_InstanceID];
		// return _Config.y * _Config.y * hash;
        // one cool trick we can do is playing iwth the bits of the hash id
        // if we do a bitwise and with 255 (11111111) we will only get the first 8 significant bits of the hash
        // essentially limiting it to a number between 0 and 255 that repeats
        // this producesa a nice repeating pattern for resolution 32 (and in general powers of 2) but breaks for everything else.

        // We can also shift the hash right by 8 bits to look at the next 8 significant bits
        // return (1.0 / 255.0) * ((hash >> 8) & 255);
        // We can combine this by taking the 3 most significant bytes of the hash value and converting it into a colour
        return (1.0 / 255.0) * float3(
            hash & 255,
            (hash >> 8) & 255,
            (hash >> 16) & 255
        );
	#else
		return 1.0;
	#endif
}

// Standard InOut function to be able to put it in a shader graph.
void ShaderGraphFunction_float (float3 In, out float3 Out, out float3 Color) {
	Out = In;
	Color = GetHashColor();
}

void ShaderGraphFunction_half (half3 In, out half3 Out, out half3 Color) {
	Out = In;
	Color = GetHashColor();
}