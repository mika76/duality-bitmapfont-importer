using Duality;
using Duality.Drawing;
using Duality.Editor;
using Duality.Editor.AssetManagement;
using Duality.Resources;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace BMFontPlugin
{
    public class BitmapFontImpoter : IAssetImporter
    {
        public string Id { get { return "BitmapFontImporter"; } }

        public string Name { get { return "Bitmap Font Importer"; } }

        public int Priority { get { return 1; } }


        public void PrepareImport(IAssetImportEnvironment env)
        {
            // File all fnt files with allocated png files
            var importFiles = from file in env.HandleAllInput(f => Path.GetExtension(f.Path).ToLower() == ".fnt")
                              select file;

            foreach (var input in importFiles)
            {
                var textureFile = Path.Combine(Path.GetDirectoryName(input.Path), Path.GetFileNameWithoutExtension(input.Path) + ".png");

                if (File.Exists(textureFile))
                {
                    env.AddOutput<Pixmap>(input.AssetName, input.Path);
                    env.AddOutput<Duality.Resources.Font>(input.AssetName, input.Path);
                }
                else
                {
                    throw new ApplicationException("No corresponding png file was imported with " + input.Path);
                }
            }
        }


        public void Import(IAssetImportEnvironment env)
        {
            foreach (AssetImportInput input in env.Input)
            {
                // Request a target Resource with a name matching the input
                ContentRef<Duality.Resources.Font> targetRef = env.GetOutput<Duality.Resources.Font>(input.AssetName);

                // If we successfully acquired one, proceed with the import
                if (targetRef.IsAvailable)
                {
                    Duality.Resources.Font target = targetRef.Res;
                    var textureFile = Path.Combine(Path.GetDirectoryName(input.Path), Path.GetFileNameWithoutExtension(input.Path) + ".png");

                    var textureData = LoadPixelData(textureFile);
                    var fontData = LoadFontData(input.Path);
                    target.SetGlyphData(textureData, 
                        fontData.Atlas, 
                        fontData.Glyps, 
                        fontData.Height, 
                        fontData.Ascent, 
                        fontData.BodyAscent, 
                        fontData.Descent, 
                        fontData.Baseline);

                    target.Size = fontData.Size;
                    //target.Kerning = true;
                    

                    env.AddOutput(targetRef, input.Path);
                }
            }
        }

        private FontData LoadFontData(string path)
        {
            return (from xFont in XDocument.Load(path).Descendants("font")
                    let height = (int)Math.Ceiling(float.Parse(xFont.Element("common").Attribute("lineHeight").Value))
                    let baseline = int.Parse(xFont.Element("common").Attribute("base").Value)
                    let size = float.Parse(xFont.Element("info").Attribute("size").Value)
                    let glyphData = (from c in xFont.Element("chars").Elements()
                                  select new
                                  {
                                      Glyph = (char)ushort.Parse(c.Attribute("id").Value),
                                      OffsetX = int.Parse(c.Attribute("xoffset").Value),
                                      OffsetY = int.Parse(c.Attribute("yoffset").Value),
                                      Width = int.Parse(c.Attribute("width").Value),
                                      Height = int.Parse(c.Attribute("height").Value)
                                  })
                    let atlas = from c in xFont.Element("chars").Elements()
                                select new Rect(
                                    float.Parse(c.Attribute("x").Value),
                                     float.Parse(c.Attribute("y").Value),
                                     float.Parse(c.Attribute("width").Value),
                                     float.Parse(c.Attribute("height").Value)
                                     )
                    select new FontData()
                    {
                        Height = height,
                        Baseline = baseline,
                        Size = size,
                        Atlas = atlas.ToArray(),
                        Glyps = (from g in glyphData
                                select new Duality.Resources.Font.GlyphData()
                                {
                                    Glyph = g.Glyph,
                                    OffsetX = g.OffsetX,
                                    OffsetY = g.OffsetY,
                                    Width = g.Width,
                                    Height = g.Height
                                }).ToArray(),
                        Ascent = glyphData.Max(g => baseline - g.OffsetY),
                        Descent = glyphData.Max(g=>g.OffsetY),
                        BodyAscent = height - baseline
                    })
                   .SingleOrDefault();
        }

        public void PrepareExport(IAssetExportEnvironment env)
        {
            //// We can export any Resource that is a Font with an embedded TrueType face
            //Duality.Resources.Font input = env.Input as Duality.Resources.Font;
            //if (input != null && input.EmbeddedTrueTypeFont != null)
            //{
            //    // Add the file path of the exported output we'll produce.
            //    env.AddOutputPath(env.Input.Name + ".fnt");
            //}
        }
        public void Export(IAssetExportEnvironment env)
        {
            //// Determine input and output path
            //Duality.Resources.Font input = env.Input as Duality.Resources.Font;
            //string outputPath = env.AddOutputPath(input.Name + ".fnt");

            //// Take the input Resource's TrueType font data and save it at the specified location
            //File.WriteAllBytes(outputPath, input.EmbeddedTrueTypeFont);
        }

        private PixelData LoadPixelData(string filePath)
        {
            PixelData pixelData = new PixelData();
            byte[] imageData = File.ReadAllBytes(filePath);
            using (Stream stream = new MemoryStream(imageData))
            using (Bitmap bitmap = Bitmap.FromStream(stream) as Bitmap)
            {
                pixelData.FromBitmap(bitmap);
            }
            return pixelData;
        }

        private class FontData
        {
            public int Ascent { get; internal set; }
            public Rect[] Atlas { get; internal set; }
            public int Baseline { get; internal set; }
            public int BodyAscent { get; internal set; }
            public int Descent { get; internal set; }
            public Duality.Resources.Font.GlyphData[] Glyps { get; internal set; }
            public bool HasKerning { get; internal set; }
            public int Height { get; internal set; }
            public float Size { get; internal set; }
        }
    }
}
