# Inspired by BlazorWebAppOidcBff and Keycloak

Tried to stay as close to the original as possible, but there are some changes.

## Features

- Keycloak authentication using OpenID Connect
- Pushed Authorization Requests (PAR)
- Role-based authorization
- CookieRefresher from original sample but not tested in this version

## Prerequisites

- .NET 9.0
- A running Keycloak instance
- Visual Studio 2022 or later
- Currently tested with:
  - Visual Studio 2022 Preview
  - .NET 9.0.200-preview.0.24575.35

## Quick Start

1. Configure Keycloak:
   - See links in References and Credits

2. Update Configuration in appsettings.json of the server project.


## References and Credits

- [Blazor WebApp OIDC BFF Original](https://github.com/dotnet/blazor-samples/tree/main/9.0/BlazorWebAppOidcBff)
- [.NET Aspire Keycloak Integration](https://learn.microsoft.com/en-us/dotnet/aspire/authentication/keycloak-integration?tabs=dotnet-cli)
  - Contains a complete realm configuration example which I used as a reference and then added roles according to article below
- [.NET Web API with Keycloak](https://medium.com/@faulycoelho/net-web-api-with-keycloak-11e0286240b9)
  - Shows how to map roles to claims in keycloak
- [Auth Enhancements in .NET 9](https://auth0.com/blog/authentication-authorization-enhancements-in-dotnet-9/)


## Contributing

As a hobby programmer hoping to transition into professional software development, I welcome all kinds of feedback and contributions!

Feel free to:
- Test and provide feedback
- Report issues
- Submit pull requests
- Share suggestions for improvements

## License

- Derived from MIT licesened Blazor sample BlazorWebAppOidcBff, so it is the same.

## License

MIT. See the [BlazorWebAppOidcBff License](https://github.com/dotnet/blazor-samples/tree/main?tab=MIT-2-ov-file) for details, as this project is derived from that sample.
