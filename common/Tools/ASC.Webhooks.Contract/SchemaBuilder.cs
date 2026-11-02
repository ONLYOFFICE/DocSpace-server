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
/// becomes the hand-written <see cref="EntryIdSchema"/>, except the ID of the envelope's target, which can also be the
/// GUID of a user or a group and becomes the hand-written <see cref="TargetIdSchema"/>. A property's
/// <c>&lt;example&gt;</c> becomes its <c>example</c>. A <c>$ref</c> property carries neither a description nor an
/// example of its own, because several generators turn a <c>$ref</c> with siblings into a new inline model.
/// </remarks>
internal sealed class SchemaBuilder(JsonSerializerOptions options, XmlDocumentation documentation)
{
    public const string EntryIdSchema = "EntryId";

    public const string TargetIdSchema = "WebhookTargetId";

    public const string EnvelopeSchema = "WebhookEnvelope";

    private const string PayloadSuffix = "WebhookDto";

    private static readonly Dictionary<Type, string> _envelopeNames = new()
    {
        [typeof(WebhookPayload<,>)] = EnvelopeSchema,
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

            properties.Add(property.Name, BuildProperty(declaration, summary, documentation.GetExample(declaration)));
        }

        return properties;
    }

    private YamlMappingNode BuildProperty(PropertyInfo declaration, string summary, string example)
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
            AddDescription(schema, enumType.IsEnum && summary != null ? summary + " " + DescribeEnum(enumType, IsWrittenAsString(enumType)) : summary);

            if (example != null)
            {
                schema.Add("example", BuildExample(example, schema));
            }
        }

        return schema;
    }

    /// <summary>
    /// The <c>&lt;example&gt;</c> as the value the schema describes: parsed as JSON where it parses into a value of
    /// the schema's type (<c>42</c> for an integer, <c>true</c> for a boolean, <c>["file.created"]</c> for an array),
    /// and taken as the plain string otherwise, so that <c>2024-01-15T10:30:00Z</c> stays a string and an entry ID
    /// <c>10</c> of a string property is not published as a number.
    /// </summary>
    private static YamlNode BuildExample(string example, YamlMappingNode schema)
    {
        var type = schema.Children.TryGetValue("type", out var typeNode) ? ((YamlScalarNode)typeNode).Value : null;

        JsonNode parsed;

        try
        {
            parsed = JsonNode.Parse(example);
        }
        catch (JsonException)
        {
            parsed = null;
        }

        var kind = parsed?.GetValueKind();

        var matches = type switch
        {
            "integer" => kind == JsonValueKind.Number && parsed.AsValue().TryGetValue<long>(out _),
            "number" => kind == JsonValueKind.Number,
            "boolean" => kind is JsonValueKind.True or JsonValueKind.False,
            "string" => kind == JsonValueKind.String,
            "array" => kind == JsonValueKind.Array,
            "object" => kind == JsonValueKind.Object,
            null => parsed != null,
            _ => false
        };

        return matches ? ToYaml(parsed) : Quoted(example);
    }

    private static YamlNode ToYaml(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                var mapping = new YamlMappingNode();

                foreach (var (key, value) in obj)
                {
                    mapping.Add(key, ToYaml(value));
                }

                return mapping;

            case JsonArray array:
                return new YamlSequenceNode(array.Select(ToYaml));

            case JsonValue value when value.TryGetValue<string>(out var text):
                return Quoted(text);

            default:
                return new YamlScalarNode(node?.ToJsonString() ?? "null") { Style = ScalarStyle.Plain };
        }
    }

    /// <summary>
    /// A string example is always quoted: written plain, a string such as <c>10</c>, <c>true</c> or a timestamp would
    /// be read back by a YAML parser as a number, a boolean or a date.
    /// </summary>
    private static YamlScalarNode Quoted(string value)
    {
        return new YamlScalarNode(value) { Style = ScalarStyle.DoubleQuoted };
    }

    private YamlMappingNode BuildType(Type type, PropertyInfo declaration)
    {
        // the target of the envelope is the entity the subscription is scoped to, which for user.* and group.* events
        // is a user or a group, so its ID is not an entry ID
        if (type.IsGenericParameter && type.DeclaringType == typeof(WebhookPayloadTargetInfo<>))
        {
            return Ref(TargetIdSchema);
        }

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

        // any JSON value: a schema without a type, which every generator maps to its untyped value
        if (type == typeof(object) || type == typeof(JsonElement) || typeof(JsonNode).IsAssignableFrom(type))
        {
            return new YamlMappingNode();
        }

        // checked before the arrays: a dictionary is an IEnumerable of its pairs, but goes on the wire as an object
        if (GetDictionaryTypes(type) is var (keyType, valueType))
        {
            if (keyType != typeof(string))
            {
                throw new NotSupportedException(
                    $"{declaration?.DeclaringType?.FullName}.{declaration?.Name}: {type} has keys of type {keyType}; only string keys map to a JSON object.");
            }

            return new YamlMappingNode { { "type", "object" }, { "additionalProperties", BuildType(valueType, null) } };
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
    /// The members of an enum, so that a receiver can read the value without the server sources: as `number = name`
    /// for one the payload sends as a number, and as the names the body carries for one it sends as a string. Decided
    /// by the same check as the schema's `type`, so the two cannot disagree.
    /// </summary>
    private string DescribeEnum(Type enumType, bool writtenAsString)
    {
        var values = Enum.GetValues(enumType)
            .Cast<object>()
            .Select(v => writtenAsString
                ? $"`{JsonSerializer.Serialize(v, enumType, options).Trim('"')}`"
                : $"{Convert.ToInt64(v)} = {Enum.GetName(enumType, v)}");

        var kind = enumType.GetCustomAttribute<FlagsAttribute>() != null ? "Flags" : "Values";

        return $"{kind} of `{enumType.Name}`: {string.Join(", ", values)}.";
    }

    private static (Type Key, Type Value)? GetDictionaryTypes(Type type)
    {
        var dictionary = type.GetInterfaces().Append(type)
            .FirstOrDefault(i => i.IsGenericType &&
                                 (i.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                                  i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));

        if (dictionary == null)
        {
            return null;
        }

        var arguments = dictionary.GetGenericArguments();

        return (arguments[0], arguments[1]);
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
