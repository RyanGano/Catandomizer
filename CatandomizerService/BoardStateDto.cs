public class BoardStateDto
{
  public LandSpaceDto[]? LandSpaces { get; set; }
  public int? Id { get; set; }

  public BoardStateDto(BoardState boardState) =>
  (LandSpaces, Id) = (boardState.BoardSpaces.Select(x => new LandSpaceDto(x)).ToArray(), boardState.Seed);
}