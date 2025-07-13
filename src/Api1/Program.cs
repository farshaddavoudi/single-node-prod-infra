using BlazorKCOidcBff.ApiService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

const bool requireHttps = false;
const string authenticationScheme = "OAuth2";
string clientId = builder.Configuration["Keycloak:ClientId"]!;
string clientSecret = builder.Configuration["Keycloak:ClientSecret"]!;
string keycloakAuthority = builder.Configuration["Keycloak:Authority"]!;
string keycloakAudience = builder.Configuration["Keycloak:Audience"]!;
string scalarRedirectUrl = builder.Configuration["Keycloak:ScalarRedirectUri"]!;
string scalarFaviconUrl = builder.Configuration["Scalar:FaviconUrl"]!;
Console.WriteLine($"Keycloak Settings: Authority={keycloakAuthority} | Audience={keycloakAudience}");
Console.WriteLine($"The Keycloak URL: {keycloakAuthority}");

builder.Services.AddAuthentication()
    .AddJwtBearer(authenticationScheme, jwtOptions =>
{
    jwtOptions.Authority = keycloakAuthority;
    jwtOptions.Audience = keycloakAudience;
    jwtOptions.RequireHttpsMetadata = requireHttps;
    jwtOptions.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = keycloakAuthority,
        ValidAudiences = ["ata-api1", "account"] 
    };
});

builder.Services.AddAuthorization();

builder.Services.AddOpenApi(options =>
{
    var openApiSecurityScheme = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Description = "OAuth2 authentication using Keycloak.",
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri(keycloakAuthority),
                TokenUrl = new Uri($"{keycloakAuthority}protocol/openid-connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "openid", "openid" },
                    { "profile", "profile" }
                }
            }
        },
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = authenticationScheme
        }
    };

    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes.Add(authenticationScheme, openApiSecurityScheme);
        return Task.CompletedTask;
    });    
    
    options.AddOperationTransformer((operation, context, ct) =>
    {
        if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any())
        {
            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = authenticationScheme,
                                Type = ReferenceType.SecurityScheme
                            }
                        },
                        ["openid", "profile"]
                    }
                }
            };
        }

        return Task.CompletedTask;
    });
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "API 1", Version = "v1" });

    options.AddSecurityDefinition("oauth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{keycloakAuthority}protocol/openid-connect/auth"),
                TokenUrl = new Uri($"{keycloakAuthority}protocol/openid-connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "openid", "OpenID Connect" },
                    { "profile", "User profile" }
                }
            }
        }
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth" },
                Scheme = "oauth",
                Name = "oauth",
                In = ParameterLocation.Header,
            },
            ["openid", "profile"]
        }
    });

    options.CustomOperationIds(apiDesc => apiDesc.TryGetMethodInfo(out var methodInfo) ? methodInfo.Name : null);
});

//builder.Services.Configure<ForwardedHeadersOptions>(options =>
//{
//    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

//    // Add Traefik IP here. You can inspect this by checking `docker network inspect`
//    // or just allow all for now (use with caution in internal environments):
//    options.KnownNetworks.Clear(); // Clear any preconfigured networks
//    options.KnownProxies.Clear();  // Clear any preconfigured proxies

//    // 👇 Allow all for testing — safe if you're in a controlled environment
//    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("0.0.0.0"), 0));
//});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
    KnownNetworks = {},
    KnownProxies = {}
});

//app.Use(async (context, next) =>
//{
//    Console.WriteLine($"🔍 Effective URL: {context.Request.Scheme}://{context.Request.Host}{context.Request.Path}");
//    await next();
//});

//var redirectUrl = builder.Configuration["Keycloak:ScalarRedirectUri"];
//Console.WriteLine($"Redirect Scalar URL: {redirectUrl}");


app.UseSwagger();
//app.UseSwaggerUI(c =>
//{
//    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API 1");
//    c.OAuthClientId(builder.Configuration["Keycloak:ClientId"]);
//    c.OAuthClientSecret(builder.Configuration["Keycloak:ClientSecret"]);
//    c.OAuthAppName("API 1 OAuth - Swagger");
//    c.OAuthUsePkce();
//    c.EnablePersistAuthorization();
//    c.ExposeSwaggerDocumentUrlsRoute = true;
//    c.OAuth2RedirectUrl("https://api1.farshaddavoudi.ir/swagger/oauth2-redirect.html");

//    // Explicitly configure the authorization URL

//    c.ConfigObject.AdditionalItems["oauth2RedirectUrl"] = "https://api1.farshaddavoudi.ir/swagger/oauth2-redirect.html";
//});


app.MapOpenApi();

app.MapScalarApiReference(options =>
{
    options
        .WithTheme(ScalarTheme.Solarized)
        .WithLayout(ScalarLayout.Modern)
        .WithFavicon(scalarFaviconUrl)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

    options.AddPreferredSecuritySchemes(authenticationScheme);

    options.AddAuthorizationCodeFlow(authenticationScheme, flow =>
        {
            flow.ClientId = clientId;
            flow.ClientSecret = clientSecret;
            flow.AuthorizationUrl = $"{keycloakAuthority}protocol/openid-connect/auth";
            flow.Pkce = Pkce.Sha256;
            flow.TokenUrl = $"{keycloakAuthority}protocol/openid-connect/token";
            flow.RedirectUri = scalarRedirectUrl;
            flow.SelectedScopes = ["openid", "profile"];
        })
        .AddDefaultScopes(authenticationScheme, ["openid", "profile"]);
});

app.MapWeatherApi();

app.Run();

