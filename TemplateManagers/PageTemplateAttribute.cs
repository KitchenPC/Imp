using System;

namespace Imp.TemplateManagers
{
   public class PageTemplateAttribute : Attribute
   {
      public PageTemplateAttribute(string resource)
      {
         Resource = resource;
      }

      public string Resource { get; set; }
   }
}
