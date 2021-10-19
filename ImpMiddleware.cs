/**********************
 * TODO:
 * -- Fix weird async issue that prevents pipeline from continuing
 * -- Real logging - maybe just have a Trace event with a "Severity" so user can log themselves (log system agnostic)
 * -- Cleanup hacks in CreatePageObject (probably none are needed) and make parsing cleaner
 * -- Investigate build error with NuGet package not compatible with .NET Standard
 * -- General code clean-up
 
 * -- Unit tests
 * -- NuGet packaging and build process stuff
 * -- Test with other frameworks, such as .NET Full, IIS Express, etc
 *
 */

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Imp.Config;
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
        public delegate BasePage NotFoundCallback(HttpRequest request);
        public delegate void PageCycleEvent(HttpRequest request, HttpResponse response);

        private static readonly ConcurrentDictionary<Type, CompiledPage> PageCache = new ConcurrentDictionary<Type, CompiledPage>();
        private readonly ITemplateManager _templateManager;
        private readonly RequestDelegate _next;
        private readonly ImpConfiguration _config;

        public static NotFoundCallback OnNotFound { get; set; }
        public static CdnResolutionEvent OnBuildCdnPath { get; set; }
        public static PageCycleEvent OnPreRender { get; set; }
        public static AuthenticateLogonCallback Authenticate { get; set; }

        public string PageAssemblyName => _config.pageAssembly?.FullName; //TODO: Should just return Assembly type
        public Type NotFoundType => _config.notFoundPageType;
        public string RootPageNamespace => _config.rootPageNamespace;
        public string RootTemplateNamespace => _config.rootTemplateNamespace;
        public string CdnPrefix => _config.cdnPrefix;

        public ImpMiddleware(RequestDelegate next, ImpConfiguration config)
        {
            _next = next;
            _config = config;

            //TODO: Should be able to configure this in Configuration file
            _templateManager = new ResourceTemplateManager(this)
            {
                Assembly = config.pageAssembly
            }; 
        }
        
        public async Task InvokeAsync(HttpContext httpContext)
        {
            var isAuth = httpContext.User.Identity.IsAuthenticated;
            log.Info($"Request for {httpContext.Request.Path} received ({httpContext.Request.ContentLength ?? 0} bytes)");

            httpContext.Response.ContentType = "text/html";
            httpContext.Response.StatusCode = 200;

            Request request = new Request(this);
            var page = request.CreatePageObject(httpContext.Request);
            log.InfoFormat("Creating Page Type: {0}", page.GetType().FullName);
            page.SetHandler(this);

            //If secure page, authenticate first
            var att = page.GetType().GetCustomAttributes(typeof(SecurePageAttribute), false);
            if (att.Length > 0 && att[0] is SecurePageAttribute)
                if (Authenticate != null)
                    if (Authenticate(httpContext, page) == false)
                        return;

            OnPreRender?.Invoke(httpContext.Request, httpContext.Response);

            page.PreRender(httpContext.Response);

            if (page is IPostable postable && httpContext.Request.Method == "POST") postable.Postback(httpContext.Response);

            if (PageCache.TryGetValue(page.GetType(), out CompiledPage p))
            {
                await p.Render(page, httpContext.Response.Body);
            }
            else //Compile page
            {
                string template = _templateManager.GetPageTemplate(page.GetType());
                if (template != null)
                {
                    var compiledPage = PageCompiler.Compile(this, template, page.GetType(), _templateManager);
                    PageCache.TryAdd(page.GetType(), compiledPage);

                    await compiledPage.Render(page, httpContext.Response.Body);
                }
                else //No defined template, call Render method
                {
                    await page.Render(httpContext.Response);
                }
            }

            // BUGBUG: This seems to mess up the stream somehow
            //await _next.Invoke(httpContext);
        }
    }
}