using System.Text;
using ActivityPubServer.Handlers;
using ActivityPubServer.Interfaces;
using ActivityPubServer.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Serilog;
using Swashbuckle.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);

// // This will log all requests
// builder.Services.AddHttpLogging(options =>
// {
//     options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
//                             HttpLoggingFields.RequestBody;
// });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "Standard Authorization Header using the Bearer scheme (\"bearer {token}\")",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    options.OperationFilter<SecurityRequirementsOperationFilter>();
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes($"{Environment.GetEnvironmentVariable("API_SECURITY_KEY")}")),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

//Logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.String));

builder.Services.AddSingleton<IMongoDbRepository, MongoDbRepository>();
builder.Services.AddSingleton<IKnownServersHandler, KnownServersHandler>();
builder.Services.AddSingleton<IHttpSignatureHandler, HttpSignatureHandler>();
builder.Services.AddSingleton<IUserVerificationHandler, UserVerificationHandler>();

var connectionString =
    $"mongodb+srv://{Environment.GetEnvironmentVariable("MONGO_USERNAME")}:{Environment.GetEnvironmentVariable("MONGO_PASSWORD")}@{Environment.GetEnvironmentVariable("MONGO_HOSTNAME")}/?retryWrites=true&w=majority";
builder.Services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
builder.Services.AddSingleton<IAuthenticationHandler, AuthenticationHandler>();

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
// app.UseHttpLogging();

Log.Information("Starting api");

app.Run();