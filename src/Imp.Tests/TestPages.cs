namespace Imp.Tests.Pages;

public sealed class Default : BasePage { }

public sealed class NotFound : BasePage { }

public sealed class Injected : BasePage
{
   public Injected(PageDependency dependency)
   {
      Dependency = dependency;
   }

   public PageDependency Dependency { get; }
}

public sealed class Parameters : BasePage
{
   public string Name { get; set; }
   public Guid Id { get; set; }
   public bool Enabled { get; set; }
   public int Count { get; set; } = 7;
   public double Ratio { get; set; }
   public DisplayMode Mode { get; set; } = DisplayMode.Summary;
   public char Initial { get; set; }
   public string ReadOnly => "original";
}

public sealed class Permalink : BasePage
{
   public string Slug { get; set; }
}

public enum DisplayMode
{
   Summary,
   Detailed,
}
