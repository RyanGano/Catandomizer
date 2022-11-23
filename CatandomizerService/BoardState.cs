public class BoardState
{
  public static async Task<BoardState> CreateAsync(int? seed, bool shuffleValues)
  {
    seed ??= new Random().Next(int.MaxValue);
    var random = new Random(seed.Value);
    var tiles = (await ShuffleAsync(m_LandTypes, random)).ToArray();
    var values = shuffleValues ? (await ShuffleAsync(m_landValues, random)).ToArray() : m_landValues.ToArray();

    LandSpace[] spaces = new LandSpace[19];

    await Task.Run(() =>
    {
      for (int i = 0; i < spaces.Length; i++)
      {
        var tile = tiles[i];
        LandValue? landValue = null;

        spaces[i] = (new LandSpace(tile, landValue, i));
      }

      AddConnections(spaces[0], new[] { spaces[1], spaces[12], spaces[11] });
      AddConnections(spaces[1], new[] { spaces[2], spaces[13], spaces[12], spaces[0] });
      AddConnections(spaces[2], new[] { spaces[3], spaces[13], spaces[1] });
      AddConnections(spaces[3], new[] { spaces[4], spaces[14], spaces[13], spaces[2] });
      AddConnections(spaces[4], new[] { spaces[5], spaces[14], spaces[3] });
      AddConnections(spaces[5], new[] { spaces[6], spaces[15], spaces[14], spaces[4] });
      AddConnections(spaces[6], new[] { spaces[7], spaces[15], spaces[5] });
      AddConnections(spaces[7], new[] { spaces[8], spaces[16], spaces[15], spaces[6] });
      AddConnections(spaces[8], new[] { spaces[9], spaces[16], spaces[7] });
      AddConnections(spaces[9], new[] { spaces[10], spaces[17], spaces[16], spaces[8] });
      AddConnections(spaces[10], new[] { spaces[11], spaces[17], spaces[9] });
      AddConnections(spaces[11], new[] { spaces[0], spaces[12], spaces[17], spaces[10] });
      AddConnections(spaces[12], new[] { spaces[0], spaces[1], spaces[13], spaces[18], spaces[17], spaces[11] });
      AddConnections(spaces[13], new[] { spaces[1], spaces[2], spaces[3], spaces[14], spaces[18], spaces[12] });
      AddConnections(spaces[14], new[] { spaces[13], spaces[3], spaces[4], spaces[5], spaces[15], spaces[18] });
      AddConnections(spaces[15], new[] { spaces[18], spaces[14], spaces[5], spaces[6], spaces[7], spaces[16] });
      AddConnections(spaces[16], new[] { spaces[17], spaces[18], spaces[15], spaces[7], spaces[8], spaces[9] });
      AddConnections(spaces[17], new[] { spaces[11], spaces[12], spaces[18], spaces[16], spaces[9], spaces[10] });
      AddConnections(spaces[18], new[] { spaces[12], spaces[13], spaces[14], spaces[15], spaces[16], spaces[17] });

      AddLandValues(spaces, values);
    });

    return new BoardState(spaces, seed.Value);
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

  public BoardState(IReadOnlyList<LandSpace> boardSpaces, int seed)
  {
    Validate(boardSpaces);

    BoardSpaces = boardSpaces;
    Seed = seed;
  }

  private void Validate(IReadOnlyList<LandSpace> boardSpaces)
  {
    foreach (var space in boardSpaces)
    {
      foreach (var adjacentSpace in space.ConnectedSpaces)
      {
        if (space.LandValue?.CanBeNextTo(adjacentSpace.LandValue) is false)
          throw new InvalidDataException("Board state is invalid.");
      }

      if (space.LandType != LandType.Desert && space.LandValue is null)
        throw new InvalidDataException("Non-Desert Space with no value");
    }
  }

  public IReadOnlyList<LandSpace> BoardSpaces { get; }

  public int Seed { get; }

  private static readonly List<LandValue> m_landValues = new[]
  {
    new LandValue(5, false),
    new LandValue(2, false),
    new LandValue(6, true),
    new LandValue(3, false),
    new LandValue(8, true),
    new LandValue(10, false),
    new LandValue(9, false),
    new LandValue(12, false),
    new LandValue(11, false),
    new LandValue(4, false),
    new LandValue(8, true),
    new LandValue(10, false),
    new LandValue(9, false),
    new LandValue(4, false),
    new LandValue(5, false),
    new LandValue(6, true),
    new LandValue(3, false),
    new LandValue(11, false),
  }.ToList();

  private static readonly List<LandType> m_LandTypes = new[]
  {
    LandType.Mountain,
    LandType.Mountain,
    LandType.Mountain,
    LandType.Hill,
    LandType.Hill,
    LandType.Hill,
    LandType.Forest,
    LandType.Forest,
    LandType.Forest,
    LandType.Forest,
    LandType.Field,
    LandType.Field,
    LandType.Field,
    LandType.Field,
    LandType.Pasture,
    LandType.Pasture,
    LandType.Pasture,
    LandType.Pasture,
    LandType.Desert,
  }.ToList();
}