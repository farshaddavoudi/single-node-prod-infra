# Features:

### Scalar + OAuth Integration

✅ Add Scalar to the API1 project and configure OAuth2 integration to enable login via our Keycloak SSO solution.

- Add `Scalar.AspNetCore`, `Swashbuckle.AspNetCore.SwaggerGen` and `Microsoft.AspNetCore.OpenApi` packages
- Add `AddSwaggerGen()`, `AddOpenApi(options => …)` and `AddEndpointsApiExplorer()` to `program.cs` services
- Add `UseForwardedHeaders(options...)`, `UseSwagger()`, `MapOpenApi()` and `MapScalarApiReference(options => ...)` middlewares
