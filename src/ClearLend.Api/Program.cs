using ClearLend.Application;
using ClearLend.Application.Abstractions;
using ClearLend.Infrastructure;
using ClearLend.Infrastructure.Identity;
using ClearLend.Infrastructure.Borrowers;
using ClearLend.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddClearLendApplication();
builder.Services.AddScoped<ICurrentActor, HttpCurrentActor>();
builder.Services.AddClearLendPersistence(builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapControllers();

app.Run();
