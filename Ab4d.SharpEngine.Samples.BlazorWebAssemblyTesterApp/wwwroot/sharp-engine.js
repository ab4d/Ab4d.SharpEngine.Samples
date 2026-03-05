// This is a JavaScript that is used for communication between JavaScript and Ab4d.SharpEngine for the browser.
// It initializes the WebGL context for the canvas and is used to report when the size of the canvas is changed,
// and to subscribe to mouse and touch events on the canvas.

let isLogging = false; // Set this to true to write log messages from javascript to console. This can be also set from CanvasInterop by setting IsLoggingJavaScript to true.

let initialCanvas;
let interop;
let spector;
let resizeObserver;
let canvasToDisplaySizeMap;
let isUpdating = false;
let isPinchZooming = false;

export async function initInteropAsync() {
    log("initInteropAsync");

    const dotnet = globalThis.getDotnetRuntime(0);

    var config = dotnet.getConfig();
    var exports = await dotnet.getAssemblyExports(config.mainAssemblyName);

    //IMORTANT:
    //If we change the namespace or class name of the Ab4d.SharpEngine.WebGL.CanvasInterop,
    //then we also need to change the following line:
    interop = exports.Ab4d.SharpEngine.WebGL.CanvasInterop;

    log(".Net interop with CanvasInterop initialized");
}

export function initWebGLCanvas(canvasId, useMSAA, preserveDrawingBuffer, subscribeMouseEvents, subscribeRequestAnimationFrame, preventShowingContextMenu, enableJavaScriptLogging) {
    if (enableJavaScriptLogging)
        isLogging = true; // if enableJavaScriptLogging is false, then do not override if isLogging is set to true here

    log("initWebGLCanvas canvasId:" + canvasId);

    const canvas = globalThis.document.getElementById(canvasId);

    if (canvas)
    {
        let webglVersion;

        var context = canvas.getContext('webgl2', { antialias: useMSAA, preserveDrawingBuffer: preserveDrawingBuffer });
        
        if (context) {
            webglVersion = "2";
        }
        else {
            context = canvas.getContext('webgl', { antialias: useMSAA, preserveDrawingBuffer: preserveDrawingBuffer });

            if (context) {
                logWarn("WebGL 2.0 is not supported. Using WebGL 1.0 but some features may not work.")
                webglVersion = "1";
            }
            else {
                var errorMessage = "WebGL 1.0 is not supported";
                logError(errorMessage);
                return errorMessage;
            }
        }

        if (!initialCanvas)
            initialCanvas = canvas;

        canvas.addEventListener("webglcontextlost",
            function (e) {
                logError("WebGL context lost for " + canvasId);
                if (interop)
                    interop.OnContextLostJsCallback(canvasId);
            }, false
        );
        

        const dotnet = globalThis.getDotnetRuntime(0);
        dotnet.Module["canvas"] = canvas; // This is requried to be able to create WebGL context (call EGL.CreateContext)

        var dpi = window.devicePixelRatio || 1.0;

        var rect = canvas.getBoundingClientRect(); // This will get the size in float values without rounding (as with clientWidth/clientHeight)
        var displayWidth  = Math.round(rect.width * dpi);
        var displayHeight = Math.round(rect.height * dpi);

        // Make the size of canvas back buffers the corect size
        canvas.width = displayWidth;
        canvas.height = displayHeight;

        // Start observing the canvas size changes
        // ResizeObserver is more accurate than other methods - see https://webglfundamentals.org/webgl/lessons/webgl-resizing-the-canvas.html
        if (!resizeObserver)
            resizeObserver = new ResizeObserver(onResize);

        resizeObserver.observe(canvas); // use default: { box: 'content-box' });

        // We use a Map to store the current display size of the canvas.
        if (!canvasToDisplaySizeMap)
            canvasToDisplaySizeMap = new Map();

        canvasToDisplaySizeMap.set(canvas, [displayWidth, displayHeight]); // Set initial size

        subscribeBrowserEventsInt(canvas, subscribeMouseEvents, subscribeRequestAnimationFrame, preventShowingContextMenu);

        // Return the size as a string in format: "OK:width;height;dpiScale"
        // It is not possible (at least in .Net 9) to pass an objects for JS to .Net
        // It was possible to encode width and height into an int, but we also need dpiScale,
        // so we need to pass it as a string
        return `OK:v${webglVersion};${displayWidth};${displayHeight};${dpi}`;
    }
    else
    {
        var errorMessage = "Canvas not found: " + canvasId;
        console.error(errorMessage);
        return errorMessage;
    }
}

export function loadTextFile(canvasId, url) {
    log("loadTextFile: start loading " + url);

    fetch(url, { mode: 'cors' })
        .then(response => {
            if (!response.ok) {
                log(`loadTextFile: HTTP error loading '${url}': ${response.status}`);
                if (interop)
                    interop.OnTextFileLoaded(canvasId, url, null, `HTTP error! Status: ${response.status}`);
            }
            else {
                return response.text();
            }
        })
        .then(text => {
            if (interop && text) {
                log("loadTextFile: text loaded: " + url);
                interop.OnTextFileLoaded(canvasId, url, text, null);
            }
        })
        .catch(error => {
            log(`loadTextFile: error loading '${url}': ${error.message}`);
            if (interop)
                interop.OnTextFileLoaded(canvasId, url, null, error.message);
        });
}

export function loadBinaryFile(canvasId, url) {
    log("loadBinaryFile: start loading " + url);

    fetch(url, { mode: 'cors' })
        .then(response => {
            if (!response.ok) {
                if (interop)
                    interop.OnBinaryFileLoaded(canvasId, url, null, `HTTP error! Status: ${response.status}`);
            }
            else {
                return response.arrayBuffer();
            }
        })
        .then(buffer => {
            if (interop && buffer) {
                log("loadBinaryFile: file loaded: " + url);
                const byteArray = new Uint8Array(buffer);
                interop.OnBinaryFileLoaded(canvasId, url, byteArray, null);
            }
        })
        .catch(error => {
            log(`loadBinaryFile: error loading '${url}': ${error.message}`);
            if (interop)
                interop.OnBinaryFileLoaded(canvasId, url, null, error.message);
        });
}

export async function loadImageBytes(canvasId, url) {
    log("loadImageBytes: start loading " + url);
     
    if (!createImageBitmap || typeof OffscreenCanvas === "undefined") {
        // Before Safari 16.4 (2024-03-27)
        await loadImageBytesOldWay(canvasId, url);
        return;
    }

    const response = await fetch(url, { mode: 'cors' });
    if (!response.ok) {
        if (interop)
            interop.OnImageBytesLoaded(canvasId, url, 0, 0, null, 'Image could not be fetched, url: ' + url);

        return;
    }

    log("loadImageBytes: Image loaded: " + url);

    try {
        const blob = await response.blob();
        await loadImageBytesFromBlob(blob, canvasId, url);
    }
    catch (ex) {
        let message = `loadImageBytes: error decoding image '${url}': ${ex.message}`;
        log(message);

        if (interop)
            interop.OnImageBytesLoaded(canvasId, url, 0, 0, null, message);
    }
}

// imageBytes should be Uint8Array
// mimeType should be: 'image/png' or 'image/jpeg' or other supported image type
export async function createImageFromBytes(canvasId, imageBytes, mimeType, imageName) {
    log("createImageFromBytes: start creating " + imageName);

    // 1. Create a Blob from the array
    const blob = new Blob([imageBytes], { type: mimeType }); 

    await loadImageBytesFromBlob(blob, canvasId, imageName);
}

async function loadImageBytesFromBlob(blob, canvasId, url) {
    try {
        const bitmap = await createImageBitmap(blob, { premultiplyAlpha: 'none' });

        const canvas = new OffscreenCanvas(bitmap.width, bitmap.height);
        const ctx = canvas.getContext('2d');
        ctx.drawImage(bitmap, 0, 0);
        const imageData = ctx.getImageData(0, 0, bitmap.width, bitmap.height);

        const data = Array.from(imageData.data);

        log("loadImageBytes: image byte array retrieved: " + url);

        if (interop)
            interop.OnImageBytesLoaded(canvasId, url, bitmap.width, bitmap.height, data, null);
    }
    catch (ex) {
        let message = `loadImageBytes: error decoding image '${url}': ${ex.message}`;
        log(message);

        if (interop)
            interop.OnImageBytesLoaded(canvasId, url, 0, 0, null, message);
    }
}

async function loadImageBytesOldWay(canvasId, url) {

    const image = new Image();
    image.src = url;

    let data;
    let width, height;

    try {
        await image.decode();

        log("image loaded by using Image: " + url);

        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');

        width = image.width;
        height = image.height;

        canvas.width = width;
        canvas.height = height;
        ctx.drawImage(image, 0, 0);
        // Get all pixels
        const imageData = ctx.getImageData(0, 0, width, height);
        data = Array.from(imageData.data); // Length = width * height * 4

        log("image byte array retrieved: " + url);

        if (interop)
            interop.OnImageBytesLoaded(canvasId, url, width, height, data, null);
    }
    catch (ex)
    {
        let message = "Error loading " + url + ": " + ex.message;
        log(message);

        if (interop)
            interop.OnImageBytesLoaded(canvasId, url, 0, 0, null, message);
    }
}

// NOTE:
// We cannot subscribe to mouse events in SetWebGLCanvas because this requires async method
// because we need to call "await getAssemblyExports" (without awit the exports are empyt).
// But if SetWebGLCanvas is async then it returns no result to .Net and we get an error that "resut is not a string".
// Therefore we require two methods: SetWebGLCanvas and SubscribeBrowserEvents
export function subscribeBrowserEvents(canvasId, subscribeMouseEvents, subscribeRequestAnimationFrame) {
    log("subscribeBrowserEvents canvasId:" + canvasId);

    let canvas = getCanvas(canvasId);
    subscribeBrowserEventsInt(canvas, subscribeMouseEvents, subscribeRequestAnimationFrame);
}

export function unsubscribeBrowserEvents(canvasId, unsubscribeMouseEvents, unsubscribeRequestAnimationFrame) {
    log("unsubscribeBrowserEvents canvasId:" + canvasId);

    if (!interop)
        return;

    if (unsubscribeMouseEvents)
    {
        const canvas = getCanvas(canvasId);

        if (canvas) {
            canvas.removeEventListener("pointermove", pointerMove, false);
            canvas.removeEventListener("pointerdown", pointerDown, false);
            canvas.removeEventListener("pointerup", pointerUp, false);
            canvas.removeEventListener("touchstart", touchStart, false);
            canvas.removeEventListener("touchmove", touchMove, false);
            canvas.removeEventListener("touchend", touchEnd, false);
            canvas.removeEventListener("wheel", mouseWheel, false);

            log("mouse events unsubscribed");
        }
    }

    if (unsubscribeRequestAnimationFrame)
        isUpdating = false;
}

export function startSpectorCapture(canvasId) {
    log("startSpectorCapture:" + canvasId);

    const canvas = getCanvas(canvasId);

    if (!spector) {
        if (typeof SPECTOR === "undefined")
            return false;

        spector = new SPECTOR.Spector();
    }

    spector.displayUI();
    spector.startCapture(canvas, 100000); // 100000 is the max amount of commands to capture
    return true;
}

export function stopSpectorCapture() {
    log("stopSpectorCapture");

    if (spector)
        spector.stopCapture();
}

export function setCursorStyle(canvasId, cursorStyle) {
    log("setCursorStyle canvasId:" + canvasId + " to " + cursorStyle);

    const canvas = getCanvas(canvasId);

    if (canvas)
        canvas.style.cursor = cursorStyle;
}

export function setPointerCapture(canvasId, pointerId) {
    log("setPointerCapture canvasId:" + canvasId);

    const canvas = getCanvas(canvasId);

    if (canvas) {
        try {
            canvas.setPointerCapture(pointerId);
        }
        catch (ex) {
            // prevent an error when pointerId event was already finished and does not exist anymore when this is called
        } 
    }
}

export function releasePointerCapture(canvasId, pointerId) {
    log("releasePointerCapture canvasId:" + canvasId);

    const canvas = getCanvas(canvasId);

    if (canvas) {
        try {
            canvas.releasePointerCapture(pointerId);
        }
        catch (ex) {
            // prevent an error when pointerId event was already finished and does not exist anymore when this is called
        } 
    }
}

export function disconnectWebGLCanvas(canvasId) {
    log("disconnectWebGLCanvas canvasId:" + canvasId);

    unsubscribeBrowserEvents(canvasId, true, true);

    const canvas = getCanvas(canvasId);

    if (canvas) {
        if (initialCanvas === canvas)
            initialCanvas = null;

        if (!resizeObserver)
            resizeObserver.unobserve(canvas);

        canvasToDisplaySizeMap.delete(canvas)

        const dotnet = globalThis.getDotnetRuntime(0);
        if (dotnet && dotnet.Module["canvas"] === canvas)
            dotnet.Module["canvas"] = null;
    }
}

export function showRawBitmap(canvasId, width, height, pixelData, displayStyle) {
    log("showRawBitmap canvasId:" + canvasId);

    const canvas = document.getElementById(canvasId);

    if (canvas) {
        const dpi = window.devicePixelRatio || 1.0;

        // CSS size
        canvas.style.width = width / dpi + "px";
        canvas.style.height = height / dpi + "px";

        // Backing buffer size (actual pixel buffer)
        canvas.width = width;
        canvas.height = height;

        if (displayStyle)
            canvas.style.display = displayStyle;

        const ctx = canvas.getContext("2d");

        // Create ImageData from your array
        const byteArray = new Uint8ClampedArray(pixelData.buffer);
        const img = new ImageData(byteArray, width, height);

        // Draw it
        ctx.putImageData(img, 0, 0);
    }
}


function onFrameUpdate()
{
    if (!interop || !isUpdating)
        return;

    interop.OnFrameUpdateJsCallback();

    requestAnimationFrame(onFrameUpdate);
}

// From https://webglfundamentals.org/webgl/lessons/webgl-resizing-the-canvas.html
function onResize(entries) {
    for (const entry of entries) {
        const canvas = entry.target;
        if (!canvasToDisplaySizeMap.has(canvas)) // if canvas is not in the canvasToDisplaySizeMap, then it may be already diconnected
            continue;

        let width;
        let height;
        let dpr = window.devicePixelRatio;
        if (entry.devicePixelContentBoxSize) {
            // NOTE: Only this path gives the correct answer
            // The other 2 paths are an imperfect fallback
            // for browsers that don't provide anyway to do this
            width = entry.devicePixelContentBoxSize[0].inlineSize;
            height = entry.devicePixelContentBoxSize[0].blockSize;
            dpr = 1; // it's already in width and height
        } else if (entry.contentBoxSize) {
            if (entry.contentBoxSize[0]) {
                width = entry.contentBoxSize[0].inlineSize;
                height = entry.contentBoxSize[0].blockSize;
            } else {
                // legacy
                width = entry.contentBoxSize.inlineSize;
                height = entry.contentBoxSize.blockSize;
            }
        } else {
            // legacy
            width = entry.contentRect.width;
            height = entry.contentRect.height;
        }

        
        const displayWidth = Math.round(width * dpr);
        const displayHeight = Math.round(height * dpr);

        const [oldWidth, oldHeight] = canvasToDisplaySizeMap.get(canvas);

        if (displayWidth !== oldWidth || displayHeight !== oldHeight) {
            canvasToDisplaySizeMap.set(canvas, [displayWidth, displayHeight]);

            // Make the size of canvas back buffers the corect size
            canvas.width = displayWidth;
            canvas.height = displayHeight;

            if (interop)
                interop.OnCanvasResizedJsCallback(canvas.id, displayWidth, displayHeight, window.devicePixelRatio); // report size change to render the next frame
        }
    }
}

function subscribeBrowserEventsInt(canvas, subscribeMouseEvents, subscribeRequestAnimationFrame, preventShowingContextMenu) {
    if (subscribeMouseEvents && canvas) {
        canvas.addEventListener("pointermove", pointerMove, false);
        canvas.addEventListener("pointerdown", pointerDown, false);
        canvas.addEventListener("pointerup", pointerUp, false);
        canvas.addEventListener("pointerenter", pointerEnter, false);
        canvas.addEventListener("pointerleave", pointerLeave, false);
        canvas.addEventListener("touchstart", touchStart, false);
        canvas.addEventListener("touchmove", touchMove, false);
        canvas.addEventListener("touchend", touchEnd, false);
        canvas.addEventListener("wheel", mouseWheel, false);

        if (preventShowingContextMenu)
            canvas.addEventListener("contextmenu", function (e) { e.preventDefault(); }, false);

        log("mouse events subscribed");
    }

    if (subscribeRequestAnimationFrame && !isUpdating) {
        isUpdating = true;
        requestAnimationFrame(onFrameUpdate);
    }
}

function getCanvas(canvasId) {
    if (initialCanvas && (!canvasId || initialCanvas.id === canvasId))
        return initialCanvas;

    const canvas = globalThis.document.getElementById(canvasId);

    if (!canvas)
        logError("Canvas not found: " + canvasId);

    return canvas;
}

function getKeyboardModifiers(e) {
    return (e.shiftKey ? 4 : 0) + (e.ctrlKey ? 2 : 0) + (e.altKey ? 1 : 0); // See Ab4d.SharpEngine.Common.KeyboardModifiers
}

function log(message) {
    if (isLogging)
        console.log("sharp-engine.js: " + message);
}

function logWarn(message) {
    console.warn("sharp-engine.js: " + message);
}

function logError(message) {
    console.error("sharp-engine.js: " + message);
}

function checkPinch(e, callPinchZoom) {
    var isPinchZoomStarted = false;

    if (e.touches.length === 2) {
        if (!isPinchZooming) {
            isPinchZooming = true;
            isPinchZoomStarted = true;
        }
    }
    else {
        if (isPinchZooming) {
            isPinchZooming = false;
            interop.OnPinchZoomEndedJsCallback(e.currentTarget.id);
        }
    }

    if (isPinchZooming) {
        var pinchDistance = Math.hypot(e.touches[0].pageX - e.touches[1].pageX, e.touches[0].pageY - e.touches[1].pageY);

        if (pinchDistance > 0) {
            var centerX = (e.touches[0].clientX + e.touches[1].clientX) * 0.5;
            var centerY = (e.touches[0].clientY + e.touches[1].clientY) * 0.5;

            const rect = e.touches[0].target.getBoundingClientRect();
            centerX -= rect.left;
            centerY -= rect.top;

            if (isPinchZoomStarted)
                interop.OnPinchZoomStartedJsCallback(e.currentTarget.id, pinchDistance, centerX, centerY);
            else if (callPinchZoom)
                interop.OnPinchZoomJsCallback(e.currentTarget.id, pinchDistance, centerX, centerY);
        }
    }
}


const pointerMove = (e) => {
    if (!interop)
        return;

    e.preventDefault(); // Prevent sending mouseMove

    if (isPinchZooming)
        return;

    interop.OnPointerMovedJsCallback(e.currentTarget.id, e.offsetX, e.offsetY, e.buttons, getKeyboardModifiers(e));
}

const pointerDown = (e) => {
    if (!interop)
        return;

    e.preventDefault(); // Prevent sending mouseDown

    if (isPinchZooming)
        return;

    interop.OnPointerDownJsCallback(e.currentTarget.id, e.offsetX, e.offsetY, e.button, e.buttons, e.pointerId, getKeyboardModifiers(e));
}

const pointerUp = (e) => {
    if (!interop)
        return;

    e.preventDefault(); // Prevent sending mouseUp

    if (isPinchZooming)
        return;

    interop.OnPointerUpJsCallback(e.currentTarget.id, e.offsetX, e.offsetY, e.button, e.buttons, e.pointerId, getKeyboardModifiers(e));
}

const pointerEnter = (e) => {
    if (!interop)
        return;

    e.preventDefault(); // Prevent sending mouseUp

    if (isPinchZooming)
        return;

    interop.OnPointerUpJsCallback(e.currentTarget.id, e.offsetX, e.offsetY, e.buttons, getKeyboardModifiers(e));
}

const pointerLeave = (e) => {
    if (!interop)
        return;

    e.preventDefault(); // Prevent sending mouseUp

    if (isPinchZooming)
        return;

    interop.OnPointerUpJsCallback(e.currentTarget.id, e.offsetX, e.offsetY, e.buttons, getKeyboardModifiers(e));
}

const mouseWheel = (e) => {
    if (!interop)
        return;

    e.preventDefault();

    interop.OnMouseWheelJsCallback(e.currentTarget.id, e.deltaX, e.deltaY, e.offsetX, e.offsetY, e.buttons, getKeyboardModifiers(e));
}

const touchStart = (e) => {
    if (!interop)
        return;

    e.preventDefault();

    if (e.touches.length === 1) {
        var button = 0;
        var touch = e.changedTouches[0];
        var bcr = e.target.getBoundingClientRect();
        var x = touch.clientX - bcr.x;
        var y = touch.clientY - bcr.y;
        var keyboardModifiers = getKeyboardModifiers(e);

        interop.OnPointerMovedJsCallback(e.currentTarget.id, x, y, e.buttons, keyboardModifiers);
        interop.OnPointerDownJsCallback(e.currentTarget.id, button, keyboardModifiers);
    }

    checkPinch(e, false);
}

const touchMove = (e) => {
    if (!interop)
        return;

    e.preventDefault();

    checkPinch(e, true);

     if (e.touches.length === 1) {
        var touch = e.changedTouches[0];
        var bcr = e.target.getBoundingClientRect();
        var x = touch.clientX - bcr.x;
        var y = touch.clientY - bcr.y;

        interop.OnPointerMovedJsCallback(e.currentTarget.id, x, y, 1, getKeyboardModifiers(e));
    }
}

const touchEnd = (e) => {
    if (!interop)
        return;

    e.preventDefault();

    if (e.touches.length === 0) {
        var button = 0;
        var touch = e.changedTouches[0];
        var bcr = e.target.getBoundingClientRect();
        var x = touch.clientX - bcr.x;
        var y = touch.clientY - bcr.y;
        var keyboardModifiers = getKeyboardModifiers(e);

        interop.OnPointerMovedJsCallback(e.currentTarget.id, x, y, e.buttons, keyboardModifiers);
        interop.OnPointerUpJsCallback(e.currentTarget.id, button, keyboardModifiers);
    }
    
    checkPinch(e, false);
}
