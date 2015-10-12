using System.ComponentModel.Composition;
using HlslTools.VisualStudio.IntelliSense.QuickInfo.QuickInfoModelProviders;
using HlslTools.VisualStudio.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;

namespace HlslTools.VisualStudio.IntelliSense.QuickInfo
{
    [Export]
    internal sealed class QuickInfoManagerProvider
    {
        [Import]
        public IQuickInfoBroker QuickInfoBroker { get; set; }

        [Import]
        public QuickInfoModelProviderService QuickInfoModelProviderService { get; set; }

        [Import]
        public VisualStudioSourceTextFactory SourceTextFactory { get; set; }

        public QuickInfoManager GetQuickInfoManager(ITextView textView)
        {
            return textView.Properties.GetOrCreateSingletonProperty(() => new QuickInfoManager(textView, QuickInfoBroker, QuickInfoModelProviderService, SourceTextFactory));
        }
    }
}