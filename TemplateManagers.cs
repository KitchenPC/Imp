using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Imp.TemplateManagers
{
   public class PageTemplateAttribute : Attribute
   {
      private string _resource;
      public string Resource { get { return _resource; } set { _resource = value; } }

      public PageTemplateAttribute(string resource)
      {
         _resource = resource;
      }
   }

   /// <summary>Page Classes marked with this attribute will call the function specified in Handler.Authenticate before rendering the page.</summary>
   public class SecurePageAttribute : Attribute
   {
   }

   class ResourceTemplateManager : ITemplateManager
   {
      public System.Reflection.Assembly Assembly { get; set; }

      private string GetResource(string resource)
      {
         Stream stream = Assembly.GetManifestResourceStream(resource);
         if (stream != null)
         {
            byte[] bytes = new byte[stream.Length];
            if (stream.Read(bytes, 0, (int)stream.Length) > 0)
            {
               string template = new String(new UTF8Encoding().GetChars(bytes, 3, bytes.Length - 3)); //HACK: Manually strip out BOM cuz it screws up XmlDocument::Load() later on
               return template;
            }
         }

         return null;
      }

      public String GetPageTemplate(Type pagetype)
      {
         object[] att = pagetype.GetCustomAttributes(typeof(PageTemplateAttribute), false);
         if (att.Length > 0 && att[0] is PageTemplateAttribute)
         {
            string resource = ((PageTemplateAttribute)att[0]).Resource;
            return GetResource(resource);
         }

         return null;
      }

      public XmlDocument GetTemplate(string resname)
      {
         string resource = String.Format("{0}.{1}.htm", Handler.RootTemplateNamespace, resname);
         string template = GetResource(resource);
         if (String.IsNullOrEmpty(template) == false)
         {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(template);
            return doc;
         }

         return null;
      }
   }
}
