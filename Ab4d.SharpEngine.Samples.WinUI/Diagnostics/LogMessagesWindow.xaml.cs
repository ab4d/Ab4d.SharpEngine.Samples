﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Ab4d.SharpEngine.Common;
using Ab4d.SharpEngine.Samples.WinUI.Common;
using Ab4d.Vulkan;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Ab4d.SharpEngine.Samples.WinUI.Diagnostics
{
    /// <summary>
    /// Interaction logic for LogMessagesWindow.xaml
    /// </summary>
    public partial class LogMessagesWindow : Window
    {
        private const int MaxLogMessages = 200;

        private int _deletedLogMessagesCount;

        private volatile bool _isUpdateLogMessagesCalled;

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

            WinUiUtils.SetWindowIcon(this, @"Assets\sharp-engine-logo.ico");
        }

        public void UpdateLogMessages()
        {
            // If we are not on UI thread and we did not yet called UpdateLogMessages ...
            if (!this.DispatcherQueue.HasThreadAccess && !_isUpdateLogMessagesCalled)
            {
                // ... then use Dispatcher.InvokeAsync to call UpdateLogMessages on the UI thread 
                _isUpdateLogMessagesCalled = true;
                this.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, UpdateLogMessages);
                return;
            }

            _isUpdateLogMessagesCalled = false;


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
            if (InfoTextBox == null)
                return;

            InfoTextBox.TextWrapping = (WordWrapCheckBox.IsChecked ?? false) ? TextWrapping.Wrap : TextWrapping.NoWrap;
        }
    }
}
