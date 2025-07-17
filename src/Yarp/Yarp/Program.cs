using ConfigurationPlaceholders;
using Yarp.YarpConfigs;

var builder = WebApplication.CreateBuilder(args);

builder.SetYarpConfigProviders();

if (builder.Environment.IsDevelopment())
{
    DotNetEnv.Env.Load();
}

builder.AddConfigurationPlaceholders(new EnvironmentVariableResolver());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors();

var app = builder.Build();

app.UseSwagger();

app.UseAuthorization();

app.UseCors(corsPolicyBuilder => corsPolicyBuilder    
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
    //.AllowCredentials()
);

app.MapReverseProxy();

app.MapControllers();

app.Run();