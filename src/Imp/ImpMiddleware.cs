using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Imp.TemplateManagers;
using log4net;
using Microsoft.AspNetCore.Http;

namespace Imp
{
   public class ImpMiddleware
   {
      private static readonly ILog log = LogManager.GetLogger(typeof(ImpMiddleware));

      public delegate bool AuthenticateLogonCallback(HttpContext context, BasePage page);
      public delegate string CdnResolutionEvent(string url);
      public delegate Type NotFoundCallback(HttpRequest request);
      public delegate void PageCycleEvent(HttpRequest request, HttpResponse response);

      private static readonly ConcurrentDictionary<Type, CompiledPage> PageCache =
         new ConcurrentDictionary<Type, CompiledPage>();
      private readonly ITemplateManager _templateManager;
      private readonly RequestDelegate _next;
      private readonly ImpConfiguration _config;

      public string PageAssemblyName => _config.pageAssembly?.FullName; //TODO: Should just return Assembly type
      public Type NotFoundType => _config.notFoundPageType;
      public string RootPageNamespace => _config.rootPageNamespace;
      public string RootTemplateNamespace => _config.rootTemplateNamespace;
      public string CdnPrefix => _config.cdnPrefix;
      public NotFoundCallback OnNotFound => _config.onNotFound;
      public CdnResolutionEvent OnBuildCdnPath => _config.onBuildCdnPath;
      public PageCycleEvent OnPreRender => _config.onPreRender;
      public AuthenticateLogonCallback Authenticate => _config.authenticate;

      public ImpMiddleware(RequestDelegate next, ImpConfiguration config)
      {
         _next = next;
         _config = config;

         //TODO: Should be able to configure this in Configuration file
         _templateManager = new ResourceTemplateManager(this) { Assembly = config.pageAssembly };
      }

      private async Task RenderHtml(HttpContext httpContext)
      {
         //var isAuth = httpContext.User.Identity.IsAuthenticated;

         httpContext.Response.ContentType = "text/html";
         httpContext.Response.StatusCode = 200;

         var request = new Request(this);
         var page = request.CreatePageObject(httpContext.Request, httpContext.RequestServices);
         log.InfoFormat("Creating Page Type: {0}", page.GetType().FullName);
         page.SetHandler(this);

         //If secure page, authenticate first
         var att = page.GetType().GetCustomAttributes(typeof(SecurePageAttribute), false);
         if (
            att.Length > 0
            && att[0] is SecurePageAttribute
            && Authenticate != null
            && Authenticate(httpContext, page) == false
         )
         {
            return;
         }

         OnPreRender?.Invoke(httpContext.Request, httpContext.Response);

         page.PreRender(httpContext.Response);

         if (httpContext.Request.Method == "POST")
         {
            if (page is IAsyncPostable asyncPostable)
               await asyncPostable.PostbackAsync(httpContext.Response);
            else if (page is IPostable postable)
               postable.Postback(httpContext.Response);
         }

         if (PageCache.TryGetValue(page.GetType(), out var p))
         {
            await p.Render(page, httpContext.Response.Body);
         }
         else //Compile page
         {
            var template = _templateManager.GetPageTemplate(page.GetType());
            if (template != null)
            {
               var compiledPage = PageCompiler.Compile(
                  this,
                  template,
                  page.GetType(),
                  _templateManager
               );
               PageCache.TryAdd(page.GetType(), compiledPage);

               await compiledPage.Render(page, httpContext.Response.Body);
            }
            else //No defined template, call Render method
            {
               await page.Render(httpContext.Response);
            }
         }
      }

      public async Task InvokeAsync(HttpContext httpContext)
      {
         log.Info(
            $"Request for {httpContext.Request.Path} received ({httpContext.Request.ContentLength ?? 0} bytes)"
         );
         await RenderHtml(httpContext);

         // BUGBUG: This seems to mess up the stream somehow
         //await _next.Invoke(httpContext);
      }
   }
}
