public class BoardState
{
  public static async Task<BoardState> CreateAsync(int? seed, bool shuffleValues)
  {
    seed ??= new Random().Next(int.MaxValue);
    var random = new Random(seed.Value);
    var tiles = shuffleValues ? (await ShuffleAsync(m_landTypes, random)).ToArray() : m_landTypes.ToArray();
    var values = shuffleValues ? (await ShuffleAsync(m_landValues, random)).ToArray() : m_landValues.ToArray();

    LandSpace[] landSpaces = new LandSpace[19];

    await Task.Run(() =>
    {
      for (int i = 0; i < landSpaces.Length; i++)
      {
        var tile = tiles[i];
        LandValue? landValue = null;

        landSpaces[i] = (new LandSpace(tile, landValue, i));
      }

      AddConnections(landSpaces[0], new[] { landSpaces[1], landSpaces[12], landSpaces[11] });
      AddConnections(landSpaces[1], new[] { landSpaces[2], landSpaces[13], landSpaces[12], landSpaces[0] });
      AddConnections(landSpaces[2], new[] { landSpaces[3], landSpaces[13], landSpaces[1] });
      AddConnections(landSpaces[3], new[] { landSpaces[4], landSpaces[14], landSpaces[13], landSpaces[2] });
      AddConnections(landSpaces[4], new[] { landSpaces[5], landSpaces[14], landSpaces[3] });
      AddConnections(landSpaces[5], new[] { landSpaces[6], landSpaces[15], landSpaces[14], landSpaces[4] });
      AddConnections(landSpaces[6], new[] { landSpaces[7], landSpaces[15], landSpaces[5] });
      AddConnections(landSpaces[7], new[] { landSpaces[8], landSpaces[16], landSpaces[15], landSpaces[6] });
      AddConnections(landSpaces[8], new[] { landSpaces[9], landSpaces[16], landSpaces[7] });
      AddConnections(landSpaces[9], new[] { landSpaces[10], landSpaces[17], landSpaces[16], landSpaces[8] });
      AddConnections(landSpaces[10], new[] { landSpaces[11], landSpaces[17], landSpaces[9] });
      AddConnections(landSpaces[11], new[] { landSpaces[0], landSpaces[12], landSpaces[17], landSpaces[10] });
      AddConnections(landSpaces[12], new[] { landSpaces[0], landSpaces[1], landSpaces[13], landSpaces[18], landSpaces[17], landSpaces[11] });
      AddConnections(landSpaces[13], new[] { landSpaces[1], landSpaces[2], landSpaces[3], landSpaces[14], landSpaces[18], landSpaces[12] });
      AddConnections(landSpaces[14], new[] { landSpaces[13], landSpaces[3], landSpaces[4], landSpaces[5], landSpaces[15], landSpaces[18] });
      AddConnections(landSpaces[15], new[] { landSpaces[18], landSpaces[14], landSpaces[5], landSpaces[6], landSpaces[7], landSpaces[16] });
      AddConnections(landSpaces[16], new[] { landSpaces[17], landSpaces[18], landSpaces[15], landSpaces[7], landSpaces[8], landSpaces[9] });
      AddConnections(landSpaces[17], new[] { landSpaces[11], landSpaces[12], landSpaces[18], landSpaces[16], landSpaces[9], landSpaces[10] });
      AddConnections(landSpaces[18], new[] { landSpaces[12], landSpaces[13], landSpaces[14], landSpaces[15], landSpaces[16], landSpaces[17] });

      AddLandValues(landSpaces, values);
    });


    var waterSpaces = (shuffleValues ? (await ShuffleAsync(m_harborTypes, random)) : m_harborTypes).SelectMany(x => new[] { new WaterSpace(x), new WaterSpace(null) }).ToArray();

    return new BoardState(landSpaces, waterSpaces, shuffleValues ? seed.Value.ToString() : "Default");
  }

  private static void AddLandValues(LandSpace[] spaces, LandValue[] values)
  {
    var theValues = values.ToList();

    foreach (var space in spaces.Where(x => x.LandValue is null && x.LandType != LandType.Desert))
    {
      var workingValue = FirstAvailableValue(space, theValues);

      if (workingValue is not null)
      {
        space.LandValue = workingValue;
        theValues.Remove(workingValue);
      }
      else
      {
        ReplaceWithValues(space, theValues.First(), spaces);
        theValues.RemoveAt(0);
      }
    }
  }

  private static LandValue? FirstAvailableValue(LandSpace space, List<LandValue> values)
  {
    return values.FirstOrDefault(x => space.ConnectedSpaces.All(y => y.LandValue?.CanBeNextTo(x) != false));
  }

  private static void ReplaceWithValues(LandSpace space, LandValue value, LandSpace[] spaces)
  {
    var possibleReplacements = spaces.Where(x => x.ConnectedSpaces.All(y => y.LandValue?.CanBeNextTo(value) is not false));
    var replacementSpace = possibleReplacements.First(x => space.ConnectedSpaces.All(y => y.LandValue?.CanBeNextTo(x.LandValue) is not false));
    space.LandValue = replacementSpace.LandValue;
    replacementSpace.LandValue = value;
  }

  private static void AddConnections(LandSpace owningSpace, LandSpace[] connections)
  {
    foreach (var connection in connections)
      owningSpace.AddConnectedSpace(connection);
  }

  private static async Task<IReadOnlyList<T>> ShuffleAsync<T>(List<T> items, Random randomizer)
  {
    var theItems = items.ToList();

    void Swap(int one, int two)
    {
      if (one >= theItems.Count || one < 0)
        throw new ArgumentOutOfRangeException(nameof(one));
      if (two >= theItems.Count || one < 0)
        throw new ArgumentOutOfRangeException(nameof(two));

      var temp = theItems[one];
      theItems[one] = theItems[two];
      theItems[two] = temp;
    }

    await Task.Run(() =>
    {
      for (int i = 0; i < 5; i++)
      {
        for (int x = 0; x < items.Count; x++)
          Swap(x, randomizer.Next(items.Count));
      }
    });

    return theItems;
  }

  public BoardState(IReadOnlyList<LandSpace> landSpaces, IReadOnlyList<WaterSpace> waterSpaces, string seed)
  {
    Validate(landSpaces, seed);

    BoardSpaces = landSpaces;
    WaterSpaces = waterSpaces;
    Seed = seed;
  }

  private void Validate(IReadOnlyList<LandSpace> boardSpaces, string seed)
  {
    foreach (var space in boardSpaces)
    {
      foreach (var adjacentSpace in space.ConnectedSpaces)
      {
        if (space.LandValue?.CanBeNextTo(adjacentSpace.LandValue) is false)
          throw new InvalidDataException($"Board state is invalid. Seed: {seed}");
      }

      if (space.LandType != LandType.Desert && space.LandValue is null)
        throw new InvalidDataException($"Non-Desert Space with no value. Seed: {seed}");
    }
  }

  public IReadOnlyList<LandSpace> BoardSpaces { get; }

  public IReadOnlyList<WaterSpace> WaterSpaces { get; }

  public string Seed { get; }

  private static readonly List<LandValue> m_landValues = new[]
  {
    new LandValue(5, false),
    new LandValue(6, true),
    new LandValue(11, false),
    new LandValue(5, false),
    new LandValue(8, true),
    new LandValue(10, false),
    new LandValue(9, false),
    new LandValue(2, false),
    new LandValue(10, false),
    new LandValue(12, false),
    new LandValue(9, false),
    new LandValue(8, true),
    new LandValue(3, false),
    new LandValue(4, false),
    new LandValue(3, false),
    new LandValue(4, false),
    new LandValue(6, true),
    new LandValue(11, false),
  }.ToList();

  private static readonly List<LandType> m_landTypes = new[]
  {
    LandType.Hill,
    LandType.Field,
    LandType.Pasture,
    LandType.Pasture,
    LandType.Mountain,
    LandType.Hill,
    LandType.Forest,
    LandType.Pasture,
    LandType.Mountain,
    LandType.Field,
    LandType.Field,
    LandType.Forest,
    LandType.Mountain,
    LandType.Field,
    LandType.Forest,
    LandType.Pasture,
    LandType.Hill,
    LandType.Forest,
    LandType.Desert,
  }.ToList();

  private static readonly List<HarborType> m_harborTypes = new[]
  {
    HarborType.Generic,
    HarborType.Generic,
    HarborType.Pasture,
    HarborType.Generic,
    HarborType.Mountain,
    HarborType.Field,
    HarborType.Generic,
    HarborType.Forest,
    HarborType.Hill,
  }.ToList();
}