# Dockerfile for Blazor BFF (Client1)
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ./Client1.Client ./Client1.Client
COPY ./Client1.Server ./Client1.Server
WORKDIR /src/Client1.Client
RUN dotnet restore
RUN dotnet publish -c Release -o /app/clientout
WORKDIR /src/Client1.Server
RUN dotnet restore
RUN dotnet publish -c Release -o /app/serverout

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/serverout .
COPY --from=build /app/clientout/wwwroot ./wwwroot
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8081
ENTRYPOINT ["dotnet", "Client1.Server.dll"]
