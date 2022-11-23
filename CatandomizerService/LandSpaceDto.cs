public class LandSpaceDto
{
  public string? Type { get; set; }
  public int? Value { get; set; }

  public LandSpaceDto(LandSpace landSpace) =>
  (Type, Value) = (LandTypeAsString(landSpace.LandType), landSpace.LandValue?.Value);

  private string LandTypeAsString(LandType landType)
  {
    return landType switch
    {
      LandType.Mountain => "Ore / Mountain",
      LandType.Hill => "Brick / Hill",
      LandType.Forest => "Lumber / Forest",
      LandType.Field => "Wheat / Field",
      LandType.Pasture => "Sheep / Pasture",
      LandType.Desert => "Desert",
      _ => "Unknown"
    };
  }
}