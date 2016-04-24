using Duality;
using Duality.Drawing;
using Duality.Editor;
using Duality.Editor.AssetManagement;
using Duality.Resources;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace BMFontPlugin
{
    /// <summary>
    /// Importer for importing bitmap font atlases and textures.
    /// </summary>
    public class BitmapFontImpoter : IAssetImporter
    {
        #region XSD

        string xsdBMFont = @"<?xml version=""1.0"" encoding=""Windows-1252""?>
<xs:schema attributeFormDefault=""unqualified"" elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""font"">
    <xs:complexType>
      <xs:sequence>
        <xs:element name=""info"">
          <xs:complexType>
            <xs:attribute name=""face"" type=""xs:string"" use=""required"" />
            <xs:attribute name=""size"" type=""xs:decimal"" use=""required"" />
            <xs:attribute name=""bold"" type=""xs:unsignedByte"" use=""required"" />
            <xs:attribute name=""italic"" type=""xs:unsignedByte"" use=""required"" />
            <xs:attribute name=""charset"" type=""xs:string"" use=""optional"" />
            <xs:attribute name=""unicode"" type=""xs:string"" use=""optional"" />
            <xs:attribute name=""stretchH"" type=""xs:unsignedByte"" use=""required"" />
            <xs:attribute name=""smooth"" type=""xs:unsignedByte"" use=""required"" />
            <xs:attribute name=""aa"" type=""xs:unsignedByte"" use=""required"" />
            <xs:attribute name=""padding"" type=""xs:string"" use=""required"" />
            <xs:attribute name=""spacing"" type=""xs:string"" use=""required"" />
            <xs:attribute name=""outline"" type=""xs:unsignedByte"" use=""required"" />
          </xs:complexType>
        </xs:element>
        <xs:element name=""common"">
          <xs:complexType>
            <xs:attribute name=""lineHeight"" type=""xs:unsignedByte"" use=""required"" />
            <xs:attribute name=""base"" type=""xs:unsignedByte"" use=""required"" />
            <xs:attribute name=""scaleW"" type=""xs:unsignedShort"" use=""required"" />
            <xs:attribute name=""scaleH"" type=""xs:unsignedShort"" use=""required"" />
            <xs:attribute name=""pages"" type=""xs:unsignedByte"" use=""required"" />
            <xs:attribute name=""packed"" type=""xs:unsignedByte"" use=""required"" />
            <xs:attribute name=""alphaChnl"" type=""xs:unsignedByte"" use=""optional"" />
            <xs:attribute name=""redChnl"" type=""xs:unsignedByte"" use=""optional"" />
            <xs:attribute name=""greenChnl"" type=""xs:unsignedByte"" use=""optional"" />
            <xs:attribute name=""blueChnl"" type=""xs:unsignedByte"" use=""optional"" />
          </xs:complexType>
        </xs:element>
        <xs:element name=""pages"">
          <xs:complexType>
            <xs:sequence>
              <xs:element name=""page"" minOccurs=""1"">
                <xs:complexType>
                  <xs:attribute name=""id"" type=""xs:unsignedByte"" use=""required"" />
                  <xs:attribute name=""file"" type=""xs:string"" use=""required"" />
                </xs:complexType>
              </xs:element>
            </xs:sequence>
          </xs:complexType>
        </xs:element>
        <xs:element name=""chars"">
          <xs:complexType>
            <xs:sequence>
              <xs:element maxOccurs=""unbounded"" name=""char"">
                <xs:complexType>
                  <xs:attribute name=""id"" type=""xs:unsignedInt"" use=""required"" />
                  <xs:attribute name=""x"" type=""xs:unsignedShort"" use=""required"" />
                  <xs:attribute name=""y"" type=""xs:unsignedShort"" use=""required"" />
                  <xs:attribute name=""width"" type=""xs:unsignedByte"" use=""required"" />
                  <xs:attribute name=""height"" type=""xs:unsignedByte"" use=""required"" />
                  <xs:attribute name=""xoffset"" type=""xs:byte"" use=""required"" />
                  <xs:attribute name=""yoffset"" type=""xs:unsignedByte"" use=""required"" />
                  <xs:attribute name=""xadvance"" type=""xs:unsignedByte"" use=""required"" />
                  <xs:attribute name=""page"" type=""xs:unsignedByte"" use=""required"" />
                  <xs:attribute name=""chnl"" type=""xs:unsignedByte"" use=""required"" />
                </xs:complexType>
              </xs:element>
            </xs:sequence>
            <xs:attribute name=""count"" type=""xs:unsignedByte"" use=""required"" />
          </xs:complexType>
        </xs:element>
        <xs:element name=""kernings"" minOccurs=""0"">
          <xs:complexType>
            <xs:sequence>
              <xs:element maxOccurs=""unbounded"" name=""kerning"">
                <xs:complexType>
                  <xs:attribute name=""first"" type=""xs:unsignedByte"" use=""required"" />
                  <xs:attribute name=""second"" type=""xs:unsignedByte"" use=""required"" />
                  <xs:attribute name=""amount"" type=""xs:byte"" use=""required"" />
                </xs:complexType>
              </xs:element>
            </xs:sequence>
            <xs:attribute name=""count"" type=""xs:unsignedShort"" use=""required"" />
          </xs:complexType>
        </xs:element>
      </xs:sequence>
    </xs:complexType>
  </xs:element>
</xs:schema>";
        #endregion

        #region IAssetImporter
        public string Id { get { return "BitmapFontImporter"; } }

        public string Name { get { return "Bitmap Font Importer"; } }

        public int Priority { get { return 1; } }

        public void PrepareImport(IAssetImportEnvironment env)
        {
            // File all fnt files with allocated png files
            var importFiles = from file in env.HandleAllInput(f => Path.GetExtension(f.Path).ToLower() == ".fnt" || Path.GetExtension(f.Path).ToLower() == ".xml")
                              select file;

            foreach (var input in importFiles)
            {
                var fileFormat = FigureOutFontFileFormat(input.Path);

                switch (fileFormat)
                {
                    case FontFileFormat.None:
                        {
                            // Not a file format we support, ignore
                            continue;
                        }
                    case FontFileFormat.BMFont:
                        {
                            var fontData = LoadBMFontData(input.Path);

                            if (fontData.Pages.Count() > 1)
                            {
                                Log.Editor.WriteError("Only single page bitmap font files are allowed");
                                continue; //skip this file
                            }

                            env.AddOutput<Duality.Resources.Font>(input.AssetName, input.Path);

                            foreach (var page in fontData.Pages)
                            {
                                var fullPath = Path.Combine(Path.GetDirectoryName(input.Path), page.File);

                                if (File.Exists(fullPath))
                                {
                                    env.AddOutput<Pixmap>(input.AssetName, fullPath);
                                }
                                else
                                {
                                    Log.Editor.WriteError("Corresponding bitmap font page file '{0}' not found ", fullPath);
                                    continue; //skip this file
                                }
                            }

                            break;
                        }
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

                    var fileFormat = FigureOutFontFileFormat(input.Path);

                    switch (fileFormat)
                    {
                        case FontFileFormat.BMFont:
                            {
                                var fontData = LoadBMFontData(input.Path);

                                var textureData = LoadPixelData(fontData, Path.GetFullPath(Path.GetDirectoryName(input.Path)));
                                target.SetGlyphData(textureData,
                                    fontData.Atlas.ToArray(),
                                    fontData.Glyps.ToArray(),
                                    fontData.Height,
                                    fontData.Ascent,
                                    fontData.BodyAscent,
                                    fontData.Descent,
                                    fontData.Baseline);

                                target.Size = fontData.Size;
                                //target.Kerning = true;


                                env.AddOutput(targetRef, input.Path);

                                break;
                            }
                    }
                }
            }
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

        #endregion

        #region Font File Parsers
        private FontFileFormat FigureOutFontFileFormat(string path)
        {
            //currently just try validate a bmfont file - if it doesn't validate, then return none;
            XmlSchemaSet schemas = new XmlSchemaSet();
            schemas.Add("", XmlReader.Create(new StringReader(xsdBMFont)));

            var errors = false;
            XDocument.Load(path).Validate(schemas, (o, e) =>
            {
                Log.Editor.WriteError(e.Message);
                errors = true;
            });

            if (errors == false)
                return FontFileFormat.BMFont;

            return FontFileFormat.None;
        }

        private FontData LoadBMFontData(string path)
        {
            return (from xFont in XDocument.Load(path).Descendants("font")
                    let height = (int)Math.Ceiling(float.Parse(xFont.Element("common").Attribute("lineHeight").Value))
                    let baseline = int.Parse(xFont.Element("common").Attribute("base").Value)
                    let size = float.Parse(xFont.Element("info").Attribute("size").Value)
                    let bold = float.Parse(xFont.Element("info").Attribute("bold").Value)
                    let italic = float.Parse(xFont.Element("info").Attribute("italic").Value)
                    let pages = from p in xFont.Element("pages").Elements()
                                select new Pages()
                                {
                                    Id = int.Parse(p.Attribute("id").Value),
                                    File = p.Attribute("file").Value
                                }
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
                    let alphaChnl = xFont.Element("common").Attribute("alphaChnl")
                    let dataInAlpha = alphaChnl == null ? false : alphaChnl.Value == "1"
                    select new FontData()
                    {
                        Height = height,
                        Baseline = baseline,
                        Size = size,
                        Atlas = atlas,
                        Pages = pages,
                        Glyps = from g in glyphData
                                select new Duality.Resources.Font.GlyphData()
                                {
                                    Glyph = g.Glyph,
                                    OffsetX = g.OffsetX,
                                    OffsetY = g.OffsetY,
                                    Width = g.Width,
                                    Height = g.Height
                                },
                        Ascent = glyphData.Max(g => baseline - g.OffsetY),
                        Descent = glyphData.Max(g => g.OffsetY),
                        BodyAscent = height - baseline,
                        Bold = bold,
                        Italic = italic,
                        FontDataInAlpha = dataInAlpha
                    })
                   .SingleOrDefault();
        }

        #endregion

        #region Helper Methods

        private PixelData LoadPixelData(FontData fontData, string sourceDir)
        {
            PixelData pixelData = new PixelData();
            byte[] imageData = File.ReadAllBytes(Path.Combine(sourceDir, fontData.Pages.First().File));
            using (Stream stream = new MemoryStream(imageData))
            using (Bitmap bitmap = Bitmap.FromStream(stream) as Bitmap)
            {
                //NOTE: Needed the following code to manipulate the bits to get alpha and other channels, but decided to only support transpatent,
                //      32bit PNG files for the moment. Will leave it in for future use.

                //BitmapData sourceData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                //byte[] resultBuffer = new byte[sourceData.Stride * sourceData.Height];
                //Marshal.Copy(sourceData.Scan0, resultBuffer, 0, resultBuffer.Length);
                //bitmap.UnlockBits(sourceData);

                //for (int i = 0; i < resultBuffer.Length; i += 4)
                //{
                //    var b = resultBuffer[i];
                //    var g = resultBuffer[i + 1];
                //    var r = resultBuffer[i + 2];
                //    var a = resultBuffer[i + 3];

                //    if (fontData.FontDataInAlpha)
                //    {
                //        b = g = r = a;
                //    }
                //}

                //Bitmap resultBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                //BitmapData resultData = resultBitmap.LockBits(new Rectangle(0, 0, resultBitmap.Width, resultBitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                //Marshal.Copy(resultBuffer, 0, resultData.Scan0, resultBuffer.Length);
                //resultBitmap.UnlockBits(resultData);

                //pixelData.FromBitmap(resultBitmap);

                pixelData.FromBitmap(bitmap);
            }
            return pixelData;
        }

        #endregion

        #region Private Classes and Enums

        private enum FontFileFormat
        {
            None = 0,
            BMFont = 1
        }

        /// <summary>
        /// Internal class used in the <see cref="FontData"/> class 
        /// </summary>
        private class Pages
        {
            public int Id { get; set; }
            public string File { get; set; }
        }

        /// <summary>
        /// Internal class for holiding all data read from the font info file
        /// </summary>
        private class FontData
        {
            public IEnumerable<Pages> Pages { get; set; }
            public int Ascent { get; internal set; }
            public IEnumerable<Rect> Atlas { get; internal set; }
            public int Baseline { get; internal set; }
            public int BodyAscent { get; internal set; }
            public float Bold { get; internal set; }
            public int Descent { get; internal set; }
            public IEnumerable<Duality.Resources.Font.GlyphData> Glyps { get; internal set; }
            public bool HasKerning { get; internal set; }
            public int Height { get; internal set; }
            public float Italic { get; internal set; }
            public float Size { get; internal set; }
            public bool FontDataInAlpha { get; internal set; }
        }

        #endregion
    }
}
