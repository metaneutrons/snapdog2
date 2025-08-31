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
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SnapDog.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LoggerMessageAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor UseNamedParametersRule = new(
        "SNAPDOG001",
        "LoggerMessage should use named parameters",
        "LoggerMessage attribute should use named parameters instead of positional parameters",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "LoggerMessage attributes should use named parameters for better readability and maintainability."
    );

    public static readonly DiagnosticDescriptor MoveToEndOfClassRule = new(
        "SNAPDOG002",
        "LoggerMessage methods should be moved to end of class",
        "LoggerMessage method '{0}' should be moved to the end of the class",
        "Organization",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "LoggerMessage methods should be grouped together at the end of the class for better organization."
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(UseNamedParametersRule, MoveToEndOfClassRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        // Find LoggerMessage attribute
        var loggerMessageAttribute = methodDeclaration
            .AttributeLists.SelectMany(al => al.Attributes)
            .FirstOrDefault(attr => IsLoggerMessageAttribute(attr));

        if (loggerMessageAttribute == null)
        {
            return;
        }

        // Check for positional parameters that should be named
        if (HasPositionalParameters(loggerMessageAttribute))
        {
            var diagnostic = Diagnostic.Create(
                UseNamedParametersRule,
                loggerMessageAttribute.GetLocation(),
                methodDeclaration.Identifier.ValueText
            );
            context.ReportDiagnostic(diagnostic);
        }

        // Check if method should be moved to end of class
        if (ShouldMoveToEndOfClass(methodDeclaration))
        {
            var diagnostic = Diagnostic.Create(
                MoveToEndOfClassRule,
                methodDeclaration.Identifier.GetLocation(),
                methodDeclaration.Identifier.ValueText
            );
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsLoggerMessageAttribute(AttributeSyntax attribute)
    {
        var name = attribute.Name.ToString();
        return name == "LoggerMessage"
            || name == "LoggerMessageAttribute"
            || name.EndsWith(".LoggerMessage")
            || name.EndsWith(".LoggerMessageAttribute");
    }

    private static bool HasPositionalParameters(AttributeSyntax attribute)
    {
        if (attribute.ArgumentList?.Arguments == null)
        {
            return false;
        }

        // Check if any arguments are positional (not named)
        return attribute.ArgumentList.Arguments.Any(arg => arg.NameEquals == null && arg.NameColon == null);
    }

    private static bool ShouldMoveToEndOfClass(MethodDeclarationSyntax method)
    {
        // Get the containing class
        var containingClass = method.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (containingClass == null)
        {
            return false;
        }

        // Get all members of the class
        var members = containingClass.Members.ToList();
        var methodIndex = members.IndexOf(method);

        if (methodIndex == -1)
        {
            return false;
        }

        // Check if there are any non-LoggerMessage members after this method
        for (var i = methodIndex + 1; i < members.Count; i++)
        {
            if (members[i] is MethodDeclarationSyntax otherMethod)
            {
                // If there's a non-LoggerMessage method after this one, suggest moving
                var hasLoggerMessage = otherMethod
                    .AttributeLists.SelectMany(al => al.Attributes)
                    .Any(IsLoggerMessageAttribute);

                if (!hasLoggerMessage)
                {
                    return true;
                }
            }
            else
            {
                // If there are other types of members (properties, fields, etc.) after this method
                return true;
            }
        }

        return false;
    }
}
