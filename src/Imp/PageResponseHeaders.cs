using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Imp
{
    internal static class PageResponseHeaders
    {
        // Cache only immutable reflection metadata, never attribute instances or scoped services.
        private static readonly ConcurrentDictionary<Type, CustomAttributeData[][]> Metadata =
            new();

        internal static void Apply(Type pageType, HttpContext context)
        {
            var declarations = Metadata.GetOrAdd(pageType, ReadMetadata);
            foreach (var level in declarations)
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var declaration in level)
                {
                    var arguments = declaration
                        .ConstructorArguments.Select(ConvertArgument)
                        .ToArray();
                    var attribute = (ResponseHeaderAttribute)
                        ActivatorUtilities.CreateInstance(
                            context.RequestServices,
                            declaration.AttributeType,
                            arguments
                        );
                    foreach (var named in declaration.NamedArguments)
                    {
                        var value = ConvertArgument(named.TypedValue);
                        if (named.MemberInfo is PropertyInfo property)
                            property.SetValue(attribute, value);
                        else if (named.MemberInfo is FieldInfo field)
                            field.SetValue(attribute, value);
                    }

                    if (!names.Add(attribute.Name))
                        throw new InvalidOperationException(
                            $"Page '{pageType.FullName}' declares response header '{attribute.Name}' more than once on the same class."
                        );
                    attribute.Apply(context.Response);
                }
            }
        }

        private static CustomAttributeData[][] ReadMetadata(Type pageType)
        {
            var hierarchy = new Stack<Type>();
            for (
                var type = pageType;
                type != null && typeof(BasePage).IsAssignableFrom(type);
                type = type.BaseType
            )
                hierarchy.Push(type);
            return hierarchy
                .Select(type =>
                    type.GetCustomAttributesData()
                        .Where(data =>
                            typeof(ResponseHeaderAttribute).IsAssignableFrom(data.AttributeType)
                            && (
                                type == pageType
                                || data.AttributeType.GetCustomAttribute<AttributeUsageAttribute>(
                                    true
                                )?.Inherited != false
                            )
                        )
                        .ToArray()
                )
                .ToArray();
        }

        private static object ConvertArgument(CustomAttributeTypedArgument argument)
        {
            if (argument.Value is IReadOnlyCollection<CustomAttributeTypedArgument> elements)
            {
                var array = Array.CreateInstance(
                    argument.ArgumentType.GetElementType(),
                    elements.Count
                );
                var index = 0;
                foreach (var element in elements)
                    array.SetValue(ConvertArgument(element), index++);
                return array;
            }
            if (argument.ArgumentType.IsEnum)
                return Enum.ToObject(argument.ArgumentType, argument.Value);
            return argument.Value;
        }
    }
}
