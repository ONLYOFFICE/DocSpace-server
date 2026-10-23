// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Webhooks.Contract;

/// <summary>
/// Turns the webhook DTOs into OpenAPI schemas, as <see cref="WebhookPublisher"/> serializes them.
/// </summary>
/// <remarks>
/// The property list comes from the publisher's own <see cref="JsonSerializerOptions"/>, so names, casing and
/// <c>[JsonIgnore]</c> follow the wire. Types are read from the open generic definitions: a property typed by the
/// entry ID parameter <c>T</c> (an <c>int</c> for DocSpace storage, a <c>string</c> for a third-party provider)
/// becomes the hand-written <see cref="EntryIdSchema"/>. A <c>$ref</c> property carries no description of its own,
/// because several generators turn a <c>$ref</c> with siblings into a new inline model.
/// </remarks>
internal sealed class SchemaBuilder(JsonSerializerOptions options, XmlDocumentation documentation)
{
    public const string EntryIdSchema = "EntryId";

    private const string PayloadSuffix = "WebhookDto";

    private static readonly Dictionary<Type, string> _envelopeNames = new()
    {
        [typeof(WebhookPayload<,>)] = "WebhookEnvelope",
        [typeof(WebhookPayloadEventInfo)] = "WebhookEventInfo",
        [typeof(WebhookPayloadTargetInfo<>)] = "WebhookTargetInfo",
        [typeof(WebhookPayloadConfigInfo<>)] = "WebhookConfigInfo"
    };

    private readonly Dictionary<Type, string> _names = new();
    private readonly Queue<Type> _pending = new();
    private readonly List<string> _undocumented = [];

    public List<(string Name, YamlMappingNode Schema)> Schemas { get; } = [];

    /// <summary>
    /// Members without a <c>&lt;summary&gt;</c>: the contract would publish them with no explanation at all.
    /// </summary>
    public IReadOnlyList<string> Undocumented => _undocumented;

    /// <summary>
    /// Adds <paramref name="type"/> and every schema it references, in the order they are first reached.
    /// </summary>
    public string Add(Type type)
    {
        var name = Reference(type);

        while (_pending.TryDequeue(out var next))
        {
            Schemas.Add((_names[next], BuildObject(next)));
        }

        return name;
    }

    private string Reference(Type type)
    {
        var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

        if (!_names.TryGetValue(definition, out var name))
        {
            name = GetSchemaName(definition);
            _names[definition] = name;
            _pending.Enqueue(definition);
        }

        return name;
    }

    /// <summary>
    /// The schema name is the model name in every SDK: <c>FileWebhookDto&lt;T&gt;</c> is published as
    /// <c>FilePayload</c>, and the envelope types keep the names they have always had.
    /// </summary>
    private static string GetSchemaName(Type definition)
    {
        if (_envelopeNames.TryGetValue(definition, out var name))
        {
            return name;
        }

        name = definition.Name;
        var arity = name.IndexOf('`');

        if (arity >= 0)
        {
            name = name[..arity];
        }

        if (!name.EndsWith(PayloadSuffix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{definition.FullName} is part of a webhook payload, but its name does not end in {PayloadSuffix}, so it has no schema name.");
        }

        return name[..^PayloadSuffix.Length] + "Payload";
    }

    private YamlMappingNode BuildObject(Type definition)
    {
        var schema = new YamlMappingNode();
        var description = GetDescription(documentation.GetDescription(definition), definition.FullName);
        var baseDefinition = GetBaseDefinition(definition);

        var inherited = baseDefinition == null
            ? []
            : GetTypeInfo(baseDefinition).Properties.Select(p => p.Name).ToHashSet();

        var properties = BuildProperties(definition, inherited);

        if (baseDefinition == null)
        {
            schema.Add("type", "object");
            AddDescription(schema, description);
            schema.Add("properties", properties);

            return schema;
        }

        AddDescription(schema, description);
        schema.Add("allOf", new YamlSequenceNode(
            Ref(Reference(baseDefinition)),
            new YamlMappingNode { { "type", "object" }, { "properties", properties } }));

        return schema;
    }

    private YamlMappingNode BuildProperties(Type definition, HashSet<string> inherited)
    {
        var properties = new YamlMappingNode();

        foreach (var property in GetTypeInfo(definition).Properties)
        {
            if (property.Get == null || inherited.Contains(property.Name))
            {
                continue;
            }

            var declaration = GetDeclaration((PropertyInfo)property.AttributeProvider);
            var summary = GetDescription(documentation.GetSummary(declaration), $"{definition.FullName}.{declaration.Name}");

            properties.Add(property.Name, BuildProperty(declaration, summary));
        }

        return properties;
    }

    private YamlMappingNode BuildProperty(PropertyInfo declaration, string summary)
    {
        var type = declaration.PropertyType;

        // The envelope's payload stays untyped so that every generator emits a plain JSON value, which the SDK
        // runtime narrows by event.trigger; a oneOf over the payloads is where codegen breaks in several languages.
        if (type.IsGenericParameter && type.DeclaringType == typeof(WebhookPayload<,>) && type.GenericParameterPosition == 0)
        {
            var untyped = new YamlMappingNode();
            AddDescription(untyped, summary);

            return untyped;
        }

        var schema = BuildType(type, declaration);

        if (!schema.Children.ContainsKey("$ref"))
        {
            var enumType = Nullable.GetUnderlyingType(type) ?? type;
            AddDescription(schema, enumType.IsEnum && summary != null ? summary + " " + DescribeEnum(enumType) : summary);
        }

        return schema;
    }

    private YamlMappingNode BuildType(Type type, PropertyInfo declaration)
    {
        if (type.IsGenericParameter)
        {
            return Ref(EntryIdSchema);
        }

        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(string))
        {
            return declaration?.GetCustomAttribute<EmailAddressAttribute>() != null
                ? Scalar("string", "email")
                : Scalar("string");
        }

        if (type == typeof(Guid))
        {
            return Scalar("string", "uuid");
        }

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return Scalar("string", "date-time");
        }

        if (type == typeof(bool))
        {
            return Scalar("boolean");
        }

        if (type == typeof(int) || type == typeof(short) || type == typeof(byte))
        {
            return Scalar("integer", "int32");
        }

        if (type == typeof(long))
        {
            return Scalar("integer", "int64");
        }

        if (type == typeof(double) || type == typeof(decimal))
        {
            return Scalar("number", "double");
        }

        if (type == typeof(float))
        {
            return Scalar("number", "float");
        }

        if (type.IsEnum)
        {
            return Scalar(IsWrittenAsString(type) ? "string" : "integer");
        }

        if (GetItemType(type) is { } itemType)
        {
            return new YamlMappingNode { { "type", "array" }, { "items", BuildType(itemType, null) } };
        }

        if (type.IsClass)
        {
            return Ref(Reference(type));
        }

        throw new NotSupportedException($"{declaration?.DeclaringType?.FullName}.{declaration?.Name}: {type} has no schema mapping.");
    }

    /// <summary>
    /// Whether the serializer writes the enum by name, which it does when the enum type carries a string converter.
    /// </summary>
    private bool IsWrittenAsString(Type enumType)
    {
        return JsonSerializer.Serialize(Activator.CreateInstance(enumType), enumType, options).StartsWith('"');
    }

    /// <summary>
    /// The members of an enum the payload sends as a number, so that a receiver can read the value without the
    /// server sources.
    /// </summary>
    private static string DescribeEnum(Type enumType)
    {
        var values = Enum.GetValues(enumType)
            .Cast<object>()
            .Select(v => $"{Convert.ToInt64(v)} = {Enum.GetName(enumType, v)}");

        var kind = enumType.GetCustomAttribute<FlagsAttribute>() != null ? "Flags" : "Values";

        return $"{kind} of `{enumType.Name}`: {string.Join(", ", values)}.";
    }

    private static Type GetItemType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        var isEnumerable = type.IsGenericType && type.GetInterfaces().Append(type)
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return isEnumerable ? type.GetGenericArguments()[0] : null;
    }

    private JsonTypeInfo GetTypeInfo(Type definition)
    {
        var closed = definition.IsGenericTypeDefinition
            ? definition.MakeGenericType(definition.GetGenericArguments().Select(_ => typeof(int)).ToArray())
            : definition;

        return options.GetTypeInfo(closed);
    }

    private static Type GetBaseDefinition(Type definition)
    {
        var baseType = definition.BaseType;

        if (baseType == null || baseType == typeof(object))
        {
            return null;
        }

        return baseType.IsGenericType ? baseType.GetGenericTypeDefinition() : baseType;
    }

    /// <summary>
    /// The property as declared on the open generic definition, where an entry ID still reads as <c>T</c>.
    /// </summary>
    private static PropertyInfo GetDeclaration(PropertyInfo closed)
    {
        var declaringType = closed.DeclaringType;

        if (declaringType is not { IsGenericType: true })
        {
            return closed;
        }

        return declaringType.GetGenericTypeDefinition()
            .GetProperty(closed.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    }

    private string GetDescription(string summary, string member)
    {
        if (summary == null)
        {
            _undocumented.Add(member);
        }

        return summary;
    }

    private static void AddDescription(YamlMappingNode schema, string description)
    {
        if (!string.IsNullOrEmpty(description))
        {
            schema.Add("description", ContractYamlWriter.Text(description));
        }
    }

    private static YamlMappingNode Scalar(string type, string format = null)
    {
        var schema = new YamlMappingNode { { "type", type } };

        if (format != null)
        {
            schema.Add("format", format);
        }

        return schema;
    }

    private static YamlMappingNode Ref(string schemaName)
    {
        return new YamlMappingNode { { "$ref", "#/components/schemas/" + schemaName } };
    }
}
