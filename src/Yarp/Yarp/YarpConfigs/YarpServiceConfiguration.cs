namespace Yarp.YarpConfigs;

public static class YarpServiceConfiguration
{
    public static IHostApplicationBuilder ConfigureYarp(this IHostApplicationBuilder builder)
    {
        builder.Configuration
            .AddJsonFile("YarpConfigs/Routes/core-apis.json", optional: false, reloadOnChange: true);

        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("CoreAppsReverseProxy"));

        return builder;
    }
}