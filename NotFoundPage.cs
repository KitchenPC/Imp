using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;

/// <summary>
/// Built-In Error 404 page.  This will be used if no class is defined in web.config.
/// </summary>

namespace Imp
{
   public class NotFoundPage : BasePage
   {
      public NotFoundPage()
      {
      }

      public override void Render(HttpResponse response)
      {
         response.StatusCode = 404;
         response.Write("<h1>404 - Page Not Found</h1>");
      }
   }
}