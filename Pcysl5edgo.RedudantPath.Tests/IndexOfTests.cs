namespace Pcysl5edgo.RedudantPath.Tests;

public class IndexOfTests
{
    //[Theory]
    //[MemberData(nameof(IndexOfTrue64bitData))]
    //public void IndexOfTrue64bit(ulong[] array, int offset, int expected)
    //{
    //    int result = BitSpan.IndexOfTrue(ref MemoryMarshal.GetArrayDataReference(array), array.Length << 6, offset);
    //    Assert.Equal(expected, result);
    //}

    [Theory]
    [InlineData(0b1ul, 1, 0, 1)]
    [InlineData(0b1ul, 2, 0, 1)]
    [InlineData(0b1ul, 4, 0, 1)]
    [InlineData(0b10ul, 2, 0, 0)]
    [InlineData(0b10ul, 1, 0, 0)]
    [InlineData(0b11ul, 2, 0, 2)]
    [InlineData(0b1011ul, 2, 0, 2)]
    [InlineData(0b1011ul, 3, 0, 2)]
    [InlineData(0b1011ul, 4, 0, 2)]
    [InlineData(0b1010ul, 4, 0, 0)]
    [InlineData(0b1001ul, 4, 0, 1)]
    [InlineData(0b1011ul, 2, 1, 2)]
    [InlineData(0b1011ul, 3, 1, 2)]
    [InlineData(0b1011ul, 4, 1, 2)]
    [InlineData(0b1010ul, 4, 1, 2)]
    [InlineData(0b1001ul, 4, 1, 1)]
    public void TrailingOneCount64Bit(ulong value, int length, int offset, int expected)
    {
        Assert.Equal(expected, BitSpan.TrailingOneCount(value, length, offset));
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