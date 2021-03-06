﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using ShaderTools.Hlsl.Compilation;
using ShaderTools.Hlsl.Symbols;
using ShaderTools.Hlsl.Symbols.Markup;
using ShaderTools.Hlsl.Syntax;
using ShaderTools.VisualStudio.Hlsl.Glyphs;

namespace ShaderTools.VisualStudio.Hlsl.IntelliSense.Completion.CompletionProviders
{
    [Export(typeof(ICompletionProvider))]
    internal sealed class SymbolCompletionProvider : ICompletionProvider
    {
        public IEnumerable<CompletionItem> GetItems(SemanticModel semanticModel, SourceLocation position)
        {
            var root = semanticModel.SyntaxTree.Root;

            // We don't want to show a completions for these cases.
            if (semanticModel.SyntaxTree.PossiblyInUserGivenName(position))
                return Enumerable.Empty<CompletionItem>();
            if (semanticModel.SyntaxTree.DefinitelyInMacro(position))
                return Enumerable.Empty<CompletionItem>();
            if (semanticModel.SyntaxTree.DefinitelyInVariableDeclaratorQualifier(position))
                return Enumerable.Empty<CompletionItem>();

            // Comments and literals don't get completion information
            if (root.InComment(position) || root.InLiteral(position))
                return Enumerable.Empty<CompletionItem>();

            if (semanticModel.SyntaxTree.DefinitelyInTypeName(position))
                return GetTypeCompletions(semanticModel, position);

            var propertyAccessExpression = GetPropertyAccessExpression(root, position);
            return propertyAccessExpression == null
                ? GetGlobalCompletions(semanticModel, position)
                : GetMemberCompletions(semanticModel, propertyAccessExpression);
        }

        private static IEnumerable<CompletionItem> GetTypeCompletions(SemanticModel semanticModel, SourceLocation position)
        {
            var symbols = semanticModel.LookupSymbols(position).OfType<TypeSymbol>();
            return CreateSymbolCompletions(symbols);
        }

        private static IEnumerable<CompletionItem> GetGlobalCompletions(SemanticModel semanticModel, SourceLocation position)
        {
            var symbols = semanticModel.LookupSymbols(position)
                .Where(x => !(x is SemanticSymbol))
                .Where(x => !(x is AttributeSymbol));

            if (!semanticModel.SyntaxTree.PossiblyInTypeName(position))
                symbols = symbols.Where(x => !(x is TypeSymbol));

            return CreateSymbolCompletions(symbols);
        }

        private static IEnumerable<CompletionItem> GetMemberCompletions(SemanticModel semanticModel, FieldAccessExpressionSyntax propertyAccessExpression)
        {
            var targetType = semanticModel.GetExpressionType(propertyAccessExpression.Expression);
            if (targetType != null && !targetType.IsUnknown() && !targetType.IsError())
                return GetTypeCompletions(targetType);

            return Enumerable.Empty<CompletionItem>();
        }

        private static IEnumerable<CompletionItem> GetTypeCompletions(TypeSymbol targetType)
        {
            return CreateSymbolCompletions(targetType.Members);
        }

        private static FieldAccessExpressionSyntax GetPropertyAccessExpression(SyntaxNode root, SourceLocation position)
        {
            var token = root.FindTokenOnLeft(position);
            var previous = token.GetPreviousToken(false, true);
            var dot = previous != null && previous.Kind == SyntaxKind.DotToken
                          ? previous
                          : token;

            var p = dot.Parent.AncestorsAndSelf().OfType<FieldAccessExpressionSyntax>().FirstOrDefault();

            if (p != null)
            {
                var afterDot = p.DotToken.SourceRange.End <= position && position <= p.Name.SourceRange.End;
                if (afterDot)
                    return p;
            }

            return null;
        }

        private static IEnumerable<CompletionItem> CreateSymbolCompletions(IEnumerable<Symbol> symbols)
        {
            return symbols
                .GroupBy(s => s.Name)
                .Select(g => CreateSymbolCompletionGroup(g.Key, g.ToImmutableArray()));
        }

        private static CompletionItem CreateSymbolCompletionGroup(string name, ImmutableArray<Symbol> symbols)
        {
            var multiple = symbols.Skip(1).Any();
            if (!multiple)
                return CreateSymbolCompletion(symbols.First());

            var hasNonInvocables = symbols.Any(s => !(s is InvocableSymbol));
            if (!hasNonInvocables)
                return CreateInvocableCompletionGroup(symbols);

            if (symbols.All(s => (s is TypeSymbol && ((TypeSymbol) s).IsIntrinsicNumericType())
                || (s is FunctionSymbol && ((FunctionSymbol) s).IsNumericConstructor)))
                return CreateSymbolCompletion(symbols.First(s => s is TypeSymbol));

            var displayText = name;
            var insertionText = name;

            var sb = new StringBuilder();
            sb.Append(Resources.AmbiguousName);
            foreach (var symbol in symbols)
            {
                sb.AppendLine();
                sb.Append(@"  ");
                sb.Append(symbol);
            }

            var description = sb.ToString();
            return new CompletionItem(displayText, insertionText, description, Glyph.CompletionWarning);
        }

        private static CompletionItem CreateInvocableCompletionGroup(ImmutableArray<Symbol> symbols)
        {
            var symbol = symbols.First();
            var first = CreateSymbolCompletion(symbol);
            var numberOfOverloads = symbols.Count() - 1;

            var displayText = first.DisplayText;
            var insertionText = first.InsertionText;
            var description = string.Format(Resources.CompletionItemWithOverloads, first.Description, numberOfOverloads);
            if (!string.IsNullOrEmpty(symbol.Documentation))
                description += Environment.NewLine + symbol.Documentation;
            var glyph = first.Glyph;
            return new CompletionItem(displayText, insertionText, description, glyph, symbol);
        }

        private static CompletionItem CreateSymbolCompletion(Symbol symbol)
        {
            var displayText = symbol.Name;
            var insertionText = symbol.Name;
            var description = SymbolMarkup.ForSymbol(symbol).ToString();
            var glyph = symbol.GetGlyph();
            return new CompletionItem(displayText, insertionText, description, glyph, symbol);
        }
    }
}