using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Imp
{
   internal interface IProxyRendering
   {
      Task RenderProxy(HttpResponse response);
   }

   public class ProxyRendering<T> : IProxyRendering
   {
      private const string PROXY_RESOURCE = "Imp.ScriptTemplates.Proxy.js";
      private static readonly Regex routeParser = new Regex(
         @"\{([a-zA-Z\d-]+)}",
         RegexOptions.Compiled
      );

      private string buildRoute(string methodName, string template)
      {
         if (String.IsNullOrEmpty(template))
         {
            return $"'{methodName}/'";
         }

         var encodedUri = routeParser.Replace(template, "' + encodeURI($1) + '");
         return $"'{methodName}/{encodedUri}'";
      }

      private string getProxyCall(MethodInfo methodInfo)
      {
         var methodName = methodInfo.Name;
         var isPost = methodInfo.GetCustomAttribute<HttpPostAttribute>() != null;
         var route = buildRoute(
            methodName,
            methodInfo.GetCustomAttribute<RouteAttribute>()?.Template
         );

         var inboundParameters = methodInfo
            .GetParameters()
            .Select(p => p.Name)
            .ToList()
            .Concat(new[] { "succeededCallback", "failedCallback" })
            .ToArray();

         var parameterList = String.Join(", ", inboundParameters);
         var signature = $"        {methodName}: function ({parameterList}) {{";

         if (isPost) // POST call
         {
            // Build payload
            var payload = String.Join(
               ", ",
               methodInfo.GetParameters().Select(p => $"{p.Name}: {p.Name}")
            );

            if (methodInfo.GetParameters().Length == 1) // Only one parameter, we just pass it in as-is
            {
               return $"{signature}\n            doPost({route}, {methodInfo.GetParameters().First().Name}, succeededCallback, failedCallback);\n        }}";
            }

            return $"{signature}\n            doPost({route}, {{ {payload} }}, succeededCallback, failedCallback);\n        }}";
         }

         // GET call
         return $"{signature}\n            doGet({route}, succeededCallback, failedCallback);\n        }}";
      }

      private string getFunctions() =>
         string.Join(
            ",\n\n",
            typeof(T)
               .GetMethods()
               .Where(m => m.GetCustomAttribute<HttpMethodAttribute>(true) != null)
               .OrderBy(m => m.Name)
               .Select(getProxyCall)
         );

      public async Task RenderProxy(HttpResponse response)
      {
         var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(PROXY_RESOURCE);
         if (stream != null)
         {
            var bytes = new byte[stream.Length];
            if (await stream.ReadAsync(bytes, 0, (int)stream.Length) > 0)
            {
               string script = new String(new UTF8Encoding().GetChars(bytes)).Replace(
                  "/*[PROXY_FUNCTIONS]*/",
                  getFunctions()
               );

               bytes = Encoding.UTF8.GetBytes(script);
               await response.Body.WriteAsync(bytes, 0, bytes.Length);
            }
         }
      }
   }
}
