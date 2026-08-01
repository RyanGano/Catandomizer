using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CatandomizerApp;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var boardServiceBaseAddress = builder.Configuration["BoardService:BaseAddress"]
  ?? throw new InvalidOperationException("BoardService:BaseAddress is not configured (see wwwroot/appsettings.json).");

builder.Services.AddHttpClient<BoardService>(client => client.BaseAddress = new Uri(boardServiceBaseAddress));

await builder.Build().RunAsync();
