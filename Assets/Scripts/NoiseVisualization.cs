using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Noise;

public class NoiseVisualization : Visualization
{
    public enum NoiseType {Lattice1D, Lattice2D, Lattice3D};

    public enum GradientType {Value, ValueTerbulant, Perlin, PerlinTerbulant, VoronoiWorleyF1, VoronoiWorleyF2, VoronoiWorleyF2MinusF1,
    VoronoiChebyshevF1, VoronoiChebyshevF2, VoronoiChebyshevF2MinusF1}

    [SerializeField]
	bool enableTiling;

    public Noise.ScheduleDelegate[,] NoiseJobs = {
        {
            Job<Lattice1D<LatticeNormal, Perlin>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Perlin>>.ScheduleParallel,
            Job<Lattice2D<LatticeNormal, Perlin>>.ScheduleParallel,
            Job<Lattice2D<LatticeTiling, Perlin>>.ScheduleParallel,
            Job<Lattice3D<LatticeNormal, Perlin>>.ScheduleParallel,
            Job<Lattice3D<LatticeTiling, Perlin>>.ScheduleParallel
        },
         {
			Job<Lattice1D<LatticeTiling, Turbulence<Perlin>>>.ScheduleParallel,
			Job<Lattice1D<LatticeNormal, Turbulence<Perlin>>>.ScheduleParallel,
			Job<Lattice2D<LatticeTiling, Turbulence<Perlin>>>.ScheduleParallel,
			Job<Lattice2D<LatticeNormal, Turbulence<Perlin>>>.ScheduleParallel,
			Job<Lattice3D<LatticeTiling, Turbulence<Perlin>>>.ScheduleParallel,
			Job<Lattice3D<LatticeNormal, Turbulence<Perlin>>>.ScheduleParallel
		},
        {
            Job<Lattice1D<LatticeNormal, Value>>.ScheduleParallel,
            Job<Lattice1D<LatticeTiling, Value>>.ScheduleParallel,
            Job<Lattice2D<LatticeNormal, Value>>.ScheduleParallel,
            Job<Lattice2D<LatticeTiling, Value>>.ScheduleParallel,
            Job<Lattice3D<LatticeNormal, Value>>.ScheduleParallel,
            Job<Lattice3D<LatticeTiling, Value>>.ScheduleParallel
        },
        {
			Job<Lattice1D<LatticeNormal, Turbulence<Value>>>.ScheduleParallel,
			Job<Lattice1D<LatticeTiling, Turbulence<Value>>>.ScheduleParallel,
			Job<Lattice2D<LatticeNormal, Turbulence<Value>>>.ScheduleParallel,
			Job<Lattice2D<LatticeTiling, Turbulence<Value>>>.ScheduleParallel,
			Job<Lattice3D<LatticeNormal, Turbulence<Value>>>.ScheduleParallel,
			Job<Lattice3D<LatticeTiling, Turbulence<Value>>>.ScheduleParallel
		},
        {
            Job<Voronoi1D<LatticeNormal, Worley, F1>>.ScheduleParallel,
			Job<Voronoi1D<LatticeTiling, Worley, F1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeNormal, Worley, F1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeTiling, Worley, F1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeNormal, Worley, F1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeTiling, Worley, F1>>.ScheduleParallel
        },
        {
            Job<Voronoi1D<LatticeNormal, Worley, F2>>.ScheduleParallel,
			Job<Voronoi1D<LatticeTiling, Worley, F2>>.ScheduleParallel,
			Job<Voronoi2D<LatticeNormal, Worley, F2>>.ScheduleParallel,
			Job<Voronoi2D<LatticeTiling, Worley, F2>>.ScheduleParallel,
			Job<Voronoi3D<LatticeNormal, Worley, F2>>.ScheduleParallel,
			Job<Voronoi3D<LatticeTiling, Worley, F2>>.ScheduleParallel
        },
        {
            Job<Voronoi1D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi1D<LatticeTiling, Worley, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeTiling, Worley, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeTiling, Worley, F2MinusF1>>.ScheduleParallel
        },
        {
            // 1D chebyshev is the same as worly so we dont need unity to generate the 
            // parallel job
            Job<Voronoi1D<LatticeNormal, Worley, F1>>.ScheduleParallel,
			Job<Voronoi1D<LatticeTiling, Worley, F1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeNormal, Chebyshev, F1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeTiling, Chebyshev, F1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeNormal, Chebyshev, F1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeTiling, Chebyshev, F1>>.ScheduleParallel
        },
        {
            Job<Voronoi1D<LatticeNormal, Worley, F2>>.ScheduleParallel,
			Job<Voronoi1D<LatticeTiling, Worley, F2>>.ScheduleParallel,
			Job<Voronoi2D<LatticeNormal, Chebyshev, F2>>.ScheduleParallel,
			Job<Voronoi2D<LatticeTiling, Chebyshev, F2>>.ScheduleParallel,
			Job<Voronoi3D<LatticeNormal, Chebyshev, F2>>.ScheduleParallel,
			Job<Voronoi3D<LatticeTiling, Chebyshev, F2>>.ScheduleParallel
        },
        {
            Job<Voronoi1D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi1D<LatticeTiling, Worley, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeNormal, Chebyshev, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi2D<LatticeTiling, Chebyshev, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeNormal, Chebyshev, F2MinusF1>>.ScheduleParallel,
			Job<Voronoi3D<LatticeTiling, Chebyshev, F2MinusF1>>.ScheduleParallel
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
	Settings noiseSettings = Settings.Default;

    [SerializeField]
    SpaceTRS domain = new SpaceTRS { 
        scale = 8f
    };
    

    ComputeBuffer noiseBuffer;
    NativeArray<float4> noise;
    static int noiseId = Shader.PropertyToID("_Noise");


    protected override void EnableVisualization(int dataLength, MaterialPropertyBlock propertyBlock) {
        // localDomainCopy = domain;
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

        if (enableAnimation)
        {
            domain.translation += new float3(animationSpeed * Time.deltaTime, animationSpeed * (1f / 5f) * Time.deltaTime, animationSpeed * Time.deltaTime);
        }

        NoiseJobs[(int)gradientType , 2 * ((int)noiseType + 1)- (enableTiling ? 1 : 2)](positions, noise, noiseSettings, domain, resolution, handle).Complete();
        // NoiseJobs[(int)gradientType, (int)noiseType](positions, noise, seed, new SpaceTRS {scale = 64}, resolution, jobHandle).Complete();
        noiseBuffer.SetData(noise.Reinterpret<float>(4 * 4));
    }
}
