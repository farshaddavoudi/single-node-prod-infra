# Features:

### Scalar + OAuth Integration

✅ Add Scalar to the API1 project and configure OAuth2 integration to enable login via our Keycloak SSO solution.

- Add `Scalar.AspNetCore`, `Swashbuckle.AspNetCore.SwaggerGen` and `Microsoft.AspNetCore.OpenApi` packages
- Add `AddSwaggerGen()`, `AddOpenApi(options => …)` and `AddEndpointsApiExplorer()` to `program.cs` services
- Add `UseForwardedHeaders(options...)`, `UseSwagger()`, `MapOpenApi()` and `MapScalarApiReference(options => ...)` middlewares

✅ Add YARP as an API gateway and use it to proxy all requests to other APIs.

- Handle `api-gateway-url/docs` to easily navigate to all downstream services OpenAPI page _(e.g. API1 Scalar)_.
- Secure docs related pages with Traefik
- Modify all Scalars to rewrite endpoint URLs so that calls go through the API gateway instead of directly to their services. (The frontend will interact only with the API gateway.)
