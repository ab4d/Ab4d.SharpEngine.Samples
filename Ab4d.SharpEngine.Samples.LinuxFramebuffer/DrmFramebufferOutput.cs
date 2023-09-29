/*
 DRM/KMS framebuffer output
 Based on double-buffered DRM/KSM example:
 https://github.com/dvdhrm/docs/blob/master/drm-howto/modeset-double-buffered.c
*/

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using uint16_t = System.UInt16;
using uint32_t = System.UInt32;
using uint64_t = System.UInt64;

namespace Ab4d.SharpEngine.Samples.LinuxFramebuffer;

public unsafe class DrmFramebufferOutput : OutputDevice
{
    private int _deviceFd = -1;
    private Native.Drm.drmModeRes* _drmResources = null;
    private Native.Drm.drmModeConnector* _selectedConnector;

    /* Two framebuffers for double-buffered drawing */
    private int _frontBuffer;
    private Framebuffer ?_framebuffer1;
    private Framebuffer ?_framebuffer2;

    private Native.Drm.drmModeCrtc* _oldCrtc = null;

    /* Logging */
    private readonly ILogger? _logger;

    /* Dictionary for printing connector type in human-readable form */
    private readonly Dictionary<UInt32, string> _connectorTypeNames = new()
    {
        { Native.Drm.DRM_MODE_CONNECTOR_Unknown, "Unknown" },
        { Native.Drm.DRM_MODE_CONNECTOR_VGA, "VGA" },
        { Native.Drm.DRM_MODE_CONNECTOR_DVII, "DVI-I" },
        { Native.Drm.DRM_MODE_CONNECTOR_DVID, "DVI-D" },
        { Native.Drm.DRM_MODE_CONNECTOR_DVIA, "DVI-A" },
        { Native.Drm.DRM_MODE_CONNECTOR_Composite, "Composite" },
        { Native.Drm.DRM_MODE_CONNECTOR_SVIDEO, "S-VIDEO" },
        { Native.Drm.DRM_MODE_CONNECTOR_LVDS, "LVDS" },
        { Native.Drm.DRM_MODE_CONNECTOR_Component, "Component" },
        { Native.Drm.DRM_MODE_CONNECTOR_9PinDIN, "9-Pin DIN" },
        { Native.Drm.DRM_MODE_CONNECTOR_DisplayPort, "DisplayPort" },
        { Native.Drm.DRM_MODE_CONNECTOR_HDMIA, "HDMI-A" },
        { Native.Drm.DRM_MODE_CONNECTOR_HDMIB, "HDMI-B" },
        { Native.Drm.DRM_MODE_CONNECTOR_TV, "TV" },
        { Native.Drm.DRM_MODE_CONNECTOR_eDP, "eDP" },
        { Native.Drm.DRM_MODE_CONNECTOR_VIRTUAL, "VIRTUAL" },
        { Native.Drm.DRM_MODE_CONNECTOR_DSI, "DSI" },
        { Native.Drm.DRM_MODE_CONNECTOR_DPI, "DPI" },
        { Native.Drm.DRM_MODE_CONNECTOR_WRITEBACK, "WRITEBACK" },
        { Native.Drm.DRM_MODE_CONNECTOR_SPI, "SPI" },
        { Native.Drm.DRM_MODE_CONNECTOR_USB, "USB" },
    };

    public static string? FindFirstDevice()
    {
        var files = Directory.GetFiles("/dev/dri");
        Array.Sort(files);
        foreach (var file in files)
        {
            var match = Regex.Match(file, "card[0-9]+");
            if (!match.Success)
            {
                continue;
            }

            var fd = Native.LibC.open(file, Native.LibC.O_RDWR | Native.LibC.O_CLOEXEC, 0);
            if (fd == -1)
            {
                continue;
            }

            Native.LibC.close(fd);
            return file;
        }

        return null;
    }

    public DrmFramebufferOutput(ILoggerFactory? loggerFactory = null)
    {
        _logger = loggerFactory?.CreateLogger<DrmFramebufferOutput>();
    }

    public override void Initialize(string deviceName, int requestedWidth, int requestedHeight)
    {
        /* Open DRM device */
        _deviceFd = Native.LibC.open(deviceName, Native.LibC.O_RDWR | Native.LibC.O_CLOEXEC, 0);
        if (_deviceFd <= 0)
        {
            Cleanup();
            throw new Exception($"Failed to open DRI device {deviceName}: {Marshal.GetLastWin32Error()}");
        }

        /* Ensure that device supports dumb buffers */
        UInt64 supportsDumpBuffers = 0;
        if (Native.Drm.drmGetCap(_deviceFd, Native.Drm.DRM_CAP_DUMB_BUFFER, &supportsDumpBuffers) < 0 ||
            supportsDumpBuffers == 0)
        {
            Cleanup();
            throw new Exception($"Device does not support dumb buffers for framebuffer mapping!");
        }

        /* Query the DRM resources */
        _drmResources = Native.Drm.drmModeGetResources(_deviceFd);
        if (_drmResources is null)
        {
            Cleanup();
            throw new Exception($"Failed to query DRM resources on  device: {Marshal.GetLastWin32Error()}");
        }

        _logger?.LogInformation("DRM resources on device:");
        _logger?.LogInformation(" - framebuffers: {count}", _drmResources->count_fbs);
        _logger?.LogInformation(" - CRTCs: {count}", _drmResources->count_crtcs);
        _logger?.LogInformation(" - connectors: {count}", _drmResources->count_connectors);
        _logger?.LogInformation(" - encoders: {count}", _drmResources->count_encoders);

        /* Display available connectors info */
        _logger?.LogInformation("Available connectors:");
        for (var i = 0; i < _drmResources->count_connectors; i++)
        {
            var connector = Native.Drm.drmModeGetConnectorCurrent(_deviceFd, _drmResources->connectors[i]);
            if (connector is null)
            {
                continue;
            }

            _connectorTypeNames.TryGetValue(connector->connector_type, out var connectorTypeName);
            _logger?.LogInformation("  {type} #{number}: {state}",
                connectorTypeName ?? "<UNKNOWN>", connector->connector_type_id, connector->connection);
            Native.Drm.drmModeFreeConnector(connector);
        }

        /* Pick the first connected connector */
        _selectedConnector = null;
        for (var i = 0; i < _drmResources->count_connectors; i++)
        {
            var connector = Native.Drm.drmModeGetConnectorCurrent(_deviceFd, _drmResources->connectors[i]);
            if (connector is null)
            {
                continue;
            }

            if (connector->connection == Native.Drm.drmModeConnection.DRM_MODE_CONNECTED)
            {
                _selectedConnector = connector;
                break;
            }

            Native.Drm.drmModeFreeConnector(connector);
        }

        if (_selectedConnector == null)
        {
            Cleanup();
            throw new Exception("No connected connectors found!");
        }

        _connectorTypeNames.TryGetValue(_selectedConnector->connector_type, out var connectorType);
        _logger?.LogInformation("Using connector: {type} #{number} ({state})",
            connectorType ?? "<UNKNOWN>", _selectedConnector->connector_type_id, _selectedConnector->connection);

        /* Dump all modes */
        _logger?.LogInformation("Available modes ({count}):", _selectedConnector->count_modes);
        for (var i = 0; i < _selectedConnector->count_modes; i++)
        {
            var mode = &_selectedConnector->modes[i];
            _logger?.LogInformation(" - mode: {width}x{height}@{refresh}", mode->hdisplay, mode->vdisplay, mode->vrefresh);
        }

        /* Select mode (resolution) */
        Native.Drm.drmModeModeInfo* selectedMode = null;
        if (requestedWidth != 0 || requestedHeight != 0)
        {
            /* Find the mode with compatible resolution */
            for (var i = 0; i < _selectedConnector->count_modes; i++)
            {
                var mode = &_selectedConnector->modes[i];
                if (requestedWidth != 0 && requestedWidth != mode->hdisplay)
                {
                    continue;
                }
                if (requestedHeight != 0 && requestedHeight != mode->vdisplay)
                {
                    continue;
                }

                /* We found compatible mode */
                selectedMode = mode;
                break;
            }

            if (selectedMode == null)
            {
                Cleanup();
                throw new Exception($"Could not find requested mode: {requestedWidth}x{requestedHeight}!");
            }
        } else {
            /* Find preferred mode */
            for (var i = 0; i < _selectedConnector->count_modes; i++)
            {
                var mode = &_selectedConnector->modes[i];
                if ((mode->type & Native.Drm.DRM_MODE_TYPE_PREFERRED) != 0)
                {
                    selectedMode = mode;
                    break;
                }
            }

            if (selectedMode == null)
            {
                Cleanup();
                throw new Exception("Could not find preferred mode for connector!");
            }
        }

        _logger?.LogInformation("Using mode: {width}x{height}@{refresh}", selectedMode->hdisplay, selectedMode->vdisplay, selectedMode->vrefresh);

        ScreenWidth = selectedMode->hdisplay;
        ScreenHeight = selectedMode->vdisplay;
        RgbaMode = false; /* BGRA */

        /* Find encoder and CRTC combination */
        var crtcId = FindCompatibleCrtc();
        if (crtcId == 0)
        {
            Cleanup();
            throw new Exception("Failed to find suitable CRTC for connector!");
        }

        /* Create dumb buffers */
        try
        {
            _framebuffer1 = new Framebuffer(
                _deviceFd,
                ScreenWidth,
                ScreenHeight,
                crtcId,
                _selectedConnector->connector_id,
                *selectedMode);
        }
        catch (Exception)
        {
            Cleanup();
            throw;
        }

        try
        {
            _framebuffer2 = new Framebuffer(
                _deviceFd,
                ScreenWidth,
                ScreenHeight,
                crtcId,
                _selectedConnector->connector_id,
                *selectedMode);
        }
        catch (Exception)
        {
            Cleanup();
            throw;
        }

        /* Store currently set CRTC... */
        _oldCrtc = Native.Drm.drmModeGetCrtc(_deviceFd, crtcId);
        /* ... and perform a buffer flip to force initial mode-set */
        _frontBuffer = 0;
        try
        {
            _framebuffer1.FillAndActivate(null);
        }
        catch (Exception e)
        {
            throw new Exception("Failed to perform initial mode-set", e);
        }
    }

    private UInt32 FindCompatibleCrtc()
    {
        Native.Drm.drmModeEncoder* encoder = null;
        UInt32 crtcId = 0;

        /* First, try the currently connected encoder + CRTC */
        if (_selectedConnector->encoder_id != 0)
        {
            encoder = Native.Drm.drmModeGetEncoder(_deviceFd, _selectedConnector->encoder_id);
        }

        if (encoder != null)
        {
            crtcId = encoder->crtc_id;
            Native.Drm.drmModeFreeEncoder(encoder);
        }

        if (crtcId != 0)
        {
            _logger?.LogInformation("Found an existing valid encoder + CRTC combination.");
            return crtcId;
        }

        /* If the connector is not currently bound to an encoder, iterate all other available encoders to find a matching CRTC. */
        for (var i = 0; i < _selectedConnector->count_encoders; i++)
        {
            encoder = Native.Drm.drmModeGetEncoder(_deviceFd, _selectedConnector->encoders[i]);
            if (encoder == null)
            {
                _logger?.LogWarning("Failed to retrieve encoder #{number}: {id}! Errno: {errno}",
                    i, _selectedConnector->encoders[i], Marshal.GetLastWin32Error());
                continue;
            }

            /* Iterate over all global CRTCs */
            for (var j = 0; j < _drmResources->count_crtcs; j++)
            {
                /* Check if this CRTC works with the encoder */
                if ((encoder->possible_crtcs & (1 << j)) == 0)
                {
                    continue;
                }

                /* Use first compatible CRTC */
                crtcId = _drmResources->crtcs[j];
                _logger?.LogInformation("Found compatible encoder {encoderId} and CRTC {crtcId}.", encoder->encoder_id, crtcId);
                break;
            }

            Native.Drm.drmModeFreeEncoder(encoder);

            if (crtcId != 0)
            {
                break;
            }
        }

        return crtcId;
    }

    public override void DisplayImageData(byte *data)
    {
        var backBuffer = _frontBuffer == 0 ? _framebuffer2 : _framebuffer1;
        Debug.Assert(backBuffer != null, nameof(backBuffer) + " != null");
        backBuffer.FillAndActivate(data);
        _frontBuffer = (_frontBuffer + 1) % 2;
    }

    public override void Cleanup()
    {
        /* Restore old CRTC, if available */
        if (_oldCrtc != null)
        {
            _logger?.LogInformation("Restoring old CRTC...");
            Native.Drm.drmModeSetCrtc(
                _deviceFd,
                _oldCrtc->crtc_id,
                _oldCrtc->buffer_id,
                _oldCrtc->x,
                _oldCrtc->y,
                &_selectedConnector->connector_id,
                1,
                &_oldCrtc->mode);
            Native.Drm.drmModeFreeCrtc(_oldCrtc);
            _oldCrtc = null;
        }

        /* Free framebuffers */
        if (_framebuffer1 != null)
        {
            _logger?.LogInformation("Freeing framebuffer #1...");
            _framebuffer1.Cleanup();
            _framebuffer1 = null;
        }

        if (_framebuffer2 != null)
        {
            _logger?.LogInformation("Freeing framebuffer #2...");
            _framebuffer2.Cleanup();
            _framebuffer2 = null;
        }

        /* Free connector data */
        if (_selectedConnector != null)
        {
            Native.Drm.drmModeFreeConnector(_selectedConnector);
            _selectedConnector = null;
        }

        /* Free resources data */
        if (_drmResources != null)
        {
            Native.Drm.drmModeFreeResources(_drmResources);
            _drmResources = null;
        }

        /* Close DRM device */
        if (_deviceFd >= 0)
        {
            if (Native.LibC.close(_deviceFd) < 0)
            {
                throw new Exception($"$Failed to close device file descriptor: {Marshal.GetLastWin32Error()}");
            }
            _deviceFd = -1;
        }
    }

    class Framebuffer
    {
        /* Device and its resolution */
        private readonly int _deviceFd;
        private readonly uint32_t _width;
        private readonly uint32_t _height;

        private readonly uint32_t _crtcId;
        private readonly uint32_t _connectorId;
        private readonly Native.Drm.drmModeModeInfo _modeInfo;

        /* Dumb buffer */
        private bool _dumbBufferCreated;
        private uint32_t _stride;
        private uint64_t _size;
        private uint32_t _handle;

        private IntPtr _bufferAddress;

        /* Framebuffer */
        private bool _framebufferCreated;
        private uint32_t _framebufferId;

        public Framebuffer(
            int deviceFd,
            int width,
            int height,
            uint32_t crtcId,
            uint32_t connectorId,
            Native.Drm.drmModeModeInfo modeInfo)
        {
            _deviceFd = deviceFd;
            _width = (uint32_t)width;
            _height = (uint32_t)height;

            _crtcId = crtcId;
            _connectorId = connectorId;

            _modeInfo = modeInfo;

            Initialize();
        }

        public void FillAndActivate(byte *data)
        {
            /* Copy data */
            if (data != null)
            {
                Unsafe.CopyBlock((byte *)_bufferAddress, data, (uint)_size);
            }

            /* Activate buffer by setting CRTC */
            fixed (uint32_t* connectorIdPtr = &_connectorId)
            {
                fixed (Native.Drm.drmModeModeInfo* modeInfoPtr = &_modeInfo)
                {
                    var ret = Native.Drm.drmModeSetCrtc(_deviceFd, _crtcId, _framebufferId, 0, 0, connectorIdPtr, 1,
                        modeInfoPtr);
                    if (ret < 0)
                    {
                        throw new Exception("Failed to set CRTC.");
                    }
                }
            }
        }

        private void Initialize()
        {
            /* Create dumb buffer */
            var createRequest = new Native.Drm.drm_mode_create_dumb
            {
                width = _width,
                height = _height,
                bpp = 32
            };
            var ret = Native.Drm.drmIoctl(_deviceFd, Native.Drm.DRM_IOCTL_MODE_CREATE_DUMB, &createRequest);
            if (ret < 0)
            {
                Cleanup();
                throw new Exception($"Failed to create dumb buffer: {Marshal.GetLastWin32Error()}");
            }

            _stride = createRequest.pitch;
            _size = createRequest.size;
            _handle = createRequest.handle;

            _dumbBufferCreated = true;

            /* Create framebuffer object for the dumb-buffer */
            ret = Native.Drm.drmModeAddFB(_deviceFd, _width, _height, 24, 32, _stride, _handle, out _framebufferId);
            if (ret < 0)
            {
                Cleanup();
                throw new Exception($"Failed to create framebuffer for dumb buffer: {Marshal.GetLastWin32Error()}");
            }

            _framebufferCreated = true;

            /* Prepare buffer for mapping */
            var mapRequest = new Native.Drm.drm_mode_map_dumb
            {
                handle = _handle
            };
            ret = Native.Drm.drmIoctl(_deviceFd, Native.Drm.DRM_IOCTL_MODE_MAP_DUMB, &mapRequest);
            if (ret < 0)
            {
                Cleanup();
                throw new Exception($"Failed to prepare dumb buffer for mapping: {Marshal.GetLastWin32Error()}");
            }

            /* Perform actual memory mapping */
            _bufferAddress = Native.LibC.mmap(
                IntPtr.Zero,
                (IntPtr)_size,
                Native.LibC.PROT_READ | Native.LibC.PROT_WRITE,
                Native.LibC.MAP_SHARED,
                _deviceFd, (IntPtr)mapRequest.offset);
            if (_bufferAddress == (IntPtr)(-1))
            {
                Cleanup();
                throw new Exception($"Failed to mmap dumb buffer: {Marshal.GetLastWin32Error()}");
            }

            /* Clear the framebuffer */
            Native.LibC.memset(_bufferAddress, 0, (IntPtr)_size);
        }

        public void Cleanup()
        {
            /* Unmap buffer */
            if (_bufferAddress != (IntPtr)(-1))
            {
                Native.LibC.munmap(_bufferAddress, (IntPtr)_size);
            }

            /* Destroy framebuffer */
            if (_framebufferCreated)
            {
                Native.Drm.drmModeRmFB(_deviceFd, _framebufferId);
            }

            /* Destroy dumb buffer */
            if (_dumbBufferCreated)
            {
                var destroyReq = new Native.Drm.drm_mode_destroy_dumb
                {
                    handle = _handle
                };
                Native.Drm.drmIoctl(_deviceFd, Native.Drm.DRM_IOCTL_MODE_DESTROY_DUMB, &destroyReq);
            }
        }
    }
}
