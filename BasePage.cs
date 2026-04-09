using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Imp
{
   public abstract class BasePage
   {
      protected ImpMiddleware Handler { get; private set; }
      public HttpRequest Request { get; set; }

      internal void SetHandler(ImpMiddleware handler)
      {
         Handler = handler;
      }

      public virtual void PreRender(HttpResponse response) { }

      /// <summary>
      ///     Method called by framework when page is instantiated and all URL parameters have been set.  This method is
      ///     only called if there is no page template defined.
      /// </summary>
      public virtual async Task Render(HttpResponse response)
      {
         await Task.CompletedTask;
      }
   }
}
