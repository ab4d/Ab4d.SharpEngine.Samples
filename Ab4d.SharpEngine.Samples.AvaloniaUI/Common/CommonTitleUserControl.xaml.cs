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
using Avalonia.Controls;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.Common
{
    /// <summary>
    /// Interaction logic for CommonTitleUserControl.xaml
    /// </summary>
    public partial class CommonTitleUserControl : UserControl
    {
        private string? _markdownText;
        private readonly AvaloniaMarkdownTextCreator _markdownTextCreator;

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

            _markdownTextCreator = new AvaloniaMarkdownTextCreator();
            _markdownTextCreator.ErrorWriterAction = delegate (string errorMessage)
            {
                System.Diagnostics.Debug.WriteLine("Markdown error:\r\n" + errorMessage);
            };

            var textBlock = _markdownTextCreator.Create("");

            MarkdownScrollViewer.Content = textBlock;
        }

        private void UpdateMarkdownText()
        {
            if (string.IsNullOrEmpty(_markdownText))
            {
                MarkdownScrollViewer.Content = null;
                return;
            }

            _markdownTextCreator.Update(_markdownText);
        }
    }
}
