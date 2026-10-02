using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var token = builder.Configuration["Tmdb:Token"]
    ?? throw new InvalidOperationException("Tmdb:Token não configurado.");

builder.Services.AddHttpClient("tmdb", client =>
{
    client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);
});

var app = builder.Build();

app.MapGet("/search", async (string query, IHttpClientFactory factory, CancellationToken ct) =>
{
    var http = factory.CreateClient("tmdb");
    var url = $"search/multi?query={Uri.EscapeDataString(query)}&language=pt-BR&include_adult=false";

    var data = await http.GetFromJsonAsync<TmdbSearchResponse>(url, ct);

    var results = (data?.Results ?? [])
        .Where(r => r.MediaType is "movie" or "tv")
        .Select(r =>
        {
            var date = r.ReleaseDate ?? r.FirstAirDate;
            return new TitleSummary(
                r.Id,
                r.MediaType,
                r.Title ?? r.Name ?? "Sem título",
                r.Overview,
                r.PosterPath is null ? null : "https://image.tmdb.org/t/p/w500" + r.PosterPath,
                date is { Length: >= 4 } ? date[..4] : null,
                r.VoteAverage);
        });

    return Results.Ok(results);
});

app.Run();

record TmdbSearchResponse(
    [property: JsonPropertyName("results")] List<TmdbResult> Results);

record TmdbResult(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("media_type")] string MediaType,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("overview")] string? Overview,
    [property: JsonPropertyName("poster_path")] string? PosterPath,
    [property: JsonPropertyName("release_date")] string? ReleaseDate,
    [property: JsonPropertyName("first_air_date")] string? FirstAirDate,
    [property: JsonPropertyName("vote_average")] double VoteAverage);

record TitleSummary(
    int Id, string MediaType, string Title, string? Overview,
    string? PosterUrl, string? Year, double Rating);