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

using System.Text.Json;

namespace ASC.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ApiControllerXmlDocumentationAnalyzer : DiagnosticAnalyzer
{
    public const string ModelDiagnosticId = "API001";
    public const string ActionDiagnosticId = "API002";
    public const string SummaryDiagnosticId = "API003";
    public const string RemarksDiagnosticId = "API004";
    public const string ModelDtoSummaryDiagnosticId = "API005";
    public const string ModelDtoExampleDiagnosticId = "API006";

    private static readonly LocalizableString _modelTitle = "API DTO model missing XML documentation";
    private static readonly LocalizableString _actionTitle = "API Action method missing XML documentation";
    private static readonly LocalizableString _summaryTitle = "API Action method missing summary";
    private static readonly LocalizableString _remarksTitle = "API Action method missing remarks";
    private static readonly LocalizableString _modelDtoSummaryTitle = "API DTO model's property missing summary";
    private static readonly LocalizableString _modelDtoExampleTitle = "API DTO model's property missing example";

    private static readonly LocalizableString _modelMessageFormat = "API DTO model '{0}' should have XML documentation";
    private static readonly LocalizableString _actionMessageFormat = "API Action method '{0}' should have XML documentation";
    private static readonly LocalizableString _summaryMessageFormat = "API Action method '{0}' should have summary";
    private static readonly LocalizableString _remarksMessageFormat = "API Action method '{0}' should have remarks";
    private static readonly LocalizableString _modelDtoSummaryMessageFormat = "API DTO model's '{0}' property '{1}' should have summary";
    private static readonly LocalizableString _modelDtoExampleMessageFormat = "API DTO model's '{0}' property '{1}' should have example";

    private static readonly LocalizableString _description = "API controllers and their methods should have XML documentation for Swagger/OpenAPI generation.";

    private const string Category = "Documentation";

    private static readonly DiagnosticDescriptor _modelRule = new(
        ModelDiagnosticId,
        _modelTitle,
        _modelMessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description);

    private static readonly DiagnosticDescriptor _actionRule = new(
        ActionDiagnosticId,
        _actionTitle,
        _actionMessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description);

    private static readonly DiagnosticDescriptor _summaryRule = new(
        SummaryDiagnosticId,
        _summaryTitle,
        _summaryMessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description);

    private static readonly DiagnosticDescriptor _remarksRule = new(
        RemarksDiagnosticId,
        _remarksTitle,
        _remarksMessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description);

    private static readonly DiagnosticDescriptor _modelDtoSummaryRule = new(
        ModelDtoSummaryDiagnosticId,
        _modelDtoSummaryTitle,
        _modelDtoSummaryMessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description);

    private static readonly DiagnosticDescriptor _modelDtoExampleRule = new(
        ModelDtoExampleDiagnosticId,
        _modelDtoExampleTitle,
        _modelDtoExampleMessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_modelRule, _actionRule, _remarksRule, _summaryRule, _modelDtoSummaryRule, _modelDtoExampleRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationContext =>
        {
            var reportedTypes = new ConcurrentDictionary<string, bool>();
            compilationContext.RegisterSyntaxNodeAction(ctx => AnalyzeMethod(ctx, reportedTypes), SyntaxKind.MethodDeclaration);
        });
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context, ConcurrentDictionary<string, bool> reportedTypes)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        if (!IsApiActionMethod(context, methodDeclaration))
        {
            return;
        }

        if (!HasXmlDocumentation(methodDeclaration))
        {
            var diagnostic = Diagnostic.Create(
                _actionRule,
                methodDeclaration.Identifier.GetLocation(),
                methodDeclaration.Identifier.Text);

            context.ReportDiagnostic(diagnostic);
            return;
        }

        CheckDocumentation(context, methodDeclaration, reportedTypes);
    }


    private static bool IsApiActionMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration)
    {
        if (!methodDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
        {
            return false;
        }

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);
        if (methodSymbol == null)
        {
            return false;
        }

        var containingClass = methodDeclaration.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (containingClass == null || !IsApiController(context, containingClass))
        {
            return false;
        }

        var httpAttributes = new[] { "HttpGetAttribute", "HttpPostAttribute", "HttpPutAttribute", "HttpDeleteAttribute", "HttpPatchAttribute", "HttpHeadAttribute", "HttpOptionsAttribute" };
        return methodSymbol.GetAttributes()
            .Any(attr => httpAttributes.Contains(attr.AttributeClass?.Name));
    }

    private static bool IsApiController(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration)
    {
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);

        if (classSymbol == null)
        {
            return false;
        }

        var attributes = classSymbol.GetAttributes();

        var apiExplorerSettings = attributes
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "ApiExplorerSettingsAttribute");

        var ignoreApiArg = apiExplorerSettings?.NamedArguments
            .FirstOrDefault(arg => arg.Key == "IgnoreApi");

        if (ignoreApiArg?.Value.Value is true)
        {
            return false;
        }

        var hasApiControllerAttribute = attributes
            .Any(attr => attr.AttributeClass?.Name == "ApiControllerAttribute");

        if (hasApiControllerAttribute)
        {
            return true;
        }

        var baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name is "ControllerBase" or "Controller")
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static bool HasXmlDocumentation(SyntaxNode node)
    {
        var trivia = node.GetLeadingTrivia();

        return trivia.Any(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                               t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
    }

    private static bool IsSystemNamespace(ITypeSymbol typeSymbol)
    {
        var ns = typeSymbol.ContainingNamespace;
        while (ns is { IsGlobalNamespace: false })
        {
            if (ns.Name.StartsWith("System") || ns.Name.StartsWith("Microsoft"))
            {
                return true;
            }
            ns = ns.ContainingNamespace;
        }
        return false;
    }

    /// <summary>
    /// Determines whether a property of the given type should have an <c>&lt;example&gt;</c> XML tag.
    /// Examples are required for leaf types (primitives, strings, dates, GUIDs, enums) and generic system
    /// collections (List, Dictionary, etc.), but NOT for properties whose type is a custom complex object —
    /// Swagger generates the example for those from the inner properties recursively.
    /// </summary>
    private static bool RequiresExample(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null)
        {
            return false;
        }

        // Unwrap Nullable<T>
        if (typeSymbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            typeSymbol = nullable.TypeArguments[0];
        }

        // System / Microsoft types (int, string, bool, DateTime, Guid, List<>, Dictionary<>, ...) — leaf
        if (IsSystemNamespace(typeSymbol))
        {
            return true;
        }

        // Enums — leaf
        if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            return true;
        }

        // Custom complex object — Swagger composes the example from the inner properties
        return false;
    }

    private static void CheckDocumentation(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, ConcurrentDictionary<string, bool> reportedTypes)
    {
        var xmlTrivia = methodDeclaration.GetLeadingTrivia()
            .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                                 t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

        if (xmlTrivia == default)
        {
            return;
        }

        if (xmlTrivia.GetStructure() is not DocumentationCommentTriviaSyntax xmlStructure)
        {
            return;
        }

        var xmlElementSyntaxes = xmlStructure.Content.OfType<XmlElementSyntax>().ToList();

        var remarkExists = xmlElementSyntaxes
            .Any(e => e.StartTag.Name.ToString() == "remarks");

        if (!remarkExists)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                _remarksRule,
                methodDeclaration.Identifier.GetLocation(),
                methodDeclaration.Identifier.Text));
        }

        var summaryExists = xmlElementSyntaxes
            .Any(e => e.StartTag.Name.ToString() == "summary");

        if (!summaryExists)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                _summaryRule,
                methodDeclaration.Identifier.GetLocation(),
                methodDeclaration.Identifier.Text));
        }
        var returnTypeToCheck = methodDeclaration.ReturnType;

        if (context.SemanticModel.GetTypeInfo(returnTypeToCheck).Type is { } returnTypeSymbol)
        {
            if (returnTypeSymbol is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol && IsSystemNamespace(namedTypeSymbol))
            {
                var typeArgument = namedTypeSymbol.TypeArguments.FirstOrDefault();
                if (typeArgument != null && !IsSystemNamespace(typeArgument))
                {
                    ReportMissingXmlDocumentation(context, methodDeclaration, typeArgument, reportedTypes);
                }
            }
            else if (!IsSystemNamespace(returnTypeSymbol))
            {
                ReportMissingXmlDocumentation(context, methodDeclaration, returnTypeSymbol, reportedTypes);
            }
        }

        foreach (var parameter in methodDeclaration.ParameterList.Parameters)
        {
            var parameterType = parameter.Type;

            if (parameterType == null || context.SemanticModel.GetSymbolInfo(parameterType).Symbol is not ITypeSymbol typeSymbol)
            {
                continue;
            }

            ReportMissingXmlDocumentation(context, methodDeclaration, typeSymbol, reportedTypes);
        }
    }

    private static void ReportMissingXmlDocumentation(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, ITypeSymbol returnTypeSymbol, ConcurrentDictionary<string, bool> reportedTypes)
    {
        var typeKey = returnTypeSymbol.ToDisplayString();

        // Skip if already reported
        if (!reportedTypes.TryAdd(typeKey, true))
        {
            return;
        }

        if (returnTypeSymbol.DeclaringSyntaxReferences.Length > 0)
        {
            CheckPropertiesFromSyntax(context, returnTypeSymbol);
        }
        else
        {
            if (!IsList(returnTypeSymbol))
            {
                CheckPropertiesFromMetadata(context, methodDeclaration, returnTypeSymbol);
            }
        }
    }

    private static void CheckPropertiesFromSyntax(SyntaxNodeAnalysisContext context, ITypeSymbol typeSymbol)
    {
        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IPropertySymbol propertySymbol ||
                propertySymbol.IsStatic ||
                propertySymbol.IsImplicitlyDeclared ||
                propertySymbol.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            if (propertySymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not PropertyDeclarationSyntax prop)
            {
                continue;
            }

            if (!HasXmlDocumentation(prop))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    _modelDtoSummaryRule,
                    prop.Identifier.GetLocation(),
                    typeSymbol.Name,
                    propertySymbol.Name));
            }

            var xmlTrivia = prop.GetLeadingTrivia()
                .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                                     t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

            if (xmlTrivia == default || xmlTrivia.GetStructure() is not DocumentationCommentTriviaSyntax xmlStructure)
            {
                continue;
            }

            var xmlElementSyntaxes = xmlStructure.Content.OfType<XmlElementSyntax>().ToList();

            var summaryExists = xmlElementSyntaxes.Any(e => e.StartTag.Name.ToString() == "summary");
            if (!summaryExists)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    _modelDtoSummaryRule,
                    prop.Identifier.GetLocation(),
                    typeSymbol.Name,
                    propertySymbol.Name));
            }

            if (!RequiresExample(propertySymbol.Type))
            {
                continue;
            }

            var exampleExists = xmlElementSyntaxes.Any(e => e.StartTag.Name.ToString() == "example");
            if (!exampleExists)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    _modelDtoExampleRule,
                    prop.Identifier.GetLocation(),
                    typeSymbol.Name,
                    propertySymbol.Name));
            }
        }
    }

    private static readonly ConcurrentDictionary<string, XDocument?> _xmlDocCache = new();
    private static readonly ConcurrentBag<string> _typeCache = [];

    private static void CheckPropertiesFromMetadata(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, ITypeSymbol typeSymbol)
    {
        if (!IsSystemNamespace(typeSymbol) && !_typeCache.Contains(typeSymbol.ToDisplayString()))
        {
            _typeCache.Add(typeSymbol.ToDisplayString());
        }

        var members = typeSymbol.GetMembers();
        foreach (var member in members)
        {
            if (member is not IPropertySymbol propertySymbol ||
                propertySymbol.IsStatic ||
                propertySymbol.GetAttributes().Any(attr => attr.AttributeClass?.Name is "JsonIgnoreAttribute") ||
                propertySymbol.IsImplicitlyDeclared ||
                propertySymbol.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            CheckPropertyXmlDocFromMetadata(context, methodDeclaration, typeSymbol, propertySymbol);

            if (propertySymbol.Type is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol && IsSystemNamespace(namedTypeSymbol))
            {
                var typeArgument = namedTypeSymbol.TypeArguments.FirstOrDefault();
                if (typeArgument != null && !IsSystemNamespace(typeArgument) && !_typeCache.Contains(typeArgument.ToDisplayString()))
                {
                    CheckPropertiesFromMetadata(context, methodDeclaration, typeArgument);
                }
                continue;
            }

            if (propertySymbol.GetAttributes().Any(attr => attr.AttributeClass?.Name is "FromBodyAttribute") ||
                !IsSystemNamespace(propertySymbol.Type))
            {
                if (!IsList(propertySymbol.Type) && !_typeCache.Contains(propertySymbol.Type.ToDisplayString()))
                {
                    CheckPropertiesFromMetadata(context, methodDeclaration, propertySymbol.Type);
                }
            }
        }
    }

    private static void CheckPropertyXmlDocFromMetadata(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, ITypeSymbol typeSymbol, IPropertySymbol propertySymbol)
    {
        var xmlDoc = propertySymbol.GetDocumentationCommentXml();

        if (string.IsNullOrWhiteSpace(xmlDoc))
        {
            xmlDoc = TryLoadXmlDocFromAssemblyFile(context, propertySymbol);
        }

        if (string.IsNullOrWhiteSpace(xmlDoc))
        {
            return;
        }

        if (!xmlDoc.Contains("<summary>"))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                _modelDtoSummaryRule,
                methodDeclaration.Identifier.GetLocation(),
                typeSymbol.Name,
                propertySymbol.Name));
        }

        if (RequiresExample(propertySymbol.Type) && !xmlDoc.Contains("<example>"))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                _modelDtoExampleRule,
                methodDeclaration.Identifier.GetLocation(),
                typeSymbol.Name,
                propertySymbol.Name));
        }
    }

    private static string? TryLoadXmlDocFromAssemblyFile(SyntaxNodeAnalysisContext context, IPropertySymbol propertySymbol)
    {
        if (context.Compilation.GetMetadataReference(propertySymbol.ContainingAssembly) is not PortableExecutableReference peRef ||
            string.IsNullOrEmpty(peRef.FilePath))
        {
            return null;
        }

        foreach (var xmlPath in EnumerateXmlPathCandidates(peRef.FilePath))
        {
            var doc = _xmlDocCache.GetOrAdd(xmlPath, path =>
            {
                try { return XDocument.Load(path); }
                catch { return null; }
            });

            if (doc is null)
            {
                continue;
            }

            var docId = propertySymbol.GetDocumentationCommentId();
            if (docId is null)
            {
                return null;
            }

            var member = doc.Descendants("member").FirstOrDefault(e =>
            {
                var name = e.Attribute("name")?.Value;
                return name == docId ||
                       name == docId.Replace("{`0}", "`1") ||
                       name == docId.Replace($"{{{typeof(int).FullName!}}}", "`1") ||
                       name == docId.Replace($"{{{typeof(string).FullName!}}}", "`1") ||
                       name == docId.Replace($"{{{typeof(JsonElement).FullName!}}}", "`1");
            });

            if (member is not null)
            {
                return member.ToString();
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateXmlPathCandidates(string assemblyPath)
    {
        var primary = Path.ChangeExtension(assemblyPath, ".xml");
        yield return primary;

        var dir = Path.GetDirectoryName(assemblyPath);
        var name = Path.GetFileNameWithoutExtension(assemblyPath);
        if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(name))
        {
            yield break;
        }

        // ref assemblies live under obj/.../ref(int)/ — XML is in the sibling implementation folder
        var refMarkers = new[] { $"{Path.DirectorySeparatorChar}ref{Path.DirectorySeparatorChar}", $"{Path.DirectorySeparatorChar}refint{Path.DirectorySeparatorChar}" };
        foreach (var marker in refMarkers)
        {
            var idx = dir.LastIndexOf(marker);
            if (idx < 0)
            {
                continue;
            }

            var implDir = dir.Remove(idx, marker.Length - 1);
            var candidate = Path.Combine(implDir, name + ".xml");
            if (candidate != primary)
            {
                yield return candidate;
            }
        }
    }

    private static bool IsList(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedType)
        {
            return false;
        }

        return namedType.IsGenericType
               && namedType.ConstructedFrom.ToString() == "System.Collections.Generic.List<T>";
    }
}
