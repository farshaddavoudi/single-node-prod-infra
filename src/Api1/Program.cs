using System.Net;
using BlazorKCOidcBff.ApiService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

var builder = WebApplication.CreateBuilder(args);

const bool requireHttps = false;
const string authenticationScheme = "OAuth2";
string clientId = builder.Configuration["Keycloak:ClientId"]!;
string clientSecret = builder.Configuration["Keycloak:ClientSecret"]!;
string keycloakAuthority = builder.Configuration["Keycloak:Authority"]!;
string keycloakAudience = builder.Configuration["Keycloak:Audience"]!;
string scalarRedirectUrl = builder.Configuration["Keycloak:ScalarRedirectUri"]!;
string apiGatewayBaseUrl = builder.Configuration["Urls:ApiGateway:BaseUrl"]!;
string apiGatewayIdentifierPath = builder.Configuration["Urls:ApiGateway:IdentifierPath"]!;
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

builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
    KnownNetworks = { new IPNetwork(IPAddress.Parse("0.0.0.0"), 0) },
    KnownProxies = { }
});

app.UseSwagger();

app.MapOpenApi();

app.MapScalarApiReference(options =>
{
    options.Title = "Api1 API doc";
    options
        .WithTheme(ScalarTheme.Solarized)
        .WithLayout(ScalarLayout.Modern)
        .WithDarkMode(false)
        .WithFavicon(scalarFaviconUrl)
        .WithDefaultHttpClient(ScalarTarget.Node, ScalarClient.Axios); //React
        //.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient); //Blazor

    if (builder.Environment.IsProduction())
    {
        options.AddServer($"{apiGatewayBaseUrl}{apiGatewayIdentifierPath}");
    }

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

