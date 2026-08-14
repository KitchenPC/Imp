using System;
using System.Reflection;
using System.Threading.Tasks;
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

   public interface IAsyncPostable
   {
      Task PostbackAsync(HttpResponse response);
   }

   internal interface IPageChunk { }
}
