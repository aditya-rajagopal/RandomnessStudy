using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Mathematics.math;

public static class Shapes {

    // We can create a delegate for the ScheduleParallel functions and the createa an enum + list of calls
    // in Hash visualization to easily change shape
    public delegate JobHandle ScheduleDelegate (
		NativeArray<float3x4> positions, NativeArray<float3x4> normals,
		 float4x4 trs, int resolution, JobHandle dependency
	);

    // We want to support multiple shapes so first lets move the uv calculation out of the job
    // we also make it so the UV is b etween 0-1 instead of -0.5-0.5
    public static float4x2 IndexTo4UV (int i, float resolution, float invResolution) {
		float4x2 uv;
		float4 i4 = 4f * i + float4(0f, 1f, 2f, 3f);
		uv.c1 = floor(invResolution * i4 + 0.00001f);
		uv.c0 = invResolution * (i4 - resolution * uv.c1 + 0.5f);
		uv.c1 = invResolution * (uv.c1 + 0.5f);
		return uv;
	}

    // The only thing the jobs should do is set the position and normal
    // We can define a struct to pass this information
    public struct Point4 {
		public float4x3 positions, normals;
	}

    // We want our code to work for any generic shape struct. To do this we can define an interface
    // An interface is kind of like an enforcement for structs or classes to mandate what public methods or properties it must have
    public interface IShape {
		Point4 GetPoint4 (int i, float resolution, float invResolution);
	}


    public struct Plane : IShape {

        // we can move the transformation of the points to make a plane (the initial default job we had) to a seperate struct and function
        public Point4 GetPoint4 (int i, float resolution, float invResolution) {
            float4x2 uv = IndexTo4UV(i, resolution, invResolution);
            return new Point4 {
                positions = float4x3(uv.c0 - 0.5f, 0f, uv.c1 - 0.5f),
                normals = float4x3(0f, 1f, 0f)
            };
        }
    }

    // now that we have made shapes hotswappable lets make some new shapes

    public struct Sphere : IShape {
        public Point4 GetPoint4 (int i, float resolution, float invResolution) {
            float4x2 uv = IndexTo4UV(i, resolution, invResolution);

            float r = 0.5f;
            float4 s = r * sin(PI * uv.c1);
            Point4 p;
            p.positions.c0 = s * sin(2f * PI * uv.c0);
            p.positions.c1 = r * cos(PI * uv.c1);
            p.positions.c2 = s * cos(2f * PI * uv.c0);
            p.normals = p.positions;
            return p;
        }
    }

    // One issue with the UV sphere is that the distribution of points is not unifrom with more points near the pole than the equator
    // So lets make a different type of sphere called the octahedron sphere.
    // To do this we first generate an octahedron from 0-1 UV coordionates and then nromalize all its position vectors.
    public struct OctaSphere : IShape {
        public Point4 GetPoint4 (int i, float resolution, float invResolution) {
            float4x2 uv = IndexTo4UV(i, resolution, invResolution);

            Point4 p;
            p.positions.c0 = uv.c0 - 0.5f;
            p.positions.c1 = uv.c1 - 0.5f;
            p.positions.c2 = 0.5f - abs(p.positions.c0) - abs(p.positions.c1);
            float4 offset = max(-p.positions.c2, 0f);
            p.positions.c0 += select(-offset, offset, p.positions.c0 < 0f);
			p.positions.c1 += select(-offset, offset, p.positions.c1 < 0f);
            p.normals = p.positions;

            // to make it a sphere find out how much to scale the position to bring its distance to 0.5 and then
            // push it along its direction and scale it up or down
            float4 scale = 0.5f * rsqrt(
				p.positions.c0 * p.positions.c0 +
				p.positions.c1 * p.positions.c1 +
				p.positions.c2 * p.positions.c2
			);
			p.positions.c0 *= scale;
			p.positions.c1 *= scale;
			p.positions.c2 *= scale;
            return p;
        }
    }

    public struct Torus : IShape {

		public Point4 GetPoint4 (int i, float resolution, float invResolution) {
			float4x2 uv = IndexTo4UV(i, resolution, invResolution);

			float r1 = 0.375f;
			float r2 = 0.125f;
			float4 s = r1 + r2 * cos(2f * PI * uv.c1);

			Point4 p;
			p.positions.c0 = s * sin(2f * PI * uv.c0);
			p.positions.c1 = r2 * sin(2f * PI * uv.c1);
			p.positions.c2 = s * cos(2f * PI * uv.c0);
			p.normals = p.positions;
            p.normals.c0 -= r1 * sin(2f * PI * uv.c0);
			p.normals.c2 -= r1 * cos(2f * PI * uv.c0);
			return p;
		}
	}
    
    // We can make our Job generic by adding a template based ont eh shape struct we pass
    // To ensure tat we can limit S to be a struct and it must implement the IShape interface
	[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
	public struct Job<S> : IJobFor where S: struct, IShape{

		[WriteOnly]
        // If we want the displacements to be in the normal direction we need the nromal vectors
		NativeArray<float3x4> positions, normals;

		public float resolution, invResolution;

        // Similar to how we apply a transformation to the hash function ( to sample colour and y offset)
        // We can similarly apply a transformation to teh shape function. This is similar to what I did in the compute shader
        // When doing the graphs

        // When we have non-uniform scaling values it causes issues in the sphere and torus as the displacement is always away from the center
        // instead of away from the surface it is on.
        // So we calculate a different transformation amtrix for the normals
        public float3x4 positionTRS, normalTRS;

        float4x3 TransformVectors (float3x4 trs, float4x3 p, float w = 1f) => float4x3(
			trs.c0.x * p.c0 + trs.c1.x * p.c1 + trs.c2.x * p.c2 + trs.c3.x * w,
			trs.c0.y * p.c0 + trs.c1.y * p.c1 + trs.c2.y * p.c2 + trs.c3.y * w,
			trs.c0.z * p.c0 + trs.c1.z * p.c1 + trs.c2.z * p.c2 + trs.c3.z * w
		);

		public void Execute (int i) {
			// float4x2 uv;
            // float4 i4 = 4f * i + float4(0f, 1f, 2f, 3f); // we need to generate a new index vector because we have 4 points now
			// uv.c1 = floor(invResolution * i4 + 0.00001f);
			// uv.c0 = invResolution * (i4 - resolution * uv.c1 + 0.5f) - 0.5f;
			// uv.c1 = invResolution * (uv.c1 + 0.5f) - 0.5f;
            // We moved all the above calculations into its own function
            // We can just call that now

            // Plane does not have any fields but we can invoke the function using the default Plane value
            // Point4 p = default(Plane).GetPoint4(i, resolution, invResolution);
            // Now that we have templated Job we can use S to generalize job
            Point4 p = default(S).GetPoint4(i, resolution, invResolution);

			positions[i] = transpose(TransformVectors(positionTRS, p.positions));
            // normals[i] = normalize(mul(positionTRS, float4(0f, 1f, 0f, 1f))); // Our initial normal is in the up direction
            // we first apply our TRS matrix to matrix where only the y column is 1s and we set w to 0 so translation is ignored
            float3x4 n =
				transpose(TransformVectors(positionTRS, p.normals, 0f));
            // we then store the normals in a 3x4 array
			normals[i] = float3x4(
				normalize(n.c0), normalize(n.c1), normalize(n.c2), normalize(n.c3)
			);
		}

        public static JobHandle ScheduleParallel (
			NativeArray<float3x4> positions, NativeArray<float3x4> normals, float4x4 trs, int resolution, JobHandle dependency
		) {
            // The correct surface normal vector is the transpose of the inverse 4x4 TRS matrix
            float4x4 tim = transpose(inverse(trs));
			return new Job<S> {
				positions = positions,
				normals = normals,
				resolution = resolution,
				invResolution = 1f / resolution,
                positionTRS = float3x4(trs.c0.xyz, trs.c1.xyz, trs.c2.xyz, trs.c3.xyz), // Convert to a 3x4 matrix
                normalTRS = float3x4(tim.c0.xyz, tim.c1.xyz, tim.c2.xyz, tim.c3.xyz)
			}.ScheduleParallel(positions.Length, resolution, dependency);
		}
	}
}