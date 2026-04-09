using System;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Imp
{
   /// <summary>
   ///     Creates Page instances based on incoming URL requests.
   /// </summary>
   public class Request
   {
      private readonly ImpMiddleware middleware;
      private static BasePage notFound;

      internal Request(ImpMiddleware middleware)
      {
         this.middleware = middleware;
      }

      private BasePage NotFoundPage(IServiceProvider serviceProvider)
      {
         if (notFound != null)
         {
            return notFound;
         }

         var page = CreatePageInstanceFromType(middleware.NotFoundType, serviceProvider);
         if (page != null)
         {
            notFound = page;
            return page;
         }

         //Could not create configured NotFound page, use build-in one
         notFound = new NotFoundPage();
         return notFound;
      }

      public BasePage CreatePageObject(HttpRequest request, IServiceProvider serviceProvider)
      {
         //Parse URL to create namespace of object type
         string path = request.Path.Value.ToLower().Trim('/').Replace('/', '.');

         string typename = String.IsNullOrWhiteSpace(path) ? "Default" : path;
         if (!String.IsNullOrWhiteSpace(middleware.RootPageNamespace))
         {
            typename = $"{middleware.RootPageNamespace}.{typename}";
         }

         var ret = CreatePageInstanceFromType(typename, serviceProvider);
         if (ret != null)
         {
            ret.Request = request;
            InitializeParameters(request, ret);
            return ret;
         }

         //Fire NotFound event
         if (ImpMiddleware.OnNotFound != null)
         {
            ret = ImpMiddleware.OnNotFound(request);
            if (ret != null)
               InitializeParameters(request, ret);

            return ret;
         }

         return NotFoundPage(serviceProvider);
      }

      private BasePage CreatePageInstanceFromType(string typename, IServiceProvider serviceProvider)
      {
         string fqtype = String.IsNullOrEmpty(middleware.PageAssemblyName)
            ? typename
            : $"{typename}, {middleware.PageAssemblyName}";
         var pageType = Type.GetType(fqtype, false, true);

         return CreatePageInstanceFromType(pageType, serviceProvider);
      }

      private static BasePage CreatePageInstanceFromType(
         Type pageType,
         IServiceProvider serviceProvider
      )
      {
         if (pageType == null)
         {
            return null;
         }

         var ret = ActivatorUtilities.CreateInstance(serviceProvider, pageType) as BasePage;

         return ret;
      }

      private static void InitializeParameters(HttpRequest request, BasePage page)
      {
         var pageType = page.GetType();
         foreach (string param in request.Query.Keys)
         {
            if (String.IsNullOrEmpty(param))
               continue;

            var prop = pageType.GetProperty(param);
            if (prop == null)
               continue;

            string sVal = request.Query[param];
            if (prop.CanWrite)
            {
               var val = ParseParameter(prop, sVal);
               if (val != null)
                  prop.SetValue(page, val, null);
            }
         }
      }

      private static object ParseParameter(PropertyInfo property, string value)
      {
         if (property.PropertyType == typeof(String))
            return value;

         if (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?))
            try
            {
               var val = new Guid(value);
               return val;
            }
            catch (FormatException) { }

         if (property.PropertyType == typeof(Boolean) || property.PropertyType == typeof(Boolean?))
         {
            if (Boolean.TryParse(value, out bool val))
               return val;
         }

         if (property.PropertyType == typeof(Int32) || property.PropertyType == typeof(Int32?))
         {
            if (Int32.TryParse(value, out int val))
               return val;
         }

         if (property.PropertyType == typeof(Byte) || property.PropertyType == typeof(Byte?))
         {
            if (Byte.TryParse(value, out byte val))
               return val;
         }

         if (property.PropertyType == typeof(Char) || property.PropertyType == typeof(Char?))
         {
            if (Char.TryParse(value, out char val))
               return val;
         }

         if (
            property.PropertyType == typeof(DateTime)
            || property.PropertyType == typeof(DateTime?)
         )
         {
            if (DateTime.TryParse(value, out var val))
               return val;
         }

         if (property.PropertyType == typeof(Decimal) || property.PropertyType == typeof(Decimal?))
         {
            if (Decimal.TryParse(value, out decimal val))
               return val;
         }

         if (property.PropertyType == typeof(Double) || property.PropertyType == typeof(Double?))
         {
            if (Double.TryParse(value, out double val))
               return val;
         }

         if (property.PropertyType == typeof(Int16) || property.PropertyType == typeof(Int16?))
         {
            if (Int16.TryParse(value, out short val))
               return val;
         }

         if (property.PropertyType == typeof(Int64) || property.PropertyType == typeof(Int64?))
         {
            if (Int64.TryParse(value, out long val))
               return val;
         }

         if (property.PropertyType == typeof(SByte) || property.PropertyType == typeof(SByte?))
         {
            if (SByte.TryParse(value, out sbyte val))
               return val;
         }

         if (property.PropertyType == typeof(Single) || property.PropertyType == typeof(Single?))
         {
            if (Single.TryParse(value, out float val))
               return val;
         }

         if (property.PropertyType == typeof(UInt16) || property.PropertyType == typeof(UInt16?))
         {
            if (UInt16.TryParse(value, out ushort val))
               return val;
         }

         if (property.PropertyType == typeof(UInt32) || property.PropertyType == typeof(UInt32?))
         {
            if (UInt32.TryParse(value, out uint val))
               return val;
         }

         if (property.PropertyType == typeof(UInt64) || property.PropertyType == typeof(UInt64?))
         {
            if (UInt64.TryParse(value, out ulong val))
               return val;
         }

         if (property.PropertyType.IsEnum && Enum.IsDefined(property.PropertyType, value))
            return Enum.Parse(property.PropertyType, value);

         return null;
      }
   }
}
