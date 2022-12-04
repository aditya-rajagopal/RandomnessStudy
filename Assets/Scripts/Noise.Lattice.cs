using Unity.Mathematics;

using static Unity.Mathematics.math;


public static partial class Noise
{
    struct LatticeSpan4 {
        public int4 p0, p1;

        // Add a variable to store the relative coordiante for the gradients
        // once again a float4 for vectorization
        // However since we only use 1 gradient we will get repeating 1D ramps going from -1 to 1 between lattice points
        // there is no blending because we use the same gradient on both sides. To turn our implementation into a proper gradient on both sides
        // so we use a second gradient relative to p1.
        public float4 g0, g1;
        public float4 t;
    }


    static LatticeSpan4 GetLatticeSpan4 (float4 coordinates) {
		float4 points = floor(coordinates);
		LatticeSpan4 span;
		span.p0 = (int4)points;
		span.p1 = span.p0 + 1;
        span.g0 = coordinates - span.p0;
        // p1 sits exactly 1 unit away so we can get g1 by subtracting 1 from g0 
        span.g1 = span.g0 - 1f;
		span.t = coordinates - points;
        // span.t = smoothstep(0f, 1f, span.t);
        // The above function smoothstep is 3t^2 - 6t^3 which means its first order derivative is continuous
        // and itends at 0 boths sides of the smoothstep. But its second derivative is not continuous
        // we instead use the following function which is C2-continious
        // This isnt a problem for us but if we do a smooth mesh or something we will see very visable creases.
        span.t = span.t * span.t * span.t * (span.t * (span.t * 6f - 15f) + 10f);
		return span;
	}

    // we can make a template of Lattice1D to use a generic gradient function
    public struct LatticeID<G> : INoise where G : struct, IGradient {
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash) {
            // int4 p0 = (int4)floor(positions.c0);
            // int4 p1 = p0 + 1;

            // return hash.Eat(p0).Floats01A + hash.Eat(p1).Floats01A - 1f;
            // We can do teh same with a lerp
            // we can take the fraction part by subtracting the floor from the original position value
            // and interpolate between the 2 positiosn based on this fraction
            // float4 t = positions.c0 - p0;

            // we can simplify the calculations above using GetLatticeSpan4
            LatticeSpan4 x = GetLatticeSpan4(positions.c0);
            // Get the default value struct
            // we will use the value function here instead 
            // This will not change the resutl but will prepare us for other possible functions
            var g = default(G);
            // return lerp(hash.Eat(x.p0).Floats01A, hash.Eat(x.p1).Floats01A, x.t) * 2f - 1f;
            // We will now evaluate the hases at the x.g points (which for now is the same as x.t)
            // note that g.Evaluate(hash.Eat(x.p0), x.g) => hash.Eat(x.p0).Floats01A which is teh same as the first implementation
            // return lerp(g.Evaluate(hash.Eat(x.p0), x.g), g.Evaluate(hash.Eat(x.p1), x.g), x.t) * 2f - 1f;
            // with the introduction of the 2 gradients we will lerp between the gradient function from one side to the other side.
            // We have also changed the Value gradient to temproarly have the function f(x) = x;
            // this produces a periodic variation. Note that the interpolation is smooth because we lerp with a c2-continuous interpolation
            // moved the normalization to the value function
            return lerp(g.Evaluate(hash.Eat(x.p0), x.g0), g.Evaluate(hash.Eat(x.p1), x.g1), x.t);
        }
    }


    // we can apply the same changes with the gradients to lattice 2D and 3D
    public struct Lattice2D<G> : INoise where G : struct, IGradient {
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash) {
            LatticeSpan4 x = GetLatticeSpan4(positions.c0), z = GetLatticeSpan4(positions.c2);
            SmallXXHash4 h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1);
            var g = default(G);
            // replace the following with gradient function version
            // return lerp(
            //         lerp(h0.Eat(z.p0).Floats01A, h0.Eat(z.p1).Floats01A, z.t),
            //         lerp(h1.Eat(z.p0).Floats01A, h1.Eat(z.p1).Floats01A, z.t),
            //         x.t)* 2f - 1f;
            // we pass the correct gradient vector for each point. 
            return lerp(
                    lerp(
                        g.Evaluate(h0.Eat(z.p0), x.g0, z.g0),
                        g.Evaluate(h0.Eat(z.p1), x.g0, z.g1), 
                        z.t),
                    lerp(
                        g.Evaluate(h1.Eat(z.p0), x.g1, z.g0),
                        g.Evaluate(h1.Eat(z.p1), x.g1, z.g1),
                        z.t),
                    x.t);
        }
    }

    public struct Lattice3D<G> : INoise where G : struct, IGradient {
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash) {
            LatticeSpan4 x = GetLatticeSpan4(positions.c0), y=GetLatticeSpan4(positions.c1), z = GetLatticeSpan4(positions.c2);

            SmallXXHash4 h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1),
            h00=h0.Eat(y.p0), h01=h0.Eat(y.p1),
            h10=h1.Eat(y.p0), h11=h1.Eat(y.p1);
            var g = default(G);
            // return // lerp between the 2 planes parallel to the YZ plane and along the X axis
            //         lerp( 
            //             // lerp between th two lines parallel to the Z axis in the YZ plane
            //             lerp(
            //                 // Lerp between h000 and h001 along z
            //                 lerp(h00.Eat(z.p0).Floats01A, h00.Eat(z.p1).Floats01A, z.t),
            //                 // lerp between h010 and h011 along z
            //                 lerp(h01.Eat(z.p0).Floats01A, h01.Eat(z.p1).Floats01A, z.t),
            //                 y.t
            //             ),
            //             // lerp between the two lines parallel to the Z axis offset from the YZ plane by 1
            //             lerp(
            //                 // Lerp between h100 and h101 along z
            //                 lerp(h10.Eat(z.p0).Floats01A, h10.Eat(z.p1).Floats01A, z.t),
            //                 // lerp between h110 and h111 along z
            //                 lerp(h11.Eat(z.p0).Floats01A, h11.Eat(z.p1).Floats01A, z.t),
            //                 y.t
            //             ),
            //             x.t
            //         ) * 2f - 1f;
            return // lerp between the 2 planes parallel to the YZ plane and along the X axis
                    lerp( 
                        // lerp between th two lines parallel to the Z axis in the YZ plane
                        lerp(
                            // Lerp between h000 and h001 along z
                            lerp(
                                g.Evaluate(h00.Eat(z.p0), x.g0, y.g0, z.g0),    // point (0, 0, 0)
                                g.Evaluate(h00.Eat(z.p1), x.g0, y.g0, z.g1),    // point (0, 0, 1)
                                z.t
                            ),
                            // lerp between h010 and h011 along z
                            lerp(
                                g.Evaluate(h01.Eat(z.p0), x.g0, y.g1, z.g0),    // point (0, 1, 0)
                                g.Evaluate(h01.Eat(z.p1), x.g0, y.g1, z.g1),    // point (0, 0, 0)
                                z.t
                            ),
                            y.t
                        ),
                        // lerp between the two lines parallel to the Z axis offset from the YZ plane by 1
                        lerp(
                            // Lerp between h100 and h101 along z
                            lerp(
                                g.Evaluate(h10.Eat(z.p0), x.g1, y.g0, z.g0),    // point (1, 0, 0)
                                g.Evaluate(h10.Eat(z.p1), x.g1, y.g0, z.g1),    // point (1, 0, 1)
                                z.t
                            ),
                            // lerp between h110 and h111 along z
                            lerp(
                                g.Evaluate(h11.Eat(z.p0), x.g1, y.g1, z.g0),    // point (1, 1, 0)
                                g.Evaluate(h11.Eat(z.p1), x.g1, y.g1, z.g1),    // point (1, 1, 1)
                                z.t
                            ),
                            y.t
                        ),
                        x.t
                    );
        }
    }

}
