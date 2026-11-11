using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Imp
{
    /// <summary>Common response header names. Custom header names can also be supplied as strings.</summary>
    public static class ResponseHeader
    {
        // Caching and conditional responses.
        public const string CacheControl = "Cache-Control";
        public const string Expires = "Expires";
        public const string Pragma = "Pragma";
        public const string ETag = "ETag";
        public const string LastModified = "Last-Modified";
        public const string Vary = "Vary";
        public const string Age = "Age";

        // Content and navigation.
        public const string ContentType = "Content-Type";
        public const string ContentDisposition = "Content-Disposition";
        public const string ContentEncoding = "Content-Encoding";
        public const string ContentLanguage = "Content-Language";
        public const string ContentLocation = "Content-Location";
        public const string ContentRange = "Content-Range";
        public const string AcceptRanges = "Accept-Ranges";
        public const string Location = "Location";
        public const string Link = "Link";
        public const string Allow = "Allow";
        public const string RetryAfter = "Retry-After";

        // Cross-origin access.
        public const string AccessControlAllowOrigin = "Access-Control-Allow-Origin";
        public const string AccessControlAllowCredentials =
            "Access-Control-Allow-Credentials";
        public const string AccessControlAllowHeaders = "Access-Control-Allow-Headers";
        public const string AccessControlAllowMethods = "Access-Control-Allow-Methods";
        public const string AccessControlExposeHeaders = "Access-Control-Expose-Headers";
        public const string AccessControlMaxAge = "Access-Control-Max-Age";

        // Security.
        public const string ContentSecurityPolicy = "Content-Security-Policy";
        public const string ContentSecurityPolicyReportOnly =
            "Content-Security-Policy-Report-Only";
        public const string StrictTransportSecurity = "Strict-Transport-Security";
        public const string WWWAuthenticate = "WWW-Authenticate";

        public const string ReferrerPolicy = "Referrer-Policy";
        public const string PermissionsPolicy = "Permissions-Policy";
        public const string CrossOriginEmbedderPolicy = "Cross-Origin-Embedder-Policy";
        public const string CrossOriginOpenerPolicy = "Cross-Origin-Opener-Policy";
        public const string CrossOriginResourcePolicy = "Cross-Origin-Resource-Policy";
        public const string XContentTypeOptions = "X-Content-Type-Options";
        public const string XFrameOptions = "X-Frame-Options";
        public const string XRobotsTag = "X-Robots-Tag";
    }

    /// <summary>
    /// Declares a page response header. Imp creates a fresh instance through request-scoped DI
    /// and applies it before authentication callbacks and page lifecycle methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public partial class ResponseHeaderAttribute : Attribute
    {
        public string Name { get; }
        public string Value { get; }

        public ResponseHeaderAttribute(string name, string value)
        {
            ValidateName(name);
            ValidateValue(value);
            Name = name;
            Value = value;
        }

        /// <summary>Declares the header managed by a custom attribute.</summary>
        protected ResponseHeaderAttribute(string name)
        {
            ValidateName(name);
            Name = name;
        }

        /// <summary>
        /// Sets the header, replacing an earlier value. Override for conditional or custom behavior.
        /// Runtime page code can subsequently override or remove the header.
        /// </summary>
        public virtual void Apply(HttpResponse response)
        {
            if (Value != null)
                response.Headers[Name] = Value;
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("A response header name is required.", nameof(name));
            if (!ValidName().IsMatch(name))
                throw new ArgumentException("Invalid response header name.", nameof(name));
        }

        private static void ValidateValue(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (InvalidValueCharacters().IsMatch(value))
                throw new ArgumentException(
                    "Response header values cannot contain control characters.",
                    nameof(value)
                );
        }

        // HTTP token characters only; absolute anchors also reject a trailing newline.
        [GeneratedRegex(@"\A[!#$%&'*+.^_`|~0-9A-Za-z-]+\z")]
        private static partial Regex ValidName();

        // Preserve horizontal tabs and visible characters, but reject C0 controls and DEL.
        [GeneratedRegex(@"[\x00-\x08\x0A-\x1F\x7F]")]
        private static partial Regex InvalidValueCharacters();
    }
}
