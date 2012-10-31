using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

using Imp;
using System.Collections;
using System.Text.RegularExpressions;
using Imp.Config;

namespace Imp.Compiler
{
   public class PageCompileException : Exception
   {
      public PageCompileException() : base() { }
      public PageCompileException(string message) : base(message) { }
   }

   public class PageMethodNotFoundException : PageCompileException
   {
      private string _methodname;
      public string MethodName { get { return _methodname; } }

      public PageMethodNotFoundException(string methodname) : base()
      {
         _methodname = methodname;
      }
   }

   public class PageMethodBindingException : PageCompileException
   {
      private string _methodname;
      public string MethodName { get { return _methodname; } }

      public PageMethodBindingException(string methodname) : base()
      {
         _methodname = methodname;
      }
   }

   public class StaticResourceNotFoundException : PageCompileException
   {
      private string _resname;
      public string ResourceName { get { return _resname; } }

      public StaticResourceNotFoundException(string resname) : base()
      {
         _resname = resname;
      }
   }

   public class MissingStaticResourceManagerException : PageCompileException
   {
   }

   internal delegate void DynamicContentPtr(TextWriter output, DynamicContentArgs args);
   internal delegate IEnumerable EnumerableContentPtr();

   internal class StaticPageChunk : IPageChunk
   {
      public string data { get; set; }
   }

   internal class DynamicPageChunk : IPageChunk
   {
      public MethodInfo function;
      public DynamicContentArgs args;
   }

   internal class EnumerablePageChunk : IPageChunk
   {
      public MethodInfo function;
      public CompiledPage subdoc;
   }

   public class DynamicContentArgs
   {
      private XmlAttributeCollection _parameters;
      public object LoopValue { get; set; }

      internal DynamicContentArgs GetLoopArgs(object value)
      {
         DynamicContentArgs ret = new DynamicContentArgs();
         ret._parameters = this._parameters;
         ret.LoopValue = value;

         return ret;
      }

      private DynamicContentArgs()
      {
      }

      internal DynamicContentArgs(XmlNode node)
      {
         _parameters = node.Attributes;
      }

      internal DynamicContentArgs(XmlNode node, object lv) : this(node)
      {
         LoopValue = lv;
      }

      public string this[string parameter]
      {
         get
         {
            return GetParameter(parameter);
         }
      }

      public string GetParameter(string parameter)
      {
         if (_parameters[parameter] != null)
         {
            return _parameters[parameter].Value;
         }
         else
         {
            return null;
         }
      }
   }

   internal class ChunkNode
   {
      private IPageChunk _data;
      private ChunkNode _next;
      
      public IPageChunk data
      {
         get { return _data; }
         set { _data = value; }
      }

      public ChunkNode next
      {
         get { return _next; }
         set { _next = value; }
      }

      public ChunkNode()
      {
         _next = null;
      }
   }

   internal class ChunkList
   {
      private ChunkNode _head;
      private ChunkNode _tail;

      public ChunkNode Head { get { return _head; } }

      public ChunkList()
      {
         _head = null;
      }

      public void AddLast(IPageChunk data)
      {
         if (_head == null)
         {
            _head = new ChunkNode();
            _head.data = data;
            _tail = _head;
         }
         else
         {
            ChunkNode temp = new ChunkNode();
            temp.data = data;
            _tail.next = temp;
            _tail = temp;
         }
      }
   }

   class CompiledPage
   {
      ChunkList _chunks;

      public CompiledPage(ChunkList chunks)
      {
         _chunks = chunks;
      }

      public void Render(BasePage page, TextWriter output)
      {
         Render(page, output, null);
      }

      public void Render(BasePage page, TextWriter output, object loopValue)
      {
         ChunkNode cur = _chunks.Head;
         while (cur != null)
         {
            StaticPageChunk staticChunk = cur.data as StaticPageChunk;
            if (staticChunk != null)
            {
               output.Write(staticChunk.data);
            }

            DynamicPageChunk dynamicChunk = cur.data as DynamicPageChunk;
            if (dynamicChunk != null)
            {
               DynamicContentPtr ptr = Delegate.CreateDelegate(typeof(DynamicContentPtr), page, dynamicChunk.function) as DynamicContentPtr;

               if (loopValue != null)
               {
                  ptr(output, dynamicChunk.args.GetLoopArgs(loopValue));
               }
               else
               {
                  ptr(output, dynamicChunk.args);
               }
            }

            EnumerablePageChunk loopChunk = cur.data as EnumerablePageChunk;
            if (loopChunk != null)
            {
               EnumerableContentPtr ptr = Delegate.CreateDelegate(typeof(EnumerableContentPtr), page, loopChunk.function) as EnumerableContentPtr;
               IEnumerable e = ptr();
               if (e != null)
               {
                  foreach (object obj in e)
                  {
                     loopChunk.subdoc.Render(page, output, obj);
                  }
               }
            }

            cur = cur.next;
         }
      }
   }

   internal class PageCompiler
   {
      const string ET_DYNAMIC  = "Dynamic";
      const string ET_CONST = "Const";
      const string ET_TEMPLATE = "Template";
      const string ET_PAGETEMPLATE = "PageTemplate";
      const string ET_CONTENT = "Content";
      const string ET_PLACEHOLDER = "Placeholder";
      const string ET_LOOP = "Loop";
      const string ET_CDNPREFIX = "Cdn";

      string _template;
      XmlDocument _doc;
      StaticPageChunk _curChunk;
      Type _pagetype;
      Stack<XmlNode> _templateStack;
      ITemplateManager _resManager;
      string cdnPrefix;
      Regex cdnMatch;

      PageCompiler(string template, Type pagetype, ITemplateManager resmanager)
      {
         SectionHandler config = (SectionHandler)System.Configuration.ConfigurationManager.GetSection(Config.SectionHandler.ConfigSectionName);

         cdnMatch = new Regex(String.Format("^{0}.", ET_CDNPREFIX), RegexOptions.IgnoreCase);
         cdnPrefix = config.CDNPrefix;
         _template = template;
         _pagetype = pagetype;

         _templateStack = new Stack<XmlNode>();
         _doc = new XmlDocument();
         _resManager = resmanager;
         _doc.LoadXml(template);
      }

      public static CompiledPage Compile(string template, Type pagetype, ITemplateManager resmanager)
      {
         PageCompiler compiler = new PageCompiler(template, pagetype, resmanager);
         return compiler.Compile();
      }

      CompiledPage Compile()
      {
         StringBuilder builder = new StringBuilder();

         //TODO: Strict mode should be configurable through web.config setting or other means
         builder.AppendLine(@"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Strict//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd"">");
         builder.AppendLine(@"<meta http-equiv=""Content Type"" content=""text/html; charset=utf-8"" />");

         ChunkList _chunks = new ChunkList();
         _curChunk = new StaticPageChunk();

         if (String.Compare(_doc.DocumentElement.Name, ET_PAGETEMPLATE, true) != 0) //Document element must be a PageTemplate
         {
            throw new FormatException("Imp: Expected top level document node to be " + ET_PAGETEMPLATE + " but instead found " + _doc.DocumentElement.Name);
         }

         CompileDoc(_chunks, _doc.DocumentElement.FirstChild, builder);
         if (builder.Length > 0) //Dump any pending chunks
         {
            _curChunk.data = builder.ToString();
            _chunks.AddLast(_curChunk);
         }

         return new CompiledPage(_chunks);
      }

      void CompileDoc(ChunkList chunks, XmlNode node, StringBuilder output)
      {
         do
         {
            if (node is XmlText)
            {
               output.Append(((XmlText)node).Value);
            }
            else if (node.LocalName.Contains("."))
            {
               //TODO: Rework "if" logic to parse only Imp commands
               string[] arrParts = node.LocalName.Split('.');
               string entityType = arrParts[0];
               string name = arrParts[1];

               if (entityType == ET_DYNAMIC) //Add dynamic function pointer
               {
                  _curChunk.data = output.ToString(); //Write pending buffer to current page chunk
                  chunks.AddLast(_curChunk);
                  _curChunk = new StaticPageChunk();
                  output.Remove(0, output.Length);

                  //Lookup matching method on _page
                  MethodInfo method = _pagetype.GetMethod(name);
                  if (method == null)
                  {
                     throw new PageMethodNotFoundException(name);
                  }

                  //Test delegate creation now so we don't have to worry about errors at render time
                  try
                  {
                     object page = _pagetype.GetConstructor(new Type[0]).Invoke(null);
                     if (Delegate.CreateDelegate(typeof(DynamicContentPtr), page, method) == null)
                     {
                        throw new PageMethodNotFoundException(name);
                     }
                  }
                  catch (Exception)
                  {
                     throw new PageMethodBindingException(name);
                  }

                  DynamicPageChunk chunk = new DynamicPageChunk();
                  chunk.function = method;
                  chunk.args = new DynamicContentArgs(node);
                  chunks.AddLast(chunk);
               }
               else if (entityType == ET_CONST) //Process string resource
               {
                  FieldInfo field = _pagetype.GetField(name);
                  if (field != null && field.IsLiteral)
                  {
                     output.Append(field.GetRawConstantValue().ToString());
                  }
                  else
                  {
                     throw new StaticResourceNotFoundException(name);
                  }
               }
               else if (entityType == ET_TEMPLATE) //Process template
               {
                  if (_resManager == null)
                  {
                     throw new MissingStaticResourceManagerException();
                  }

                  //Keep stack of templates currently being processed so we can "fill in" the contents later on
                  XmlDocument xml = _resManager.GetTemplate(name);
                  if (xml != null)
                  {
                     if (xml.DocumentElement.Name != ET_TEMPLATE) //Document element must be a Template
                     {
                        throw new FormatException("Imp: Expected top level document node to be " + ET_TEMPLATE + " but instead found " + xml.DocumentElement.Name);
                     }

                     _templateStack.Push(node);
                     CompileDoc(chunks, xml.DocumentElement.FirstChild, output);
                     _templateStack.Pop();
                  }
                  else
                  {
                     throw new StaticResourceNotFoundException(name);
                  }
               }
               else if (entityType == ET_PLACEHOLDER) //Render out matching content from template
               {
                  XmlNode template = _templateStack.Peek();
                  if (template != null) //undefined placeholders are valid scenario, no error out
                  {
                     XmlNode content = template.SelectSingleNode(String.Format("{0}.{1}", ET_CONTENT, name));
                     if (content != null)
                     {
                        CompileDoc(chunks, content.FirstChild, output);
                     }
                  }
               }
               else if (entityType == ET_LOOP) //Get loop iterator and render children for each value in enumeration
               {
                  //Flush current text chunk
                  _curChunk.data = output.ToString(); //Write pending buffer to current page chunk
                  chunks.AddLast(_curChunk);
                  _curChunk = new StaticPageChunk();
                  output.Remove(0, output.Length);

                  //Children of this node will become a new compiled sub-doc
                  MethodInfo looper = _pagetype.GetMethod(name);
                  EnumerablePageChunk chunk = new EnumerablePageChunk();
                  chunk.function = looper;

                  ChunkList subChunks = new ChunkList();
                  StringBuilder subText = new StringBuilder();
                  CompileDoc(subChunks, node.FirstChild, subText);

                  if (subText.Length > 0) //Dump any pending chunks
                  {
                     _curChunk.data = subText.ToString();
                     subChunks.AddLast(_curChunk);
                     _curChunk = new StaticPageChunk();
                  }

                  chunk.subdoc = new CompiledPage(subChunks);
                  chunks.AddLast(chunk);
               }
            }
            else if (node is XmlCDataSection)
            {
               output.Append(Environment.NewLine);
               output.Append(node.InnerText);
               output.Append(Environment.NewLine);
            }
            else
            {
               if (node.HasChildNodes)
               {
                  string nodeName = node.LocalName;
                  FormatNode(node, output);
                  CompileDoc(chunks, node.FirstChild, output);
                  output.AppendFormat("</{0}>", nodeName);
               }
               else
               {
                  FormatNode(node, output);
               }
            }

            node = node.NextSibling;
         } while (node != null);
      }

      void FormatNode(XmlNode node, StringBuilder output)
      {
         if (node is XmlComment) //Parse out comments from page output
         {
            if (node.Value.ToLower().StartsWith("[if")) //Render condition comments
            {
               output.Append(Environment.NewLine);
               output.AppendFormat("<!--{0}-->", node.Value);
               output.Append(Environment.NewLine);
            }

            return;
         }

         output.Append(Environment.NewLine);
         output.AppendFormat("<{0}", node.LocalName);

         if (node.Attributes != null && node.Attributes.Count > 0)
         {
            foreach (XmlAttribute att in node.Attributes)
            {
               string name = att.Name;
               string value = att.Value;

               if (cdnMatch.IsMatch(name)) //Insert CDN prefix if available
               {
                  name = cdnMatch.Replace(name, String.Empty);
                  value = cdnPrefix + value; //Note: If no prefix is configured, this will just no-op
               }

               output.AppendFormat(" {0}=\"{1}\"", name, value);
            }
         }

         if (node.HasChildNodes)
         {
            output.Append(">");
         }
         else
         {
            if (node.OuterXml.EndsWith("</" + node.LocalName + ">")) //HACK: Some tags such as <Script> and <IFrame> require explicit closing tags otherwise page doesn't render
            {
               output.Append("></" + node.LocalName + ">");
            }
            else
            {
               output.Append(" />");
            }
         }
      }
   }
}
