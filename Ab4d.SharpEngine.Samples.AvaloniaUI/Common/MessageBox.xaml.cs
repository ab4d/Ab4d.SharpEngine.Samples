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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

// Based on https://stackoverflow.com/questions/55706291/how-to-show-a-message-box-in-avaloniaui-beta

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.Common
{
    /// <summary>
    /// Interaction logic for MessageBox.xaml
    /// </summary>
    public partial class MessageBox : Window
    {
        public enum MessageBoxButtons
        {
            Ok,
            OkCancel,
            YesNo,
            YesNoCancel
        }

        public enum MessageBoxResult
        {
            Ok,
            Cancel,
            Yes,
            No
        }

        public MessageBox()
        {
            InitializeComponent();
        }

        public static Task<MessageBoxResult> Show(string text, string title, MessageBoxButtons buttons)
        {
            Window? parentWindow;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                parentWindow = desktopLifetime.MainWindow;
            else
                parentWindow = null;

            return Show(parentWindow, text, title, buttons);
        }

        public static Task<MessageBoxResult> Show(Window? parent, string text, string title, MessageBoxButtons buttons)
        {
            var msgbox = new MessageBox()
            {
                Title = title
            };

            msgbox.MessageTextBlock.Text = text;

            var res = MessageBoxResult.Ok;

            void AddButton(string caption, MessageBoxResult r, bool def = false)
            {
                var btn = new Button
                {
                    Content = caption,
                    Margin = new Thickness(10, 0, 0, 0),
                    Padding = new Thickness(3, 2),
                    MinWidth = 60
                };

                btn.Click += (_, __) => {
                    res = r;
                    msgbox.Close();
                };

                msgbox.ButtonsStackPanel.Children.Add(btn);

                if (def)
                    res = r;
            }

            if (buttons == MessageBoxButtons.Ok || buttons == MessageBoxButtons.OkCancel)
                AddButton("Ok", MessageBoxResult.Ok, true);

            if (buttons == MessageBoxButtons.YesNo || buttons == MessageBoxButtons.YesNoCancel)
            {
                AddButton("Yes", MessageBoxResult.Yes);
                AddButton("No", MessageBoxResult.No, true);
            }

            if (buttons == MessageBoxButtons.OkCancel || buttons == MessageBoxButtons.YesNoCancel)
                AddButton("Cancel", MessageBoxResult.Cancel, true);


            var tcs = new TaskCompletionSource<MessageBoxResult>();
            msgbox.Closed += delegate { tcs.TrySetResult(res); };

            if (parent != null)
                msgbox.ShowDialog(parent);
            else 
                msgbox.Show();

            return tcs.Task;
        }


    }
}
