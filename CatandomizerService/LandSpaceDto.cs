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
      LandType.Mountain => "Mountain",
      LandType.Hill => "Hill",
      LandType.Forest => "Forest",
      LandType.Field => "Field",
      LandType.Pasture => "Pasture",
      LandType.Desert => "Desert",
      _ => "Unknown"
    };
  }
}