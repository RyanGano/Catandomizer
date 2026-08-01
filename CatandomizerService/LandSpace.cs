public class LandSpace(LandType landType, LandValue? landValue, int id)
{
  public void AddConnectedSpace(LandSpace landSpace)
  {
    if (ConnectedSpaces.Contains(landSpace))
      throw new ArgumentException("LandSpace already connected.");

    if (ConnectedSpaces.Count > 5)
      throw new InvalidOperationException("Cannot add more than six connected spaces.");

    ConnectedSpaces.Add(landSpace);
  }

  public override string ToString()
  {
    return $"ID: {Id} / LandType: {LandType} / LandValue: {LandValue}";
  }

  public LandType LandType { get; } = landType;
  public LandValue? LandValue { get; set; } = landValue;
  public List<LandSpace> ConnectedSpaces { get; } = new List<LandSpace>();
  public int Id { get; } = id;
}
