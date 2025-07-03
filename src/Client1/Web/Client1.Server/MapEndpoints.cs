using Microsoft.AspNetCore.Authentication;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;

namespace ShopNet.Portal.Extensions;

internal static class MapEndpointsExtensions
{
    internal static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder endpoints, IConfiguration configuration)
    {
        string? weatherApiUrl = configuration["ApiUrls:WeatherApi"];

        endpoints.MapLoginAndLogout();
        endpoints.MapApiForwarder("", weatherApiUrl).RequireAuthorization();
        return endpoints;
    }

    private static RouteGroupBuilder MapApiForwarder(this IEndpointRouteBuilder routes, string fromPath, string toPath)
    {
        var group = routes.MapGroup(fromPath);

        group.MapForwarder("{*path}", toPath, new ForwarderRequestConfig(), b =>
        {
            b.AddRequestTransform(async c =>
            {
                var accessToken = await c.HttpContext.GetTokenAsync("access_token");
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    c.ProxyRequest.Headers.Authorization = new("Bearer", accessToken);
                }
            });
        });

        return group;
    }
}
