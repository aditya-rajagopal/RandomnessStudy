using Unity.Mathematics;


public readonly struct SmallXXHash {
	const uint primeA = 0b10011110001101110111100110110001;
	const uint primeB = 0b10000101111010111100101001110111;
	const uint primeC = 0b11000010101100101010111000111101;
	const uint primeD = 0b00100111110101001110101100101111;
	const uint primeE = 0b00010110010101100110011110110001;

    readonly uint accumulator;

    public SmallXXHash (uint accumulator) {
        // This is the algorithm of the smallXXHash algorithm.
        // we are changing SmallXXHash to be readonly so we set the accumulator here
        // only the constructor can modify readonly values.
        this.accumulator = accumulator;
    }

    // we can add teh ability to chain methods by returning a reference to the object
    public SmallXXHash Eat (int data) => RotateLeft(accumulator + (uint)data * primeC, 17) * primeD;
        // This is the algorithm of the smallXXHash algorithm.


    public SmallXXHash Eat (byte data) => RotateLeft(accumulator + data * primeE, 11) * primeA;
        // This is the variant XXHash32 of the smallXXHash algorithm.

    public static SmallXXHash Seed (int seed) => (uint)seed + primeE;

    // The burst compiler recognizes our implementation as a rotateleft instruction for the CPU
    // However there is no vectorized rotate left function hence we do it with 2 shifts and a logical or
    static uint RotateLeft (uint data, int steps) => (data << steps) | (data >> 32 - steps);

    public static implicit operator SmallXXHash (uint accumulator) =>
		new SmallXXHash(accumulator);

    // We can make the fucntion a cast to unit operator and make it implicit. This means we dont have to write (uint) to convert
    public static implicit operator uint (SmallXXHash hash) {
        // The final step in the xxHash algorithm is to mix the bits of the accumulator to spread the influence of all the input bits
        // This is called the avalanch effect. This is executed after all the data is eaten and the final hash is needed.
        uint avalanche = hash.accumulator;
		avalanche ^= avalanche >> 15;
		avalanche *= primeB;
		avalanche ^= avalanche >> 13;
		avalanche *= primeC;
		avalanche ^= avalanche >> 16;
		return avalanche;
    }

    public static implicit operator SmallXXHash4 (SmallXXHash hash) =>
		new SmallXXHash4(hash.accumulator);
}

public readonly struct SmallXXHash4 {
    // Currently it is not possibel to vectorize our jobs because we use float3 instead of float 4
    // Lets start by making a new hash function

	//const uint primeA = 0b10011110001101110111100110110001;
	const uint primeB = 0b10000101111010111100101001110111;
	const uint primeC = 0b11000010101100101010111000111101;
	const uint primeD = 0b00100111110101001110101100101111;
	const uint primeE = 0b00010110010101100110011110110001;

	readonly uint4 accumulator;

	public SmallXXHash4 (uint4 accumulator) {
		this.accumulator = accumulator;
	}

	public static implicit operator SmallXXHash4 (uint4 accumulator) =>
		new SmallXXHash4(accumulator);

	public static SmallXXHash4 Seed (int4 seed) => (uint4)seed + primeE;

	static uint4 RotateLeft (uint4 data, int steps) =>
		(data << steps) | (data >> 32 - steps);

	public SmallXXHash4 Eat (int4 data) =>
		RotateLeft(accumulator + (uint4)data * primeC, 17) * primeD;

    // There is no vector of type byte so we dont need this
	//public SmallXXHash Eat (byte data) =>
	//	RotateLeft(accumulator + data * primeE, 11) * primeA;


    // we will be using this operation very often so it is useful to have these properties.
    public uint4 BytesA => (uint4)this & 255;
    public uint4 BytesB => ((uint4)this >> 8) & 255;

	public uint4 BytesC => ((uint4)this >> 16) & 255;

	public uint4 BytesD => (uint4)this >> 24;

    public float4 Floats01A => (float4)BytesA * (1f / 255f);

    public float4 Floats01B => (float4)BytesB * (1f / 255f);

	public float4 Floats01C => (float4)BytesC * (1f / 255f);

	public float4 Floats01D => (float4)BytesD * (1f / 255f);

    public static SmallXXHash4 operator + (SmallXXHash4 h, int v) =>
		h.accumulator + (uint)v;


	public uint4 GetBits (int count, int shift) =>
		((uint4)this >> shift) & (uint)((1 << count) - 1);


	public float4 GetBitsAsFloats01 (int count, int shift) =>
		(float4)GetBits(count, shift) * (1f / ((1 << count) - 1));

	public static implicit operator uint4 (SmallXXHash4 hash) { 
        uint4 avalanche = hash.accumulator;
		avalanche ^= avalanche >> 15;                         
		avalanche *= primeB;
		avalanche ^= avalanche >> 13;
		avalanche *= primeC;
		avalanche ^= avalanche >> 16;
		return avalanche;
     }
}

