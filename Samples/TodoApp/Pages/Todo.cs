using System.IO;
using System.Text.Encodings.Web;
using Imp.Samples.Todo.Models;
using Imp.TemplateManagers;
using Microsoft.AspNetCore.Http;

namespace Imp.Samples.Todo.Pages;

[PageTemplate("Imp.Samples.Todo.PageTemplates.Todo.htm")]
public sealed class Todo(TodoStore store) : BasePage
{
   private TodoItem? item;

   public static bool TryMatch(PathString path, out Guid id)
   {
      var segments = path.Value?.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
      id = Guid.Empty;
      return segments is [var route, var value]
         && string.Equals(route, "todo", StringComparison.OrdinalIgnoreCase)
         && Guid.TryParse(value, out id);
   }

   public override void PreRender(HttpResponse response)
   {
      if (TryMatch(Request.Path, out var id))
         item = store.Get(id);
      if (item is null)
         response.StatusCode = StatusCodes.Status404NotFound;
   }

   public Task Title(TextWriter output, DynamicContentArgs args) =>
      output.WriteAsync(Html(item?.Title ?? "Task not found"));

   public Task Details(TextWriter output, DynamicContentArgs args)
   {
      if (item is null)
         return output.WriteAsync("<p>This task no longer exists.</p>");

      var status = item.IsComplete ? "Completed" : "Active";
      return output.WriteAsync(
         $"<dl><dt>Status</dt><dd>{status}</dd><dt>Created</dt><dd>{item.CreatedAt.LocalDateTime:g}</dd></dl>"
      );
   }

   private static string Html(string value) => HtmlEncoder.Default.Encode(value);
}
