//
// SnapDog
// The Snapcast-based Smart Home Audio System with MQTT & KNX integration
// Copyright (C) 2025 Fabian Schmieder
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//

namespace SnapDog.Analyzers;

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LoggerMessageCodeFixProvider))]
[Shared]
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
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            switch (diagnostic.Id)
            {
                case "SNAPDOG001":
                    RegisterPositionalParametersFix(context, root, node, diagnostic);
                    break;
                case "SNAPDOG002":
                    RegisterMoveToEndFix(context, root, node, diagnostic);
                    break;
            }
        }
    }

    private static void RegisterPositionalParametersFix(
        CodeFixContext context,
        SyntaxNode root,
        SyntaxNode node,
        Diagnostic diagnostic
    )
    {
        var attribute = node.FirstAncestorOrSelf<AttributeSyntax>();
        if (attribute?.ArgumentList?.Arguments == null)
        {
            return;
        }

        var action = CodeAction.Create(
            title: "Convert to named parameters",
            createChangedDocument: _ => ConvertToNamedParameters(context.Document, root, attribute),
            equivalenceKey: "ConvertToNamedParameters"
        );

        context.RegisterCodeFix(action, diagnostic);
    }

    private static void RegisterMoveToEndFix(
        CodeFixContext context,
        SyntaxNode root,
        SyntaxNode node,
        Diagnostic diagnostic
    )
    {
        var method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (method == null)
        {
            return;
        }

        var action = CodeAction.Create(
            title: "Move LoggerMessage method to end of class",
            createChangedDocument: _ => MoveMethodToEnd(context.Document, root, method),
            equivalenceKey: "MoveLoggerMessageToEnd"
        );

        context.RegisterCodeFix(action, diagnostic);
    }

    private static Task<Document> ConvertToNamedParameters(
        Document document,
        SyntaxNode root,
        AttributeSyntax attribute
    )
    {
        var arguments = attribute.ArgumentList!.Arguments;
        var newArguments = SyntaxFactory.SeparatedList<AttributeArgumentSyntax>();

        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];

            // If already named, keep as is
            if (argument.NameEquals != null || argument.NameColon != null)
            {
                newArguments = newArguments.Add(argument);
                continue;
            }

            // Convert positional to named based on position
            var namedArgument = i switch
            {
                0 => argument.WithNameEquals(SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName("EventId"))),
                1 => argument.WithNameEquals(SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName("Level"))),
                2 => argument.WithNameEquals(SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName("Message"))),
                _ => argument, // Keep any additional arguments as-is
            };

            newArguments = newArguments.Add(namedArgument);
        }

        var newArgumentList = SyntaxFactory.AttributeArgumentList(newArguments);
        var newAttribute = attribute.WithArgumentList(newArgumentList);
        var newRoot = root.ReplaceNode(attribute, newAttribute);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static Task<Document> MoveMethodToEnd(
        Document document,
        SyntaxNode root,
        MethodDeclarationSyntax method
    )
    {
        var containingClass = method.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (containingClass == null)
        {
            return Task.FromResult(document);
        }

        // Get all members and find LoggerMessage methods
        var members = containingClass.Members.ToList();
        var loggerMessageMethods = members.OfType<MethodDeclarationSyntax>().Where(IsLoggerMessageMethod).ToList();

        // Remove all LoggerMessage methods from their current positions
        var newMembers = members
            .Where(m => !(m is MethodDeclarationSyntax methodSyntax && IsLoggerMessageMethod(methodSyntax)))
            .ToList();

        // Add all LoggerMessage methods at the end
        newMembers.AddRange(loggerMessageMethods);

        var newClass = containingClass.WithMembers(SyntaxFactory.List(newMembers));
        var newRoot = root.ReplaceNode(containingClass, newClass);

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }

    private static bool IsLoggerMessageMethod(MethodDeclarationSyntax method)
    {
        return method.AttributeLists.SelectMany(al => al.Attributes).Any(IsLoggerMessageAttribute);
    }

    private static bool IsLoggerMessageAttribute(AttributeSyntax attribute)
    {
        var name = attribute.Name.ToString();
        return name == "LoggerMessage"
            || name == "LoggerMessageAttribute"
            || name.EndsWith(".LoggerMessage")
            || name.EndsWith(".LoggerMessageAttribute");
    }
}
