using System.Security.Cryptography;
using Fedodo.NuGet.Common.Handlers;
using Fedodo.NuGet.Common.Interfaces;
using Fedodo.NuGet.Common.Repositories;
using Fedodo.Server.APIs;
using Fedodo.Server.Handlers;
using Fedodo.Server.Interfaces;
using Fedodo.Server.Model.ActivityPub;
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

namespace Fedodo.Server;

public class Startup
{
    public void AddSwagger(WebApplicationBuilder webApplicationBuilder)
    {
#if DEBUG
        var tokenUrl = new Uri("http://localhost/oauth/token");
        var authUrl = new Uri("http://localhost/oauth/authorize");
#else
        var tokenUrl = new Uri(
            $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/oauth/token");
        var authUrl = new Uri(
            $"https://{Environment.GetEnvironmentVariable("DOMAINNAME")}/oauth/authorize");
#endif

        webApplicationBuilder.Services.AddSwaggerGen(
            c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ApiPlayground", Version = "v1" });
                c.AddSecurityDefinition(
                    "oauth2",
                    new OpenApiSecurityScheme
                    {
                        Flows = new OpenApiOAuthFlows
                        {
                            AuthorizationCode = new OpenApiOAuthFlow
                            {
                                Scopes = new Dictionary<string, string>
                                {
                                    ["email"] = "api scope description",
                                    ["profile"] = "api scope description",
                                    ["roles"] = "api scope description"
                                },
                                TokenUrl = tokenUrl,
                                AuthorizationUrl = authUrl
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
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Id = "oauth2", //The name of the previously defined security scheme.
                                    Type = ReferenceType.SecurityScheme
                                }
                            },
                            new List<string>()
                        }
                    });
            }
        );
    }

    public void AddOpenIdDict(WebApplicationBuilder webApplicationBuilder, MongoClient mongoClient1)
    {
        webApplicationBuilder.Services.AddOpenIddict()
            .AddCore(options =>
            {
                // Note: to use a remote server, call the MongoClient constructor overload
                // that accepts a connection string or an instance of MongoClientSettings.
                options.UseMongoDb().UseDatabase(mongoClient1.GetDatabase("OpenIdDict"));
            })
            .AddServer(options =>
            {
                var encryptionCert = RSA.Create();
                encryptionCert.ImportFromPem(Environment.GetEnvironmentVariable("API_ENCRYPTION_CERT"));
                var signingCert = RSA.Create();
                signingCert.ImportFromPem(Environment.GetEnvironmentVariable("API_SIGNING_CERT"));

                options.AddEncryptionKey(new RsaSecurityKey(encryptionCert));
                options.AddSigningKey(new RsaSecurityKey(signingCert));

                options.UseAspNetCore().DisableTransportSecurityRequirement();

                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableLogoutEndpointPassthrough()
                    .EnableStatusCodePagesIntegration()
                    .EnableTokenEndpointPassthrough();

                // Mark the "email", "profile" and "roles" scopes as supported scopes.
                options.RegisterScopes(OpenIddictConstants.Scopes.Email, OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Roles, "read", "write", "follow");

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
    }

    public void AddApp(WebApplication app, bool httpLogging = false)
    {
        if (httpLogging) app.UseHttpLogging();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.OAuthClientId("swagger2");
            c.OAuthClientSecret("test");
            c.OAuthAppName("Swagger API");
            c.OAuthScopeSeparator(",");
            // c.OAuthUsePkce();
        });
        app.UseCors(x => x.AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins("*"));

        app.UseStaticFiles();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(options =>
        {
            options.MapRazorPages();
            options.MapControllers();
            options.MapFallbackToFile("index.html");
        });

        app.Run();
    }

    public async Task CreateMongoDbIndexes(WebApplicationBuilder webApplicationBuilder)
    {
        var provider = webApplicationBuilder.Services.BuildServiceProvider();
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

        var authorizations =
            database.GetCollection<OpenIddictMongoDbAuthorization>(options.AuthorizationsCollectionName);

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
                    PartialFilterExpression =
                        Builders<OpenIddictMongoDbToken>.Filter.Exists(token => token.ReferenceId),
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
    }

    public void AddCustomServices(WebApplicationBuilder builder, MongoClient mongoClient1)
    {
        builder.Services.AddSingleton<IMongoDbRepository, MongoDbRepository>();
        builder.Services.AddSingleton<IHttpSignatureHandler, HttpSignatureHandler>();
        builder.Services.AddSingleton<IActivityHandler, ActivityHandler>();
        builder.Services.AddSingleton<IUserHandler, UserHandler>();
        builder.Services.AddSingleton<IMongoClient>(mongoClient1);
        builder.Services.AddSingleton<IAuthenticationHandler, AuthenticationHandler>();
        builder.Services.AddSingleton<IActorAPI, ActorApi>();
        builder.Services.AddSingleton<IActivityAPI, ActivityAPI>();
        builder.Services.AddSingleton<IKnownSharedInboxHandler, KnownSharedInboxHandler>();
        builder.Services.AddSingleton<ICollectionApi, CollectionApi>();
    }

    public void SetupMongoDb()
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
        BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.String));
        var objectSerializer = new ObjectSerializer(type =>
            ObjectSerializer.DefaultAllowedTypes(type) || type.FullName.StartsWith("Fedodo.Server"));
        BsonSerializer.RegisterSerializer(objectSerializer);
        BsonSerializer.RegisterDiscriminator(typeof(Post), "Post");
    }
}