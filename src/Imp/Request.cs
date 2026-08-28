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
         var path = request.Path.Value.ToLower().Trim('/').Replace('/', '.');

         var typename = string.IsNullOrWhiteSpace(path) ? "Default" : path;
         if (!string.IsNullOrWhiteSpace(middleware.RootPageNamespace))
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
         if (middleware.OnNotFound == null)
            return NotFoundPage(serviceProvider);
         var pageType = middleware.OnNotFound(request);
         ret = CreatePageInstanceFromType(pageType, serviceProvider);
         if (ret == null)
            return NotFoundPage(serviceProvider);
         ret.Request = request;
         InitializeParameters(request, ret);

         return ret;
      }

      private BasePage CreatePageInstanceFromType(string typename, IServiceProvider serviceProvider)
      {
         var fqtype = string.IsNullOrEmpty(middleware.PageAssemblyName)
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
         foreach (var param in request.Query.Keys)
         {
            if (string.IsNullOrEmpty(param))
               continue;

            var prop = pageType.GetProperty(param);
            if (prop == null)
               continue;

            string sVal = request.Query[param];
            if (!prop.CanWrite)
               continue;
            var val = ParseParameter(prop, sVal);
            if (val != null)
               prop.SetValue(page, val, null);
         }
      }

      private static object ParseParameter(PropertyInfo property, string value)
      {
         if (property.PropertyType == typeof(string))
            return value;

         if (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?))
            try
            {
               var val = new Guid(value);
               return val;
            }
            catch (FormatException) { }

         if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
         {
            if (bool.TryParse(value, out var val))
               return val;
         }

         if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
         {
            if (int.TryParse(value, out var val))
               return val;
         }

         if (property.PropertyType == typeof(byte) || property.PropertyType == typeof(byte?))
         {
            if (byte.TryParse(value, out var val))
               return val;
         }

         if (property.PropertyType == typeof(char) || property.PropertyType == typeof(char?))
         {
            if (char.TryParse(value, out var val))
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

         if (property.PropertyType == typeof(decimal) || property.PropertyType == typeof(decimal?))
         {
            if (decimal.TryParse(value, out var val))
               return val;
         }

         if (property.PropertyType == typeof(double) || property.PropertyType == typeof(double?))
         {
            if (double.TryParse(value, out var val))
               return val;
         }

         if (property.PropertyType == typeof(short) || property.PropertyType == typeof(short?))
         {
            if (short.TryParse(value, out var val))
               return val;
         }

         if (property.PropertyType == typeof(long) || property.PropertyType == typeof(long?))
         {
            if (long.TryParse(value, out var val))
               return val;
         }

         if (property.PropertyType == typeof(sbyte) || property.PropertyType == typeof(sbyte?))
         {
            if (sbyte.TryParse(value, out var val))
               return val;
         }

         if (property.PropertyType == typeof(float) || property.PropertyType == typeof(float?))
         {
            if (float.TryParse(value, out var val))
               return val;
         }

         if (property.PropertyType == typeof(ushort) || property.PropertyType == typeof(ushort?))
         {
            if (ushort.TryParse(value, out var val))
               return val;
         }

         if (property.PropertyType == typeof(uint) || property.PropertyType == typeof(uint?))
         {
            if (uint.TryParse(value, out var val))
               return val;
         }

         if (property.PropertyType == typeof(ulong) || property.PropertyType == typeof(ulong?))
         {
            if (ulong.TryParse(value, out var val))
               return val;
         }

         if (property.PropertyType.IsEnum && Enum.IsDefined(property.PropertyType, value))
            return Enum.Parse(property.PropertyType, value);

         return null;
      }
   }
}
