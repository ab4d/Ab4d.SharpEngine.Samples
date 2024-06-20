// ----------------------------------------------------------------
// <copyright file="DragAndDropHelper.cs" company="AB4D d.o.o.">
//     Copyright (c) AB4D d.o.o.  All Rights Reserved
// </copyright>
// ----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Ab4d.SharpEngine.Samples.WinForms.Common
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

        private Control? _subscribedControl;

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

                if (_subscribedControl != null)
                    _subscribedControl.AllowDrop = true;
            }
        }

        // set usePreviewEvents to true for TextBox or some other controls that handle dropping by themselves
        public DragAndDropHelper(Control controlToAddDragAndDrop, string? allowedFileExtensions = null)
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
            
            controlToAddDragAndDrop.DragOver += OnDragOver;
            controlToAddDragAndDrop.DragDrop += OnDragDrop;

            controlToAddDragAndDrop.AllowDrop = true;
            _isEnabled = true;

            _subscribedControl = controlToAddDragAndDrop;
        }

        public void OnDragOver(object? sender, DragEventArgs args)
        {
            if (!_isEnabled)
                return;

            args.Effect = DragDropEffects.None;

            if (args.Data != null && args.AllowedEffect.HasFlag(DragDropEffects.Move) && args.Data.GetDataPresent("FileNameW"))
            {
                var dropData = args.Data.GetData("FileNameW");

                if (dropData is string[] dropFileNames && dropFileNames.Length > 0)
                {
                    var fileName = dropFileNames[0];
                    var fileExtension = System.IO.Path.GetExtension(fileName);

                    if (_allowedFileExtensions == null ||
                        _allowedFileExtensions.Any(e => e.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)))
                    {
                        args.Effect = DragDropEffects.Move;
                        args.DropImageType = DropImageType.Move;
                    }
                }
            }
        }

        public void OnDragDrop(object? sender, DragEventArgs args)
        {
            if (!_isEnabled)
                return;

            if (args.Data != null && args.Data.GetDataPresent("FileNameW"))
            {
                var dropData = args.Data.GetData("FileNameW");

                if (dropData is string[] dropFileNames && dropFileNames.Length > 0)
                    OnFileDropped(dropFileNames[0]);
            }
        }

        protected void OnFileDropped(string fileName)
        {
            if (FileDropped != null)
                FileDropped(this, new FileDroppedEventArgs(fileName));
        }

        public void Dispose()
        {
            if (_subscribedControl == null)
                return;

            _subscribedControl.DragOver -= OnDragOver;
            _subscribedControl.DragDrop -= OnDragDrop;

            _subscribedControl = null;
        }
    }
}