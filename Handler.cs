using System;
using System.Data;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;

using Imp.Config;
using Imp.Compiler;

/// <summary>
/// Summary description for Handler
/// </summary>

namespace Imp
{
   public class Handler : IHttpHandler
   {
      static Dictionary<Type, CompiledPage> _pageCache;
      static ITemplateManager _templateManager;
      public delegate bool AuthenticateLogonCallback(HttpContext context, BasePage page);
      public delegate BasePage NotFoundCallback(HttpRequest request);
      
      public static AuthenticateLogonCallback Authenticate { get; set; }
      public static NotFoundCallback OnNotFound { get; set; }

      static string _pageassembly = null;
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

      static string _roottemplatenamespace = null;
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

         BasePage page = Request.CreatePageObject(context.Request);
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
               _pageCache.Add(page.GetType(), compiledPage);
               compiledPage.Render(page, context.Response.Output);
            }
            else //No defiend template, call Render method
            {
               page.Render(context.Response);
            }
         }
      }
   }
}