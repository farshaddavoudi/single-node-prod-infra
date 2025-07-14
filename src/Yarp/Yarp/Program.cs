using Yarp.YarpConfigs;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureYarp();

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