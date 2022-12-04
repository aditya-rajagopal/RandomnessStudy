using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public abstract class Visualization : MonoBehaviour
{
    public enum Shape { Plane, Sphere, OctaSphere, Torus }

	public static Shapes.ScheduleDelegate[] shapeJobs = {
		Shapes.Job<Shapes.Plane>.ScheduleParallel,
		Shapes.Job<Shapes.Sphere>.ScheduleParallel,
		Shapes.Job<Shapes.OctaSphere>.ScheduleParallel,
		Shapes.Job<Shapes.Torus>.ScheduleParallel
	};

    [SerializeField]
    Mesh instanceMesh;
    [SerializeField]
    Material material;
    [SerializeField]
    public bool enableAnimation = false;

    [SerializeField, Range(0, 100)]
    public float animationSpeed = 0.1f;
    [SerializeField]
	Shape shape = Shape.OctaSphere;
    [SerializeField, Range(0.1f, 10f)]
	float instanceScale = 2f;
    [SerializeField, Range(2, 512)]
    int resolution = 16;
    // [SerializeField, Range(-2f, 2f)]
	// float verticalOffset = 1f;
    // We dont want the offset to always be vertical
    // Instead we want the offset to be in the direction of the normal vector
    [SerializeField, Range(-0.5f, 0.5f)]
	float displacement = 0.1f;

    ComputeBuffer positionsBuffer, normalsBuffer;
    NativeArray<float3x4> positions, normals;
    MaterialPropertyBlock propertyBlock;

    static int
        positionsId = Shader.PropertyToID("_Positions"),
        normalsId = Shader.PropertyToID("_Normals"),
        configID = Shader.PropertyToID("_Config");

    // We want to be able to update the position of the game object live and have the rendfer respond to it
    // But we dont want to calculate the matrices in the  Update unless ther eis a change
    bool isDirty;

    Bounds bounds;

    protected abstract void EnableVisualization (int dataLength, MaterialPropertyBlock propertyBlock);

    protected abstract void DisableVisualization ();

    protected abstract void UpdateVisualization (
		NativeArray<float3x4> positions, int resolution, JobHandle handle
	);


    private void OnEnable() {
        isDirty = true;
        // length of our arrays
        int length = resolution * resolution;
        length = length / 4 + (length & 1);
        positions = new NativeArray<float3x4>(length, Allocator.Persistent);
        normals = new NativeArray<float3x4>(length, Allocator.Persistent);
        positionsBuffer = new ComputeBuffer(length * 4, 3 * 4);
        normalsBuffer = new ComputeBuffer(length * 4, 3 * 4);

        // we can provide the transform of the Hash object so that we may move the entire render.
        // JobHandle handle = Shapes.Job.ScheduleParallel(positions, transform.localToWorldMatrix, resolution, default);

        // new HashJob{
        //     positions = positions,
        //     hashes = hashes,
        //     hash = SmallXXHash.Seed(seed),
        //     domainTRS = domain.Matrix
        // }.ScheduleParallel(length, resolution, handle).Complete();

        // hashesBuffer.SetData(hashes);
        // positionsBuffer.SetData(positions);
        // This is now handeled in the Update function

        propertyBlock ??= new MaterialPropertyBlock();
        EnableVisualization(length, propertyBlock);
        propertyBlock.SetBuffer(positionsId, positionsBuffer);
        propertyBlock.SetBuffer(normalsId, normalsBuffer);
        propertyBlock.SetVector(configID, new Vector4(resolution,  instanceScale / resolution, displacement));
    }

    void OnDisable()
    {
		positions.Dispose();
		normals.Dispose();
		positionsBuffer.Release();
		normalsBuffer.Release();
		normalsBuffer = null;
        DisableVisualization();
    }

    void OnValidate()
    {
        if (positionsBuffer != null & enabled)
        {
            OnDisable();
            OnEnable();
        }
    }

    private void Update() {
        if (isDirty || transform.hasChanged || enableAnimation) {
			isDirty = false;
            transform.hasChanged = false;

            // We can now pass the template arguemtn Shapes.Plane (or any other) to generically generate a new shape
			UpdateVisualization(
				positions, resolution,
				shapeJobs[(int)shape](
					positions, normals, transform.localToWorldMatrix, resolution, default
				)
			);

			positionsBuffer.SetData(positions.Reinterpret<float3>(3 * 4 * 4));
			normalsBuffer.SetData(normals.Reinterpret<float3>(3 * 4 * 4));
            bounds = new Bounds(transform.position,
				float3(2f * cmax(abs(transform.lossyScale)) + displacement));
		}
        
        Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, material, bounds, resolution * resolution, propertyBlock);    
    }
}
