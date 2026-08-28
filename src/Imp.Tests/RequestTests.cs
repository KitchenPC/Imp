using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Imp.Tests;

[TestClass]
public sealed class RequestTests
{
   [TestMethod]
   public void RootPathCreatesDefaultPage()
   {
      using var services = CreateServices();
      var httpContext = CreateContext("/");

      var page = CreateRequest().CreatePageObject(httpContext.Request, services);

      Assert.IsInstanceOfType<Pages.Default>(page);
      Assert.AreSame(httpContext.Request, page.Request);
   }

   [TestMethod]
   public void NestedPathIsResolvedCaseInsensitively()
   {
      using var services = CreateServices();
      var httpContext = CreateContext("/ACCOUNT/PROFILE/");

      var page = CreateRequest().CreatePageObject(httpContext.Request, services);

      Assert.IsInstanceOfType<Pages.Account.Profile>(page);
   }

   [TestMethod]
   public void PageConstructorUsesRegisteredServices()
   {
      var dependency = new PageDependency("injected");
      using var services = CreateServices(collection => collection.AddSingleton(dependency));
      var httpContext = CreateContext("/injected");

      var page = CreateRequest().CreatePageObject(httpContext.Request, services);

      var injectedPage = Assert.IsInstanceOfType<Pages.Injected>(page);
      Assert.AreSame(dependency, injectedPage.Dependency);
   }

   [TestMethod]
   public void QueryStringValuesAreBoundToWritablePageProperties()
   {
      var id = Guid.NewGuid();
      using var services = CreateServices();
      var httpContext = CreateContext(
         "/parameters",
         $"?Name=Ada&Id={id}&Enabled=true&Count=42&Ratio=1.5&Mode=Detailed&Initial=K"
      );

      var page = CreateRequest().CreatePageObject(httpContext.Request, services);

      var parameters = Assert.IsInstanceOfType<Pages.Parameters>(page);
      Assert.AreEqual("Ada", parameters.Name);
      Assert.AreEqual(id, parameters.Id);
      Assert.IsTrue(parameters.Enabled);
      Assert.AreEqual(42, parameters.Count);
      Assert.AreEqual(1.5, parameters.Ratio);
      Assert.AreEqual(Pages.DisplayMode.Detailed, parameters.Mode);
      Assert.AreEqual('K', parameters.Initial);
   }

   [TestMethod]
   public void InvalidAndUnknownQueryStringValuesAreIgnored()
   {
      using var services = CreateServices();
      var httpContext = CreateContext(
         "/parameters",
         "?Count=not-a-number&Mode=Unknown&ReadOnly=changed&DoesNotExist=value"
      );

      var page = CreateRequest().CreatePageObject(httpContext.Request, services);

      var parameters = Assert.IsInstanceOfType<Pages.Parameters>(page);
      Assert.AreEqual(7, parameters.Count);
      Assert.AreEqual(Pages.DisplayMode.Summary, parameters.Mode);
      Assert.AreEqual("original", parameters.ReadOnly);
   }

   [TestMethod]
   public void OnNotFoundCanResolveAPageAndBindItsParameters()
   {
      using var services = CreateServices();
      var httpContext = CreateContext("/permalink/example", "?Slug=example");
      var request = CreateRequest(configuration =>
         configuration.OnNotFound(_ => typeof(Pages.Permalink))
      );

      var page = request.CreatePageObject(httpContext.Request, services);

      var permalink = Assert.IsInstanceOfType<Pages.Permalink>(page);
      Assert.AreEqual("example", permalink.Slug);
      Assert.AreSame(httpContext.Request, permalink.Request);
   }

   private static Request CreateRequest(Action<ImpConfiguration> configure = null)
   {
      var configuration = new ImpConfiguration()
         .PageAssembly(typeof(Pages.Default).Assembly)
         .RootPageNamespace("Imp.Tests.Pages")
         .NotFoundPageType<Pages.NotFound>();
      configure?.Invoke(configuration);

      var middleware = new ImpMiddleware(_ => Task.CompletedTask, configuration);
      return new Request(middleware);
   }

   private static DefaultHttpContext CreateContext(string path, string queryString = null)
   {
      var context = new DefaultHttpContext();
      context.Request.Path = path;
      if (queryString != null)
         context.Request.QueryString = new QueryString(queryString);

      return context;
   }

   private static ServiceProvider CreateServices(Action<IServiceCollection> configure = null)
   {
      var services = new ServiceCollection();
      configure?.Invoke(services);
      return services.BuildServiceProvider();
   }
}

public sealed record PageDependency(string Value);
