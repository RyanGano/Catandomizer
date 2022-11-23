public class LandValue
{
  public LandValue(int value, bool isRed)
  {
    Value = value;
    IsRed = isRed;
  }

  public bool CanBeNextTo(LandValue? other) => other is null || (this.Value != other.Value && (!this.IsRed || !other.IsRed));

  public override string ToString()
  {
    return $"Value: {Value} / Red: {IsRed}";
  }

  public int Value { get; }
  bool IsRed { get; }
}