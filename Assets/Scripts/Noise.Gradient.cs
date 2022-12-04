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

        // We want to introduce terbulance variat of our noise functions. To achieve this we need to add up the
        // absolute values of the octaves. We will define a function that runs post noise value generation 
        // and handeled by each gradient type.
        float4 EvaluateAfterInterpolation (float4 value);
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

        // the normal value noise returns the normal value
        public float4 EvaluateAfterInterpolation (float4 value) => value;
    }

    
    // Perlin Noise
    public struct Perlin : IGradient {

        // In the case of a 1D noise we can use the f(x) = x and f(x) = -x. Switching before them
        // based on the first bit of the hash value.
        // This leads to 4 combinations between a lattice span, p-p, p-n, n-n and n-p.
        // realistically we only have 2: same gradient sign and opposite sign.
        // And in both cases the 
        // public float4 Evaluate (SmallXXHash4 hash, float4 x) => 
        // // the noise reaches a maximum of 0.5 halfway between gradient points with opposite gradients
        // // we scale it by 2 to achieve max noise of 1.
        //     2f * select(-x, x, ((uint4)hash & 1) == 0);
        public float4 Evaluate (SmallXXHash4 hash, float4 x) =>
        // Tje above selection b ased gradient is too rigid where it has peaks when gradients are opposite direcion
        // else it has a very strucuted periodic pattern.
        // alternatively instead of the above binary selection we can scale the relative coordinatge by a factor
        // in the range of -1 to .
            // 2f * (hash.Floats01A * 2f - 1f) * x;
        // This produces more interesting looking variations. It has different amplitudes and is negative often as well
        // One drawback is that it is not very likely to reach the maximum amplitude.
        // we can combine the 2 approaches. We select sign with the first bit and then scale by floatA. This does make it a bit
        // too dependant on the first bit. We can instead use the 9th bit of the hash to determine sign
            // 2f * hash.Floats01A * select(-x, x, ((uint4)hash & 1 << 8) == 0);
        // Taking it one step further we can introduce a minimum amplitude regardlss of the gradient direction. This avoids
        // areas where the scale ends up close to 0 (from FloatA). We can simply set the minimum amplitude to 1
        // This will garuntee a minimum amplitude wave similar to the binary selection approach but also have variation on top of that
            (1f + hash.Floats01A) * select(-x, x, ((uint4)hash & 1 << 8) == 0);
        


        public float4 Evaluate (SmallXXHash4 hash, float4 x, float4 y) 
        // Lets try the simple binary selection hash based on the x domain
            // =>select(-x, x, ((uint4)hash & 1) == 0);
        // Similarly lets also try the second approach we tried in 1D
            //=> 2f * (hash.Floats01A * 2f - 1f) * x;
        // We are not limited to 1D noise here. We can use both axis
        // here what we do is first generate a line of x in range [-1, 1] using the hash value of floatA
        // then we generate y component of the gradient by taking the line 0.5 - abs(x) this will make a wedge /\
        // Around the y axis. Then we close the square by subtracting floor(x + 0.5) from x. This will force all the x > 0.5
        // to be between -0.5 and 0 and all the x < -0.5 to be between 0 and 0.5. Everything from -0.5 to 0.5 will remain the same
        // Thus closing the square.
        // This generates a gradient vector that could be on any point of the square depending on the hash value
        // The maximum value it can take is 0.5 at the corners of the square hence we scale it by at the end.
        {
            float4 gx = hash.Floats01A * 2f - 1f;
			float4 gy = 0.5f - abs(gx);
			gx -= floor(gx + 0.5f);
            // magic normalization number that I dont really understand how we arrived at?
            // we try to find the maximum magnitude of the gradient given a 2D grid. We have to find the maximum
            // of xs(1-x) + 0.5s(x) for x \in [0, 1] where s(t) = 6t^5 - 15t^4 + 10t^3. We use wolframAlpha to find the maximum value
            // which is 0.53528f
			return (gx * x + gy * y) * ( 2f / 0.53528f);
        }

        public float4 Evaluate (SmallXXHash4 hash, float4 x, float4 y, float4 z) {
            // For 3D we do the same thing we did for 2D but expanded to the 3rd dimension
            // this is similar to how the octahedral worked
            // generate a plane in x, y [0, 1]
            float4 gx = hash.Floats01A * 2f - 1f, gy = hash.Floats01D * 2f - 1f;

            // deform so that yhou form a wedge around the z axis
			float4 gz = 1f - abs(gx) - abs(gy);

            // calculate an offset based on the z position
			float4 offset = max(-gz, 0f);

            // apply the offset based on which quadrant of the x, y plane the point is on to get
            // a rombus
			gx += select(-offset, offset, gx < 0f);
			gy += select(-offset, offset, gy < 0f);

            // Actual perlin noise actually only has 16 gradients. This way we can convert 4bits into a gradient.
            // However this nested binary branching is not very efficient for SIMD code. This octahedron method
            // is actually faster if we dont normalize the vector. Additionally it gives a bit more variety.

            // magic number of maximum using a similar method used in the 2D case. Here the 2D lattice plane has achieved
            // its maximum of 0.53528 so we need to maximize xs(1-x) + 0.53528s(x) which yeilds 0.56290
			return (gx * x + gy * y + gz * z) * (1f / 0.56290f);
        }
        
        // So does the basic gradient function
        public float4 EvaluateAfterInterpolation (float4 value) => value;

    }

    // instead of duplicating noise functions to have turbulent variants we will create a template
    public struct Turbulence<G> : IGradient where G : struct, IGradient {

		public float4 Evaluate (SmallXXHash4 hash, float4 x) =>
			default(G).Evaluate(hash, x);

		public float4 Evaluate (SmallXXHash4 hash, float4 x, float4 y) =>
			default(G).Evaluate(hash, x, y);

		public float4 Evaluate (SmallXXHash4 hash, float4 x, float4 y, float4 z) =>
			default(G).Evaluate(hash, x, y, z);

		public float4 EvaluateAfterInterpolation (float4 value) =>
			abs(default(G).EvaluateAfterInterpolation(value));
	}

}
