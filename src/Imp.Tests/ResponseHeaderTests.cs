using System.Net;
using Imp.TemplateManagers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Imp.Tests
{
    [TestClass]
    public sealed class ResponseHeaderTests
    {
        [TestMethod]
        public async Task HeadersRunBeforeLifecycleAndAuthenticationOnEveryRequest()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<RequestMarker>();
            await using var app = builder.Build();
            app.UseImp(config =>
                config
                    .PageAssembly(typeof(HeaderPages.Direct).Assembly)
                    .RootPageNamespace("Imp.Tests.HeaderPages")
                    .OnPreRender(
                        (_, response) =>
                        {
                            if (response.Headers.ContainsKey("X-Order"))
                                response.Headers["X-Order"] += ",global";
                        }
                    )
                    .Authenticate(
                        (context, _) =>
                        {
                            Assert.AreEqual(
                                "private, no-store",
                                context.Response.Headers.CacheControl.ToString()
                            );
                            context.Response.Redirect("/direct");
                            return false;
                        }
                    )
            );
            await app.StartAsync();
            try
            {
                var address = app
                    .Services.GetRequiredService<IServer>()
                    .Features.Get<IServerAddressesFeature>()
                    .Addresses.Single();
                using var client = new HttpClient(
                    new HttpClientHandler { AllowAutoRedirect = false }
                )
                {
                    BaseAddress = new Uri(address),
                };
                using var direct = await client.GetAsync("/direct");
                Assert.AreEqual("direct", await direct.Content.ReadAsStringAsync());
                Assert.AreEqual("derived", direct.Headers.GetValues("X-Inherited").Single());
                Assert.AreEqual(
                    "attribute,global,page",
                    direct.Headers.GetValues("X-Order").Single()
                );
                Assert.IsFalse(direct.Headers.Contains("X-Removed"));
                Assert.IsTrue(direct.Headers.CacheControl.NoStore);
                Assert.IsTrue(direct.Headers.CacheControl.Private);
                using var template = await client.PostAsync("/template", new StringContent(""));
                StringAssert.Contains(
                    await template.Content.ReadAsStringAsync(),
                    "template:headers"
                );
                Assert.AreEqual("posted", template.Headers.GetValues("X-Post").Single());
                Assert.AreEqual("base", template.Headers.GetValues("X-Inherited").Single());
                using var cachedTemplate = await client.GetAsync("/template");
                Assert.AreEqual("base", cachedTemplate.Headers.GetValues("X-Inherited").Single());
                Assert.IsFalse(cachedTemplate.Headers.Contains("X-Post"));
                using var secure = await client.GetAsync("/secure");
                Assert.AreEqual(HttpStatusCode.Redirect, secure.StatusCode);
                Assert.IsTrue(secure.Headers.CacheControl.NoStore);

                var responses = await Task.WhenAll(
                    Enumerable
                        .Range(0, 8)
                        .Select(async index =>
                        {
                            using var request = new HttpRequestMessage(HttpMethod.Get, "/custom");
                            request.Headers.Add("X-Input", index.ToString());
                            using var response = await client.SendAsync(request);
                            Assert.AreEqual(
                                index.ToString(),
                                response.Headers.GetValues("X-Input").Single()
                            );
                            Assert.AreEqual(
                                "named",
                                response.Headers.GetValues("X-Label").Single()
                            );
                            Assert.AreEqual("1", response.Headers.GetValues("X-Calls").Single());
                            return response.Headers.GetValues("X-Scope").Single();
                        })
                );
                Assert.AreEqual(8, responses.Distinct().Count());
            }
            finally
            {
                await app.StopAsync();
            }
        }

        [TestMethod]
        [DataRow("X-Custom")]
        [DataRow("x-custom-123")]
        [DataRow("!#$%&'*+-.^_`|~")]
        public void HeaderNamesAcceptHttpTokenCharacters(string name)
        {
            Assert.AreEqual(name, new ResponseHeaderAttribute(name, "value").Name);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("Bad Name")]
        [DataRow("Bad:Name")]
        [DataRow("Bad\tName")]
        [DataRow("X-Test\n")]
        [DataRow("X-Test\r\nInjected")]
        [DataRow("X-Café")]
        public void HeaderNamesRejectNonTokenCharacters(string name)
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new ResponseHeaderAttribute(name, "value")
            );
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("private, no-store")]
        [DataRow("one\ttwo")]
        [DataRow("café")]
        public void HeaderValuesPreserveAllowedCharacters(string value)
        {
            Assert.AreEqual(value, new ResponseHeaderAttribute("X-Test", value).Value);
        }

        [TestMethod]
        [DataRow("bad\0value")]
        [DataRow("bad\bvalue")]
        [DataRow("bad\nvalue")]
        [DataRow("bad\rvalue")]
        [DataRow("bad\u001fvalue")]
        [DataRow("bad\u007fvalue")]
        public void HeaderValuesRejectControlCharacters(string value)
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                new ResponseHeaderAttribute("X-Test", value)
            );
        }

        [TestMethod]
        public void DuplicateHeadersAreRejectedAndHeaderSyntaxIsValidated()
        {
            using var services = new ServiceCollection().BuildServiceProvider();
            var context = new DefaultHttpContext { RequestServices = services };
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                PageResponseHeaders.Apply(typeof(HeaderPages.Duplicate), context)
            );
            Assert.ThrowsExactly<ArgumentException>(() =>
                new ResponseHeaderAttribute("Bad\r\nName", "x")
            );
            Assert.ThrowsExactly<ArgumentException>(() =>
                new ResponseHeaderAttribute("X-Test", "x\r\nInjected: yes")
            );
            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new ResponseHeaderAttribute("X-Test", null)
            );
            var plain = new DefaultHttpContext { RequestServices = services };
            PageResponseHeaders.Apply(typeof(HeaderPages.Plain), plain);
            Assert.AreEqual(0, plain.Response.Headers.Count);
        }
    }

    public sealed class RequestMarker
    {
        public string Id { get; } = Guid.NewGuid().ToString();
    }

    public sealed class RequestHeaderAttribute : ResponseHeaderAttribute
    {
        private readonly IHttpContextAccessor _accessor;
        private readonly RequestMarker _marker;
        private int _calls;
        public string Label { get; set; }

        public RequestHeaderAttribute(string name)
            : base(name) { }

        [ActivatorUtilitiesConstructor]
        public RequestHeaderAttribute(
            string name,
            IHttpContextAccessor accessor,
            RequestMarker marker
        )
            : this(name)
        {
            _accessor = accessor;
            _marker = marker;
        }

        public override void Apply(HttpResponse response)
        {
            response.Headers[Name] = _accessor.HttpContext.Request.Headers[Name];
            response.Headers["X-Scope"] = _marker.Id;
            response.Headers["X-Calls"] = (++_calls).ToString();
            response.Headers["X-Label"] = Label;
        }
    }
}

namespace Imp.Tests.HeaderPages
{
    [ResponseHeader("X-Inherited", "base")]
    public abstract class HeaderBase : BasePage { }

    [ResponseHeader(ResponseHeader.CacheControl, "private, no-store")]
    [ResponseHeader("x-inherited", "derived")]
    [ResponseHeader("X-Order", "attribute")]
    [ResponseHeader("X-Removed", "remove-me")]
    public sealed class Direct : HeaderBase
    {
        public override void PreRender(HttpResponse response)
        {
            response.Headers["X-Order"] += ",page";
            response.Headers.Remove("X-Removed");
        }

        public override Task Render(HttpResponse response) => response.WriteAsync("direct");
    }

    [PageTemplate("Imp.Tests.PageTemplates.IntegrationTemplate.htm")]
    public sealed class Template : HeaderBase, IPostable
    {
        public Task Greeting(TextWriter output, DynamicContentArgs _) =>
            output.WriteAsync("template:headers");

        public void Postback(HttpResponse response) => response.Headers["X-Post"] = "posted";
    }

    [SecurePage]
    [ResponseHeader(ResponseHeader.CacheControl, "private, no-store")]
    public sealed class Secure : BasePage { }

    [RequestHeader("X-Input", Label = "named")]
    public sealed class Custom : BasePage { }

    [ResponseHeader("X-Duplicate", "first")]
    [ResponseHeader("x-duplicate", "second")]
    public sealed class Duplicate : BasePage { }

    public sealed class Plain : BasePage { }
}
