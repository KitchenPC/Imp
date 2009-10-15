using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Imp
{
   public abstract class BasePage
   {
      protected Handler Handler { get; private set; }
      public HttpRequest Request { get; set; }

      internal void SetHandler(Handler handler)
      {
         Handler = handler;
      }

      public virtual void PreRender(HttpResponse response)
      {
      }

      /// <summary>Method called by framework when page is instantiated and all URL parameters have been set.  This method is only called if there is no page template defined.</summary>
      public virtual void Render(HttpResponse response)
      {
      }
   }
}
