public class LandSpace
{
  public LandSpace(LandType landType, LandValue? landValue, int id)
  {
    LandType = landType;
    LandValue = landValue;
    ConnectedSpaces = new List<LandSpace>();
    Id = id;
  }

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

  public LandType LandType { get; }
  public LandValue? LandValue { get; set; }
  public List<LandSpace> ConnectedSpaces { get; }
  public int Id { get; }
}