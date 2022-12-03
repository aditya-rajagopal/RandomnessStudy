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
    public SmallXXHash Eat (int data) => new SmallXXHash(RotateLeft(accumulator + (uint)data * primeC, 17) * primeD);
        // This is the algorithm of the smallXXHash algorithm.


    public SmallXXHash Eat (byte data) => new SmallXXHash(RotateLeft(accumulator + data * primeE, 11) * primeA);
        // This is the variant XXHash32 of the smallXXHash algorithm.

    public static SmallXXHash Seed (int seed) => new SmallXXHash((uint)seed + primeE);

    // The burst compiler recognizes our implementation as a rotateleft instruction for the CPU
    // However there is no vectorized rotate left function hence we do it with 2 shifts and a logical or
    static uint RotateLeft (uint data, int steps) => (data << steps) | (data >> 32 - steps);

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
}

