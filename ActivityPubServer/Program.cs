using ActivityPubServer;
using Microsoft.AspNetCore.Authentication.Cookies;
using MongoDB.Driver;
using Serilog;

var startup = new Startup();

var connectionString =
    $"mongodb+srv://{Environment.GetEnvironmentVariable("MONGO_USERNAME")}:{Environment.GetEnvironmentVariable("MONGO_PASSWORD")}@{Environment.GetEnvironmentVariable("MONGO_HOSTNAME")}/?retryWrites=true&w=majority";
var mongoClient = new MongoClient(connectionString);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => { options.LoginPath = "/account/login"; });

builder.Services.AddEndpointsApiExplorer();
startup.AddSwagger(builder);

startup.AddOpenIdDict(builder, mongoClient);

await startup.CreateMongoDbIndexes(builder);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

startup.SetupMongoDb();

startup.AddCustomServices(builder, mongoClient);

builder.WebHost.UseUrls("http://*:");

startup.AddApp(builder.Build());