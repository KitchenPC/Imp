# Imp

[![Build](https://github.com/KitchenPC/Imp/actions/workflows/build.yml/badge.svg)](https://github.com/KitchenPC/Imp/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/KitchenPC.Imp.svg)](https://www.nuget.org/packages/KitchenPC.Imp)

Imp (In Memory Pages) is a lightweight page framework built on ASP.NET Core middleware. It maps request paths to .NET classes, creates pages through dependency injection, binds query-string values to page properties, and renders responses directly or through embedded XML templates.

## The idea in one example

Given this page class:

```csharp
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Imp;
using Microsoft.AspNetCore.Http;

namespace MySite.Pages;

public sealed class Hello : BasePage
{
   public string Name { get; set; } = "World";

   public override Task Render(HttpResponse response)
   {
      string name = HtmlEncoder.Default.Encode(Name);
      return response.WriteAsync($"<h1>Hello, {name}!</h1>");
   }
}
```

Imp maps the request:

```text
/hello?Name=Mike
```

to `MySite.Pages.Hello`, binds `Mike` to its `Name` property, and renders:

```html
<h1>Hello, Mike!</h1>
```

That is Imp's central model: **URL paths map to page classes, and query-string parameters map to public writable properties.** Query-property names are case-sensitive, so this example uses `Name`, not `name`.

## Get started

Install the package:

```bash
dotnet add package KitchenPC.Imp
```

Then follow the Wiki's [Getting Started guide](https://github.com/KitchenPC/Imp/wiki/Getting-Started) to configure the middleware and create your first page.

For a complete working application, see the [Todo sample](Samples/TodoApp) or read its [guided tour](https://github.com/KitchenPC/Imp/wiki/Todo-Sample).

## Documentation

The [Imp Wiki](https://github.com/KitchenPC/Imp/wiki) contains the complete developer guide:

- [Architecture and request lifecycle](https://github.com/KitchenPC/Imp/wiki/Architecture-and-Request-Lifecycle)
- [Routing and page classes](https://github.com/KitchenPC/Imp/wiki/Routing-and-Page-Classes)
- [Pages and direct rendering](https://github.com/KitchenPC/Imp/wiki/Pages-and-Direct-Rendering)
- [Embedded templates and layouts](https://github.com/KitchenPC/Imp/wiki/Embedded-Templates-and-Layouts)
- [Dynamic content, constants, and loops](https://github.com/KitchenPC/Imp/wiki/Dynamic-Content-Constants-and-Loops)
- [Forms, postbacks, and antiforgery](https://github.com/KitchenPC/Imp/wiki/Forms-Postbacks-and-Antiforgery)
- [Dependency injection and lifetimes](https://github.com/KitchenPC/Imp/wiki/Dependency-Injection-and-Lifetimes)
- [Authentication and secure pages](https://github.com/KitchenPC/Imp/wiki/Authentication-and-Secure-Pages)
- [Static assets and CDN paths](https://github.com/KitchenPC/Imp/wiki/Static-Assets-and-CDN-Paths)
- [Configuration reference](https://github.com/KitchenPC/Imp/wiki/Configuration-Reference)
- [API quick reference](https://github.com/KitchenPC/Imp/wiki/API-Quick-Reference)
- [Testing and diagnostics](https://github.com/KitchenPC/Imp/wiki/Testing-and-Diagnostics)
- [Troubleshooting](https://github.com/KitchenPC/Imp/wiki/Troubleshooting)

## Build and test

From the repository root:

```bash
dotnet test
```

To create local NuGet and symbol packages:

```bash
dotnet pack src/Imp/Imp.csproj --configuration Release --output artifacts
```

See [Build, Package, and Contribute](https://github.com/KitchenPC/Imp/wiki/Build-Package-and-Contribute) for repository structure, release details, and contribution guidance.

## License

Imp is available under the [MIT License](LICENSE).
