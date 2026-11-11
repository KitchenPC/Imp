# Response headers (Imp 0.3)

Imp 0.3.0 adds class-level response header policies. Use them instead of overriding
`PreRender` solely to set headers.

## Static headers

```csharp
using Imp;

[ResponseHeader(ResponseHeader.CacheControl, "private, no-store")]
[ResponseHeader(ResponseHeader.XRobotsTag, "noindex, nofollow")]
[ResponseHeader("X-Application", "Example")]
public sealed class Account : BasePage
{
    // Render directly or use a PageTemplate as usual.
}
```

`ResponseHeader` includes the following common response-header constants:

| Category | Constants |
| --- | --- |
| Caching | `CacheControl`, `Expires`, `Pragma`, `ETag`, `LastModified`, `Vary`, `Age` |
| Content | `ContentType`, `ContentDisposition`, `ContentEncoding`, `ContentLanguage`, `ContentLocation`, `ContentRange`, `AcceptRanges` |
| Navigation and status | `Location`, `Link`, `Allow`, `RetryAfter`, `WWWAuthenticate` |
| CORS | `AccessControlAllowOrigin`, `AccessControlAllowCredentials`, `AccessControlAllowHeaders`, `AccessControlAllowMethods`, `AccessControlExposeHeaders`, `AccessControlMaxAge` |
| Security and privacy | `ContentSecurityPolicy`, `ContentSecurityPolicyReportOnly`, `StrictTransportSecurity`, `ReferrerPolicy`, `PermissionsPolicy`, `CrossOriginEmbedderPolicy`, `CrossOriginOpenerPolicy`, `CrossOriginResourcePolicy`, `XContentTypeOptions`, `XFrameOptions` |
| Search engines | `XRobotsTag` |

The list follows ASP.NET Core's
[`HeaderNames`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.net.http.headers.headernames)
where applicable, with additional browser security headers. Imp exposes compile-time
`const string` values so they can be used in attribute declarations. Any valid header name can also be supplied as a string. Attribute values
replace an existing header value rather than appending to it. Use the ASP.NET Core
cookie APIs for cookies, not these attributes. Header constants only name headers;
choose values appropriate for your application and any middleware managing them.

These attributes control HTTP response headers, not Imp's compiled-template cache
or application data caches. A static `private, no-store` policy applies to every
response for that page, including anonymous requests.

## Conditional headers and dependency injection

Subclass `ResponseHeaderAttribute` and override `Apply(HttpResponse)` for a dynamic
policy. Imp constructs a **new runtime attribute instance for each request** using
`ActivatorUtilities` and `HttpContext.RequestServices`. Scoped services are safe to
inject; attribute types do not need to be registered themselves.

C# attribute declarations only accept compile-time arguments. Provide a constructor
that the declaration can use, plus a DI constructor marked
`[ActivatorUtilitiesConstructor]`. The latter is the constructor Imp uses at runtime.
Do not resolve services or run request logic in the declaration constructor.

Register the HTTP context accessor before building the app:

```csharp
builder.Services.AddHttpContextAccessor();
// Register any other services needed by your custom policy here.
```

```csharp
using Imp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

public sealed class AuthenticatedCacheControlAttribute : ResponseHeaderAttribute
{
    private readonly IHttpContextAccessor _context;

    public AuthenticatedCacheControlAttribute()
        : base(ResponseHeader.CacheControl) { }

    [ActivatorUtilitiesConstructor]
    public AuthenticatedCacheControlAttribute(IHttpContextAccessor context)
        : this()
    {
        _context = context;
    }

    public override void Apply(HttpResponse response)
    {
        // IHttpContextAccessor exposes the current request, response, and user.
        if (_context.HttpContext?.User.Identity?.IsAuthenticated == true)
            response.Headers.CacheControl = "private, no-store";
    }
}

[AuthenticatedCacheControl]
public abstract class SitePage : BasePage { }
```

Place ASP.NET Core authentication middleware **before** `UseImp`, so the current
user is available when the header policy runs. Imp's optional authentication
callback runs after header policies; if that callback itself establishes the user,
put dependent header logic in that callback instead.

For attributes with declaration arguments, the DI constructor must also accept
those arguments, plus its service dependencies. Named attribute properties/fields
are assigned before `Apply`. Keep attributes lightweight: injected scoped services
are disposed by the request scope; attribute instances are not registered with DI
for disposal. Do not store requests or dependencies in static fields.

## Inheritance and precedence

- Attributes are inherited by default. Imp applies base-class declarations before
  derived-class declarations, even when a page does not call `base.PreRender()`.
- Header names are case-insensitive. A derived declaration can replace a base
  declaration for the same header. A custom policy can also leave the earlier value
  unchanged or explicitly remove it.
- Declaring the same header twice on one class is an error, including two custom
  policies declaring the same `Name`. Avoid policies whose effects depend on the
  ordering of different attributes on the same class.
- Custom attributes can opt out of inheritance with `AttributeUsage(Inherited = false)`.
- Only reflection metadata is cached. Attribute instances, scoped services, and
  response values are never shared between requests.

## Request lifecycle and runtime overrides

Headers are applied after the page is constructed and its query properties are
bound, but **before** the Imp authentication callback, global `OnPreRender`, page
`PreRender`, postbacks, and rendering. They apply to both template-backed and
direct-render pages, including authentication short-circuits and redirects.

Later code can deliberately replace or remove a header:

```csharp
public override void PreRender(HttpResponse response)
{
    response.Headers.CacheControl = "private, max-age=30";
    response.Headers.Remove("X-Application");
}
```

Imp does not reapply policies in `OnStarting`. Middleware outside Imp can still
change the final response headers. Pages without header attributes retain their
existing behavior.

Static header names and values are validated; invalid names, null values, and
control characters such as CR/LF are rejected. Custom policies are responsible for
validating dynamically generated values. Do not copy untrusted request text into
response headers without validation.

## Moving from lifecycle-only header code

Replace an unconditional header assignment with a static attribute. Use a custom
attribute for conditional behavior such as authentication-dependent cache control.
Keep resource-dependent decisions (for example, whether a loaded recipe is hidden)
in the page after the resource is loaded. Attributes run before `PreRender`, so
that loaded page state is not available to them.
