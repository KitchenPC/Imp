using Imp;
using Imp.Samples.Todo.Models;
using Imp.Samples.Todo.Pages;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAntiforgery();
builder.Services.AddSingleton<TodoStore>();

var app = builder.Build();

app.UseStaticFiles();
app.UseImp(config =>
   config
      .PageAssembly(typeof(Program).Assembly)
      .RootPageNamespace("Imp.Samples.Todo.Pages")
      .RootTemplateNamespace("Imp.Samples.Todo.Templates")
      .NotFoundPageType<NotFound>()
      .OnNotFound(request => Todo.TryMatch(request.Path, out _) ? typeof(Todo) : null)
      .CdnPrefix(string.Empty)
      .OnBuildCdnPath(path => path + "?v=1")
      .OnPreRender((_, response) => response.Headers["X-Rendered-By"] = "Imp")
);

app.Run();

public partial class Program { }
