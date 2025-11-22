using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Pcysl5edgo.RemoveRedundantPath.Tests;

public class IndexOfTests
{
    [Theory]
    [MemberData(nameof(IndexOfTrue64bitData))]
    public void IndexOfTrue64bit(ulong[] array, int offset, int expected)
    {
        int result = BitSpan.IndexOfTrue(ref Unsafe.As<ulong, byte>(ref MemoryMarshal.GetArrayDataReference(array)), array.Length << 6, offset);
        Assert.Equal(expected, result);
    }

    public static TheoryData<ulong[], int, int> IndexOfTrue64bitData => new()
    {
        { new ulong[] { 0 }, 0, -1 },
        { new ulong[] { 0b0000_0000_0000_0000 }, 0, -1 },
        { new ulong[] { 0b0000_0000_0000_0001 }, 0, 0 },
        { new ulong[] { 0b1000_0000_0000_0000 }, 0, 15 },
        { new ulong[] { 0b0000_1000_0000_0000 }, 0, 11 },
        { new ulong[] { 0b0000_1000_0000_0000 }, 12, -1 },
        { new ulong[] { 0b1111_1111_1111_1111 }, 0, 0 },
        { new ulong[] { 0b1111_1111_1111_1111 }, 5, 5 },
        { new ulong[] { 0b1111_1111_1111_1111 }, 15, 15 },
        { new ulong[] { 0b1111_1111_1111_1111 }, 16, -1 },
        { new ulong[] { 0b0000_0000_0000_0000, 0b0000_0000_0000_0001 }, 0, 64 },
        { new ulong[] { 0b0000_0000_0000_0000, 0b1000_0000_0000_0000 }, 0, 79 },
    };
}