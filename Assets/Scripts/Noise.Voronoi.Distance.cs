using Unity.Mathematics;

using static Unity.Mathematics.math;

public static partial class Noise
{
    // We can even use non eucalidian distance
    public interface IVoronoiDistance {
		float4 GetDistance (float4 x);

		float4 GetDistance (float4 x, float4 y);

		float4 GetDistance (float4 x, float4 y, float4 z);

        float4x2 Finalize1D (float4x2 minima);

		float4x2 Finalize2D (float4x2 minima);

		float4x2 Finalize3D (float4x2 minima);
	}


    // This is the eucaledian distance struct. It was introduced by steave worly so it is called
    // the worly distance voronoi noise.
    public struct Worley : IVoronoiDistance {
        public float4 GetDistance (float4 x) => abs(x);

        // We can delay the sqrt to the finalize step since we know x and y are always positive
        // this will save us from calculating sqrt every iteration.
		public float4 GetDistance (float4 x, float4 y) => x * x + y * y;

		public float4 GetDistance (float4 x, float4 y, float4 z) =>
			x * x + y * y + z * z;

		public float4x2 Finalize1D (float4x2 minima) => minima;

		public float4x2 Finalize2D (float4x2 minima) {
			minima.c0 = sqrt(min(minima.c0, 1f));
			minima.c1 = sqrt(min(minima.c1, 1f));
			return minima;
		}

		public float4x2 Finalize3D (float4x2 minima) => Finalize2D(minima);
    }


    // Another distance function we can use is the chebyshev distance. The chess distance
    // describes how many steps would it take a king to reach a destination on the chess board
    // Because of the way it is defined the maximum possible distance is the same in all directions so we dont need
    // to limit the minima
    public struct Chebyshev : IVoronoiDistance {

		public float4 GetDistance (float4 x) => abs(x);

		public float4 GetDistance (float4 x, float4 y) => max(abs(x), abs(y));

		public float4 GetDistance (float4 x, float4 y, float4 z) =>
			max(max(abs(x), abs(y)), abs(z));

		public float4x2 Finalize1D (float4x2 minima) => minima;

		public float4x2 Finalize2D (float4x2 minima) => minima;

		public float4x2 Finalize3D (float4x2 minima) => minima;
	}



}
