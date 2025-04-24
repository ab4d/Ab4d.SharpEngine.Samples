libMoltenVK.dylib was copied from Vulkan SKD v1.3.290.0

Except the libMoltenVK.dylib from the macOS-arm64_x86_64/widelines-v1.3 folder that was compiled from the git repo from 2025-04-23 (to get Vulcan API 1.3 support). The dylib was also compiled with MVK_USE_METAL_PRIVATE_API to allow using wide lines (see https://github.com/KhronosGroup/MoltenVK#metal_private_api). Note that this libMoltenVK.dylib cannot be used in App Store so this version is only available for macOS and not for iOS.


Wide lines with that libMoltenVK.dylib will be available with Ab4d.SharpEngine v3.1.