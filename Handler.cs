using Imp.Compiler;
using Imp.Config;
using System;
using System.Collections.Generic;
using System.Web;

namespace Imp
{
   public class Handler : IHttpHandler
   {
      static Dictionary<Type, CompiledPage> _pageCache;
      static ITemplateManager _templateManager;
      public delegate bool AuthenticateLogonCallback(HttpContext context, BasePage page);
      public delegate BasePage NotFoundCallback(HttpRequest request);
      public delegate void PageCycleEvent(HttpRequest request, HttpResponse response);
      public delegate string CdnResolutionEvent(string url);
      
      public static AuthenticateLogonCallback Authenticate { get; set; }
      public static NotFoundCallback OnNotFound { get; set; }
      public static PageCycleEvent OnPreRender { get; set; }
      public static CdnResolutionEvent OnBuildCdnPath { get; set; }

      static string _pageassembly;
      public static string PageAssemblyName
      {
         get
         {
            if (_pageassembly == null) //Try to lookup in config
            {
               SectionHandler config = (SectionHandler)System.Configuration.ConfigurationManager.GetSection(Config.SectionHandler.ConfigSectionName);
               if (String.IsNullOrEmpty(config.PageAssemblyName))
               {
                  throw new ApplicationException("Handler was not given assembly to use for application pages.  Please configure this in web.config or in global.asax.");
               }
               else
               {
                  _pageassembly = config.PageAssemblyName;
               }
            }

            return _pageassembly;
         }

         set
         {
            _pageassembly = value;
         }
      }

      static string _roottemplatenamespace;
      public static string RootTemplateNamespace
      {
         get
         {
            if (_roottemplatenamespace == null) //Try to lookup in config
            {
               SectionHandler config = (SectionHandler)System.Configuration.ConfigurationManager.GetSection(Config.SectionHandler.ConfigSectionName);
               if (String.IsNullOrEmpty(config.RootTemplateNamespace))
               {
                  throw new ApplicationException("Handler was not given root namespace to find page template resources in assembly.  Please configure this in web.config or in global.asax.");
               }
               else
               {
                  _roottemplatenamespace = config.RootTemplateNamespace;
               }
            }

            return _roottemplatenamespace;
         }

         set
         {
            _roottemplatenamespace = value;
         }
      }

      public static log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Handler));

      public ITemplateManager TemplateManager
      {
         get { return _templateManager; }
      }

      public Handler()
      {
         if (_pageCache == null)
         {
            _pageCache = new Dictionary<Type, CompiledPage>();
         }

         if (_templateManager == null)
         {
            _templateManager = new TemplateManagers.ResourceTemplateManager(); //TODO: Should be able to configure this in Configuration file
            _templateManager.Assembly = System.Reflection.Assembly.Load(Handler.PageAssemblyName);
         }
      }

      public bool IsReusable
      {
         get { return true; }
      }

      public void ProcessRequest(HttpContext context)
      {
         /* Ideas
          *   - New attributes to control caching, Cache.Always, Cache.Never, Cache.Default
          *   - Do we need any other control over template rendering, can you abort at runtime, maybe raise an event and let the class cancel/override?
          */

         Log.InfoFormat("Processing Request: {0}", context.Request.Url.AbsoluteUri);
         
         BasePage page = Request.CreatePageObject(context.Request);
         Log.InfoFormat("Creating Page Type: {0}", page.GetType().FullName);
         page.SetHandler(this);

         //If secure page, authenticate first
         object[] att = page.GetType().GetCustomAttributes(typeof(Imp.TemplateManagers.SecurePageAttribute), false);
         if (att.Length > 0 && att[0] is Imp.TemplateManagers.SecurePageAttribute)
         {
            if (Handler.Authenticate != null)
            {
               if (Handler.Authenticate(context, page) == false)
               {
                  return;
               }
            }
         }

         if (Handler.OnPreRender != null) //Fire OnPreRender event
         {
            Handler.OnPreRender(context.Request, context.Response);
         }

         page.PreRender(context.Response);

         if (page is IPostable && context.Request.RequestType == "POST")
         {
            ((IPostable)page).Postback(context.Response);
         }

         if (_pageCache.ContainsKey(page.GetType()))
         {
            CompiledPage compiledPage = _pageCache[page.GetType()];
            compiledPage.Render(page, context.Response.Output);
         }
         else //Compile page
         {
            string template = _templateManager.GetPageTemplate(page.GetType());
            if (template != null)
            {
               CompiledPage compiledPage = PageCompiler.Compile(template, page.GetType(), _templateManager);

               lock (page.GetType()) //It's possible another request compiled this page at the same time, so we need to check again
               {
                  //This page might have been compiled by another request, but locking would hurt perf so just check for it
                  if (!_pageCache.ContainsKey(page.GetType()))
                     _pageCache.Add(page.GetType(), compiledPage);
               }

               compiledPage.Render(page, context.Response.Output);
            }
            else //No defined template, call Render method
            {
               page.Render(context.Response);
            }
         }
      }
   }
}