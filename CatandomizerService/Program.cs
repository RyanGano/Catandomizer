var AllowCatandomizerApp = "_allowCatandomizerApp";
const string version = "0.0.2";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
  options.AddPolicy(name: AllowCatandomizerApp,
                    policy =>
                    {
                      policy.WithOrigins("http://localhost:5052");
                      policy.WithOrigins("https://catandomizer.azurewebsites.net");
                    });
});

var app = builder.Build();
app.UseCors();

RouteGroupBuilder todoItems = app.MapGroup("/getboard");

todoItems.MapGet("/", () => GetBoard(null)).RequireCors(AllowCatandomizerApp);
todoItems.MapGet("/default", () => GetBoard(null, false)).RequireCors(AllowCatandomizerApp);
todoItems.MapGet("/{id}", GetBoard).RequireCors(AllowCatandomizerApp);

app.MapGet("/", GetStatus);


app.Run();

static async Task<IResult> GetBoard(string? id, bool randomValues = true)
{
  try
  {
    // Accept either a legacy integer seed or a board code produced by BoardCode.
    BoardState result;
    if (id is null)
      result = await BoardState.CreateAsync(null, randomValues);
    else if (int.TryParse(id, out var seed) && seed >= 0)
      result = await BoardState.CreateAsync(seed, randomValues);
    else
      result = BoardState.CreateFromCode(id);

    return TypedResults.Ok(new BoardStateDto(result));
  }
  catch (FormatException e)
  {
    return TypedResults.BadRequest(e.Message);
  }
  catch (InvalidDataException)
  {
    return TypedResults.BadRequest("Board code decodes to an invalid board layout.");
  }
}

static IResult GetStatus()
{
  return TypedResults.Ok($"CatandomizerService Status:Up  Version:{version}");
}
