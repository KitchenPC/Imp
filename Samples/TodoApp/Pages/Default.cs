using System.Collections;
using System.IO;
using System.Text.Encodings.Web;
using Imp.Samples.Todo.Models;
using Imp.TemplateManagers;
using Microsoft.AspNetCore.Antiforgery;

namespace Imp.Samples.Todo.Pages;

public enum TodoFilter
{
   All,
   Active,
   Completed,
}

[PageTemplate("Imp.Samples.Todo.PageTemplates.Default.htm")]
public sealed class Default(TodoStore store, IAntiforgery antiforgery) : BasePage, IAsyncPostable
{
   private string? message;

   public TodoFilter Filter { get; set; }

   public IEnumerable Tasks() =>
      store
         .GetAll()
         .Where(item =>
            Filter == TodoFilter.All
            || (Filter == TodoFilter.Active && !item.IsComplete)
            || (Filter == TodoFilter.Completed && item.IsComplete)
         )
         .ToArray();

   public Task Antiforgery(TextWriter output, DynamicContentArgs args)
   {
      var tokens = antiforgery.GetAndStoreTokens(Request.HttpContext);
      return output.WriteAsync(
         $"<input type=\"hidden\" name=\"{Html(tokens.FormFieldName)}\" value=\"{Html(tokens.RequestToken)}\" />"
      );
   }

   public Task TaskRow(TextWriter output, DynamicContentArgs args)
   {
      var item = (TodoItem)args.LoopValue;
      var state = item.IsComplete ? "complete" : "active";
      var action = item.IsComplete ? "Reopen" : "Complete";
      var tokens = antiforgery.GetAndStoreTokens(Request.HttpContext);
      var token = $"<input type=\"hidden\" name=\"{Html(tokens.FormFieldName)}\" value=\"{Html(tokens.RequestToken)}\" />";

      return output.WriteAsync(
         $"""
         <li class="todo {state}">
           <div>
             <a class="todo-title" href="/todo/{item.Id}">{Html(item.Title)}</a>
             <span class="todo-state">{state}</span>
           </div>
           <div class="actions">
             <form method="post">{token}<input type="hidden" name="action" value="toggle" /><input type="hidden" name="id" value="{item.Id}" /><button type="submit">{action}</button></form>
             <form method="post">{token}<input type="hidden" name="action" value="delete" /><input type="hidden" name="id" value="{item.Id}" /><button class="danger" type="submit">Delete</button></form>
           </div>
         </li>
         """
      );
   }

   public Task Summary(TextWriter output, DynamicContentArgs args)
   {
      var all = store.GetAll();
      var remaining = all.Count(item => !item.IsComplete);
      return output.WriteAsync($"{remaining} remaining · {all.Count - remaining} completed");
   }

   public Task Message(TextWriter output, DynamicContentArgs args) =>
      string.IsNullOrWhiteSpace(message)
         ? Task.CompletedTask
         : output.WriteAsync($"<p class=\"message\">{Html(message)}</p>");

   public async Task PostbackAsync(HttpResponse response)
   {
      try
      {
         await antiforgery.ValidateRequestAsync(Request.HttpContext);
         var form = await Request.ReadFormAsync();
         var action = form["action"].ToString();

         if (action == "add")
         {
            store.Add(form["title"].ToString());
            message = "Task added.";
         }
         else if (action == "clear")
         {
            message = $"Removed {store.ClearCompleted()} completed task(s).";
         }
         else if (Guid.TryParse(form["id"], out var id) && action == "toggle")
         {
            message = store.Toggle(id) ? "Task updated." : "Task was not found.";
         }
         else if (Guid.TryParse(form["id"], out id) && action == "delete")
         {
            message = store.Delete(id) ? "Task deleted." : "Task was not found.";
         }
         else
         {
            message = "The requested action was not recognized.";
         }
      }
      catch (AntiforgeryValidationException)
      {
         response.StatusCode = StatusCodes.Status400BadRequest;
         message = "The form expired. Reload the page and try again.";
      }
      catch (ArgumentException exception)
      {
         response.StatusCode = StatusCodes.Status400BadRequest;
         message = exception.Message;
      }
   }

   private static string Html(string? value) => HtmlEncoder.Default.Encode(value ?? string.Empty);
}
