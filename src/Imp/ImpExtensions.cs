using System;
using Microsoft.AspNetCore.Builder;

namespace Imp
{
   public static class ImpExtensions
   {
      public static IApplicationBuilder UseImp(
         this IApplicationBuilder builder,
         Action<ImpConfiguration> configurationBuilder = null
      )
      {
         var config = new ImpConfiguration();
         configurationBuilder?.Invoke(config);

         return builder.UseMiddleware<ImpMiddleware>(config);
      }
   }
}
