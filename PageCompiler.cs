using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Imp
{
    public class PageCompileException : Exception
    {
        public PageCompileException()
        {
        }

        public PageCompileException(string message) : base(message)
        {
        }
    }

    public class PageMethodNotFoundException : PageCompileException
    {
        public PageMethodNotFoundException(string methodname)
        {
            MethodName = methodname;
        }

        public string MethodName { get; }
    }

    public class PageMethodBindingException : PageCompileException
    {
        public PageMethodBindingException(string methodname)
        {
            MethodName = methodname;
        }

        public string MethodName { get; }
    }

    public class StaticResourceNotFoundException : PageCompileException
    {
        public StaticResourceNotFoundException(string resname)
        {
            ResourceName = resname;
        }

        public string ResourceName { get; }
    }

    public class MissingStaticResourceManagerException : PageCompileException
    {
    }

    internal delegate Task DynamicContentPtr(TextWriter output, DynamicContentArgs args);

    internal delegate IEnumerable EnumerableContentPtr(); // TODO: Support async enumerables?

    internal class StaticPageChunk : IPageChunk
    {
        public string Data { get; set; }
    }

    internal class DynamicPageChunk : IPageChunk
    {
        public DynamicContentArgs Args;
        public MethodInfo Function;
    }

    internal class EnumerablePageChunk : IPageChunk
    {
        public MethodInfo Function;
        public CompiledPage Subdoc;
    }

    public class DynamicContentArgs
    {
        private XmlAttributeCollection _parameters;

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

        public object LoopValue { get; set; }

        public string this[string parameter] => GetParameter(parameter);

        internal DynamicContentArgs GetLoopArgs(object value)
        {
            return new DynamicContentArgs {_parameters = _parameters, LoopValue = value};
        }

        public string GetParameter(string parameter)
        {
            return _parameters[parameter] != null ? _parameters[parameter].Value : null;
        }
    }

    internal class ChunkNode
    {
        public ChunkNode()
        {
            Next = null;
        }

        public IPageChunk Data { get; set; }

        public ChunkNode Next { get; set; }
    }

    internal class ChunkList
    {
        private ChunkNode _tail;

        public ChunkList()
        {
            Head = null;
        }

        public ChunkNode Head { get; private set; }

        public void AddLast(IPageChunk data)
        {
            if (Head == null)
            {
                Head = new ChunkNode();
                Head.Data = data;
                _tail = Head;
            }
            else
            {
                var temp = new ChunkNode();
                temp.Data = data;
                _tail.Next = temp;
                _tail = temp;
            }
        }
    }

    internal class CompiledPage
    {
        private readonly ChunkList _chunks;

        public CompiledPage(ChunkList chunks)
        {
            _chunks = chunks;
        }

        public async Task Render(BasePage page, Stream output)
        {
            using (var writer = new StreamWriter(output))
            {
                await Render(page, writer, null);
                await writer.FlushAsync();
            }
        }

        public async Task Render(BasePage page, TextWriter output, object loopValue)
        {
            var cur = _chunks.Head;
            while (cur != null)
            {
                if (cur.Data is StaticPageChunk staticChunk)
                {
                    await output.WriteAsync(staticChunk.Data);
                }

                if (cur.Data is DynamicPageChunk dynamicChunk)
                {
                    var ptr = Delegate.CreateDelegate(typeof(DynamicContentPtr), page, dynamicChunk.Function) as DynamicContentPtr;

                    await ptr(output, loopValue != null ? dynamicChunk.Args.GetLoopArgs(loopValue) : dynamicChunk.Args);
                }

                if (cur.Data is EnumerablePageChunk loopChunk)
                {
                    var ptr = Delegate.CreateDelegate(typeof(EnumerableContentPtr), page, loopChunk.Function) as EnumerableContentPtr;
                    var e = ptr();
                    if (e != null)
                    {
                        foreach (var obj in e)
                        {
                            await loopChunk.Subdoc.Render(page, output, obj);
                        }
                    }
                }

                cur = cur.Next;
            }
        }
    }

    internal class PageCompiler
    {
        private const string ET_DYNAMIC = "Dynamic";
        private const string ET_CONST = "Const";
        private const string ET_TEMPLATE = "Template";
        private const string ET_PAGETEMPLATE = "PageTemplate";
        private const string ET_CONTENT = "Content";
        private const string ET_PLACEHOLDER = "Placeholder";
        private const string ET_LOOP = "Loop";
        private const string ET_CDNPREFIX = "Cdn";

        private readonly XmlDocument _doc;
        private readonly Type _pagetype;
        private readonly ITemplateManager _resManager;
        private readonly Stack<XmlNode> _templateStack;
        private readonly Regex cdnMatch;
        private readonly string cdnPrefix;

        private StaticPageChunk _curChunk;

        private PageCompiler(ImpMiddleware middleware, string template, Type pagetype, ITemplateManager resmanager)
        {
            cdnMatch = new Regex($"^{ET_CDNPREFIX}.", RegexOptions.IgnoreCase);
            cdnPrefix = middleware.CdnPrefix;
            _pagetype = pagetype;

            _templateStack = new Stack<XmlNode>();
            _doc = new XmlDocument();
            _resManager = resmanager;
            _doc.LoadXml(template);
        }

        public static CompiledPage Compile(ImpMiddleware middleware, string template, Type pagetype, ITemplateManager resmanager)
        {
            var compiler = new PageCompiler(middleware, template, pagetype, resmanager);
            return compiler.Compile();
        }

        private CompiledPage Compile()
        {
            var builder = new StringBuilder();

            //TODO: Strict mode should be configurable through web.config setting or other means
            builder.AppendLine(@"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Strict//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd"">");
            builder.AppendLine(@"<meta http-equiv=""Content Type"" content=""text/html; charset=utf-8"" />");

            var chunks = new ChunkList();
            _curChunk = new StaticPageChunk();

            if (String.Compare(_doc.DocumentElement.Name, ET_PAGETEMPLATE, StringComparison.OrdinalIgnoreCase) != 0) //Document element must be a PageTemplate
                throw new FormatException("Imp: Expected top level document node to be " + ET_PAGETEMPLATE + " but instead found " + _doc.DocumentElement.Name);

            CompileDoc(chunks, _doc.DocumentElement.FirstChild, builder);
            if (builder.Length > 0) //Dump any pending chunks
            {
                _curChunk.Data = builder.ToString();
                chunks.AddLast(_curChunk);
            }

            return new CompiledPage(chunks);
        }

        private void CompileDoc(ChunkList chunks, XmlNode node, StringBuilder output)
        {
            do
            {
                if (node is XmlText)
                {
                    output.Append(node.Value);
                }
                else if (node.LocalName.Contains("."))
                {
                    //TODO: Rework "if" logic to parse only Imp commands
                    var arrParts = node.LocalName.Split('.');
                    string entityType = arrParts[0];
                    string name = arrParts[1];

                    if (entityType == ET_DYNAMIC) //Add dynamic function pointer
                    {
                        _curChunk.Data = output.ToString(); //Write pending buffer to current page chunk
                        chunks.AddLast(_curChunk);
                        _curChunk = new StaticPageChunk();
                        output.Remove(0, output.Length);

                        //Lookup matching method on _page
                        var method = _pagetype.GetMethod(name);
                        if (method == null) throw new PageMethodNotFoundException(name);

                        //Test delegate creation now so we don't have to worry about errors at render time
                        // TODO: Fix this again, we need a ServiceProvider to create a page type
                        // try
                        // {
                        //     _pagetype.GetConstructor(new Type[0]).Invoke(null);
                        // }
                        // catch (Exception)
                        // {
                        //     throw new PageMethodBindingException(name);
                        // }

                        var chunk = new DynamicPageChunk
                        {
                            Function = method,
                            Args = new DynamicContentArgs(node)
                        };

                        chunks.AddLast(chunk);
                    }
                    else if (entityType == ET_CONST) //Process string resource
                    {
                        var field = _pagetype.GetField(name);
                        if (field != null && field.IsLiteral)
                            output.Append(field.GetRawConstantValue());
                        else
                            throw new StaticResourceNotFoundException(name);
                    }
                    else if (entityType == ET_TEMPLATE) //Process template
                    {
                        if (_resManager == null) throw new MissingStaticResourceManagerException();

                        //Keep stack of templates currently being processed so we can "fill in" the contents later on
                        var xml = _resManager.GetTemplate(name);
                        if (xml != null)
                        {
                            if (xml.DocumentElement.Name != ET_TEMPLATE) //Document element must be a Template
                                throw new FormatException("Imp: Expected top level document node to be " + ET_TEMPLATE + " but instead found " + xml.DocumentElement.Name);

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
                        var template = _templateStack.Peek();
                        var content = template?.SelectSingleNode($"{ET_CONTENT}.{name}");
                        if (content != null) CompileDoc(chunks, content.FirstChild, output);
                    }
                    else if (entityType == ET_LOOP) //Get loop iterator and render children for each value in enumeration
                    {
                        //Flush current text chunk
                        _curChunk.Data = output.ToString(); //Write pending buffer to current page chunk
                        chunks.AddLast(_curChunk);
                        _curChunk = new StaticPageChunk();
                        output.Remove(0, output.Length);

                        //Children of this node will become a new compiled sub-doc
                        var looper = _pagetype.GetMethod(name);
                        var chunk = new EnumerablePageChunk();
                        chunk.Function = looper;

                        var subChunks = new ChunkList();
                        var subText = new StringBuilder();
                        CompileDoc(subChunks, node.FirstChild, subText);

                        if (subText.Length > 0) //Dump any pending chunks
                        {
                            _curChunk.Data = subText.ToString();
                            subChunks.AddLast(_curChunk);
                            _curChunk = new StaticPageChunk();
                        }

                        chunk.Subdoc = new CompiledPage(subChunks);
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

        private void FormatNode(XmlNode node, StringBuilder output)
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
                foreach (XmlAttribute att in node.Attributes)
                {
                    string name = att.Name;
                    string value = att.Value;

                    if (cdnMatch.IsMatch(name)) //Insert CDN prefix if available
                    {
                        name = cdnMatch.Replace(name, String.Empty);
                        value = cdnPrefix + value; //Note: If no prefix is configured, this will just no-op

                        if (ImpMiddleware.OnBuildCdnPath != null) value = ImpMiddleware.OnBuildCdnPath(value);
                    }

                    output.AppendFormat(" {0}=\"{1}\"", name, value);
                }

            if (node.HasChildNodes)
            {
                output.Append(">");
            }
            else
            {
                if (node.OuterXml.EndsWith("</" + node.LocalName + ">")) //HACK: Some tags such as <Script> and <IFrame> require explicit closing tags otherwise page doesn't render
                    output.Append("></" + node.LocalName + ">");
                else
                    output.Append(" />");
            }
        }
    }
}