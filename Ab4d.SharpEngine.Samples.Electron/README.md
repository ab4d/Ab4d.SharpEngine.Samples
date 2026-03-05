# Electron app with Ab4d.SharpEngine

The Electron framework can be used to create desktop applications using web technologies that are rendered using a version of the Chromium browser engine and a back end using the Node.js runtime environment.

Electron is a very popular framework and is used by many applications, including Visual Studio Code, Slack, Discord, GitHub Desktop, and many others.

This project demonstrates how to use the files for the published Ab4d.SharpEngine sample
to create the Electron desktop app.


### Electron prerequisites

Electron requires Node.js to be installed. 

To install Node.js see also:
- [Download Node.js](https://nodejs.org/en/download/)
- [Electron prerequisites](https://www.electronjs.org/docs/latest/tutorial/tutorial-prerequisites)


### Prepare Electron app with Ab4d.SharpEngine

First prepare the published WebAssembly project. 
This can be from:
1. Blazor WebAssembly project (see [Ab4d.SharpEngine.Samples.BlazorWebAssembly project](../Ab4d.SharpEngine.Samples.BlazorWebAssembly/README.md)).
2. WebAssembly project (without Blazor) - see [Ab4d.SharpEngine.Samples.WebAssemblyDemo project](../Ab4d.SharpEngine.Samples.WebAssemblyDemo/README.md).

To prepare the files for the Electron app, you can either use the Visual Studio publish option or the `dotnet publish` command.

Then copy the content of the published wwwroot folder to the wwwroot folder of this project.
After copying you can delete all compressed .br and .gz files in the target wwwroot folder because the Electron app does not support compressed files. Compression is not needed because files are served from local disk. But when the Electron app is distributed, then the files will be compressed when creating the installer.

Update the `main.js` file to point to the correct path of the `index.html` file  in out case it is set to `wwwroot/index.html`.

### Starting Electron app

In this project, the Electron app can be started by executing the `start electron app.bat` script. This also checks the prerequisites and copies the files to the local wwwroot folder.

Then the scipt stats the Electron app by executing the following commmand:
`npm run start`

### Debugging

To redirect the console output to the terminal, set the following environment variable before starting the Electron app:
`ELECTRON_ENABLE_LOGGING=true`

You can also use the Chromium developer tools to debug the web page. To open the developer tools, select the "View" - "Toggle Developer Tools" menu item in the Electron app or press "CTRL + SHIFT + I".

To debug the WebAssembly code, you will need to use a Blazor project. If you use a WebAssembly project without a Blazor project, then it is recommened to create a new Blazor WebAssemply project that uses linked .cs files from the main project (Ab4d.SharpEngine.Samples.WebAssemblyDemo). This way you can start the Blazor project and debug its code. See also [Ab4d.SharpEngine.Samples.BlazorWebAssemblyTesterApp project](../Ab4d.SharpEngine.Samples.BlazorWebAssemblyTesterApp/README.md).
