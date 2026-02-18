using System.Text.Json;

namespace ASC.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ApiControllerXmlDocumentationAnalyzer : DiagnosticAnalyzer
{
    public const string ModelDiagnosticId = "API001";
    public const string ActionDiagnosticId = "API002";
    public const string SummaryDiagnosticId = "API003";
    public const string RemarksDiagnosticId = "API004";
    public const string ModelDtoDiagnosticId = "API005";

    private static readonly LocalizableString _modelTitle = "API DTO model missing XML documentation";
    private static readonly LocalizableString _actionTitle = "API Action method missing XML documentation";
    private static readonly LocalizableString _summaryTitle = "API Action method missing summary";
    private static readonly LocalizableString _remarksTitle = "API Action method missing remarks";
    private static readonly LocalizableString _modelDtoTitle = "API DTO model's property missing XML documentation";
    
    private static readonly LocalizableString _modelMessageFormat = "API DTO model '{0}' should have XML documentation";
    private static readonly LocalizableString _actionMessageFormat = "API Action method '{0}' should have XML documentation";
    private static readonly LocalizableString _summaryMessageFormat = "API Action method '{0}' should have summary";
    private static readonly LocalizableString _remarksMessageFormat = "API Action method '{0}' should have remarks";
    private static readonly LocalizableString _modelDtoMessageFormat = "API DTO model's '{0}' property '{1}' should have XML documentation";

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
    
    private static readonly DiagnosticDescriptor _modelDtoRule = new(
        ModelDtoDiagnosticId, 
        _modelDtoTitle, 
        _modelDtoMessageFormat, 
        Category, 
        DiagnosticSeverity.Warning, 
        isEnabledByDefault: true, 
        description: _description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_modelRule, _actionRule, _remarksRule, _summaryRule, _modelDtoRule];

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
            if (ns.Name == "System" || ns.Name.StartsWith("Microsoft"))
            {
                return true;
            }
            ns = ns.ContainingNamespace;
        }
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

        // Check return type, unwrapping Task<T> if needed
        var returnTypeToCheck = methodDeclaration.ReturnType;

        if (context.SemanticModel.GetTypeInfo(returnTypeToCheck).Type is { } returnTypeSymbol)
        {
            // Unwrap Task<T> or ValueTask<T>
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

        var syntaxReferences = returnTypeSymbol.DeclaringSyntaxReferences;
        if (syntaxReferences.Length > 0)
        {
            CheckPropertiesFromSyntax(context, syntaxReferences);
        }
        else
        {
            CheckPropertiesFromMetadata(context, methodDeclaration, returnTypeSymbol);
        }
    }

    private static void CheckPropertiesFromSyntax(SyntaxNodeAnalysisContext context, ImmutableArray<SyntaxReference> syntaxReferences)
    {
        foreach (var syntaxReference in syntaxReferences)
        {
            var syntaxNode = syntaxReference.GetSyntax();

            if (syntaxNode is not ClassDeclarationSyntax modelDeclaration)
            {
                continue;
            }

            foreach (var prop in modelDeclaration.ChildNodes().OfType<PropertyDeclarationSyntax>())
            {
                if (!HasXmlDocumentation(prop))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        _modelDtoRule,
                        prop.Identifier.GetLocation(),
                        modelDeclaration.Identifier.Text,
                        prop.Identifier.Text));
                }
            }
        }
    }
    
    private static readonly ConcurrentDictionary<string, XDocument?> _xmlDocCache = new();

    private static void CheckPropertiesFromMetadata(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, ITypeSymbol typeSymbol)
    {
        var members = typeSymbol.GetMembers();
        foreach (var member in members)
        {
            if (member is not IPropertySymbol propertySymbol || 
                propertySymbol.IsStatic || 
                propertySymbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == "JsonIgnoreAttribute"))
            {
                continue;
            }

            if (propertySymbol.IsImplicitlyDeclared ||
                propertySymbol.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            var xmlDoc = propertySymbol.GetDocumentationCommentXml();

            if (string.IsNullOrWhiteSpace(xmlDoc))
            {
                var reference = context.Compilation.GetMetadataReference(propertySymbol.ContainingAssembly);
                if (reference is PortableExecutableReference peRef)
                {
                    var xmlPath = Path.ChangeExtension(peRef.FilePath, ".xml");

                    if (string.IsNullOrEmpty(xmlPath))
                    {
                        return;
                    }

                    var doc = _xmlDocCache.GetOrAdd(xmlPath, path =>
                    {
                        try { return XDocument.Load(path); }
                        catch { return null; }
                    });
                    
                    if (doc is null)
                    {
                        return;
                    }
                    
                    var docId = propertySymbol.GetDocumentationCommentId();
                    if (docId is null)
                    {
                        continue;
                    }
                    
                    xmlDoc = doc.Descendants("member").FirstOrDefault(e =>
                    {
                        var name = e.Attribute("name")?.Value;
                        return name == docId.Replace("{`0}", "`1") ||
                               name == docId.Replace($"{{{typeof(int).FullName!}}}", "`1") ||
                               name == docId.Replace($"{{{typeof(string).FullName!}}}", "`1") ||
                               name == docId.Replace($"{{{typeof(JsonElement).FullName!}}}", "`1")
                               ;
                    })?.ToString();
                }
            }

            if (string.IsNullOrWhiteSpace(xmlDoc) || !xmlDoc.Contains("<summary>"))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    _modelDtoRule,
                    methodDeclaration.Identifier.GetLocation(),
                    typeSymbol.Name,
                    propertySymbol.Name));
            }
        }
    }
}