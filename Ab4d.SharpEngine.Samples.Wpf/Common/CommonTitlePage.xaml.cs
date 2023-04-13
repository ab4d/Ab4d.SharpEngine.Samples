using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.Common;
using Ab4d.SharpEngine.Samples.Wpf.UIProvider;
using Ab4d.SharpEngine.Wpf;
using Ab4d.Vulkan;

namespace Ab4d.SharpEngine.Samples.Wpf.Common
{
    /// <summary>
    /// Interaction logic for CommonTitlePage.xaml
    /// </summary>
    public partial class CommonTitlePage : Page
    {
        private string? _markdownText;
        private readonly WpfMarkdownTextCreator _wpfMarkdownTextCreator;

        public string? MarkdownText
        {
            get => _markdownText;
            set
            {
                _markdownText = value;
                UpdateMarkdownText();
            }
        }

        public CommonTitlePage()
        {
            InitializeComponent();

            _wpfMarkdownTextCreator = new WpfMarkdownTextCreator();
            _wpfMarkdownTextCreator.ErrorWriterAction = delegate (string errorMessage) { MessageBox.Show("Markdown error:\r\n" + errorMessage); };

            var textBlock = _wpfMarkdownTextCreator.Create("");

            MarkdownScrollViewer.Content = textBlock;
        }

        private void UpdateMarkdownText()
        {
            if (string.IsNullOrEmpty(_markdownText))
            {
                MarkdownScrollViewer.Content = null;
                return;
            }

            _wpfMarkdownTextCreator.Update(_markdownText);
        }
    }
}
