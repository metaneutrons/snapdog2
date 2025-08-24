using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SnapDog.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LoggerMessageCodeFixProvider)), Shared]
public class LoggerMessageCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(
            LoggerMessageAnalyzer.UseNamedParametersRule.Id,
            LoggerMessageAnalyzer.MoveToEndOfClassRule.Id
        );

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            if (diagnostic.Id == LoggerMessageAnalyzer.UseNamedParametersRule.Id)
            {
                var attribute = root.FindNode(diagnosticSpan).FirstAncestorOrSelf<AttributeSyntax>();
                if (attribute != null)
                {
                    var action = CodeAction.Create(
                        title: "Convert to named parameters",
                        createChangedDocument: c => ConvertToNamedParameters(context.Document, attribute, c),
                        equivalenceKey: "ConvertToNamedParameters"
                    );

                    context.RegisterCodeFix(action, diagnostic);
                }
            }
            else if (diagnostic.Id == LoggerMessageAnalyzer.MoveToEndOfClassRule.Id)
            {
                var method = root.FindNode(diagnosticSpan).FirstAncestorOrSelf<MethodDeclarationSyntax>();
                if (method != null)
                {
                    var action = CodeAction.Create(
                        title: "Move LoggerMessage methods to end of class",
                        createChangedDocument: c => MoveLoggerMessagesToEnd(context.Document, method, c),
                        equivalenceKey: "MoveLoggerMessagesToEnd"
                    );

                    context.RegisterCodeFix(action, diagnostic);
                }
            }
        }
    }

    private static async Task<Document> ConvertToNamedParameters(
        Document document,
        AttributeSyntax attribute,
        CancellationToken cancellationToken
    )
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null || attribute.ArgumentList?.Arguments == null)
            return document;

        var newArguments = SyntaxFactory.SeparatedList<AttributeArgumentSyntax>();

        foreach (var arg in attribute.ArgumentList.Arguments)
        {
            if (arg.NameEquals != null || arg.NameColon != null)
            {
                // Already named, keep as is
                newArguments = newArguments.Add(arg);
                continue;
            }

            // Convert positional to named based on common LoggerMessage parameters
            var newArg = ConvertPositionalToNamed(arg, newArguments.Count);
            newArguments = newArguments.Add(newArg);
        }

        var newArgumentList = SyntaxFactory.AttributeArgumentList(newArguments);
        var newAttribute = attribute.WithArgumentList(newArgumentList);
        var newRoot = root.ReplaceNode(attribute, newAttribute);

        return document.WithSyntaxRoot(newRoot);
    }

    private static AttributeArgumentSyntax ConvertPositionalToNamed(AttributeArgumentSyntax arg, int position)
    {
        string parameterName = position switch
        {
            0 => "EventId",
            1 => "Level",
            2 => "Message",
            _ => $"Parameter{position}",
        };

        var nameEquals = SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName(parameterName));
        return arg.WithNameEquals(nameEquals);
    }

    private static async Task<Document> MoveLoggerMessagesToEnd(
        Document document,
        MethodDeclarationSyntax triggerMethod,
        CancellationToken cancellationToken
    )
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var containingClass = triggerMethod.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (containingClass == null)
            return document;

        // Find all LoggerMessage methods in the class
        var loggerMessageMethods = containingClass
            .Members.OfType<MethodDeclarationSyntax>()
            .Where(IsLoggerMessageMethod)
            .ToList();

        if (loggerMessageMethods.Count == 0)
            return document;

        // Remove LoggerMessage methods from their current positions
        var membersWithoutLoggerMessages = containingClass
            .Members.Where(member => !(member is MethodDeclarationSyntax method && IsLoggerMessageMethod(method)))
            .ToList();

        // Add LoggerMessage methods at the end
        var newMembers = SyntaxFactory.List(membersWithoutLoggerMessages.Concat(loggerMessageMethods));
        var newClass = containingClass.WithMembers(newMembers);
        var newRoot = root.ReplaceNode(containingClass, newClass);

        return document.WithSyntaxRoot(newRoot);
    }

    private static bool IsLoggerMessageMethod(MethodDeclarationSyntax method)
    {
        return method
            .AttributeLists.SelectMany(al => al.Attributes)
            .Any(attr =>
            {
                var name = attr.Name.ToString();
                return name == "LoggerMessage"
                    || name == "LoggerMessageAttribute"
                    || name.EndsWith(".LoggerMessage")
                    || name.EndsWith(".LoggerMessageAttribute");
            });
    }
}
