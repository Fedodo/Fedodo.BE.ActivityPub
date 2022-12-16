using ActivityPubServer.Interfaces;
using ActivityPubServer.Repositories;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IInMemRepository, InMemRepository>();

//Logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

builder.WebHost.UseUrls("http://*:");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors(x => x.AllowAnyHeader()
                  .AllowAnyMethod()
                  .WithOrigins("*"));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Log.Information("Starting api");

app.Run();