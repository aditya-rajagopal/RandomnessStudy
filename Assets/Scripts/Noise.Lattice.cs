using Unity.Mathematics;

using static Unity.Mathematics.math;


public static partial class Noise
{
    // we need to change this struct from private to public to be able to access it in the Ilattice interfaces
    public struct LatticeSpan4 {
        public int4 p0, p1;

        // Add a variable to store the relative coordiante for the gradients
        // once again a float4 for vectorization
        // However since we only use 1 gradient we will get repeating 1D ramps going from -1 to 1 between lattice points
        // there is no blending because we use the same gradient on both sides. To turn our implementation into a proper gradient on both sides
        // so we use a second gradient relative to p1.
        public float4 g0, g1;
        public float4 t;
    }


    public interface ILattice {
		LatticeSpan4 GetLatticeSpan4 (float4 coordinates, int frequency);

        // To combat tiling not working with voronoi noise we will generate a validation function that check edge cases
        // since we only do +1 and -1 to the coordinate. This function will assume that the positions are alreeady offset
        int4 ValidateSingleStep(int4 points, int frequency);
	}

    // Put our old lattice span function into a struct that is an ILattice interface
    public struct LatticeNormal : ILattice {
        public LatticeSpan4 GetLatticeSpan4 (float4 coordinates, int frequency) {
            // we scale the coordinates here instead of when we pass the position to lattice
            coordinates *= frequency;
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

        // the validate step for normal lattice noise functions will  just be the point itself since we are not tiling
        public int4 ValidateSingleStep (int4 points, int frequency) => points;
    }

    // We can make a seperate struct for the LatticeTiling method
    public struct LatticeTiling : ILattice {
        public LatticeSpan4 GetLatticeSpan4 (float4 coordinates, int frequency) {
            // we scale the coordinates here instead of when we pass the position to lattice
            coordinates *= frequency;
            float4 points = floor(coordinates);
            LatticeSpan4 span;
            span.p0 = (int4)points;
            span.g0 = coordinates - span.p0;
            // p1 sits exactly 1 unit away so we can get g1 by subtracting 1 from g0 
            span.g1 = span.g0 - 1f;

            // we will make it so it repeats span points equal to the frequency
            // we do this after calculating gradients
            
            // span.p0 %= frequency;
            // modulo is not vectorized so this step is inefficient
            // we can instead use floating point division of points with frequnecy taking the floor
            // the multiplying frequency again . Then subtract it from p0 to get the remaineder.
            // another thing to be aware of is directly converting floating point divisions can be dangerous
            // due to precision issues. It is better to round up before converting. This will ensure
            // that it will work on all CPU types.
            span.p0 -= (int4)ceil(points / frequency) * frequency;
            // Although with just the modulos we get tiling it ends up being incorrect. 
            span.p0 = select(span.p0, span.p0 + frequency, span.p0 < 0);

            // we know that we need to repeat the pattern every frequency lattice points
            // so we can simply calculate p1 from p0 and check if p1 is frequency
            // if it is set it ot 0 to reset it. This works because p0 is also reset.
            span.p1 = span.p0 + 1;
			span.p1 = select(span.p1, 0, span.p1 == frequency);
            span.t = coordinates - points;
            // span.t = smoothstep(0f, 1f, span.t);
            // The above function smoothstep is 3t^2 - 6t^3 which means its first order derivative is continuous
            // and itends at 0 boths sides of the smoothstep. But its second derivative is not continuous
            // we instead use the following function which is C2-continious
            // This isnt a problem for us but if we do a smooth mesh or something we will see very visable creases.
            span.t = span.t * span.t * span.t * (span.t * (span.t * 6f - 15f) + 10f);
            return span;
        }

        // For the tiling function we need to check 2 edge cases. 
        // 1. when the position after tiling is = to the frequency then the point needs to loop back around to 0
        // 2. if the point after offset is -1 then it has to loop back to the other side of the tiling to frequency - 1
        // We do this as we exepect each tile to have points 0 to frquency - 1.
        public int4 ValidateSingleStep (int4 points, int frequency) => 
            select(select(points, 0, points == frequency), frequency - 1, points == -1);
    }



    // we can make a template of Lattice1D to use a generic gradient function
    public struct Lattice1D<L, G> : INoise where L : struct, ILattice where G : struct, IGradient {
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency) {
            // int4 p0 = (int4)floor(positions.c0);
            // int4 p1 = p0 + 1;

            // return hash.Eat(p0).Floats01A + hash.Eat(p1).Floats01A - 1f;
            // We can do teh same with a lerp
            // we can take the fraction part by subtracting the floor from the original position value
            // and interpolate between the 2 positiosn based on this fraction
            // float4 t = positions.c0 - p0;

            // we can simplify the calculations above using GetLatticeSpan4
            LatticeSpan4 x = default(L).GetLatticeSpan4(positions.c0, frequency);
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
            
            // we run the EvaluateAfterInterpolation to do any final transformation post interpolation to noise values
            // such as absolute function to generate turbulent noise.
            return g.EvaluateAfterInterpolation(lerp(g.Evaluate(hash.Eat(x.p0), x.g0), g.Evaluate(hash.Eat(x.p1), x.g1), x.t));
        }
    }


    // we can apply the same changes with the gradients to lattice 2D and 3D
    public struct Lattice2D<L, G> : INoise where L : struct, ILattice where G : struct, IGradient {
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency) {
            var l = default(L);
            LatticeSpan4 
                x = l.GetLatticeSpan4(positions.c0, frequency), 
                z = l.GetLatticeSpan4(+positions.c2, frequency);
            SmallXXHash4 h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1);
            var g = default(G);
            // replace the following with gradient function version
            // return lerp(
            //         lerp(h0.Eat(z.p0).Floats01A, h0.Eat(z.p1).Floats01A, z.t),
            //         lerp(h1.Eat(z.p0).Floats01A, h1.Eat(z.p1).Floats01A, z.t),
            //         x.t)* 2f - 1f;
            // we pass the correct gradient vector for each point. 
            return g.EvaluateAfterInterpolation(lerp(
                    lerp(
                        g.Evaluate(h0.Eat(z.p0), x.g0, z.g0),
                        g.Evaluate(h0.Eat(z.p1), x.g0, z.g1), 
                        z.t),
                    lerp(
                        g.Evaluate(h1.Eat(z.p0), x.g1, z.g0),
                        g.Evaluate(h1.Eat(z.p1), x.g1, z.g1),
                        z.t),
                    x.t));
        }
    }

    public struct Lattice3D<L, G> : INoise where L : struct, ILattice where G : struct, IGradient {
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency) {
            var l = default(L);
            LatticeSpan4 
                x = l.GetLatticeSpan4(positions.c0, frequency),
                y = l.GetLatticeSpan4(positions.c1, frequency),
                z = l.GetLatticeSpan4(positions.c2, frequency);

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
                    g.EvaluateAfterInterpolation(lerp( 
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
                    ));
        }
    }

}
