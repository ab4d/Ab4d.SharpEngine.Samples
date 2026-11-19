# Blazor project to debug the code in Ab4d.SharpEngine.Samples.WebAssemblyDemo

The purpose of this project is to be able to debug the code in the Ab4d.SharpEngine.Samples.WebAssemblyDemo project 
- the project that does not use Blazor (the root element in the csproj does not have Sdk attribute set to "Microsoft.NET.Sdk.BlazorWebAssembly"
but is set to the standard "Microsoft.NET.Sdk" which is used for standard .Net projects.

It is not possible to debug pure WebAssembly projects when they are used in Asp.Net or any other web server.
This can be only done when the code from WebAssemblyDemo project runs in a Blazor WebAssembly project.

See the following readmes for more info:
- [Ab4d.SharpEngine.Samples.WebAssemblyDemo readme](../Ab4d.SharpEngine.Samples.WebAssemblyDemo/README.md)
- [Ab4d.SharpEngine.Samples.HtmlWebPage readme](../Ab4d.SharpEngine.Samples.HtmlWebPage/README.md)
- [Ab4d.SharpEngine.Samples.AspNetCoreApp readme](../Ab4d.SharpEngine.Samples.AspNetCoreApp/README.md)
