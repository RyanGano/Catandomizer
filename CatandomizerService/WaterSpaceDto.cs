public class WaterSpaceDto
{
  public string? Type { get; set; }

  public WaterSpaceDto(WaterSpace waterSpace) =>
  (Type) = (WaterTypeAsString(waterSpace.HarborType));

  private string? WaterTypeAsString(HarborType? harborType)
  {
    return harborType switch
    {
      HarborType.Mountain => "Mountain",
      HarborType.Hill => "Hill",
      HarborType.Forest => "Forest",
      HarborType.Field => "Field",
      HarborType.Pasture => "Pasture",
      HarborType.Generic => "Generic",
      null => null,
      _ => "Unknown"
    };
  }
}