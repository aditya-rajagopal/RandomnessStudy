using Unity.Mathematics;

using static Unity.Mathematics.math;


public static partial class Noise
{
    struct LatticeSpan4 {
        public int4 p0, p1;
        public float4 t;
    }


    static LatticeSpan4 GetLatticeSpan4 (float4 coordinates) {
		float4 points = floor(coordinates);
		LatticeSpan4 span;
		span.p0 = (int4)points;
		span.p1 = span.p0 + 1;
		span.t = coordinates - points;
        // span.t = smoothstep(0f, 1f, span.t);
        // The above function smoothstep is 3t^2 - 6t^3 which means its first order derivative is continuous
        // and itends at 0 boths sides of the smoothstep. But its second derivative is not continuous
        // we instead use the following function which is C2-continious
        // This isnt a problem for us but if we do a smooth mesh or something we will see very visable creases.
        span.t = span.t * span.t * span.t * (span.t * (span.t * 6f - 15f) + 10f);
		return span;
	}

    public struct LatticeID : INoise {
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
            return lerp(hash.Eat(x.p0).Floats01A, hash.Eat(x.p1).Floats01A, x.t) * 2f - 1f;
        }
    }


    public struct Lattice2D : INoise {
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash) {
            LatticeSpan4 x = GetLatticeSpan4(positions.c0), z = GetLatticeSpan4(positions.c2);
            SmallXXHash4 h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1);
            return lerp(lerp(h0.Eat(z.p0).Floats01A, h0.Eat(z.p1).Floats01A, z.t),
                        lerp(h1.Eat(z.p0).Floats01A, h1.Eat(z.p1).Floats01A, z.t),
                        x.t)* 2f - 1f;
        }
    }

    public struct Lattice3D : INoise {
        public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash) {
            LatticeSpan4 x = GetLatticeSpan4(positions.c0), y=GetLatticeSpan4(positions.c1), z = GetLatticeSpan4(positions.c2);

            SmallXXHash4 h0 = hash.Eat(x.p0), h1 = hash.Eat(x.p1),
            h00=h0.Eat(y.p0), h01=h0.Eat(y.p1),
            h10=h1.Eat(y.p0), h11=h1.Eat(y.p1);
            return // lerp between the 2 planes parallel to the YZ plane and along the X axis
                    lerp( 
                        // lerp between th two lines parallel to the Z axis in the YZ plane
                        lerp(
                            // Lerp between h000 and h001 along z
                            lerp(h00.Eat(z.p0).Floats01A, h00.Eat(z.p1).Floats01A, z.t),
                            // lerp between h010 and h011 along z
                            lerp(h01.Eat(z.p0).Floats01A, h01.Eat(z.p1).Floats01A, z.t),
                            y.t
                        ),
                        // lerp between the two lines parallel to the Z axis offset from the YZ plane by 1
                        lerp(
                            // Lerp between h100 and h101 along z
                            lerp(h10.Eat(z.p0).Floats01A, h10.Eat(z.p1).Floats01A, z.t),
                            // lerp between h110 and h111 along z
                            lerp(h11.Eat(z.p0).Floats01A, h11.Eat(z.p1).Floats01A, z.t),
                            y.t
                        ),
                        x.t
                    ) * 2f - 1f;
        }
    }

}
