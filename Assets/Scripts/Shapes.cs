using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Mathematics.math;

public static class Shapes {

	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
	public struct Job : IJobFor {

		[WriteOnly]
        // If we want the displacements to be in the normal direction we need the nromal vectors
		NativeArray<float3x4> positions, normals;

		public float resolution, invResolution;

        // Similar to how we apply a transformation to the hash function ( to sample colour and y offset)
        // We can similarly apply a transformation to teh shape function. This is similar to what I did in the compute shader
        // When doing the graphs
        public float3x4 positionTRS;

        float4x3 TransformVectors (float3x4 trs, float4x3 p, float w = 1f) => float4x3(
			trs.c0.x * p.c0 + trs.c1.x * p.c1 + trs.c2.x * p.c2 + trs.c3.x * w,
			trs.c0.y * p.c0 + trs.c1.y * p.c1 + trs.c2.y * p.c2 + trs.c3.y * w,
			trs.c0.z * p.c0 + trs.c1.z * p.c1 + trs.c2.z * p.c2 + trs.c3.z * w
		);

		public void Execute (int i) {
			float4x2 uv;
            float4 i4 = 4f * i + float4(0f, 1f, 2f, 3f); // we need to generate a new index vector because we have 4 points now
			uv.c1 = floor(invResolution * i4 + 0.00001f);
			uv.c0 = invResolution * (i4 - resolution * uv.c1 + 0.5f) - 0.5f;
			uv.c1 = invResolution * (uv.c1 + 0.5f) - 0.5f;

			positions[i] = transpose(TransformVectors(positionTRS, float4x3(uv.c0, 0f, uv.c1)));
            // normals[i] = normalize(mul(positionTRS, float4(0f, 1f, 0f, 1f))); // Our initial normal is in the up direction
            // we first apply our TRS matrix to matrix where only the y column is 1s and we set w to 0 so translation is ignored
            float3x4 n =
				transpose(TransformVectors(positionTRS, float4x3(0f, 1f, 0f), 0f));
            // we then store the normals in a 3x4 array
			normals[i] = float3x4(
				normalize(n.c0), normalize(n.c1), normalize(n.c2), normalize(n.c3)
			);
		}

        public static JobHandle ScheduleParallel (
			NativeArray<float3x4> positions, NativeArray<float3x4> normals, float4x4 trs, int resolution, JobHandle dependency
		) {
			return new Job {
				positions = positions,
				normals = normals,
				resolution = resolution,
				invResolution = 1f / resolution,
                positionTRS = float3x4(trs.c0.xyz, trs.c1.xyz, trs.c2.xyz, trs.c3.xyz) // Convert to a 3x4 matrix
			}.ScheduleParallel(positions.Length, resolution, dependency);
		}
	}
}