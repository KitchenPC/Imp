using System.Text.Encodings.Web;

namespace Imp.Samples.Todo.Pages;

public sealed class About : BasePage
{
   public override Task Render(HttpResponse response)
   {
      var assembly = HtmlEncoder.Default.Encode(typeof(About).Assembly.GetName().Name ?? "TodoApp");
      return response.WriteAsync(
         $$"""
         <!doctype html>
         <html lang="en"><head><meta charset="utf-8" /><meta name="viewport" content="width=device-width" /><title>About Imp Todo</title><link rel="stylesheet" href="/styles/site.css?v=1" /></head>
         <body><main class="shell"><a href="/">← Tasks</a><h1>About</h1><p>This page is rendered directly by <code>{{assembly}}.Pages.About.Render</code> without an embedded template.</p></main></body></html>
         """
      );
   }
}
