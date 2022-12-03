using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public class HashVisualization : MonoBehaviour
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct HashJob : IJobFor {

        [WriteOnly]
        public NativeArray<uint> hashes;

        public int resolution;
        public float invResolution;
        public SmallXXHash hash;

        public void Execute(int i) {
            // hashes[i] = (uint)i;
            // To add a bit more randomness we can use the Weyl sequence.
            // we try the sequence with 0.381 we then multiply by 256 so we get hashes in the range of 0 to 255 (inclusive)
            // hashes[i] = (uint)(frac(i * 0.381f) * 256f);
            // We can go one step further by using the x y coordiante rather than i
            int v = (int)floor(invResolution * i + 0.000001f);
            int u = i - resolution * v;

            // var hash = new SmallXXHash(0);
            // hash.Eat(u);
			// hash.Eat(v);
            // hashes[i] = (uint)(frac(u * v * 0.381f) * 256f);
            // Because of the introduciton of the method chaining we can compress the code
            hashes[i] = hash.Eat(u).Eat(v);
        }
    }

    [SerializeField]
    Mesh instanceMesh;
    [SerializeField]
    Material material;
    [SerializeField, Range(1, 512)]
    int resolution = 16;
    [SerializeField, Range(-2f, 2f)]
	float verticalOffset = 1f;
    [SerializeField]
	int seed;
    

    ComputeBuffer hashesBuffer;
    NativeArray<uint> hashes;
    MaterialPropertyBlock propertyBlock;

    static int
        hashesId = Shader.PropertyToID("_Hashes"),
        configID = Shader.PropertyToID("_Config");

    private void OnEnable() {
        // length of our arrays
        int length = resolution * resolution;

        hashes = new NativeArray<uint>(length, Allocator.Persistent);
        hashesBuffer = new ComputeBuffer(length, 4);

        new HashJob{
            hashes = hashes,
            resolution = resolution,
            invResolution = 1f / resolution,
            hash = SmallXXHash.Seed(seed)
        }.ScheduleParallel(length, resolution, default).Complete();

        hashesBuffer.SetData(hashes);

        propertyBlock ??= new MaterialPropertyBlock();
        propertyBlock.SetBuffer(hashesId, hashesBuffer);
        propertyBlock.SetVector(configID, new Vector4(resolution, 1f / resolution, verticalOffset / resolution));
    }

    void OnDisable()
    {
        hashes.Dispose();
        hashesBuffer.Release();
        hashesBuffer = null;
    }

    void OnValidate()
    {
        if (hashesBuffer != null & enabled)
        {
            OnDisable();
            OnEnable();
        }
    }

    private void Update() {
        Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, material, new Bounds(Vector3.zero, Vector3.one), hashes.Length, propertyBlock);    
    }
}
