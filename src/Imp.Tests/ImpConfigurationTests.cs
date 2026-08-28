using Microsoft.AspNetCore.Http;

namespace Imp.Tests;

[TestClass]
public sealed class ImpConfigurationTests
{
   [TestMethod]
   public void FluentMethodsReturnSameConfigurationAndExposeValuesThroughMiddleware()
   {
      ImpMiddleware.NotFoundCallback onNotFound = _ => typeof(Pages.Default);
      ImpMiddleware.CdnResolutionEvent onBuildCdnPath = path => $"{path}?v=1";
      ImpMiddleware.PageCycleEvent onPreRender = (_, _) => { };
      ImpMiddleware.AuthenticateLogonCallback authenticate = (_, _) => true;

      var configuration = new ImpConfiguration();

      Assert.AreSame(configuration, configuration.PageAssembly(typeof(Pages.Default).Assembly));
      Assert.AreSame(configuration, configuration.NotFoundPageType<Pages.NotFound>());
      Assert.AreSame(configuration, configuration.RootPageNamespace("Imp.Tests.Pages"));
      Assert.AreSame(configuration, configuration.RootTemplateNamespace("Imp.Tests.Templates"));
      Assert.AreSame(configuration, configuration.CdnPrefix("https://cdn.example.com"));
      Assert.AreSame(configuration, configuration.OnNotFound(onNotFound));
      Assert.AreSame(configuration, configuration.OnBuildCdnPath(onBuildCdnPath));
      Assert.AreSame(configuration, configuration.OnPreRender(onPreRender));
      Assert.AreSame(configuration, configuration.Authenticate(authenticate));

      var middleware = CreateMiddleware(configuration);

      Assert.AreEqual(typeof(Pages.Default).Assembly.FullName, middleware.PageAssemblyName);
      Assert.AreEqual(typeof(Pages.NotFound), middleware.NotFoundType);
      Assert.AreEqual("Imp.Tests.Pages", middleware.RootPageNamespace);
      Assert.AreEqual("Imp.Tests.Templates", middleware.RootTemplateNamespace);
      Assert.AreEqual("https://cdn.example.com", middleware.CdnPrefix);
      Assert.AreSame(onNotFound, middleware.OnNotFound);
      Assert.AreSame(onBuildCdnPath, middleware.OnBuildCdnPath);
      Assert.AreSame(onPreRender, middleware.OnPreRender);
      Assert.AreSame(authenticate, middleware.Authenticate);
   }

   private static ImpMiddleware CreateMiddleware(ImpConfiguration configuration)
   {
      return new ImpMiddleware(_ => Task.CompletedTask, configuration);
   }
}
