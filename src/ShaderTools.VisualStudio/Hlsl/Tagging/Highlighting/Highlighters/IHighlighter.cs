﻿using System.Collections.Generic;
using ShaderTools.Hlsl.Compilation;
using ShaderTools.Hlsl.Syntax;

namespace ShaderTools.VisualStudio.Hlsl.Tagging.Highlighting.Highlighters
{
    internal interface IHighlighter
    {
        IEnumerable<HighlightSpan> GetHighlights(SemanticModel semanticModel, SourceLocation position);
    }
}