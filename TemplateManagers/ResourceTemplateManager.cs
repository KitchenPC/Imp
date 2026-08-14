using System;
using System.IO;
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
         _middleware = middleware;
      }

      public String GetPageTemplate(Type pagetype)
      {
         var att = pagetype.GetCustomAttributes(typeof(PageTemplateAttribute), false);
         if (att.Length <= 0 || !(att[0] is PageTemplateAttribute))
            return null;
         var resource = ((PageTemplateAttribute)att[0]).Resource;

         return GetResource(resource);
      }

      public XmlDocument GetTemplate(string resname)
      {
         var resource = $"{_middleware.RootTemplateNamespace}.{resname}.htm";
         var template = GetResource(resource);
         if (String.IsNullOrEmpty(template))
            return null;
         var doc = new XmlDocument();
         doc.LoadXml(template);

         return doc;
      }

      private string GetResource(string resource)
      {
         using (var stream = Assembly.GetManifestResourceStream(resource))
         {
            if (stream == null)
               return null;

            using (
               var reader = new StreamReader(
                  stream,
                  Encoding.UTF8,
                  detectEncodingFromByteOrderMarks: true
               )
            )
            {
               return reader.ReadToEnd();
            }
         }
      }
   }
}
