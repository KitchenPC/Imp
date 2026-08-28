namespace Imp.Samples.Todo.Models;

public sealed class TodoStore
{
   private readonly object sync = new();
   private readonly List<TodoItem> items = [];

   public TodoStore()
   {
      Add("Explore the Imp sample");
      Add("Add a new task");
   }

   public IReadOnlyList<TodoItem> GetAll()
   {
      lock (sync)
         return items.OrderBy(item => item.CreatedAt).ToArray();
   }

   public TodoItem? Get(Guid id)
   {
      lock (sync)
         return items.FirstOrDefault(item => item.Id == id);
   }

   public TodoItem Add(string title)
   {
      var normalized = title.Trim();
      if (normalized.Length is < 1 or > 120)
         throw new ArgumentException("A task must contain between 1 and 120 characters.", nameof(title));

      var item = new TodoItem(Guid.NewGuid(), normalized, false, DateTimeOffset.UtcNow);
      lock (sync)
         items.Add(item);
      return item;
   }

   public bool Toggle(Guid id)
   {
      lock (sync)
      {
         var index = items.FindIndex(item => item.Id == id);
         if (index < 0)
            return false;

         items[index] = items[index] with { IsComplete = !items[index].IsComplete };
         return true;
      }
   }

   public bool Delete(Guid id)
   {
      lock (sync)
         return items.RemoveAll(item => item.Id == id) > 0;
   }

   public int ClearCompleted()
   {
      lock (sync)
         return items.RemoveAll(item => item.IsComplete);
   }
}
