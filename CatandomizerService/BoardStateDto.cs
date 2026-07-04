public class BoardStateDto
{
  public LandSpaceDto[]? LandSpaces { get; set; }

  public WaterSpaceDto[]? WaterSpaces { get; set; }
  public string? Id { get; set; }

  public BoardStateDto(BoardState boardState) =>
  (LandSpaces, WaterSpaces, Id) = (boardState.BoardSpaces.Select(x => new LandSpaceDto(x)).ToArray(), boardState.WaterSpaces.Select(x => new WaterSpaceDto(x)).ToArray(), boardState.Seed);
}