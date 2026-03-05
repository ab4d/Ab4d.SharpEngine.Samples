// This JavaScript is used to initialize the WebGL context and
// to communicate with the JavaScriptInterop .Net class that is defined in the Ab4d.SharpEngine.Samples.WebAssemblyDemo project.

// The code in this file does the following:
// - loads and initializes the dotnet runtime
// - calls .Net method JavaScriptInterop.InitSharpEngineJSExport that initializes the Ab4d.SharpEngine
// - provides functions that can call .Net methods that are defined in JavaScriptInterop.cs (have JSExport attribute)

console.log("js: Starting loading and initializing dotnet");

// Set up the .NET WebAssembly runtime
import { dotnet } from './_framework/dotnet.js'

// Get exported methods from the .NET assembly
const { getAssemblyExports, getConfig } = await dotnet
    .withDiagnosticTracing(false)
    .create();

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);

// Initialize the WebAssembly code that use SharpEngine
console.log("js: Start calling JavaScriptInterop.InitSharpEngineJSExport");
exports.Ab4d.SharpEngine.Samples.WebAssemblyDemo.JavaScriptInterop.InitSharpEngineJSExport();

// stop and hide the loader animation:
document.getElementById('loader').style.display = 'none';

// Expose the following function globally so they can be called from the <a href="javascript:..." code above

export function toggleCameraRotation() {
    console.log("js: toggleCameraRotation");
    exports.Ab4d.SharpEngine.Samples.WebAssemblyDemo.JavaScriptInterop.ToggleCameraRotationJSExport();
}

export function changeMaterial(colorText) {
    console.log("js: ChangeMaterialJSExport:", colorText);
    exports.Ab4d.SharpEngine.Samples.WebAssemblyDemo.JavaScriptInterop.ChangeMaterialJSExport(colorText); // empty text or null will generate a random color
}

export function addObjects(objectsCount) {
    console.log("js: AddObjectsJSExport:", objectsCount);
    exports.Ab4d.SharpEngine.Samples.WebAssemblyDemo.JavaScriptInterop.AddObjectsJSExport(objectsCount);
}

// The following function can be called from .Net (see ShowInfoTextJS method in JavaScriptInterop.cs)
export function showInfoText(infoText) {
    console.log("js: showInfoText:", infoText);

    document.getElementById('infoSpan').innerText = infoText;
}
