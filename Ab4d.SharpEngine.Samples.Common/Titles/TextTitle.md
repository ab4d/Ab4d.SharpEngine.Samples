# 3D Text

Ab4d.SharpEngine supports the following text rendering methods:

- **Vector text** is created from TrueType files (ttf). The engine generates the meshes by triangulating the curves that define the font.
- **Bitmap text** is created by using a font bitmap where the font characters are rendered to font texture. The engine then generates the meshes that show the characters from the font bitmap.
- **Instanced text** uses instancing and can show millions of characters. There each character is rendered by its own InstancedMeshNode that shows one character from the BitmapFont that is rendered as many instances of that char (providing different position, scale, orientation and color).
