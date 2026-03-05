using Ab4d.SharpEngine.Samples.BlazorWebAssembly;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });


// Ab4d.SharpEngine Samples License can be used only for Ab4d.SharpEngine samples.
// To use Ab4d.SharpEngine in your project, get a license from ab4d.com/trial or ab4d.com/purchase 
Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D",
                                      licenseType: "SamplesLicense",
                                      license: "5B53-8A17-DAEB-5B03-3B90-DD5B-958B-CA4D-0B88-CE79-FBB4-6002-D9C9-19C2-AFF8-1662-B2B2");

await builder.Build().RunAsync();
