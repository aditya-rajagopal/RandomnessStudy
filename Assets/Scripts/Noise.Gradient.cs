using Unity.Mathematics;

using static Unity.Mathematics.math;

public static partial class Noise
{
    // Gradient noise is an extension of the concept of lattice noise
    // In lattice noise each lattice point is associated with a noise value and the rest of the points are interpolated. But lattice points are still visable
    // An alternative is to interpolate between functions instgead of constant values. This means each lattice point has its own function.
    // all points usually get the same function but the parametrization of the function changes. 
    // the most trivial function is the constant value function which is value noise studied earlier
    // A simple one we can start with is f(x) = x. Where x is the relative coordinate from the alttice point. This would produce a linear ramp

    public interface IGradient {

        // The goal of this function is to evaluate the a function with a given hash and relative coordinate
        // we use float4s to keep it vectorized like the other functions we have used.
        float4 Evaluate (SmallXXHash4 hash, float4 x);
        // Let's also support 2D and 3D noise in the interface

        float4 Evaluate (SmallXXHash4 hash, float4 x, float4 y);

        float4 Evaluate (SmallXXHash4 hash, float4 x, float4 y, float4 z);
    }

    public struct Value : IGradient {

        // The trivial case where we just return the A float of the hash value.
        // we move the normalization here
        public float4 Evaluate (SmallXXHash4 hash, float4 x) => hash.Floats01A * 2f - 1f;
        // we can temporarily replace Value 1D with f(x) = x
        // public float4 Evaluate (SmallXXHash4 hash, float4 x) => x;

        // here for 2D and 3D noise we return all the values back as it is
        // However using these below will not affect performance more because the burst
        // compiler will remove any unused parameters and invocation overhead

        public float4 Evaluate (SmallXXHash4 hash, float4 x, float4 y) => hash.Floats01A * 2f - 1f;

		public float4 Evaluate (SmallXXHash4 hash, float4 x, float4 y, float4 z) => hash.Floats01A * 2f - 1f;
    }

}
