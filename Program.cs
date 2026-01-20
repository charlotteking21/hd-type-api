using Microsoft.AspNetCore.Mvc;
using SharpAstrology.HumanDesign;
using SharpAstrology.Interfaces;
using SharpAstrology.SwissEph;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// CORS (allow browser calls)
app.Use(async (context, next) =>
{
    context.Response.Headers["Access-Control-Allow-Origin"] = "*";
    context.Response.Headers["Access-Control-Allow-Methods"] = "POST, OPTIONS";
    context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";

    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 204;
        return;
    }

    await next();
});

app.MapPost("/api/hd-type", ([FromBody] HdRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.BirthDate) ||
        string.IsNullOrWhiteSpace(req.BirthTime) ||
        string.IsNullOrWhiteSpace(req.UtcOffset))
    {
        return Results.BadRequest(new { error = "Missing BirthDate, BirthTime, or UtcOffset" });
    }

    if (!DateTime.TryParse($"{req.BirthDate}T{req.BirthTime}:00", out var localDateTime))
    {
        return Results.BadRequest(new { error = "Invalid date or time format" });
    }

    if (!TimeSpan.TryParse(req.UtcOffset, out var offset))
    {
        return Results.BadRequest(new { error = "Invalid UTC offset format" });
    }

    // Convert local birth time → UTC
    var utcBirthTime = new DateTimeOffset(localDateTime, offset).UtcDateTime;

    // Create ephemeris context (Moshier = no external files)
    using IEphemerides eph = new SwissEphemerides();

    // Build Human Design bodygraph
    var bodyGraph = new HumanDesignBodyGraph(utcBirthTime, eph);

    return Results.Ok(new
    {
        type = bodyGraph.Type.ToString()
    });
});

app.Run();

public record HdRequest(string BirthDate, string BirthTime, string UtcOffset);
