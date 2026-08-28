using System.Net;
using Imp.Tests.IntegrationPages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Imp.Tests;

[TestClass]
public sealed class KestrelIntegrationTests
{
   [TestMethod]
   public async Task MiddlewareRunsEndToEndOnKestrel()
   {
      var builder = WebApplication.CreateBuilder();
      builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
      builder.Services.AddSingleton(new IntegrationDependency("service"));

      await using var app = builder.Build();
      app.UseImp(config =>
         config
            .PageAssembly(typeof(Direct).Assembly)
            .RootPageNamespace("Imp.Tests.IntegrationPages")
            .NotFoundPageType<NotFound>()
            .OnNotFound(request =>
               request.Path == "/custom-route" ? typeof(Fallback) : null
            )
            .Authenticate((context, _) =>
            {
               context.Response.StatusCode = StatusCodes.Status401Unauthorized;
               return false;
            })
      );

      await app.StartAsync();
      try
      {
         var server = app.Services.GetRequiredService<IServer>();
         var address = server.Features.Get<IServerAddressesFeature>()!.Addresses.Single();
         using var client = new HttpClient { BaseAddress = new Uri(address) };

         Assert.AreEqual("direct:Mike", await client.GetStringAsync("/direct?Name=Mike"));
         StringAssert.Contains(
            await client.GetStringAsync("/template?Name=Ada"),
            "<p id=\"template-result\">template:Ada</p>"
         );
         Assert.AreEqual("injected:service", await client.GetStringAsync("/injected"));

         using var postResponse = await client.PostAsync(
            "/postback",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["value"] = "saved" })
         );
         Assert.AreEqual(HttpStatusCode.OK, postResponse.StatusCode);
         Assert.AreEqual("postback:saved", await postResponse.Content.ReadAsStringAsync());

         using var secureResponse = await client.GetAsync("/secure");
         Assert.AreEqual(HttpStatusCode.Unauthorized, secureResponse.StatusCode);
         Assert.AreEqual(string.Empty, await secureResponse.Content.ReadAsStringAsync());

         Assert.AreEqual("fallback:rendered", await client.GetStringAsync("/custom-route"));

         using var notFoundResponse = await client.GetAsync("/missing");
         Assert.AreEqual(HttpStatusCode.NotFound, notFoundResponse.StatusCode);
         Assert.AreEqual("not-found:rendered", await notFoundResponse.Content.ReadAsStringAsync());
      }
      finally
      {
         await app.StopAsync();
      }
   }
}
