using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Yarp.Controllers;

[AllowAnonymous]
[ApiController]
[Route("[controller]")]
public class DocsController(IConfiguration configuration, IWebHostEnvironment webHostEnvironment) : ControllerBase
{
    [HttpGet]
    public ContentResult Get()
    {
        var services = configuration.GetSection("BackendServices").Get<List<BackendService>>() ?? new List<BackendService>();
        var categories = services.GroupBy(s => s.Category);

        if (webHostEnvironment.IsProduction())
        {
            services.ForEach(s => s.Url = $"{HttpContext.Request.PathBase}/{s.IdentifierPath}/scalar");
        }

        var htmlBuilder = new StringBuilder();
        htmlBuilder.Append(GetHtmlHeader());

        foreach (var category in categories)
        {
            htmlBuilder.Append($"""
                                    <h2>{category.Key}</h2>
                                    <ul>
                                """);

            foreach (var service in category)
            {
                htmlBuilder.Append($"""
                                        <li>🔗 <a href="{service.Url}/scalar" target="_blank">{service.Name}</a></li>
                                    """);
            }

            htmlBuilder.Append("</ul>");
        }

        htmlBuilder.Append("</body></html>");

        return Content(htmlBuilder.ToString(), "text/html");
    }

    private static string GetHtmlHeader() =>
        """
         <!DOCTYPE html>
         <html lang='en'>
         <head>
             <meta charset='UTF-8'>
             <title>AirParsiana API Docs</title>
             <style>
                 body {
                     font-family: Arial, sans-serif;
                     margin: 40px;
                     line-height: 1.6;
                 }
                 h1 {
                     color: #333;
                 }
                 h2 {
                     color: #666;
                     margin-top: 30px;
                 }
                 ul {
                     list-style-type: none;
                     padding-left: 20px;
                 }
                 a {
                     color: #0066cc;
                     text-decoration: none;
                     font-size: 20px;
                     line-height: 35px;
                 }
                 a:hover {
                     text-decoration: underline;
                 }
             </style>
         </head>
         <body>
             <h1>AirParsiana API Docs</h1>
        """;
}