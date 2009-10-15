using System;
using System.Xml;
using System.Web;
using System.Reflection;

namespace Imp
{
   public interface ITemplateManager
   {
      String GetPageTemplate(Type pagetype);
      XmlDocument GetTemplate(string resname);
      Assembly Assembly { get; set; }
   }

   public interface IPostable
   {
      void Postback(HttpResponse response);
   }

   internal interface IPageChunk
   {
   }
}