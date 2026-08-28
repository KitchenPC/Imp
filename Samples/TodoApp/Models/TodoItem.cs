namespace Imp.Samples.Todo.Models;

public sealed record TodoItem(Guid Id, string Title, bool IsComplete, DateTimeOffset CreatedAt);
