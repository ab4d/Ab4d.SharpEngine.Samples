using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.Vulkan;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Popups;

namespace Ab4d.SharpEngine.Samples.WinUI.Common
{
    /// <summary>
    /// Interaction logic for CommonTitleUserControl.xaml
    /// </summary>
    public partial class CommonTitleUserControl : UserControl
    {
        private string? _markdownText;
        private readonly WinUIMarkdownTextCreator _winUIMarkdownTextCreator;

        public string? MarkdownText
        {
            get => _markdownText;
            set
            {
                _markdownText = value;
                UpdateMarkdownText();
            }
        }

        public CommonTitleUserControl()
        {
            InitializeComponent();

            _winUIMarkdownTextCreator = new WinUIMarkdownTextCreator();
            _winUIMarkdownTextCreator.ErrorWriterAction = ErrorWriterAction;

            var textBlock = _winUIMarkdownTextCreator.Create("");

            MarkdownScrollViewer.Content = textBlock;
        }

        private async void ErrorWriterAction(string errorMessage)
        {
            var messageDialog = new MessageDialog("Markdown error:\r\n" + errorMessage);
            await messageDialog.ShowAsync();
        }

        private void UpdateMarkdownText()
        {
            if (string.IsNullOrEmpty(_markdownText))
            {
                MarkdownScrollViewer.Content = null;
                return;
            }

            _winUIMarkdownTextCreator.Update(_markdownText);
        }
    }
}
