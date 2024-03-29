using System.Text.Json.Serialization;
using Fedodo.BE.ActivityPub;
using Fedodo.BE.ActivityPub.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

var startup = new Startup();
const bool useHttpLogging = true; // Only for debug purposes
var connectionString =
    $"mongodb+srv://{Environment.GetEnvironmentVariable("MONGO_USERNAME")}:{Environment.GetEnvironmentVariable("MONGO_PASSWORD")}@{Environment.GetEnvironmentVariable("MONGO_HOSTNAME")}/?retryWrites=true&w=majority";
var mongoClient = new MongoClient(connectionString);

var builder = WebApplication.CreateBuilder(args);

if (useHttpLogging)
    builder.Services.AddHttpLogging(options =>
    {
        options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
                                HttpLoggingFields.RequestBody;
    });

builder.Services.AddControllersWithViews()
    .AddControllersAsServices()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddRazorPages();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = "https://auth." + GeneralConstants.DomainName;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "https://auth." + GeneralConstants.DomainName
        // ValidAudience = builder.Configuration["Jwt:Issuer"],
    };
});

startup.AddSwagger(builder);

startup.SetupMongoDb();

startup.AddCustomServices(builder, mongoClient);

builder.WebHost.UseUrls("http://*:");

startup.AddApp(builder.Build(), useHttpLogging);