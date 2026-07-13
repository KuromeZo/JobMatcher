using System.Text;
using JobMatcher.API.Data;
using JobMatcher.API.Repositories;
using JobMatcher.API.Repositories.Interfaces;
using JobMatcher.API.Services;
using JobMatcher.API.Services.Claude;
using JobMatcher.API.Services.Gemini;
using JobMatcher.API.Services.Interfaces;
using JobMatcher.API.Services.JustJoinIt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=jobmatcher.db"));

// Repositories
builder.Services.AddScoped<IJobRepository, JobRepository>();

// Services
builder.Services.AddHttpClient<IJobFetcherService, JustJoinItFetcherService>();
builder.Services.AddHttpClient<IAiScoringService, ClaudeAiScoringService>();
builder.Services.AddHttpClient<IAiCvParserService, ClaudeAiCvParserService>();
//builder.Services.AddHttpClient<IAiScoringService, GeminiAiScoringService>();
//builder.Services.AddHttpClient<IAiCvParserService, GeminiAiCvParserService>();
builder.Services.AddScoped<IJobScoringOrchestrator, JobScoringOrchestrator>();
builder.Services.AddScoped<DocxTextExtractorService>();
builder.Services.AddScoped<AuthService>();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
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

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();