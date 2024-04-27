// ----------------------------------------------------------------
// <copyright file="DragAndDropHelper.cs" company="AB4D d.o.o.">
//     Copyright (c) AB4D d.o.o.  All Rights Reserved
// </copyright>
// ----------------------------------------------------------------

using Avalonia.Controls;
using System;
using System.Linq;
using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace Ab4d.SharpEngine.Samples.AvaloniaUI.Common;

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
    
    private Control? _dragDropControl;

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

            if (_dragDropControl != null)
                DragDrop.SetAllowDrop(_dragDropControl, value);
        }
    }

    public DragAndDropHelper(Control dragDropControl, string? allowedFileExtensions = null)
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

        _dragDropControl = dragDropControl;

        dragDropControl.AddHandler(DragDrop.DragOverEvent, AvaloniaDragOverHandler);
        dragDropControl.AddHandler(DragDrop.DropEvent, AvaloniaDropHandler);
        
        // No need to handle DragEnterEvent and DragLeaveEvent
        //dragDropControl.AddHandler(DragDrop.DragEnterEvent, AvaloniaDragEnterHandler);
        //dragDropControl.AddHandler(DragDrop.DragLeaveEvent, AvaloniaDragLeaveHandler);

        DragDrop.SetAllowDrop(dragDropControl, true);
        _isEnabled = true;
    }

    private void AvaloniaDragOverHandler(object? sender, DragEventArgs e)
    {
        var firstStorageItem = GetFirstStorageItem(e);

        if (firstStorageItem == null || !_isEnabled)
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        var fileName = firstStorageItem.Name;
        var fileExtension = System.IO.Path.GetExtension(fileName);

        if (_allowedFileExtensions == null || _allowedFileExtensions.Any(ext => ext.Equals(fileExtension, StringComparison.OrdinalIgnoreCase)))
            e.DragEffects = DragDropEffects.Move;
        else
            e.DragEffects = DragDropEffects.None;

        e.Handled = true;
    }

    //private void AvaloniaDragEnterHandler(object? sender, DragEventArgs e)
    //{

    //}

    //private void AvaloniaDragLeaveHandler(object? sender, DragEventArgs e)
    //{
            
    //}

    private void AvaloniaDropHandler(object? sender, DragEventArgs e)
    {
        var firstStorageItem = GetFirstStorageItem(e);

        if (firstStorageItem != null && _isEnabled)
        {
            var localPath = firstStorageItem.TryGetLocalPath();

            if (localPath != null)
            {
                OnFileDropped(localPath);
                e.Handled = true;
            }
        }
    }

    protected void OnFileDropped(string fileName)
    {
        if (FileDropped != null)
            FileDropped(this, new FileDroppedEventArgs(fileName));
    }
    
    private IStorageItem? GetFirstStorageItem(DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            var dropFileNames = e.Data.GetFiles();
            if (dropFileNames != null)
            {
                var storageItem = dropFileNames.FirstOrDefault();
                return storageItem;
            }
        }

        return null;
    }

    public void Dispose()
    {
        if (_dragDropControl == null)
            return;

        _dragDropControl.AddHandler(DragDrop.DragOverEvent, AvaloniaDragOverHandler);
        _dragDropControl.AddHandler(DragDrop.DropEvent, AvaloniaDropHandler);

        //_dragDropControl.AddHandler(DragDrop.DragEnterEvent, AvaloniaDragEnterHandler);
        //_dragDropControl.AddHandler(DragDrop.DragLeaveEvent, AvaloniaDragLeaveHandler);

        DragDrop.SetAllowDrop(_dragDropControl, true);
        _isEnabled = false;

        _dragDropControl = null;
    }
}
