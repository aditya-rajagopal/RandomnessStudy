using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public class HashVisualization : Visualization
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct HashJob : IJobFor {

        [ReadOnly]
        // We will also work on 4 posistions at once (The job tries to execute 4 iterations in parallel taking advantage of SMID instructions)
        // we could nto do this before because we were using float3
        // So instead we pass 4 positions at once
		public NativeArray<float3x4> positions;

        [WriteOnly]
        public NativeArray<uint4> hashes;
        public float3x4 domainTRS;
        // public SmallXXHash hash;
        // We will use the vectorized struct instead
        public SmallXXHash4 hash;

        public void Execute(int i) {
            // hashes[i] = (uint)i;
            // To add a bit more randomness we can use the Weyl sequence.
            // we try the sequence with 0.381 we then multiply by 256 so we get hashes in the range of 0 to 255 (inclusive)
            // hashes[i] = (uint)(frac(i * 0.381f) * 256f);
            // We can go one step further by using the x y coordiante rather than i
            // float vf = floor(invResolution * i + 0.000001f);
            // float uf = invResolution * (i - resolution * vf + 0.5f) - 0.5f;
            // vf = invResolution * (vf + 0.5f) - 0.5f;

            // We are now recieving positions from the position array
            
            // we can be more general and use a TRS matrix to transform the block
            // float3 p = mul(domainTRS, float4(positions[i], 1.0f));

            // // wehn we divide by 4 we are esentially grouping together groups of 4x4 but since int division rounds towards 0
            // // we have an issue where there is misalignment around the origin.
            // // we can solve this by first calculating u and v as floats and then cast to int at the end.
            // // int u = (int)floor(uf * resolution / 4f);
			// // int v = (int)floor(vf * resolution / 4f);
            // int u = (int)floor(p.x);
            // int v = (int)floor(p.y);
            // int w = (int)floor(p.z);

            // In the vecotrized form we will recieve 4 positions at once
            // We do a transpose herre to make each column a set of x, y and z coordinates
            // this allows us to vecotrize the u, v, and w calculations
            float4x3 p = domainTRS.TransformVectors(transpose(positions[i]));
            int4 u = (int4)floor(p.c0);
			int4 v = (int4)floor(p.c1);
			int4 w = (int4)floor(p.c2);

            // var hash = new SmallXXHash(0);
            // hash.Eat(u);
			// hash.Eat(v);
            // hashes[i] = (uint)(frac(u * v * 0.381f) * 256f);
            // Because of the introduciton of the method chaining we can compress the code
            hashes[i] = hash.Eat(u).Eat(v).Eat(w);
        }
    }

    [SerializeField]
	int seed;
    [SerializeField]
    SpaceTRS domain = new SpaceTRS { 
        scale = 8f
    };

    ComputeBuffer hashesBuffer;

    // Since we are vectorizing everyuthihng lets just store it all in a vectorizaton compatible form
    NativeArray<uint4> hashes;

    static int hashesId = Shader.PropertyToID("_Hashes");



    protected override void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock) {
        hashes = new NativeArray<uint4>(dataLength, Allocator.Persistent);
        hashesBuffer = new ComputeBuffer(dataLength * 4, 4);

        // we can provide the transform of the Hash object so that we may move the entire render.
        // JobHandle handle = Shapes.Job.ScheduleParallel(positions, transform.localToWorldMatrix, resolution, default);

        // new HashJob{
        //     positions = positions,
        //     hashes = hashes,
        //     hash = SmallXXHash.Seed(seed),
        //     domainTRS = domain.Matrix
        // }.ScheduleParallel(length, resolution, handle).Complete();

        // hashesBuffer.SetData(hashes);
        // positionsBuffer.SetData(positions);
        // This is now handeled in the Update function
        propertyBlock.SetBuffer(hashesId, hashesBuffer);
    }

    protected override void DisableVisualization()
    {
        hashes.Dispose();
		hashesBuffer.Release();
		hashesBuffer = null;
    }

    protected override void UpdateVisualization(NativeArray<float3x4> positions, int resolution, JobHandle handle) {
        new HashJob {
            // We can use the reinterpret function on a NativeArray to make with the original element size
            // As the input.
            // this requires that the array size be divisible by 4.
            positions = positions,
            hashes = hashes,
            hash = SmallXXHash.Seed(seed),
            domainTRS = domain.Matrix
        }.ScheduleParallel(hashes.Length, resolution, handle).Complete();

        hashesBuffer.SetData(hashes.Reinterpret<uint>(4 * 4));
    }
}
