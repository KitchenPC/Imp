using System;
using System.Reflection;
using System.Xml;
using Microsoft.AspNetCore.Http;

namespace Imp
{
   public interface ITemplateManager
   {
      Assembly Assembly { get; set; }
      String GetPageTemplate(Type pagetype);
      XmlDocument GetTemplate(string resname);
   }

   public interface IPostable
   {
      void Postback(HttpResponse response);
   }

   internal interface IPageChunk { }
}
