using Microsoft.AspNetCore.Mvc;
using SharpAstrology.Enums;
using SharpAstrology.Ephemerides;
using SharpAstrology.HumanDesign;
using SharpAstrology.Interfaces;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Allow your website to call this API (CORS)
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

    // Combine date + time
    if (!DateTime.TryParse($"{req.BirthDate}T{req.BirthTime}:00", out var localDateTime))
    {
        return Results.BadRequest(new { error = "Invalid date or time format" });
    }

    // Parse UTC offset like -05:00 or +01:00
    if (!TimeSpan.TryParse(req.UtcOffset, out var offset))
    {
        return Results.BadRequest(new { error = "Invalid UTC offset format" });
    }

    // Convert local birth time → UTC
    var utcBirthTime = new DateTimeOffset(localDateTime, offset).UtcDateTime;

    // Create ephemeris (no external files needed)
    var ephService = new SwissEphemeridesService(EphType.Moshier);
    using IEphemerides eph = ephService.CreateContext();

    // Build Human Design chart
    var chart = new HumanDesignChart(utcBirthTime, eph);

    // Return ONLY the Type
    return Results.Ok(new
    {
        type = chart.Type.ToString()
    });
});

app.Run();

public record HdRequest(string BirthDate, string BirthTime, string UtcOffset);
