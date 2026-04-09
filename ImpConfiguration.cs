/**************
.UseImp<MyPages>()

or

.UseImp<MyPages>(ImpConfiguration
.NotFoundType<NotFoundPage>()
.RootPageNamespace("ConsoleTest.Pages")
.RootTemplateNamespace("ConsoleTest.Templates")
.CDNPrefix("cnd.domain.com")
);

Overloads to get strings with a delegate instead
Or a .FromConfiguration which takes an IImpConfiguration instance
NotFoundType could potentially be found by looking for attributes in assembly
- error out if more than one is found, or just use first one
Any way to avoid using a string for namespaces?
**************/

using System;
using System.Reflection;

namespace Imp
{
   public class ImpConfiguration
   {
      internal Assembly pageAssembly { get; private set; }
      internal Type notFoundPageType { get; private set; }
      internal IProxyRendering ProxyRendering { get; private set; }
      internal string rootPageNamespace { get; private set; }
      internal string rootTemplateNamespace { get; private set; }
      internal string cdnPrefix { get; private set; }

      internal ImpConfiguration() { }

      public ImpConfiguration PageAssembly(Assembly assembly)
      {
         pageAssembly = assembly;
         return this;
      }

      public ImpConfiguration NotFoundPageType<T>()
      {
         notFoundPageType = typeof(T);
         return this;
      }

      public ImpConfiguration ApiType<T>()
      {
         ProxyRendering = new ProxyRendering<T>();
         return this;
      }

      public ImpConfiguration RootPageNamespace(string value)
      {
         rootPageNamespace = value;
         return this;
      }

      public ImpConfiguration RootTemplateNamespace(string value)
      {
         rootTemplateNamespace = value;
         return this;
      }

      public ImpConfiguration CdnPrefix(string value)
      {
         cdnPrefix = value;
         return this;
      }
   }
}
