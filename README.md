# duality-bitmapfont-importer
A bitmap font asset importer for the Duality game engine

## How to use
* Generate the image (see Editors below)
* Drag and drop both the png (currently only supports pngs) and the xml bmfont file into Duality
* Create a TextRenderer and allocate the font to it (or just drag the font onto the scene)

![How to screencap](http://mika76.github.io/duality-bitmapfont-importer/how_bitmap_font.gif)

## Editors 
The following links are to editors (some online, some not) which should be ssupported to some extent or another.

* Online
 * http://kvazars.com/littera/
 * https://www.glyphite.com/editor
 
* Desktop 
  * http://www.angelcode.com/products/bmfont/ (xml format used, but channels not yet supported)
  * http://renderhjs.net/shoebox/
  * http://www.lmnopc.com/bitmapfontbuilder/
  * https://www.scirra.com/forum/sprite-font-generator-v2_t86546
  * https://github.com/scriptum/UBFG (very interesting tool but totally different file format)

## Notes ##

Current Requirements
* Use Padding
* Use XML BMFont format (currently only one supported)
* Save to transparent png file
