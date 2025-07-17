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
        var categories = configuration.GetSection("BackendServices").Get<List<BackendServiceCategory>>() ?? new List<BackendServiceCategory>();

        if (webHostEnvironment.IsProduction())
        {
            foreach (var category in categories)
            {
                if (category.Services == null) continue;
                foreach (var service in category.Services)
                {
                    service.Url = $"{HttpContext.Request.PathBase}{service.IdentifierPath}";
                }
            }
        }

        var htmlBuilder = new StringBuilder();
        htmlBuilder.Append(GetHtmlHeader());

        foreach (var category in categories)
        {
            htmlBuilder.Append($"""
                <h2>📑 {category.Category} APIs</h2>
                <ul>
            """);

            if (category.Services != null)
            {
                foreach (var service in category.Services)
                {
                    htmlBuilder.Append($"""
                        <li>🔗 <a href="{service.Url}/scalar" target="_blank">{service.Name} doc</a></li>
                    """);
                }
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
                 .gateway-status {
                     display: flex;
                     align-items: center;
                     font-size: 1.2em;
                     margin-bottom: 18px;
                     color: #2d7a2d;
                     font-weight: bold;
                 }
                 .gear {
                     width: 24px;
                     height: 24px;
                     margin-right: 10px;
                     animation: spin 1.2s linear infinite;
                 }
                 @keyframes spin {
                     0% { transform: rotate(0deg); }
                     100% { transform: rotate(360deg); }
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
             <div class="gateway-status">
                 <svg class="gear" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                     <g>
                         <circle cx="12" cy="12" r="3.5" stroke="#2d7a2d" stroke-width="2"/>
                         <path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41" stroke="#2d7a2d" stroke-width="2" stroke-linecap="round"/>
                     </g>
                 </svg>
                 API Gateway is running...
             </div>
             <h1>AirParsiana API Docs</h1>
        """;
}