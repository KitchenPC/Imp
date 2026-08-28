using Imp.Samples.Todo.Models;

namespace Imp.Samples.Todo.Tests;

[TestClass]
public sealed class TodoStoreTests
{
   [TestMethod]
   public void AddTrimsAndStoresTask()
   {
      var store = new TodoStore();

      var item = store.Add("  Review Imp  ");

      Assert.AreEqual("Review Imp", item.Title);
      Assert.AreEqual(item, store.Get(item.Id));
   }

   [TestMethod]
   public void ToggleAndClearCompletedUpdateStore()
   {
      var store = new TodoStore();
      var item = store.Add("Ship sample");

      Assert.IsTrue(store.Toggle(item.Id));
      Assert.IsTrue(store.Get(item.Id)?.IsComplete);
      Assert.AreEqual(1, store.ClearCompleted());
      Assert.IsNull(store.Get(item.Id));
   }

   [TestMethod]
   public void AddRejectsBlankAndOversizedTitles()
   {
      var store = new TodoStore();

      Assert.ThrowsExactly<ArgumentException>(() => store.Add("  "));
      Assert.ThrowsExactly<ArgumentException>(() => store.Add(new string('x', 121)));
   }
}
