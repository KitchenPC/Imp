using System.Web;

namespace Imp
{
   /// <summary>
   /// Built-In Error 404 page.  This will be used if no class is defined in web.config.
   /// </summary>
   public class NotFoundPage : BasePage
   {
      public override void Render(HttpResponse response)
      {
         response.StatusCode = 404;
         response.Write("<h1>404 - Page Not Found</h1>");
      }
   }
}