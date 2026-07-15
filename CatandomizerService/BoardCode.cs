// Encodes a full board layout as a short Base64url string:
// [4b version][4b game set][19 x 3b land types][18 x 4b values][9 x 3b harbors]
// padded to a byte boundary, followed by a CRC-8 byte.
// Base game: 165 bits -> 21 data bytes + 1 CRC byte = 22 bytes = 30 Base64url characters.
public static class BoardCode
{
  private const int Version = 1;

  public static string Encode(IReadOnlyList<LandSpace> landSpaces, IReadOnlyList<WaterSpace> waterSpaces, GameSet gameSet = GameSet.Base)
  {
    var landCount = landSpaces.Count;
    var valueCount = landSpaces.Count(x => x.LandType != LandType.Desert);
    var harborCount = waterSpaces.Count(x => x.HarborType is not null);

    var dataByteLength = ByteLengthForBits(4 + 4 + landCount * 3 + valueCount * 4 + harborCount * 3);
    var writer = new BitWriter(dataByteLength + 1);
    writer.Write(Version, 4);
    writer.Write((int)gameSet, 4);

    foreach (var space in landSpaces)
      writer.Write((int)space.LandType, 3);

    foreach (var space in landSpaces.Where(x => x.LandType != LandType.Desert))
      writer.Write(space.LandValue!.Value, 4);

    foreach (var water in waterSpaces.Where(x => x.HarborType is not null))
      writer.Write((int)water.HarborType!, 3);

    var bytes = writer.ToArray();
    bytes[dataByteLength] = Crc8(bytes.AsSpan(0, dataByteLength));

    return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
  }

  public static (GameSet GameSet, LandType[] LandTypes, int[] Values, HarborType[] Harbors) Decode(string code)
  {
    byte[] bytes;
    try
    {
      var base64 = code.Trim().Replace('-', '+').Replace('_', '/');
      bytes = Convert.FromBase64String(base64.PadRight((base64.Length + 3) / 4 * 4, '='));
    }
    catch (FormatException)
    {
      throw new FormatException("Board code is not valid Base64url.");
    }

    if (bytes.Length < 2)
      throw new FormatException("Board code has the wrong length.");

    var reader = new BitReader(bytes);

    if (reader.Read(4) != Version)
      throw new FormatException("Board code version is not supported.");

    var gameSet = (GameSet)reader.Read(4);
    if (gameSet != GameSet.Base)
      throw new FormatException("Board code is for a game set this service does not support yet.");

    var landCount = BoardState.DefaultLandTypes.Count;
    var valueCount = BoardState.DefaultLandValues.Count;
    var harborCount = BoardState.DefaultHarborTypes.Count;

    var dataByteLength = ByteLengthForBits(4 + 4 + landCount * 3 + valueCount * 4 + harborCount * 3);
    if (bytes.Length != dataByteLength + 1)
      throw new FormatException("Board code has the wrong length.");

    if (Crc8(bytes.AsSpan(0, dataByteLength)) != bytes[dataByteLength])
      throw new FormatException("Board code checksum does not match; the code is corrupted.");

    var landTypes = new LandType[landCount];
    for (int i = 0; i < landCount; i++)
    {
      var raw = reader.Read(3);
      if (raw > (int)LandType.Desert)
        throw new FormatException("Board code contains an unknown land type.");
      landTypes[i] = (LandType)raw;
    }

    var values = new int[valueCount];
    for (int i = 0; i < valueCount; i++)
      values[i] = reader.Read(4);

    var harbors = new HarborType[harborCount];
    for (int i = 0; i < harborCount; i++)
    {
      var raw = reader.Read(3);
      if (raw > (int)HarborType.Generic)
        throw new FormatException("Board code contains an unknown harbor type.");
      harbors[i] = (HarborType)raw;
    }

    ValidateMultiset(landTypes, BoardState.DefaultLandTypes, "land tiles");
    ValidateMultiset(values, BoardState.DefaultLandValues.Select(x => x.Value), "number tokens");
    ValidateMultiset(harbors, BoardState.DefaultHarborTypes, "harbors");

    return (gameSet, landTypes, values, harbors);
  }

  private static int ByteLengthForBits(int bits) => (bits + 7) / 8;

  private static void ValidateMultiset<T>(IEnumerable<T> actual, IEnumerable<T> expected, string what) where T : notnull
  {
    var expectedCounts = expected.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
    var actualCounts = actual.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());

    if (expectedCounts.Count != actualCounts.Count ||
        expectedCounts.Any(kvp => !actualCounts.TryGetValue(kvp.Key, out var count) || count != kvp.Value))
      throw new FormatException($"Board code does not contain the standard set of {what}.");
  }

  private static byte Crc8(ReadOnlySpan<byte> data)
  {
    byte crc = 0;
    foreach (var b in data)
    {
      crc ^= b;
      for (int i = 0; i < 8; i++)
        crc = (byte)((crc & 0x80) != 0 ? (crc << 1) ^ 0x07 : crc << 1);
    }
    return crc;
  }

  private class BitWriter
  {
    private readonly byte[] m_bytes;
    private int m_bitPosition;

    public BitWriter(int byteLength) => m_bytes = new byte[byteLength];

    public void Write(int value, int bitCount)
    {
      for (int i = bitCount - 1; i >= 0; i--)
      {
        if ((value & (1 << i)) != 0)
          m_bytes[m_bitPosition / 8] |= (byte)(0x80 >> (m_bitPosition % 8));
        m_bitPosition++;
      }
    }

    public byte[] ToArray() => m_bytes;
  }

  private class BitReader
  {
    private readonly byte[] m_bytes;
    private int m_bitPosition;

    public BitReader(byte[] bytes) => m_bytes = bytes;

    public int Read(int bitCount)
    {
      int value = 0;
      for (int i = 0; i < bitCount; i++)
      {
        value <<= 1;
        if ((m_bytes[m_bitPosition / 8] & (0x80 >> (m_bitPosition % 8))) != 0)
          value |= 1;
        m_bitPosition++;
      }
      return value;
    }
  }
}
