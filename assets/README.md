# SnapDog Assets

This directory contains assets for the SnapDog project.

## Icons

The `icons` directory contains the following icon files:

### Vector Format (SVG)
- **snapdog-icon.svg**: Main application icon (512x512px)
- **snapdog-icon-small.svg**: Smaller version of the icon (64x64px)

### Bitmap Format (PNG)
The `icons/png` directory contains the following sizes:
- snapdog-512.png (512x512px)
- snapdog-256.png (256x256px)
- snapdog-128.png (128x128px)
- snapdog-64.png (64x64px)
- snapdog-48.png (48x48px)
- snapdog-32.png (32x32px)
- snapdog-16.png (16x16px)

### Windows Icon Format (ICO)
- **snapdog-icon.ico**: Windows icon file containing all the above PNG sizes

### Usage

These icons are used in the following locations:

1. Package Icon in the SnapDog.csproj file (SVG)
2. Application Icon in the SnapDog.csproj file (ICO)
3. Web interface favicon (symbolic link from docker/caddy/site/favicon.svg → assets/icons/snapdog-icon.svg)
4. Solution Items in SnapDog.sln for easy access

### Color Palette

The icon uses the following color palette:

- Primary blue: #3498DB (background)
- Dark blue: #2980B9 (background stroke)
- Primary orange: #F39C12 (dog face)
- Dark orange: #E67E22 (dog ears, snout)
- Deepest orange: #D35400 (strokes)
- Red: #E74C3C (collar)
- White: #ECF0F1 (sound waves)
- Gold: #F1C40F (tag)
- Dark gray: #34495E (eyes, nose)
- Deepest gray: #2C3E50 (nose stroke)

### Modifying

If you need to modify these icons, please maintain the same style and color palette for consistency.
When modifying the main SVG icon, the changes will automatically be reflected in the web interface
favicon since it uses a symbolic link.