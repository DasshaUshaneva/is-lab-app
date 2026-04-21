using Microsoft.AspNetCore.Mvc;
using IsLabApp;
// Если установили пакет:
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

// /health
app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.Now }));

// /version
app.MapGet("/version", (IConfiguration config) =>
{
    var appName = config["App:Name"];
    var appVersion = config["App:Version"];
    return Results.Ok(new { name = appName, version = appVersion });
});

// /weatherforecast
app.MapGet("/weatherforecast", () =>
{
    var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
    return Enumerable.Range(1, 5).Select(index => new
    {
        Date = DateTime.Now.AddDays(index),
        TemperatureC = Random.Shared.Next(-20, 55),
        TemperatureF = 32 + (int)(Random.Shared.Next(-20, 55) * 9.0 / 5.0),
        Summary = summaries[Random.Shared.Next(summaries.Length)]
    });
});

// --- Заметки CRUD ---
var notes = new List<Note>
{
    new Note { Id = 1, Title = "Приветственная заметка", Text = "Это первая заметка", CreatedAt = DateTime.Now },
    new Note { Id = 2, Title = "Вторая заметка", Text = "Тестовый текст", CreatedAt = DateTime.Now }
};
var nextId = 3;

app.MapGet("/api/notes", () => Results.Ok(notes));

app.MapGet("/api/notes/{id}", (int id) =>
{
    var note = notes.FirstOrDefault(n => n.Id == id);
    return note is null ? Results.NotFound(new { error = $"Заметка с ID {id} не найдена" }) : Results.Ok(note);
});

app.MapPost("/api/notes", (Note newNote) =>
{
    if (string.IsNullOrWhiteSpace(newNote.Title))
        return Results.BadRequest(new { error = "Заголовок обязателен" });
    if (string.IsNullOrWhiteSpace(newNote.Text))
        return Results.BadRequest(new { error = "Текст обязателен" });

    newNote.Id = nextId++;
    newNote.CreatedAt = DateTime.Now;
    notes.Add(newNote);
    return Results.Created($"/api/notes/{newNote.Id}", newNote);
});

app.MapDelete("/api/notes/{id}", (int id) =>
{
    var note = notes.FirstOrDefault(n => n.Id == id);
    if (note is null)
        return Results.NotFound(new { error = $"Заметка с ID {id} не найдена" });
    notes.Remove(note);
    return Results.Ok(new { message = $"Заметка с ID {id} удалена" });
});

// --- Эндпоинт /db/ping ---
app.MapGet("/db/ping", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("Mssql");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.BadRequest(new { error = "Строка подключения не найдена" });
    }

    try
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return Results.Ok(new { status = "ok", message = "Подключение к базе данных успешно" });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            status = "error",
            message = "Не удалось подключиться к базе данных",
            error = ex.Message
        });
    }
});

app.Run();
