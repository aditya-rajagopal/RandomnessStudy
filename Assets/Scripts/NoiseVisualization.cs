using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Noise;

public class NoiseVisualization : Visualization
{
    public enum NoiseType {Lattice1D, Lattice2D, Lattice3D};

    public enum GradientType {Value, Perlin}

    public Noise.ScheduleDelegate[,] NoiseJobs = {
        {
        Job<LatticeID<Value>>.ScheduleParallel,
        Job<Lattice2D<Value>>.ScheduleParallel,
        Job<Lattice3D<Value>>.ScheduleParallel
        },
        {
            Job<LatticeID<Perlin>>.ScheduleParallel,
            Job<Lattice2D<Perlin>>.ScheduleParallel,
            Job<Lattice3D<Perlin>>.ScheduleParallel
        }
    };

    // void GenerateNoiseDelegateArray(){
    //     for (int i = 0; i < NoiseType)
    // }
    
    [SerializeField]
    public NoiseType noiseType = NoiseType.Lattice3D;
    [SerializeField]
	GradientType gradientType = GradientType.Perlin;
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
        NoiseJobs[(int)gradientType, (int)noiseType](positions, noise, seed, domain, resolution, handle).Complete();
        // NoiseJobs[(int)gradientType, (int)noiseType](positions, noise, seed, new SpaceTRS {scale = 64}, resolution, jobHandle).Complete();
        noiseBuffer.SetData(noise.Reinterpret<float>(4 * 4));
    }
}
