namespace Yarp.YarpConfigs;

public static class YarpInstaller
{
    public static IHostApplicationBuilder SetYarpConfigProviders(this IHostApplicationBuilder builder)
    {
        builder.Configuration
            .AddJsonFile("YarpConfigs/config-files/core-proxy.json", optional: false, reloadOnChange: true);

        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("core-proxy"));

        return builder;
    }
}