using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class NoiseVisualization : Visualization
{
    [SerializeField]
	int seed;
    [SerializeField]
    SpaceTRS domain = new SpaceTRS { 
        scale = 8f
    };

    ComputeBuffer noiseBuffer;
    NativeArray<float4> noise;
    static int noiseId = Shader.PropertyToID("_Noise");


    protected override void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock) {
        noise = new NativeArray<float4>(dataLength, Allocator.Persistent);
        noiseBuffer = new ComputeBuffer(dataLength * 4, 4);
        propertyBlock.SetBuffer(noiseId, noiseBuffer);
    }

    protected override void DisableVisualization()
    {
        noise.Dispose();
		noiseBuffer.Release();
		noiseBuffer = null;
    }

    protected override void UpdateVisualization(NativeArray<float3x4> positions, int resolution, JobHandle handle) {
        handle.Complete();
        noiseBuffer.SetData(noise.Reinterpret<float>(4 * 4));
    }
}
