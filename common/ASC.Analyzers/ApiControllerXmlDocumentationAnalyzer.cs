using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ASC.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ApiControllerXmlDocumentationAnalyzer : DiagnosticAnalyzer
{
    public const string ControllerDiagnosticId = "API001";
    public const string ActionDiagnosticId = "API002";
    public const string SummaryDiagnosticId = "API003";
    public const string RemarksDiagnosticId = "API004";

    private static readonly LocalizableString _controllerTitle = "API Controller missing XML documentation";
    private static readonly LocalizableString _actionTitle = "API Action method missing XML documentation";
    private static readonly LocalizableString _summaryTitle = "API Action method missing summary";
    private static readonly LocalizableString _remarksTitle = "API Action method missing remarks";

    private static readonly LocalizableString _controllerMessageFormat = "API Controller '{0}' should have XML documentation";
    private static readonly LocalizableString _actionMessageFormat = "API Action method '{0}' should have XML documentation";
    private static readonly LocalizableString _summaryMessageFormat = "API Action method '{0}' should have summary";
    private static readonly LocalizableString _remarksMessageFormat = "API Action method '{0}' should have remarks";

    private static readonly LocalizableString _description = "API controllers and their methods should have XML documentation for Swagger/OpenAPI generation.";

    private const string Category = "Documentation";

    private static readonly DiagnosticDescriptor _controllerRule = new(
        ControllerDiagnosticId, 
        _controllerTitle, 
        _controllerMessageFormat, 
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
    

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_controllerRule, _actionRule, _remarksRule, _summaryRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
            
        //context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
            
        if (!IsApiController(context, classDeclaration))
        {
            return;
        }

        if (!HasXmlDocumentation(classDeclaration))
        {
            var diagnostic = Diagnostic.Create(
                _controllerRule, 
                classDeclaration.Identifier.GetLocation(), 
                classDeclaration.Identifier.Text);
                
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
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
            
        CheckDocumentation(context, methodDeclaration);
    }

    private static bool IsApiController(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classDeclaration)
    {
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
            
        if (classSymbol == null)
        {
            return false;
        }

        var hasApiControllerAttribute = classSymbol.GetAttributes()
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

    private static bool HasXmlDocumentation(SyntaxNode node)
    {
        var trivia = node.GetLeadingTrivia();
        return trivia.Any(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                               t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
    }

    private static void CheckDocumentation(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration)
    {
        if (methodDeclaration.ParameterList.Parameters.Count == 0)
        {
            return;
        }

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
                _remarksRule,
                methodDeclaration.Identifier.GetLocation(), 
                methodDeclaration.Identifier.Text));
        }
        
        // var documentedParams = xmlStructure.Content
        //     .OfType<XmlElementSyntax>()
        //     .Where(e => e.StartTag.Name.ToString() == "param")
        //     .Select(e => e.StartTag.Attributes
        //         .OfType<XmlNameAttributeSyntax>()
        //         .FirstOrDefault()?.Identifier.ToString())
        //     .Where(name => !string.IsNullOrEmpty(name))
        //     .ToHashSet();
        
        // foreach (var parameter in methodDeclaration.ParameterList.Parameters)
        // {
        //     var paramName = parameter.Identifier.Text;
        //     if (documentedParams.Contains(paramName))
        //     {
        //         continue;
        //     }
        //
        //     var diagnostic = Diagnostic.Create(
        //         _parameterRule,
        //         parameter.Identifier.GetLocation(),
        //         paramName,
        //         methodDeclaration.Identifier.Text);
        //
        //     context.ReportDiagnostic(diagnostic);
        // }
    }
}