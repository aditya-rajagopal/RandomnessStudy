using Unity.Mathematics;

public static partial class Noise
{

    // The specific result we use for voronoi noise is called the voronoi function.
    // we can create a new interface that contains an Evaluate funciton
    public interface IVoronoiFunction {
		float4 Evaluate (float4x2 minima);
	}

    // F1 is the function that takes teh first nearest cell point
    public struct F1 : IVoronoiFunction {

		public float4 Evaluate (float4x2 distances) => distances.c0;
	}

    // F2 is the fucntion that takes the second nearest cell point
	public struct F2 : IVoronoiFunction {

		public float4 Evaluate (float4x2 distances) => distances.c1;
	}

    public struct F2MinusF1 : IVoronoiFunction {

		public float4 Evaluate (float4x2 distances) => distances.c1 - distances.c0;
	}

}
