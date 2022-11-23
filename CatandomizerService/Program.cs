var AllowCatandomizerApp = "_allowCatandomizerApp";
const string version = "0.0.1";

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
todoItems.MapGet("/{id}", GetBoard).RequireCors(AllowCatandomizerApp);

app.MapGet("/", GetStatus);


app.Run();

static async Task<IResult> GetBoard(int? id, bool randomValues = true)
{
  var result = await BoardState.CreateAsync(id, randomValues);
  return TypedResults.Ok(new BoardStateDto(result));
}

static IResult GetStatus()
{
  return TypedResults.Ok($"CatandomizerService Status:Up  Version:{version}");
}
