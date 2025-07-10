# Features:

### Swagger + OAuth Integration

✅ Add Swagger to the API1 project and configure OAuth2 integration to enable login via our Keycloak SSO solution.

- Add `Swashbuckle.AspNetCore` package
- Add `services.AddSwaggerGen(options => ...)` block to services
- Add `UserSwagger()` and `UserSwaggerUI(options => ...)` middlewares
