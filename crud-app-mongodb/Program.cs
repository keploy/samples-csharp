using PlaylistsApi.Services;
using PlaylistsApi.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoDBSettings>(builder.Configuration.GetSection("MongoDBSettings"));

builder.Services.AddSingleton<MongoDBService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/api/playlists", async (MongoDBService mongoDBService) => {
    var playlists = await mongoDBService.GetAsync();
    return Results.Ok(playlists);
});


app.MapGet("/api/playlists/{id}", async (MongoDBService mongoDBService, string id) => {
    var playlist = await mongoDBService.GetByIdAsync(id);
    return playlist is not null ? Results.Ok(playlist) : Results.NotFound();
});

app.MapPost("/api/playlists", async (MongoDBService mongoDBService, [FromBody] Playlist playlist) => {
    // The playlist from the body won't have an ID yet. The MongoDB driver will generate one.
    await mongoDBService.CreateAsync(playlist);
    // Return a 201 Created status with the newly created playlist.
    return Results.Created($"/api/playlists/{playlist.Id}", playlist);
});

app.MapPut("/api/playlists/{id}", async (MongoDBService mongoDBService, string id, [FromBody] Playlist updatedPlaylist) => {
    var playlist = await mongoDBService.GetByIdAsync(id);
    if (playlist is null)
    {
        return Results.NotFound();
    }

    updatedPlaylist.Id = id;
    await mongoDBService.UpdateAsync(id, updatedPlaylist);
    return Results.NoContent();
});

app.MapDelete("/api/playlists/{id}", async (MongoDBService mongoDBService, string id) => {
    var playlist = await mongoDBService.GetByIdAsync(id);
    if (playlist is null)
    {
        return Results.NotFound();
    }

    await mongoDBService.DeleteAsync(id);
    return Results.NoContent();
});


app.Run();
