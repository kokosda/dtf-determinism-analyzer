# DTF Determinism Analyzer Icon

This directory contains the package icon for the DTF Determinism Analyzer NuGet package.

## Files

- `icon.svg` - Vector source file (128x128)
- `icon.png` - Rasterized version for NuGet package (128x128)

## Design

The icon features:
- Blue circular background representing reliability and trust
- Magnifying glass symbolizing code analysis
- Gear/cog inside representing deterministic behavior
- "DTF" text at bottom for clear identification

## Usage

The PNG version is embedded in the NuGet package and displayed in package managers.

## Creating PNG from SVG

To regenerate the PNG from SVG (requires appropriate tools):

```bash
# Using rsvg-convert (Linux/macOS with librsvg)
rsvg-convert -w 128 -h 128 icon.svg -o icon.png

# Using ImageMagick
convert -background transparent -size 128x128 icon.svg icon.png

# Using Inkscape
inkscape -w 128 -h 128 icon.svg --export-filename=icon.png
```