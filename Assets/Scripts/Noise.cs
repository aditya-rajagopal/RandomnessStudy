using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public static partial class Noise {

    [Serializable]
    public struct Settings {
        public int seed;

        // the frequency is the domain scale of the noise. The larger the scale the slower the rate of change and
        // larger the features. It must be > 1
        [Min(1)]
		public int frequency;
        // Octaves define how many samples we take at multiple frequencies. Ideally we would take infinte samples
        // but we need to calculate each octave seperately so we keep it small as it takes longer to calculate.
        [Range(1, 6)]
        public int octaves;

        // the frequency does not always have to scale by a factor of 2. We can make a setting that informs
        // us how to scale the frequency called lacunarity. lacunarity is a geomtric description of how fractals
        // fill the space. Higher the value hte more gap there is.
        [Range(2, 4)]
		public int lacunarity;

        // Similarly we dont always have to scale the amplitude by 0.5 every octave. We define the parameter persistence
        // for this purpose
        [Range(0f, 1f)]
		public float persistence;


        public static Settings Default => new Settings { 
            frequency = 4,
            octaves = 1,
            lacunarity = 2,
			persistence = 0.5f
        };
    }

    public delegate JobHandle ScheduleDelegate (
		NativeArray<float3x4> positions, NativeArray<float4> noise,
		Settings settings, SpaceTRS domainTRS, int resolution, JobHandle dependency
	);

    public interface INoise {
        float4 GetNoise4 (float4x3 positions, SmallXXHash4 hash, int frequency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job<N> : IJobFor where N : struct, INoise {

        [ReadOnly]
        public NativeArray<float3x4> positions;

        [WriteOnly]
        public NativeArray<float4> noise;

        // public SmallXXHash4 hash;
        // we wil use settings instead of hash here so we can support multiple layers of noise
        public Settings settings;
        public float3x4 domainTRS;

        public void Execute(int i){
            float4x3 position = domainTRS.TransformVectors(transpose(positions[i]));
            var hash = SmallXXHash4.Seed(settings.seed);

            // currently we are allowing 2 different options to scale the noise
            // both frequency and scale in the domainTRS will do the samething
            // however frequency is a uniform change while scale can ebe postive or negative and non-uniform

            // If we just keep doubling the frequency the higher frequency octaves will dominate the noise
            // To counteract this we introduce an amplitude. Every time the frequency si doubled the amplitude is halved
            // additionally the sum of the noise will exceed [-1, 1] so we will calculate the sum of the amplitudes
            // and then normalize the final noise
            
            // another issue is we are repeating the same noise function at different frequencies causing the patterns
            // to converge at the domain origin. Also visually obvious patterns repeat
            // To eliminate this we can have different seeds per octave.
            float amplitude = 1.0f, amplitudeSum = 0f;
            float4 sum = 0f;
            int frequency = settings.frequency;
            for (int o = 0; o < settings.octaves; o++) {
                // we want to be able to tile the noise to create texture sand such so we
                // will let the lattice control the scaling of the position with frequency
                // to create a tiling pattern opposite sides of teh sample area must be identical
                // since we are using lattice noise we can do this by repeating the same sequence of lattice spans
                sum += amplitude * default(N).GetNoise4(position, hash + o, frequency);
                amplitudeSum += amplitude;
                frequency *= settings.lacunarity;
                amplitude *= settings.persistence;
            }
            noise[i] = sum / amplitudeSum;
        }

        public static JobHandle ScheduleParallel (
            NativeArray<float3x4> positions, NativeArray<float4> noise,
            Settings settings, SpaceTRS domainTRS, int resolution, JobHandle dependency
        ) => new Job<N> {
            positions = positions,
            noise = noise,
            settings = settings,
            domainTRS = domainTRS.Matrix
        }.ScheduleParallel(positions.Length, resolution, dependency);
    }

}