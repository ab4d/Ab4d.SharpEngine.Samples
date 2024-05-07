// ----------------------------------------------------------------
// <copyright file="DragAndDropHelper.cs" company="AB4D d.o.o.">
//     Copyright (c) AB4D d.o.o.  All Rights Reserved
// </copyright>
// ----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace Ab4d.SharpEngine.Samples.Wpf.Common
{
    public class FileDroppedEventArgs : EventArgs
    {
        public string FileName;

        public FileDroppedEventArgs(string fileName)
        {
            FileName = fileName;
        }
    }

    public class DragAndDropHelper : IDisposable
    {
        private readonly string[]? _allowedFileExtensions;

        private bool _usePreviewEvents;
        private FrameworkElement? _subscribedFrameworkElement;

        public event EventHandler<FileDroppedEventArgs>? FileDropped;

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                    return;

                _isEnabled = value;

                if (_subscribedFrameworkElement != null)
                    _subscribedFrameworkElement.AllowDrop = true;
            }
        }

        // set usePreviewEvents to true for TextBox or some other controls that handle dropping by themselves
        public DragAndDropHelper(FrameworkElement controlToAddDragAndDrop, string? allowedFileExtensions = null, bool usePreviewEvents = false)
        {
            if (string.IsNullOrEmpty(allowedFileExtensions) || allowedFileExtensions == "*" || allowedFileExtensions == ".*")
            {
                // no filter
                _allowedFileExtensions = null;
            }
            else
            {
                _allowedFileExtensions = allowedFileExtensions.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                                              .Select(e => e.Trim())
                                                              .Select(e => e.StartsWith(".") ? e : '.' + e) // make sure that each extension has leading '.' because Path.GetExtension also gets such extension
                                                              .ToArray();
            }
            
            if (usePreviewEvents)
            {
                controlToAddDragAndDrop.PreviewDrop += OnDrop;
                controlToAddDragAndDrop.PreviewDragOver += OnDragOver;
            }
            else
            {
                controlToAddDragAndDrop.Drop += OnDrop;
                controlToAddDragAndDrop.DragOver += OnDragOver;
            }

            controlToAddDragAndDrop.AllowDrop = true;
            _isEnabled = true;

            _subscribedFrameworkElement = controlToAddDragAndDrop;
            _usePreviewEvents = usePreviewEvents;
        }

        public void OnDragOver(object sender, DragEventArgs args)
        {
            if (!_isEnabled)
                return;

            args.Effects = DragDropEffects.None;

            if (args.Data.GetDataPresent("FileNameW"))
            {
                var dropData = args.Data.GetData("FileNameW");

                var dropFileNames = dropData as string[];
                if (dropFileNames != null && dropFileNames.Length > 0)
                {
                    var fileName = dropFileNames[0];
                    var fileExtension = System.IO.Path.GetExtension(fileName);

                    if (_allowedFileExtensions == null || _allowedFileExtensions.Any(e => e.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)))
                        args.Effects = DragDropEffects.Move;
                }
            }

            args.Handled = true;
        }

        public void OnDrop(object sender, DragEventArgs args)
        {
            if (!_isEnabled)
                return;

            if (args.Data.GetDataPresent("FileNameW"))
            {
                var dropData = args.Data.GetData("FileNameW");

                var dropFileNames = dropData as string[];
                if (dropFileNames != null && dropFileNames.Length > 0)
                {
                    OnFileDropped(dropFileNames[0]);
                    args.Handled = true;
                }
            }
        }

        protected void OnFileDropped(string fileName)
        {
            if (FileDropped != null)
                FileDropped(this, new FileDroppedEventArgs(fileName));
        }

        public void Dispose()
        {
            if (_subscribedFrameworkElement == null)
                return;

            if (_usePreviewEvents)
            {
                _subscribedFrameworkElement.PreviewDrop -= OnDrop;
                _subscribedFrameworkElement.PreviewDragOver -= OnDragOver;
            }
            else
            {
                _subscribedFrameworkElement.Drop -= OnDrop;
                _subscribedFrameworkElement.DragOver -= OnDragOver;
            }

            _subscribedFrameworkElement = null;
        }
    }
}