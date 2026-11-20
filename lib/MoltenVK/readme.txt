libMoltenVK.dylib for macos-arm64_x86_64 was copied from Vulkan SKD v1.4.363.1 (https://vulkan.lunarg.com/sdk/home#mac)

This MoltenVK library does not support wide lines.

To support that, a special build of the library (compiled with MVK_USE_METAL_PRIVATE_API) is compiled to macOS-arm64_x86_64/widelines-v1.3 folder.
This version is compiled from MoltenVK git repo (https://github.com/KhronosGroup/MoltenVK) from 2025-04-23.
See more here: https://github.com/KhronosGroup/MoltenVK#metal_private_api). 

Note that libMoltenVK.dylib for wide lines cannot be used in App Store so this version is only available for macOS and not for iOS.

Because this library is signed with ad-hoc certificate, you may get the following message box:
"Apple could not verify “libMoltenVK.dylib” is free of malware that may harm your Mac or compromise your privacy."

To solve that you need to make the library trusted with the following:
xattr -d com.apple.quarantine libMoltenVK.dylib