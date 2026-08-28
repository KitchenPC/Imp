# Imp Todo sample

A small ASP.NET Core website demonstrating Imp with an in-memory task list. It supports adding, completing, reopening, deleting, filtering, and viewing tasks. Data is intentionally process-local and resets whenever the application restarts.

## Run

From the repository root:

```bash
dotnet run --project Samples/TodoApp/TodoApp.csproj
```

Open the HTTP URL printed by ASP.NET Core, normally `http://localhost:5000`.

## Imp features demonstrated

- `Program.cs` configures page/template namespaces, custom not-found routing, CDN-aware static paths, and a pre-render response header.
- `Pages/Default.cs` demonstrates constructor injection, an enum query property, `IAsyncPostable`, antiforgery validation, dynamic methods, and a loop data source.
- `PageTemplates/Default.htm` is an embedded page template.
- `Templates/Site.htm` is a reusable layout populated through placeholders.
- `/todo/{id}` demonstrates `OnNotFound` as a dynamic permalink route.
- `/about` demonstrates direct rendering without a page template.
- Unknown paths use the configured 404 page.

Try `/?Filter=Active` and inspect the `X-Rendered-By: Imp` response header. The stylesheet uses a `Cdn.href` template attribute; this sample leaves the prefix local and adds a cache-version query through `OnBuildCdnPath`.

This sample intentionally does not demonstrate `[SecurePage]`. Authentication should be configured using the host application's real ASP.NET Core authentication scheme rather than a sample-only shortcut.
