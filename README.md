# Imp

[![Build](https://github.com/KitchenPC/Imp/actions/workflows/build.yml/badge.svg)](https://github.com/KitchenPC/Imp/actions/workflows/build.yml)

Imp (In Memory Pages) is a lightweight page framework built on ASP.NET Core middleware. It maps request paths to .NET classes, creates those classes through ASP.NET Core dependency injection, binds query-string values to page properties, and renders the response. Pages can render HTML directly or use HTML templates embedded in the application's assembly. Embedded templates are compiled once and cached in memory, so no page-template files need to be read from disk while the application is running.

## Building and testing

Restore, build, and run the fast unit-test suite from the repository root:

```bash
dotnet test
```

The tests use in-memory request objects and do not start a web server or make network calls.

Create a local NuGet package and symbol package with:

```bash
dotnet pack Imp.csproj --configuration Release --output artifacts
```

## Releasing

Every push and pull request builds, tests, and packs `KitchenPC.Imp` for validation. NuGet publication is intentionally separate and only runs for version tags. To publish a configured release, create and push a tag matching the package version:

```bash
git tag v0.1.0
git push origin v0.1.0
```

Package versions on NuGet are immutable. Never reuse a release tag or version; increment the version for each subsequent release.

## Getting started

Reference the Imp project from an ASP.NET Core application:

```xml
<ItemGroup>
  <ProjectReference Include="..\Imp\Imp.csproj" />
</ItemGroup>
```

Register Imp near the end of the ASP.NET Core pipeline in `Startup.cs`:

```csharp
using Imp;

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
   app.UseStaticFiles();
   app.UseRouting();

   app.UseImp(config =>
      config.PageAssembly(typeof(Startup).Assembly)
         .RootPageNamespace("HelloWorld.Pages")
         .RootTemplateNamespace("HelloWorld.Templates")
         .NotFoundPageType<Pages.NotFound>()
   );
}
```

With the root page namespace set to `HelloWorld.Pages`, Imp uses the URL path to find a page class:

| Request path | Page type |
| --- | --- |
| `/` | `HelloWorld.Pages.Default` |
| `/hello` | `HelloWorld.Pages.Hello` |
| `/account/settings` | `HelloWorld.Pages.Account.Settings` |

Page type lookup is case-insensitive. The root path is represented by a class named `Default`.

## Hello World

Every page derives from `BasePage`. The smallest page can override `Render` and write directly to the ASP.NET Core response:

```csharp
using System.Threading.Tasks;
using Imp;
using Microsoft.AspNetCore.Http;

namespace HelloWorld.Pages
{
   public sealed class Hello : BasePage
   {
      public override Task Render(HttpResponse response)
      {
         return response.WriteAsync("<h1>Hello, World!</h1>");
      }
   }
}
```

This page is available at `/hello`.

### Query-string parameters

Imp binds query-string values to matching public writable properties before rendering the page. Property names are case-sensitive during binding.

```csharp
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Imp;
using Microsoft.AspNetCore.Http;

namespace HelloWorld.Pages
{
   public sealed class Hello : BasePage
   {
      public string Name { get; set; } = "World";

      public override Task Render(HttpResponse response)
      {
         string name = HtmlEncoder.Default.Encode(Name);
         return response.WriteAsync($"<h1>Hello, {name}!</h1>");
      }
   }
}
```

`/hello?Name=Mike` renders `Hello, Mike!`. Imp supports strings, GUIDs, dates, Boolean and numeric values, and enums. Nullable forms are supported for GUIDs, dates, Boolean values, and numeric values. Values should still be HTML-encoded before being placed in a response.

## Embedded page templates

A page can keep its HTML in an embedded resource instead of implementing `Render`. First, include the template files as embedded resources in the website project:

```xml
<ItemGroup>
  <EmbeddedResource Include="PageTemplates\*.htm" />
  <EmbeddedResource Include="Templates\*.htm" />
</ItemGroup>
```

Create `PageTemplates/Hello.htm`:

```xml
<PageTemplate>
  <html>
    <head>
      <title>Imp example</title>
    </head>
    <body>
      <h1><Dynamic.Greeting /></h1>
    </body>
  </html>
</PageTemplate>
```

Associate the embedded resource with a page class and implement the dynamic element as a method:

```csharp
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Imp;
using Imp.TemplateManagers;

namespace HelloWorld.Pages
{
   [PageTemplate("HelloWorld.PageTemplates.Hello.htm")]
   public sealed class Hello : BasePage
   {
      public string Name { get; set; } = "World";

      public Task Greeting(TextWriter output, DynamicContentArgs args)
      {
         string name = HtmlEncoder.Default.Encode(Name);
         return output.WriteAsync($"Hello, {name}!");
      }
   }
}
```

`<Dynamic.Greeting />` calls the page's `Greeting` method while the compiled template is being rendered. Dynamic methods accept a `TextWriter` and `DynamicContentArgs` and return a `Task`. Attributes on a dynamic element are available through `args`, for example `<Dynamic.Link Text="Home" />` can read `args["Text"]`.

Templates are XML-based and must be well-formed. The outer element for a page is `<PageTemplate>`.

### Reusable templates

Reusable layout templates are embedded beneath the namespace configured by `RootTemplateNamespace`. A layout named `Simple.htm` starts with `<Template>` and can expose placeholders:

```xml
<Template>
  <html>
    <head><title><Placeholder.Title /></title></head>
    <body><Placeholder.Body /></body>
  </html>
</Template>
```

A page template supplies the placeholder content:

```xml
<PageTemplate>
  <Template.Simple>
    <Content.Title>Hello</Content.Title>
    <Content.Body>
      <p><Dynamic.Greeting /></p>
    </Content.Body>
  </Template.Simple>
</PageTemplate>
```

## Page lifecycle and dependency injection

Imp creates page objects with `ActivatorUtilities`, so constructor dependencies registered with ASP.NET Core can be injected normally. For each HTML request, Imp:

1. Resolves the page type from the request path.
2. Invokes `OnNotFound` if no page type exists, then falls back to `NotFoundPageType` if necessary.
3. Assigns the current `HttpRequest` to `BasePage.Request` and binds query-string properties.
4. Runs `Authenticate` for pages marked with `[SecurePage]`.
5. Runs the global `OnPreRender` callback and the page's `PreRender` method.
6. Calls `IPostable.Postback` for a `POST` request when the page implements `IPostable`.
7. Renders the compiled template, or calls `Render` when the page has no template.

## Configuration reference

All options use the fluent `ImpConfiguration` interface:

| Option | Purpose |
| --- | --- |
| `PageAssembly(assembly)` | Selects the assembly containing page classes and embedded template resources. |
| `RootPageNamespace(value)` | Prefixes the namespace inferred from the request path. |
| `RootTemplateNamespace(value)` | Sets the embedded-resource namespace used to locate reusable `<Template>` resources. |
| `NotFoundPageType<T>()` | Selects the page rendered when neither normal routing nor `OnNotFound` resolves a request. |
| `OnNotFound(callback)` | Runs custom fallback routing. The callback receives the `HttpRequest` and returns a page `Type`, or `null` to use the configured not-found page. Imp creates the returned type through dependency injection. This is useful for database-backed permalinks. |
| `CdnPrefix(value)` | Prefixes attribute values written as `cdn.href`, `cdn.src`, and similar `cdn.*` attributes in templates. Imp removes `cdn.` from the rendered attribute name. |
| `OnBuildCdnPath(callback)` | Allows the final CDN URL to be rewritten, for example to append a deployment version for cache invalidation. |
| `OnPreRender(callback)` | Runs application code immediately before the page's own `PreRender` method. It receives the request and response. |
| `Authenticate(callback)` | Authenticates pages marked with `[SecurePage]`. Returning `false` stops page rendering. |
A more complete configuration might look like this:

```csharp
private static readonly string CdnTag =
   $"?v={typeof(Startup).Assembly.GetName().Version}";

app.UseImp(config =>
   config.PageAssembly(typeof(Startup).Assembly)
      .RootPageNamespace("MySite.Pages")
      .RootTemplateNamespace("MySite.Templates")
      .NotFoundPageType<Pages.NotFound>()
      .CdnPrefix("https://cdn.example.com")
      .OnNotFound(request =>
         request.Path.StartsWithSegments("/articles")
            ? typeof(Pages.ArticlePermalink)
            : null
      )
      .OnBuildCdnPath(url => url + CdnTag)
      .OnPreRender((request, response) =>
         response.Headers["X-Rendered-By"] = "Imp"
      )
      .Authenticate((context, page) =>
      {
         if (context.User.Identity?.IsAuthenticated == true)
            return true;

         context.Response.Redirect("/login");
         return false;
      })
);
```

## Not-found pages and custom routes

A basic not-found page can render directly:

```csharp
public sealed class NotFound : BasePage
{
   public override Task Render(HttpResponse response)
   {
      response.StatusCode = StatusCodes.Status404NotFound;
      return response.WriteAsync("<h1>Page not found</h1>");
   }
}
```

`OnNotFound` is intended for URLs that cannot be represented by a fixed page class. A recipe site, for example, can recognize `/recipes/chocolate-cake/` and return its permalink page type. Imp creates that type through ASP.NET Core dependency injection. If the callback returns `null`, Imp renders `NotFoundPageType`; if that type cannot be created, Imp uses its built-in not-found page.

## CDN-aware template attributes

Prefix an HTML attribute with `cdn.` when its value should pass through Imp's CDN handling:

```xml
<link rel="stylesheet" cdn.href="/styles/site.css" />
<script cdn.src="/scripts/site.js"></script>
```

With `CdnPrefix("https://cdn.example.com")`, these render as normal `href` and `src` attributes pointing to the CDN. `OnBuildCdnPath` can then apply any final transformation, such as adding an application version to selected files. Without a CDN prefix, the paths remain local.
