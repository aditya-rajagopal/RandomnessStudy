using Unity.Mathematics;

using static Unity.Mathematics.math;

public static partial class Noise {

    // sometimes the point that is closest to a given coordinate might be in an adjacent span
    // we will define a static method (vectorized). It takes the current minmima and a new set of distances
    // and returns the updated minima

    // the getNoise4 functions we will loop through adjacent spans and find the minima

    // Another issue arises due to tiling being enabled. Given that the adjacent spans can extend beyond the tiling region
    // This causes discontinuities when the tiling lattice point suddenly shifts after crossing frequency thresolds.
    // static float4 UpdateVoronoiMinima (float4 minima, float4 distances) {
	// 	return select(minima, distances, distances < minima);
	// }
    // We are not limited to only finding the nearest point we can also find the second, thrid, etc. nearest points as well
    // We can rewrite the update minima function to accept a 4x2 minima where the first coloumn is the nearest and the second coloumn
    // is the second nearest
    static float4x2 UpdateVoronoiMinima (float4x2 minima, float4 distances) {
        bool4 newMinimum = distances < minima.c0; // Check if the distance is less than the old nearest point.
        // If there is a new minimum the old minimum becomes the second minimum
        minima.c1 = select(
            select(minima.c1, distances, distances < minima.c1),  // Check if the distances is smaller than c1 but greater than c0
            minima.c0,
            newMinimum);
        minima.c0 = select(minima.c0, distances, newMinimum);

        // INitially retun just the minima
		return minima;
	}


    // moved distance functins to their own structs
    // static float4 GetDistance (float4 x, float4 y) => sqrt(x * x + y * y);

    // static float4 GetDistance (float4 x, float4 y, float4 z) => sqrt(x * x + y * y + z * z);


    public struct Voronoi1D<L, D, F> : INoise 
        where L : struct, ILattice 
        where D: struct, IVoronoiDistance
        where F: struct, IVoronoiFunction
    {
        public float4 GetNoise4 (float4x3 positions, SmallXXHash4 hash, int frequency) {
            var l = default(L);
            var d = default(D);
            LatticeSpan4 x = l.GetLatticeSpan4(positions.c0, frequency);

            // Voronoi noise's base idea is that you have a set of random points
            // and the noise is the distance to the closest point
            // In the case of 1D we can (instead of infinity) pick 1 random point within 
            // the span we are considering each poin
            // SmallXXHash4 h = hash.Eat(x.p0);
            // To update the noise function to conider the neighbouring spans we will loop throug them
            // and get the best minima
            // in the case of 1D this is simply -1, 0 and +1 to the x.p0 position (the lattice point in consideration)
            // float4 minima = 2f; // 2 is the maximum theoritcal distance a point can be from lattice points from 2 adjacent spans in 1D
            float4x2 minima = 2f; // we define the new minima to be a 4x2 matrix to keep track of the second closest point
			for (int u = -1; u <= 1; u++) {
				// SmallXXHash4 h = hash.Eat(x.p0 + u);
                // before we eat teh position we need to validate if x.p0 + u is an edge case for tiling
                // when tiling is disabled it will retrun the same point back
                SmallXXHash4 h = hash.Eat(l.ValidateSingleStep(x.p0 + u, frequency));
				minima = UpdateVoronoiMinima(minima, d.GetDistance(h.Floats01A + u - x.g0));
			}
            // return abs(h.Floats01A - x.g0);
            // We then return this minima as the distance noise
            return default(F).Evaluate(d.Finalize1D(minima));
        }
    }

    public struct Voronoi2D<L, D, F> : INoise 
        where L : struct, ILattice 
        where D: struct, IVoronoiDistance
        where F: struct, IVoronoiFunction
    {
        public float4 GetNoise4 (float4x3 positions, SmallXXHash4 hash, int frequency) {
            var l = default(L);
            var d = default(D);

            LatticeSpan4 x = l.GetLatticeSpan4(positions.c0, frequency),
            z = l.GetLatticeSpan4(positions.c2, frequency);

            // For 2D vornoi noise we need to get the 2D distance which is the length of the vector from point A to B
            float4x2 minima = 2f;

            for (int u = -1; u <= 1; u++) {
                SmallXXHash4 hx = hash.Eat(l.ValidateSingleStep(x.p0 + u, frequency));
                float4 xoffset = u - x.g0;
                for (int v = -1; v <= 1; v++) {
                    SmallXXHash4 h = hx.Eat(l.ValidateSingleStep(z.p0 + v, frequency));
                    float4 zOffset = v - z.g0;
                    minima = UpdateVoronoiMinima(minima, d.GetDistance(
                        h.Floats01A + xoffset, h.Floats01B + zOffset
                    ));
                    minima = UpdateVoronoiMinima(minima, d.GetDistance(
                        h.Floats01C + xoffset, h.Floats01D + zOffset
                    ));
                }
            }
            // There are situations where the distance to the nearest point can be greater than 1.
                    // This is an issue because it can produce artifacts in our outptus
                    // To combat this we can sample 2 points per span instead of 1. We update the minima twice per lattice
                    // we do have 4 parts to our hash so we can generate 2 points
            // we want the noise to not exceed 1 so we can clamp it in the rare situations taht it does exeed one.
            // minima.c0 = min(minima.c0, 1f);
            // minima.c1 = min(minima.c1, 1f);
            return default(F).Evaluate(d.Finalize2D(minima));
        }
    }

    public struct Voronoi3D<L, D, F> : INoise 
        where L : struct, ILattice 
        where D: struct, IVoronoiDistance
        where F: struct, IVoronoiFunction
    {
        public float4 GetNoise4 (float4x3 positions, SmallXXHash4 hash, int frequency) {
            var l = default(L);
            var d = default(D);

            LatticeSpan4 x = l.GetLatticeSpan4(positions.c0, frequency),
            y = l.GetLatticeSpan4(positions.c1, frequency),
            z = l.GetLatticeSpan4(positions.c2, frequency);

            float4x2 minima = 2f;
			for (int u = -1; u <= 1; u++) {
				SmallXXHash4 hx = hash.Eat(l.ValidateSingleStep(x.p0 + u, frequency));
				float4 xOffset = u - x.g0;
				for (int v = -1; v <= 1; v++) {
					SmallXXHash4 hy = hx.Eat(l.ValidateSingleStep(y.p0 + v, frequency));
					float4 yOffset = v - y.g0;
					for (int w = -1; w <= 1; w++) {
						SmallXXHash4 h =
							hy.Eat(l.ValidateSingleStep(z.p0 + w, frequency));
						float4 zOffset = w - z.g0;
                        // we can divide 32 bits into approx 5 parts ot 6 bits (with 2 bits remaining)
						minima = UpdateVoronoiMinima(minima, d.GetDistance(
							h.GetBitsAsFloats01(5, 0) + xOffset,
							h.GetBitsAsFloats01(5, 5) + yOffset,
							h.GetBitsAsFloats01(5, 10) + zOffset
						));
                        // We have a similar issue  here of points allowed to be > 1 becaue the maximum theoretical distance a point can 
                        // be from the offset nodes is sqrt(3). However this is uncommon. We can reduce this even further bys ampling 2
                        // points per span ocne again. This does have issues though since we only have 4 parts to our hash.
                        // We will need to find a way to get 6 numbers from our hash values.

                        minima = UpdateVoronoiMinima(minima, d.GetDistance(
							h.GetBitsAsFloats01(5, 15) + xOffset,
							h.GetBitsAsFloats01(5, 20) + yOffset,
							h.GetBitsAsFloats01(5, 25) + zOffset
						));
					}
				}
			}

            return default(F).Evaluate(d.Finalize3D(minima));
        }
    }

}
