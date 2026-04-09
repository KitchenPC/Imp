using System;

namespace Imp.TemplateManagers
{
   /// <summary>
   ///     Page Classes marked with this attribute will call the function specified in Handler.Authenticate before
   ///     rendering the page.
   /// </summary>
   public class SecurePageAttribute : Attribute { }
}
