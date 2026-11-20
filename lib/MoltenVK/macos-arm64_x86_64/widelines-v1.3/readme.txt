The libMoltenVK.dylib in this folder that was compiled from the git repo from 2025-04-23 (to get Vulcan API 1.3 support). 
The dylib was also compiled with MVK_USE_METAL_PRIVATE_API to allow using wide lines (see https://github.com/KhronosGroup/MoltenVK#metal_private_api).

Note that this libMoltenVK.dylib cannot be used in App Store so this version is only available for macOS and not for iOS.

Because this library is signed with ad-hoc certificate, you may get the following message box:
"Apple could not verify “libMoltenVK.dylib” is free of malware that may harm your Mac or compromise your privacy."

To solve that you need to make the library trusted with the following:
xattr -d com.apple.quarantine libMoltenVK.dylib
