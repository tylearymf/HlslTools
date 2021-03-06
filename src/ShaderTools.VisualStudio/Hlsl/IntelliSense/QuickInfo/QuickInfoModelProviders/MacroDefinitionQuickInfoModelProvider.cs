using System.ComponentModel.Composition;
using ShaderTools.Hlsl.Compilation;
using ShaderTools.Hlsl.Syntax;

namespace ShaderTools.VisualStudio.Hlsl.IntelliSense.QuickInfo.QuickInfoModelProviders
{
    [Export(typeof(IQuickInfoModelProvider))]
    internal sealed class MacroDefinitionQuickInfoModelProvider : QuickInfoModelProvider<DefineDirectiveTriviaSyntax>
    {
        protected override QuickInfoModel CreateModel(SemanticModel semanticModel, SourceLocation position, DefineDirectiveTriviaSyntax node)
        {
            if (!node.MacroName.SourceRange.ContainsOrTouches(position))
                return null;

            if (!node.MacroName.Span.IsInRootFile)
                return null;

            return QuickInfoModel.ForMacroDefinition(semanticModel, node.MacroName.Span, node);
        }
    }
}