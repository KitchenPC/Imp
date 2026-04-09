using System;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Imp.TemplateManagers
{
   internal class ResourceTemplateManager : ITemplateManager
   {
      private readonly ImpMiddleware _middleware;

      public Assembly Assembly { get; set; }

      internal ResourceTemplateManager(ImpMiddleware middleware)
      {
         this._middleware = middleware;
      }

      public String GetPageTemplate(Type pagetype)
      {
         var att = pagetype.GetCustomAttributes(typeof(PageTemplateAttribute), false);
         if (att.Length > 0 && att[0] is PageTemplateAttribute)
         {
            string resource = ((PageTemplateAttribute)att[0]).Resource;
            return GetResource(resource);
         }

         return null;
      }

      public XmlDocument GetTemplate(string resname)
      {
         string resource = $"{_middleware.RootTemplateNamespace}.{resname}.htm";
         string template = GetResource(resource);
         if (String.IsNullOrEmpty(template) == false)
         {
            var doc = new XmlDocument();
            doc.LoadXml(template);
            return doc;
         }

         return null;
      }

      private string GetResource(string resource)
      {
         var stream = Assembly.GetManifestResourceStream(resource);
         if (stream != null)
         {
            var bytes = new byte[stream.Length];
            if (stream.Read(bytes, 0, (int)stream.Length) > 0)
            {
               var template = new String(new UTF8Encoding().GetChars(bytes, 3, bytes.Length - 3)); //HACK: Manually strip out BOM cuz it screws up XmlDocument::Load() later on
               return template;
            }
         }

         return null;
      }
   }
}
