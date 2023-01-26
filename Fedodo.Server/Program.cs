using Fedodo.Server;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpLogging;
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
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => { options.LoginPath = "/account/login"; });

builder.Services.AddEndpointsApiExplorer();
startup.AddSwagger(builder);

startup.AddOpenIdDict(builder, mongoClient);

await startup.CreateMongoDbIndexes(builder);

startup.SetupMongoDb();

startup.AddCustomServices(builder, mongoClient);

builder.WebHost.UseUrls("http://*:");

startup.AddApp(builder.Build(), useHttpLogging);