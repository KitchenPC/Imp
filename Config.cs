using System;
using System.Configuration;

namespace Imp.Config
{
   public class SectionHandler : ConfigurationSection
   {
      internal const string ConfigSectionName = "Imp";

      [ConfigurationProperty("PageAssemblyName", IsRequired = false)]
      public string PageAssemblyName
      {
         get { return (String)this["PageAssemblyName"]; }
         set { this["PageAssemblyName"] = value; }
      }

      [ConfigurationProperty("NotFoundPageType", IsRequired = false)]
      public String NotFoundPageType
      {
         get { return (String)this["NotFoundPageType"]; }
         set { this["NotFoundPageType"] = value; }
      }

      [ConfigurationProperty("RootPageNamespace", IsRequired = true)]
      public String RootPageNamespace
      {
         get { return (String)this["RootPageNamespace"]; }
         set { this["RootPageNamespace"] = value; }
      }

      [ConfigurationProperty("RootTemplateNamespace", IsRequired = false)]
      public String RootTemplateNamespace
      {
         get { return (String)this["RootTemplateNamespace"]; }
         set { this["RootTemplateNamespace"] = value; }
      }

      [ConfigurationProperty("CDNPrefix", IsRequired = false)]
      public String CDNPrefix
      {
         get { return (String)this["CDNPrefix"]; }
         set { this["CDNPrefix"] = value; }
      }

      public SectionHandler()
      {
      }

      public SectionHandler(String attribVal)
      {
         NotFoundPageType = attribVal;
      }
   }
}