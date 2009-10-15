using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;

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

      public SectionHandler()
      {
      }

      public SectionHandler(String attribVal)
      {
         NotFoundPageType = attribVal;
      }
   }
}