namespace Ab4d.SharpEngine.Samples.AvaloniaUI.CrossPlatform;

public static class SharpEngineLicenseHelper
{
    public static void ActivateLicense()
    {
        // NOTE:
        // SetLicense method must be called from the entry assembly (otherwise an SDK license is needed).
        // This class is called from each entry assembly so we have license written in a single location.

        // Ab4d.SharpEngine Samples License can be used only for Ab4d.SharpEngine samples.
        // To use Ab4d.SharpEngine in your project, get a license from ab4d.com/trial or ab4d.com/purchase 
        // We need to call SetLicense in the entry assembly so we cannot move this call to CrossPlatform project.
        Ab4d.SharpEngine.Licensing.SetLicense(licenseOwner: "AB4D",
                                              licenseType: "SamplesLicense",
                                              platforms: "All",
                                              license: "5B53-8A17-DAEB-5B03-3B90-DD5B-958B-CA4D-0B88-CE79-FBB4-6002-D9C9-19C2-AFF8-1662-B2B2");
    }
}