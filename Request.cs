using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;

using Imp.Config;

/// <summary>
/// Creates Page instances based on incoming URL requests.
/// </summary>

namespace Imp
{
   public static class Request
   {
      private static BasePage _notFound;
      private static BasePage NotFoundPage
      {
         get
         {
            if (_notFound != null)
               return _notFound;

            SectionHandler config = (SectionHandler)System.Configuration.ConfigurationManager.GetSection(Config.SectionHandler.ConfigSectionName);
            BasePage page = CreatePageInstanceFromType(config.NotFoundPageType);
            if (page != null)
            {
               _notFound = page;
               return page;
            }

            //Could not create configured NotFound page, use build-in one
            _notFound = new Imp.NotFoundPage();
            return _notFound;
         }
      }

      private static string _rootNamespace;
      private static string RootNamespace
      {
         get
         {
            if (_rootNamespace != null)
               return _rootNamespace;

            SectionHandler config = (SectionHandler)System.Configuration.ConfigurationManager.GetSection(Config.SectionHandler.ConfigSectionName);
            _rootNamespace = config.RootPageNamespace;
            
            return _rootNamespace;
         }
      }


      public static BasePage CreatePageObject(HttpRequest request)
      {
         //Parse URL to create namespace of object type
         string appPath = request.ApplicationPath.ToLower();
         string path = request.Path.ToLower();
         string url = path;

         if (appPath != "/") //Site running in virtual directory
         {
            url = path.Replace(appPath + "/", String.Empty);
         }
         else if(url.StartsWith("/")) //Chop off starting /
         {
            url = url.Substring(1);
         }

         int extensionIndex = url.LastIndexOf('.');
         string typename = String.Format("{0}.{1}",
            RootNamespace,
            url.Substring(0, extensionIndex).Replace('/', '.')
            );

         BasePage ret = CreatePageInstanceFromType(typename);
         if (ret != null)
         {
            ret.Request = request;
            InitializeParameters(request, ret);
            return ret;
         }

         return NotFoundPage;
      }

      private static BasePage CreatePageInstanceFromType(string typename)
      {
         string fqtype = String.Empty;
         if (String.IsNullOrEmpty(Handler.PageAssemblyName))
         {
            fqtype = typename;
         }
         else
         {
            fqtype = String.Format("{0}, {1}", typename, Handler.PageAssemblyName);
         }

         Type pageType = Type.GetType(fqtype, false, true);
         if (pageType == null)
         {
            return null;
         }

         ConstructorInfo constructor = pageType.GetConstructor(new Type[0]);
         object page = constructor.Invoke(null);
         BasePage ret = page as BasePage;

         return ret;
      }

      private static void InitializeParameters(HttpRequest request, BasePage page)
      {
         Type pageType = page.GetType();
         foreach (string param in request.QueryString.AllKeys)
         {
            if (String.IsNullOrEmpty(param))
            {
               continue;
            }

            PropertyInfo prop = pageType.GetProperty(param);
            if (prop == null)
            {
               continue;
            }

            string sVal = request.QueryString[param];
            if (prop.CanWrite)
            {
               object val = ParseParameter(prop, sVal);
               if (val != null)
               {
                  prop.SetValue(page, val, null);
               }
            }
         }
      }

      private static object ParseParameter(PropertyInfo property, string value)
      {
         if (property.PropertyType == typeof(System.String))
         {
            return value;
         }

         if (property.PropertyType == typeof(System.Guid) || property.PropertyType == typeof(System.Guid?))
         {
            try
            {
               Guid val = new Guid(value);
               return val;
            }
            catch (FormatException) { }
         }

         if (property.PropertyType == typeof(System.Boolean) || property.PropertyType == typeof(System.Boolean?))
         {
            bool val;
            if (Boolean.TryParse(value, out val))
               return val;

         }

         if (property.PropertyType == typeof(System.Int32) || property.PropertyType == typeof(System.Int32?))
         {
            int val;
            if (Int32.TryParse(value, out val))
               return val;
         }

         if (property.PropertyType == typeof(System.Byte) || property.PropertyType == typeof(System.Byte?))
         {
            byte val;
            if (Byte.TryParse(value, out val))
               return val;
         }

         if (property.PropertyType == typeof(System.Char) || property.PropertyType == typeof(System.Char?))
         {
            Char val;
            if (Char.TryParse(value, out val))
               return val;
         }

         if (property.PropertyType == typeof(System.DateTime) || property.PropertyType == typeof(System.DateTime?))
         {
            DateTime val;
            if (DateTime.TryParse(value, out val))
               return val;
         }

         if (property.PropertyType == typeof(System.Decimal) || property.PropertyType == typeof(System.Decimal?))
         {
            Decimal val;
            if (Decimal.TryParse(value, out val))
               return val;
         }

         if (property.PropertyType == typeof(System.Double) || property.PropertyType == typeof(System.Double?))
         {
            Double val;
            if (Double.TryParse(value, out val))
               return val;
         }

         if (property.PropertyType == typeof(System.Int16) || property.PropertyType == typeof(System.Int16?))
         {
            Int16 val;
            if (Int16.TryParse(value, out val))
               return val;
         }

         if (property.PropertyType == typeof(System.Int64) || property.PropertyType == typeof(System.Int64?))
         {
            Int64 val;
            if (Int64.TryParse(value, out val))
               return val;
         }

         if (property.PropertyType == typeof(System.SByte) || property.PropertyType == typeof(System.SByte?))
         {
            SByte val;
            if (SByte.TryParse(value, out val))
               return val;
         }

         if (property.PropertyType == typeof(System.Single) || property.PropertyType == typeof(System.Single?))
         {
            Single val;
            if (Single.TryParse(value, out val))
               return val;
         }

         if (property.PropertyType == typeof(System.UInt16) || property.PropertyType == typeof(System.UInt16?))
         {
            UInt16 val;
            if (UInt16.TryParse(value, out val))
               return val;
         }

         if (property.PropertyType == typeof(System.UInt32) || property.PropertyType == typeof(System.UInt32?))
         {
            UInt32 val;
            if (UInt32.TryParse(value, out val))
               return val;
         }

         if (property.PropertyType == typeof(System.UInt64) || property.PropertyType == typeof(System.UInt64?))
         {
            UInt64 val;
            if (UInt64.TryParse(value, out val))
               return val;
         }

         return null;
      }
   }
}