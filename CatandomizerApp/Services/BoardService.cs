using System.Net.Http.Json;

namespace CatandomizerApp;

/// <summary>
/// Typed client for the Catandomizer board service. The base address is
/// supplied by configuration (wwwroot/appsettings.json), so the deployed and
/// local-service URLs differ by config rather than by editing source.
/// </summary>
public class BoardService(HttpClient httpClient)
{
  /// <param name="id">A board code (or legacy integer seed) to reproduce a specific layout, or null for a new one.</param>
  /// <param name="beginnerLayout">True to request the fixed "beginner" layout.</param>
  public async Task<BoardState?> GetBoardAsync(string? id = null, bool beginnerLayout = false)
  {
    var path = beginnerLayout ? "getboard/default" : "getboard";

    if (id is not null)
      path += $"/{id}";

    return await httpClient.GetFromJsonAsync<BoardState>(path);
  }
}
