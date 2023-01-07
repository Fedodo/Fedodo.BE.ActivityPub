using System.Text;
using ActivityPubServer;
using ActivityPubServer.Handlers;
using ActivityPubServer.Interfaces;
using ActivityPubServer.Model.Authentication;
using ActivityPubServer.Model.OAuth;
using ActivityPubServer.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using OpenIddict.Abstractions;
using OpenIddict.MongoDb;
using OpenIddict.MongoDb.Models;
using Serilog;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerUI;


var connectionString =
    $"mongodb+srv://{Environment.GetEnvironmentVariable("MONGO_USERNAME")}:{Environment.GetEnvironmentVariable("MONGO_PASSWORD")}@{Environment.GetEnvironmentVariable("MONGO_HOSTNAME")}/?retryWrites=true&w=majority";
var mongoClient = new MongoClient(connectionString);


var builder = WebApplication.CreateBuilder(args);

// // This will log all requests
// builder.Services.AddHttpLogging(options =>
// {
//     options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
//                             HttpLoggingFields.RequestBody;
// });

builder.Services.AddControllers();
builder.Services.AddRazorPages();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "ApiPlayground", Version = "v1" });
        c.AddSecurityDefinition(
            "oauth2",
            new OpenApiSecurityScheme
            {
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow()
                    {
                        Scopes = new Dictionary<string, string>
                        {
                            ["email"] = "api scope description",
                            ["profile"] = "api scope description",
                            ["roles"] = "api scope description"
                        },
                        TokenUrl = new Uri("http://localhost/oauth/token"),
                        AuthorizationUrl = new Uri("http://localhost/oauth/authorize"),
                    }
                },
                In = ParameterLocation.Header,
                Name = HeaderNames.Authorization,
                Type = SecuritySchemeType.OAuth2
            }
        );
        c.AddSecurityRequirement(
            new OpenApiSecurityRequirement 
            {
                {
                    new OpenApiSecurityScheme{
                        Reference = new OpenApiReference{
                            Id = "oauth2", //The name of the previously defined security scheme.
                            Type = ReferenceType.SecurityScheme
                        }
                    },
                    new List<string>()
                }
            });
    }
);









builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        // Note: to use a remote server, call the MongoClient constructor overload
        // that accepts a connection string or an instance of MongoClientSettings.
        options.UseMongoDb().UseDatabase(mongoClient.GetDatabase("OpenIdDict"));
    })
    .AddServer(options =>
    {
        options.AddEncryptionKey(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("API_SECURITY_KEY"))));
        
        options.UseAspNetCore().DisableTransportSecurityRequirement();

        options.AddDevelopmentSigningCertificate(); // TODO
        
        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableLogoutEndpointPassthrough()
            .EnableStatusCodePagesIntegration()
            .EnableTokenEndpointPassthrough();
        
        // Mark the "email", "profile" and "roles" scopes as supported scopes.
        options.RegisterScopes(OpenIddictConstants.Scopes.Email, OpenIddictConstants.Scopes.Profile, OpenIddictConstants.Scopes.Roles);

        options.SetTokenEndpointUris("oauth/token");
        options.SetAuthorizationEndpointUris("oauth/authorize");
        
        options.AllowAuthorizationCodeFlow()
            .AllowRefreshTokenFlow();
    })
    // Register the OpenIddict validation components.
    .AddValidation(options =>
    {
        // Import the configuration from the local OpenIddict server instance.
        options.UseLocalServer();

        // Register the ASP.NET Core host.
        options.UseAspNetCore();
    });


var provider = builder.Services.BuildServiceProvider();
    var context = provider.GetRequiredService<IOpenIddictMongoDbContext>();
    var options = provider.GetRequiredService<IOptionsMonitor<OpenIddictMongoDbOptions>>().CurrentValue;
    var database = await context.GetDatabaseAsync(CancellationToken.None);

    var applications = database.GetCollection<OpenIddictMongoDbApplication>(options.ApplicationsCollectionName);

    await applications.Indexes.CreateManyAsync(new[]
    {
        new CreateIndexModel<OpenIddictMongoDbApplication>(
            Builders<OpenIddictMongoDbApplication>.IndexKeys.Ascending(application => application.ClientId),
            new CreateIndexOptions
            {
                Unique = true
            }),

        new CreateIndexModel<OpenIddictMongoDbApplication>(
            Builders<OpenIddictMongoDbApplication>.IndexKeys.Ascending(
                application => application.PostLogoutRedirectUris),
            new CreateIndexOptions
            {
                Background = true
            }),

        new CreateIndexModel<OpenIddictMongoDbApplication>(
            Builders<OpenIddictMongoDbApplication>.IndexKeys.Ascending(application => application.RedirectUris),
            new CreateIndexOptions
            {
                Background = true
            })
    });

    var authorizations = database.GetCollection<OpenIddictMongoDbAuthorization>(options.AuthorizationsCollectionName);

    await authorizations.Indexes.CreateOneAsync(
        new CreateIndexModel<OpenIddictMongoDbAuthorization>(
            Builders<OpenIddictMongoDbAuthorization>.IndexKeys
                .Ascending(authorization => authorization.ApplicationId)
                .Ascending(authorization => authorization.Scopes)
                .Ascending(authorization => authorization.Status)
                .Ascending(authorization => authorization.Subject)
                .Ascending(authorization => authorization.Type),
            new CreateIndexOptions
            {
                Background = true
            }));

    var scopes = database.GetCollection<OpenIddictMongoDbScope>(options.ScopesCollectionName);

    await scopes.Indexes.CreateOneAsync(new CreateIndexModel<OpenIddictMongoDbScope>(
        Builders<OpenIddictMongoDbScope>.IndexKeys.Ascending(scope => scope.Name),
        new CreateIndexOptions
        {
            Unique = true
        }));

    var tokens = database.GetCollection<OpenIddictMongoDbToken>(options.TokensCollectionName);

    await tokens.Indexes.CreateManyAsync(new[]
    {
        new CreateIndexModel<OpenIddictMongoDbToken>(
            Builders<OpenIddictMongoDbToken>.IndexKeys.Ascending(token => token.ReferenceId),
            new CreateIndexOptions<OpenIddictMongoDbToken>
            {
                // Note: partial filter expressions are not supported on Azure Cosmos DB.
                // As a workaround, the expression and the unique constraint can be removed.
                PartialFilterExpression = Builders<OpenIddictMongoDbToken>.Filter.Exists(token => token.ReferenceId),
                Unique = true
            }),

        new CreateIndexModel<OpenIddictMongoDbToken>(
            Builders<OpenIddictMongoDbToken>.IndexKeys
                .Ascending(token => token.ApplicationId)
                .Ascending(token => token.Status)
                .Ascending(token => token.Subject)
                .Ascending(token => token.Type),
            new CreateIndexOptions
            {
                Background = true
            })
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
builder.Services.AddSingleton<IActivityHandler, ActivityHandler>();
builder.Services.AddSingleton<IUserHandler, UserHandler>();

builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddSingleton<IAuthenticationHandler, AuthenticationHandler>();

builder.WebHost.UseUrls("http://*:");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // // c.SwaggerEndpoint("/swagger/v1/swagger.json", "Versioned API v1.0");
    // c.DocumentTitle = "Title Documentation";
    // c.DocExpansion(DocExpansion.None);
    // c.RoutePrefix = string.Empty;
    c.OAuthClientId("swagger2");
    c.OAuthAppName("Combitime API");
    c.OAuthScopeSeparator(",");
    c.OAuthUsePkce();
});
app.UseCors(x => x.AllowAnyHeader()
    .AllowAnyMethod()
    .WithOrigins("*"));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// app.UseHttpLogging();

Log.Information("Starting api");

app.Run();