using System.IO;
using Imp.TemplateManagers;
using Microsoft.AspNetCore.Http;

namespace Imp.Tests.IntegrationPages;

public sealed class Direct : BasePage
{
   public string Name { get; set; } = "World";

   public override Task Render(HttpResponse response) =>
      response.WriteAsync($"direct:{Name}");
}

[PageTemplate("Imp.Tests.PageTemplates.IntegrationTemplate.htm")]
public sealed class Template : BasePage
{
   public string Name { get; set; } = "World";

   public Task Greeting(TextWriter output, DynamicContentArgs _) =>
      output.WriteAsync($"template:{Name}");
}

public sealed class Injected(IntegrationDependency dependency) : BasePage
{
   public override Task Render(HttpResponse response) =>
      response.WriteAsync($"injected:{dependency.Value}");
}

public sealed class Postback : BasePage, IAsyncPostable
{
   private string value = "not-posted";

   public async Task PostbackAsync(HttpResponse response)
   {
      var form = await Request.ReadFormAsync();
      value = form["value"].ToString();
   }

   public override Task Render(HttpResponse response) => response.WriteAsync($"postback:{value}");
}

[SecurePage]
public sealed class Secure : BasePage
{
   public override Task Render(HttpResponse response) => response.WriteAsync("secure:rendered");
}

public sealed class Fallback : BasePage
{
   public override Task Render(HttpResponse response) => response.WriteAsync("fallback:rendered");
}

public sealed class NotFound : BasePage
{
   public override Task Render(HttpResponse response)
   {
      response.StatusCode = StatusCodes.Status404NotFound;
      return response.WriteAsync("not-found:rendered");
   }
}

public sealed record IntegrationDependency(string Value);
