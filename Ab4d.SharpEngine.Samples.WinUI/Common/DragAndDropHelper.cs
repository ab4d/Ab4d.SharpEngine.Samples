// ----------------------------------------------------------------
// <copyright file="DragAndDropHelper.cs" company="AB4D d.o.o.">
//     Copyright (c) AB4D d.o.o.  All Rights Reserved
// </copyright>
// ----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace Ab4d.SharpEngine.Samples.WinUI.Common
{
    public class FileDroppedEventArgs : EventArgs
    {
        public string FileName;

        public FileDroppedEventArgs(string fileName)
        {
            FileName = fileName;
        }
    }

    public class DragAndDropHelper
    {
        private readonly string[]? _allowedFileExtensions;

        public event EventHandler<FileDroppedEventArgs>? FileDropped;

        // set usePreviewEvents to true for TextBox or some other controls that handle dropping by themselves
        public DragAndDropHelper(FrameworkElement controlToAddDragAndDrop, string? allowedFileExtensions = null)
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

            controlToAddDragAndDrop.AllowDrop = true;

            controlToAddDragAndDrop.DragEnter += ControlToAddDragAndDropOnDragEnter;
            controlToAddDragAndDrop.DragOver += pageToAddDragAndDrop_DragOver;
            controlToAddDragAndDrop.Drop += pageToAddDragAndDrop_Drop;
        }

        private async void ControlToAddDragAndDropOnDragEnter(object sender, DragEventArgs args)
        {
            await HandleDragEvent(args);
        }

        public async void pageToAddDragAndDrop_DragOver(object sender, DragEventArgs args)
        {
            await HandleDragEvent(args);
        }

        public async void pageToAddDragAndDrop_Drop(object sender, DragEventArgs args)
        {
            if (!args.DataView.AvailableFormats.Contains(StandardDataFormats.StorageItems))
                return;

            IReadOnlyList<IStorageItem> files = await args.DataView.GetStorageItemsAsync();

            if (files != null && files.Count > 0)
            {
                var firstFile = files[0];

                OnFileDropped(firstFile.Path);
                args.Handled = true;
            }
        }

        private async Task HandleDragEvent(DragEventArgs args)
        {
            if (!args.DataView.AvailableFormats.Contains(StandardDataFormats.StorageItems))
                return;

            IReadOnlyList<IStorageItem> files = await args.DataView.GetStorageItemsAsync();

            if (files != null && files.Count > 0)
            {
                var firstFile = files[0];
                var fileName = firstFile.Name;
                var fileExtension = System.IO.Path.GetExtension(fileName);

                if (_allowedFileExtensions == null)
                {
                    args.AcceptedOperation = DataPackageOperation.Copy;
                }
                else
                {
                    if (_allowedFileExtensions.Any(e => e.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)))
                        args.AcceptedOperation = DataPackageOperation.Copy;
                }
            }

            if (args.AcceptedOperation != DataPackageOperation.None)
                args.Handled = true;
        }

        protected void OnFileDropped(string fileName)
        {
            if (FileDropped != null)
                FileDropped(this, new FileDroppedEventArgs(fileName));
        }
    }
}