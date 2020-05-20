﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;

namespace RestEase.SourceGenerator.Implementation
{
    public static class AttributeInstantiator
    {
        private static readonly Assembly assembly = typeof(AttributeInstantiator).Assembly;

        public static IEnumerable<Attribute?> Instantiate(ISymbol symbol)
        {
            return symbol.GetAttributes().Select(Instantiate);
        }

        public static Attribute? Instantiate(AttributeData attributeData)
        {
            string? attributeMetadataName = attributeData.AttributeClass?.ToDisplayString(SymbolDisplayFormats.TypeLookup);
            if (attributeMetadataName == null)
            {
                return null;
            }

            var attribute = attributeMetadataName switch
            {
                "RestEase.AllowAnyStatusCodeAttribute" => ParseAllowAnyStatusCodeAttribute(attributeData),
                "RestEase.QueryAttribute" => ParseQueryAttribute(attributeData),
                "RestEase.SerializationMethodsAttribute" => ParseSerializationMethodsAttribute(attributeData),
                "RestEase.GetAttribute" => ParseRequestAttributeSubclass(attributeData, () => new GetAttribute(), x => new GetAttribute(x)),
                _ => null,
            };

            return attribute;
        }

        private static Attribute? ParseAllowAnyStatusCodeAttribute(AttributeData attributeData)
        {
            AllowAnyStatusCodeAttribute? attribute = null;
            if (attributeData.ConstructorArguments.Length == 0)
            {
                attribute = new AllowAnyStatusCodeAttribute();
            }
            else if (attributeData.ConstructorArguments.Length == 1 &&
                attributeData.ConstructorArguments[0].Type.SpecialType == SpecialType.System_Boolean)
            {
                attribute = new AllowAnyStatusCodeAttribute((bool)attributeData.ConstructorArguments[0].Value!);
            }

            if (attribute != null)
            {
                foreach (var namedArgument in attributeData.NamedArguments)
                {
                    if (namedArgument.Key == "AllowAnyStatusCode" &&
                        namedArgument.Value.Type.SpecialType == SpecialType.System_Boolean)
                    {
                        attribute.AllowAnyStatusCode = (bool)namedArgument.Value.Value!;
                    }
                }
            }

            return attribute;
        }

        private static Attribute? ParseQueryAttribute(AttributeData attributeData)
        {
            QueryAttribute? attribute = null;
            if (attributeData.ConstructorArguments.Length == 0)
            {
                attribute = new QueryAttribute();
            }
            else if (attributeData.ConstructorArguments.Length == 1)
            {
                if (IsString(attributeData.ConstructorArguments[0]))
                {
                    attribute = new QueryAttribute((string?)attributeData.ConstructorArguments[0].Value);
                }
                else if (IsQuerySerializationMethod(attributeData.ConstructorArguments[0]))
                {
                    attribute = new QueryAttribute((QuerySerializationMethod)attributeData.ConstructorArguments[0].Value!);
                }
            }
            else if (attributeData.ConstructorArguments.Length == 2)
            {
                if (IsString(attributeData.ConstructorArguments[0]) && IsQuerySerializationMethod(attributeData.ConstructorArguments[1]))
                {
                    attribute = new QueryAttribute((string?)attributeData.ConstructorArguments[0].Value, (QuerySerializationMethod)attributeData.ConstructorArguments[1].Value!);
                }
            }

            if (attribute != null)
            {
                foreach (var namedArgument in attributeData.NamedArguments)
                {
                    if (namedArgument.Key == "Name" && IsString(namedArgument.Value))
                    {
                        attribute.Name = (string?)namedArgument.Value.Value;
                    }
                    else if (namedArgument.Key == "SerializationMethod" && IsQuerySerializationMethod(namedArgument.Value))
                    {
                        attribute.SerializationMethod = (QuerySerializationMethod)namedArgument.Value.Value!;
                    }
                    else if (namedArgument.Key == "Format" && IsString(namedArgument.Value))
                    {
                        attribute.Format = (string?)namedArgument.Value.Value;
                    }
                }
            }

            return attribute;

            bool IsString(TypedConstant typedConstant) => typedConstant.Type.SpecialType == SpecialType.System_String;
            bool IsQuerySerializationMethod(TypedConstant typedConstant) => typedConstant.Type.ToDisplayString(SymbolDisplayFormats.TypeLookup) == "RestEase.QuerySerializationMethod";
        }

        private static Attribute? ParseSerializationMethodsAttribute(AttributeData attributeData)
        {
            var attribute = attributeData.ConstructorArguments.Length == 0 ? new SerializationMethodsAttribute() : null;

            if (attribute != null)
            {
                foreach (var namedArgument in attributeData.NamedArguments)
                {
                    if (namedArgument.Key == "Body" &&
                        namedArgument.Value.Type.ToDisplayString(SymbolDisplayFormats.TypeLookup) == "RestEase.BodySerializationMethod")
                    {
                        attribute.Body = (BodySerializationMethod)namedArgument.Value.Value!;
                    }
                    else if (namedArgument.Key == "Query" &&
                        namedArgument.Value.Type.ToDisplayString(SymbolDisplayFormats.TypeLookup) == "RestEase.QuerySerializationMethod")
                    {
                        attribute.Query = (QuerySerializationMethod)namedArgument.Value.Value!;
                    }
                    else if (namedArgument.Key == "Path" &&
                        namedArgument.Value.Type.ToDisplayString(SymbolDisplayFormats.TypeLookup) == "RestEase.PathSerializationMethod")
                    {
                        attribute.Path = (PathSerializationMethod)namedArgument.Value.Value!;
                    }
                }
            }

            return attribute;
        }

        private static Attribute? ParseRequestAttributeSubclass(
        AttributeData attributeData,
        Func<RequestAttribute> parameterlessCtor,
        Func<string, RequestAttribute> pathCtor)
        {
            RequestAttribute attribute;
            if (attributeData.ConstructorArguments.Length == 0)
            {
                attribute = parameterlessCtor();
            }
            else if (attributeData.ConstructorArguments.Length == 1 &&
                attributeData.ConstructorArguments[0].Type.SpecialType == SpecialType.System_String)
            {
                attribute = pathCtor((string)attributeData.ConstructorArguments[0].Value!);
            }
            else
            {
                return null;
            }

            foreach (var namedArgument in attributeData.NamedArguments)
            {
                if (namedArgument.Key == "Path" && namedArgument.Value.Type.SpecialType == SpecialType.System_String)
                {
                    attribute.Path = (string?)namedArgument.Value.Value;
                }
            }

            return attribute;
        }
    }
}
