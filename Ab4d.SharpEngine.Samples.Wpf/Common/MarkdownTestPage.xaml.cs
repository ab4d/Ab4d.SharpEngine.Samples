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
    /// Interaction logic for MarkdownTestPage.xaml
    /// </summary>
    public partial class MarkdownTestPage : Page
    {
        private WpfMarkdownTextCreator? _wpfMarkdownTextCreator;

        private string? _lastErrorMessage;

        public MarkdownTestPage()
        {
            InitializeComponent();

            InputTextBox.Text =
@"# Heading 1
## Heading 2
### Heading 3

Enter text below to test the **MarkdownTextCreator**.
- bullet
+ bullet**2**
* bullet**3**

Test `code` and multi line code:```
    int a = 1;
    int b = a + 1;```

Test new line\nin one line. Test \t\ttabs.

Test link without url [ab4d] and with url: [ab4d.com](https://www.ab4d.com)

Test image:
![Tree texture](Resources/Textures/TreeTexture.png)
";

            _wpfMarkdownTextCreator = new WpfMarkdownTextCreator();
            _wpfMarkdownTextCreator.ErrorWriterAction = delegate(string errorMessage) { _lastErrorMessage = errorMessage; };

            var textBlock = _wpfMarkdownTextCreator.Create(InputTextBox.Text);

            MarkdownScrollViewer.Content = textBlock;
        }

        private void InputTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_wpfMarkdownTextCreator == null)
                return;

            _lastErrorMessage = null;
            _wpfMarkdownTextCreator.Update(InputTextBox.Text);

            if (_lastErrorMessage != null)
            {
                ErrorTextBlock.Text = _lastErrorMessage;
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                ErrorTextBlock.Visibility = Visibility.Collapsed;
            }
        }
    }
}
