using System.Text;
using Fedodo.Server;
using Microsoft.AspNetCore.Authentication.Cookies;
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

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = false,
        ValidIssuer = "dev.fedodo.social",
        // ValidAudience = builder.Configuration["Jwt:Issuer"],
        // IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

startup.AddSwagger(builder);

startup.SetupMongoDb();

startup.AddCustomServices(builder, mongoClient);

builder.WebHost.UseUrls("http://*:");

startup.AddApp(builder.Build(), useHttpLogging);