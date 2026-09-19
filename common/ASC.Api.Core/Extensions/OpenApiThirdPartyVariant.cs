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

using System.Text.Json.Nodes;

using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ASC.Api.Core.Extensions;

/// <summary>
/// Keeps the third-party twin of a generic controller action in the ApiExplorer model instead of leaving it to
/// Swashbuckle's conflict resolution.
/// </summary>
/// <remarks>
/// A generic controller is closed twice - over <c>int</c> for the entries the portal stores itself and over
/// <c>string</c> for the entries on a connected third-party account - and both closings register the very same
/// route; only the constraint of <see cref="ConstraintRouteAttribute"/> tells them apart at request time.
/// ApiExplorer therefore reports two descriptions with one path and method, and Swashbuckle can keep only one
/// of them, so the documents used to say that every such operation takes an integer and answers with the
/// portal's shape, and no SDK could address a third-party file at all.
/// This provider runs after the default one and moves the <c>string</c> twin to a path of its own, ending in
/// <see cref="Marker"/>, so that Swashbuckle documents it in full - parameters, request body, responses and the
/// schemas they need. <see cref="ThirdPartyVariantDocumentFilter"/> then folds that operation back into the real
/// one as an extension and drops the marked path, which never existed.
/// The twin also inherits the <c>[SwaggerResponse]</c> attributes of the shared action, and those name the
/// <c>int</c> shape (<c>typeof(FileDto&lt;int&gt;)</c>) because an attribute cannot mention the type parameter.
/// The response types are corrected here from the closed method: where the attribute repeats the declared return
/// type of the <c>int</c> action, the twin gets the declared return type of its own, and otherwise the <c>int</c>
/// arguments of the documented type are replaced with <c>string</c>.
/// </remarks>
public class ThirdPartyVariantApiDescriptionProvider(IModelMetadataProvider modelMetadataProvider) : IApiDescriptionProvider
{
    /// <summary>
    /// The segment appended to the twin's path while the document is generated. A tilde cannot start a real
    /// route segment of this API, so the marked paths can be told apart without a registry.
    /// </summary>
    public const string Marker = "/~thirdparty";

    // After DefaultApiDescriptionProvider (-1000): its results are what is grouped here.
    public int Order => 0;

    public void OnProvidersExecuting(ApiDescriptionProviderContext context)
    {
        foreach (var group in context.Results.GroupBy(d => (d.RelativePath, d.HttpMethod, d.GroupName)))
        {
            var portal = group.FirstOrDefault(d => EntryIdType(d) == typeof(int));
            var thirdParty = group.FirstOrDefault(d => EntryIdType(d) == typeof(string));

            if (portal == null || thirdParty == null || !SameAction(portal, thirdParty) || !ReachableByPath(portal, thirdParty))
            {
                continue;
            }

            FixResponseTypes(portal, thirdParty);
            thirdParty.RelativePath += Marker;
        }
    }

    public void OnProvidersExecuted(ApiDescriptionProviderContext context)
    {
    }

    /// <summary>
    /// Whether a request can reach the third-party twin at all: only through a route whose path carries the
    /// entry id. The two controllers register the same route, and on a tie the precedence that
    /// <see cref="ConstraintRouteAttribute"/> sets up hands the request to the portal's one, so a twin whose
    /// route names no id - the id sits in the query or in the body - can never be called and is not documented.
    /// Documenting it would also leave the schemas only it refers to dangling in the components.
    /// </summary>
    private static bool ReachableByPath(ApiDescription portal, ApiDescription thirdParty)
    {
        return thirdParty.ParameterDescriptions.Any(parameter =>
            parameter.Source == BindingSource.Path
            && parameter.Type == typeof(string)
            && portal.ParameterDescriptions.Any(p => p.Name == parameter.Name && p.Source == BindingSource.Path && p.Type == typeof(int)));
    }

    private void FixResponseTypes(ApiDescription portal, ApiDescription thirdParty)
    {
        var portalDeclared = DeclaredResponseType(portal);
        var thirdPartyDeclared = DeclaredResponseType(thirdParty);

        foreach (var response in thirdParty.SupportedResponseTypes)
        {
            if (response.Type == null || response.Type == typeof(void))
            {
                continue;
            }

            var corrected = response.Type == portalDeclared ? thirdPartyDeclared : WithThirdPartyId(response.Type);

            if (corrected == response.Type)
            {
                continue;
            }

            response.Type = corrected;
            response.ModelMetadata = modelMetadataProvider.GetMetadataForType(corrected);
        }
    }

    private static bool SameAction(ApiDescription portal, ApiDescription thirdParty)
    {
        return portal.ActionDescriptor is ControllerActionDescriptor portalAction
            && thirdParty.ActionDescriptor is ControllerActionDescriptor thirdPartyAction
            && portalAction.ActionName == thirdPartyAction.ActionName
            && GenericBase(portalAction).GetGenericTypeDefinition() == GenericBase(thirdPartyAction).GetGenericTypeDefinition();
    }

    private static Type EntryIdType(ApiDescription description)
    {
        return description.ActionDescriptor is ControllerActionDescriptor action
            ? GenericBase(action)?.GetGenericArguments()[0]
            : null;
    }

    /// <summary>
    /// The closed generic controller in the base chain - <c>FilesController&lt;int&gt;</c> for
    /// <c>FilesControllerInternal</c> - or null when the controller is not closed over an entry id.
    /// </summary>
    private static Type GenericBase(ControllerActionDescriptor action)
    {
        for (var type = action.ControllerTypeInfo.AsType(); type != null && type != typeof(object); type = type.BaseType)
        {
            if (type.IsGenericType
                && type.GetGenericArguments() is [var idType]
                && (idType == typeof(int) || idType == typeof(string)))
            {
                return type;
            }
        }

        return null;
    }

    private static Type DeclaredResponseType(ApiDescription description)
    {
        var type = ((ControllerActionDescriptor)description.ActionDescriptor).MethodInfo.ReturnType;

        if (type.IsGenericType
            && (type.GetGenericTypeDefinition() == typeof(Task<>) || type.GetGenericTypeDefinition() == typeof(ValueTask<>)))
        {
            type = type.GetGenericArguments()[0];
        }

        return type;
    }

    /// <summary>
    /// The same type with every <c>int</c> type argument replaced by <c>string</c>, at any depth. A bare
    /// <c>int</c> is left alone: a count is a count for either storage.
    /// </summary>
    private static Type WithThirdPartyId(Type type)
    {
        if (type.IsArray)
        {
            var element = WithThirdPartyId(type.GetElementType());

            return element == type.GetElementType() ? type : element.MakeArrayType();
        }

        if (!type.IsGenericType)
        {
            return type;
        }

        var arguments = type.GetGenericArguments();
        var replaced = arguments.Select(argument => argument == typeof(int) ? typeof(string) : WithThirdPartyId(argument)).ToArray();

        return replaced.SequenceEqual(arguments) ? type : type.GetGenericTypeDefinition().MakeGenericType(replaced);
    }
}

/// <summary>
/// Folds the operations documented under a <see cref="ThirdPartyVariantApiDescriptionProvider.Marker"/> path
/// into the real operation as <c>x-thirdparty-variant</c>, and removes the marked path.
/// </summary>
/// <remarks>
/// The extension holds only what changes when the operation is addressed with a third-party identifier: the
/// parameters whose schema differs (the id becomes a string), the request body when it differs, and the responses
/// whose payload differs - <c>FileWrapper</c> becomes <c>ThirdPartyFileWrapper</c>. Everything it names is a schema
/// of this document, generated for the twin operation as for any other, so a generator can build a second overload
/// from it without guessing; the C# SDK does. Registered after <see cref="OpenApi31SchemaDocumentFilter"/> on
/// purpose: it moves finished content and produces none, and a piece captured before that rewrite would be the one
/// place in the document still spelled the 3.0 way.
/// </remarks>
public class ThirdPartyVariantDocumentFilter : IDocumentFilter
{
    public const string ExtensionName = "x-thirdparty-variant";

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        const string marker = ThirdPartyVariantApiDescriptionProvider.Marker;

        foreach (var key in swaggerDoc.Paths.Keys.Where(k => k.EndsWith(marker, StringComparison.Ordinal)).ToList())
        {
            var variantPath = swaggerDoc.Paths[key];
            swaggerDoc.Paths.Remove(key);

            if (variantPath.Operations == null
                || !swaggerDoc.Paths.TryGetValue(key[..^marker.Length], out var path)
                || path.Operations == null)
            {
                continue;
            }

            foreach (var (method, variant) in variantPath.Operations)
            {
                if (!path.Operations.TryGetValue(method, out var operation))
                {
                    continue;
                }

                var difference = Difference(operation, variant);

                if (difference.Count == 0)
                {
                    continue;
                }

                operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();
                operation.Extensions[ExtensionName] = new JsonNodeExtension(difference);
            }
        }
    }

    private static JsonObject Difference(OpenApiOperation operation, OpenApiOperation variant)
    {
        var difference = new JsonObject();

        var parameters = new JsonArray();

        foreach (var parameter in variant.Parameters ?? [])
        {
            var counterpart = operation.Parameters?.FirstOrDefault(p => p.Name == parameter.Name && p.In == parameter.In);
            var json = Serialize(parameter);

            if (counterpart == null || !JsonNode.DeepEquals(json, Serialize(counterpart)))
            {
                parameters.Add(json);
            }
        }

        // A guard behind ThirdPartyVariantApiDescriptionProvider.ReachableByPath: a twin whose path carries no
        // differently typed id would come out as a second overload with the very same signature.
        if (!parameters.Any(parameter => parameter?["in"]?.GetValue<string>() == "path"))
        {
            return difference;
        }

        difference["parameters"] = parameters;

        if (variant.RequestBody != null)
        {
            var json = Serialize(variant.RequestBody);

            if (operation.RequestBody == null || !JsonNode.DeepEquals(json, Serialize(operation.RequestBody)))
            {
                difference["requestBody"] = json;
            }
        }

        var responses = new JsonObject();

        if (variant.Responses != null)
        {
            foreach (var (status, response) in variant.Responses)
            {
                var json = Serialize(response);

                if (operation.Responses == null
                    || !operation.Responses.TryGetValue(status, out var counterpart)
                    || !JsonNode.DeepEquals(json, Serialize(counterpart)))
                {
                    responses[status] = json;
                }
            }
        }

        if (responses.Count > 0)
        {
            difference["responses"] = responses;
        }

        return difference;
    }

    private static JsonNode Serialize(IOpenApiSerializable element)
    {
        using var text = new StringWriter(CultureInfo.InvariantCulture);
        var writer = new OpenApiJsonWriter(text);
        element.SerializeAsV31(writer);

        return JsonNode.Parse(text.ToString());
    }
}
