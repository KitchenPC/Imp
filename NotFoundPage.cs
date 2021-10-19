using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Imp
{
    /// <summary>
    ///     Built-In Error 404 page.  This will be used if no other class is configured.
    /// </summary>
    public class NotFoundPage : BasePage
    {
        public override async Task Render(HttpResponse response)
        {
            response.StatusCode = 404;
            await response.WriteAsync("<h1>404 - Page Not Found</h1>");
        }
    }
}