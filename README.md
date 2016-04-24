# Duality Bitmap Font Asset Importer
A bitmap font asset importer for the [Duality game engine](http://duality.adamslair.net/)

## How to use
* Generate the image (see Editors below)
* Drag and drop both the png (currently only supports pngs) and the xml bmfont file into Duality
* Create a TextRenderer and allocate the font to it (or just drag the font onto the scene)

![How to screencap](http://mika76.github.io/duality-bitmapfont-importer/how_bitmap_font.gif)

## Editors 
The following links are to editors (some online, some not) which should be supported to some extent or another.

* Online
 * http://kvazars.com/littera/
 * https://www.glyphite.com/editor
 
* Desktop 
  * Bitmap Font Generator - http://www.angelcode.com/products/bmfont/ - make sure you use 32 bit depth, png file, and xml file type. White on alpha is the best preset. (check http://mika76.github.io/duality-bitmapfont-importer/bmfont_settings.png)
  
* Editors with currently un-supported file formats
  * Ultimate Bitmap Font Generator - https://github.com/scriptum/UBFG (very interesting tool but totally different file format)
  * Font Studio - https://bitbucket.org/mikepote/font-studio
  * Bitmap Font Builder - http://www.lmnopc.com/bitmapfontbuilder/ - old VB6 tool - doubt there is a need to support this one
  * Codehead’s Bitmap Font Generator - http://www.codehead.co.uk/cbfg/
  
* Found but don't know much about yet
  * ShoeBox - http://renderhjs.net/shoebox/
  * Sprite Font Generator - https://www.scirra.com/forum/sprite-font-generator-v2_t86546

## Notes ##

Current Requirements
* Use Padding
* Use XML BMFont format (currently only one supported)
* Save to transparent png file
