using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Ab4d.SharpEngine.Common;
using Ab4d.Vulkan;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.Diagnostics
{
    /// <summary>
    /// Interaction logic for LogMessagesWindow.xaml
    /// </summary>
    public partial class LogMessagesWindow : Window
    {
        private const int MaxLogMessages = 200;

        private int _deletedLogMessagesCount;

        private List<Tuple<LogLevels, string>>? _logMessages;

        public List<Tuple<LogLevels, string>>? LogMessages
        {
            get
            {
                return _logMessages;
            }

            set
            {
                _logMessages = value;
                UpdateLogMessages();
            }
        }

        public Action? OnLogMessagesClearedAction;
        
        public LogMessagesWindow()
        {
            InitializeComponent();
        }

        public void UpdateLogMessages()
        {
            if (_logMessages == null || _logMessages.Count == 0)
            {
                InfoTextBox.Text = "";
                return;
            }

            if (_logMessages.Count >= MaxLogMessages)
            {
                // remove first 1/10 of messages
                int logMessagesToDelete = (int)(MaxLogMessages / 10);
                _logMessages.RemoveRange(0, logMessagesToDelete);

                _deletedLogMessagesCount += logMessagesToDelete;
            }

            var sb = new StringBuilder();

            int indexOffset = _deletedLogMessagesCount + 1;

            for (var i = _logMessages.Count - 1; i >= 0; i--)
                sb.AppendFormat("{0,2}  {1}: {2}\r\n\r\n", i + indexOffset, _logMessages[i].Item1, _logMessages[i].Item2);

            InfoTextBox.Text = sb.ToString();
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            OnLogMessagesClearedAction?.Invoke();
            
            this.Close();
        }

        private void OnWordWrapCheckBoxCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            InfoTextBox.TextWrapping = (WordWrapCheckBox.IsChecked ?? false) ? TextWrapping.Wrap : TextWrapping.NoWrap;
        }
    }
}
