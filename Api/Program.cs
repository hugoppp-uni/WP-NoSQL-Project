using Microsoft.Extensions.DependencyInjection;
using Neo4jClient;
using Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Configuration.AddEnvironmentVariables();

//add DBs
builder.Services.AddSingleton<IGraphClient>(ConnectionCreator.Neo4J());
builder.Services.AddSingleton(ConnectionCreator.Mongo());
//add APIs
builder.Services.AddSingleton(static (services) =>
    ConnectionCreator.TwitterApi(services.GetRequiredService<IConfiguration>()));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
}

app.UseAuthorization();

app.MapControllers();

app.Run();
