namespace CatandomizerApp;

public class BoardState
{
  public LandSpace[]? LandSpaces { get; set; }

  public WaterSpace[]? WaterSpaces { get; set; }

  public string? Id { get; set; }
}

public class LandSpace
{
  public string? Type { get; set; }
  public int? Value { get; set; }
}

public class WaterSpace
{
  public string? Type { get; set; }
}

public class BoardSpace
{
  public BoardSpace(LandSpace landSpace)
  {
    Image = landSpace.Type switch
    {
      "Mountain" => "assets/Mountain.png",
      "Hill" => "assets/Hill.png",
      "Forest" => "assets/Forest.png",
      "Field" => "assets/Field.png",
      "Pasture" => "assets/Pasture.png",
      "Desert" => "assets/Desert.png",
      _ => throw new NotSupportedException()
    };

    (ResourceEmoji, ResourceName) = landSpace.Type switch
    {
      "Mountain" => ("🪨", "Ore"),
      "Hill" => ("🧱", "Brick"),
      "Forest" => ("🌲", "Lumber"),
      "Field" => ("🌾", "Grain"),
      "Pasture" => ("🐑", "Wool"),
      "Desert" => ("🏜️", "Desert"),
      _ => throw new NotSupportedException()
    };

    Value = landSpace?.Value;
    IsRed = Value is 6 or 8;
    PipCount = Value switch
    {
      2 or 12 => 1,
      3 or 11 => 2,
      4 or 10 => 3,
      5 or 9 => 4,
      6 or 8 => 5,
      _ => 0
    };
  }

  public int? Value { get; }

  public string Image { get; }

  public string ResourceEmoji { get; }

  public string ResourceName { get; }

  public bool IsRed { get; }

  public int PipCount { get; }
}

public class HarborSpace
{
  public HarborSpace(WaterSpace waterSpace)
  {
    Image = waterSpace.Type switch
    {
      "Mountain" => "assets/MountainHarbor.png",
      "Hill" => "assets/HillHarbor.png",
      "Forest" => "assets/ForestHarbor.png",
      "Field" => "assets/FieldHarbor.png",
      "Pasture" => "assets/PastureHarbor.png",
      "Generic" => "assets/GenericHarbor.png",
      null => "assets/NoHarbor.png",
      _ => throw new NotSupportedException()
    };

    (RatioText, ResourceEmoji) = waterSpace.Type switch
    {
      "Mountain" => ("2:1", "🪨"),
      "Hill" => ("2:1", "🧱"),
      "Forest" => ("2:1", "🌲"),
      "Field" => ("2:1", "🌾"),
      "Pasture" => ("2:1", "🐑"),
      "Generic" => ("3:1", ""),
      _ => ("", "")
    };
  }

  public string Image { get; }

  public string RatioText { get; }

  public string ResourceEmoji { get; }

  public bool IsHarbor => !string.IsNullOrEmpty(RatioText);
}
