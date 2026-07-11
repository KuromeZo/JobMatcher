using JobMatcher.API.Data;
using JobMatcher.API.Repositories;
using JobMatcher.API.Repositories.Interfaces;
using JobMatcher.API.Services;
using JobMatcher.API.Services.Claude;
using JobMatcher.API.Services.Interfaces;
using JobMatcher.API.Services.JustJoinIt;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=jobmatcher.db"));

// Repositories
builder.Services.AddScoped<IJobRepository, JobRepository>();

// Services
builder.Services.AddHttpClient<IJobFetcherService, JustJoinItFetcherService>();
builder.Services.AddHttpClient<IAiScoringService, ClaudeAiScoringService>();
builder.Services.AddScoped<IJobScoringOrchestrator, JobScoringOrchestrator>();

// CV parsing
builder.Services.AddScoped<DocxTextExtractorService>();
builder.Services.AddHttpClient<IAiCvParserService, ClaudeAiCvParserService>();

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();
app.Run();