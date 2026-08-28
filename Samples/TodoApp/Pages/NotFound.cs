namespace Imp.Samples.Todo.Pages;

public sealed class NotFound : BasePage
{
   public override void PreRender(HttpResponse response) =>
      response.StatusCode = StatusCodes.Status404NotFound;

   public override Task Render(HttpResponse response) =>
      response.WriteAsync(
         """
         <!doctype html><html lang="en"><head><meta charset="utf-8" /><meta name="viewport" content="width=device-width" /><title>Not found</title><link rel="stylesheet" href="/styles/site.css?v=1" /></head><body><main class="shell"><p class="eyebrow">404</p><h1>Page not found</h1><p>Imp could not map this URL to a page class or custom route.</p><a href="/">Return to the task list</a></main></body></html>
         """
      );
}
