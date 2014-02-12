// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// -----------------------------------------------------------------------------
// The following code is a port of MakeSpriteFont from DirectXTk
// http://go.microsoft.com/fwlink/?LinkId=248929
// -----------------------------------------------------------------------------
// Microsoft Public License (Ms-PL)
//
// This license governs use of the accompanying software. If you use the 
// software, you accept this license. If you do not accept the license, do not
// use the software.
//
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and 
// "distribution" have the same meaning here as under U.S. copyright law.
// A "contribution" is the original software, or any additions or changes to 
// the software.
// A "contributor" is any person that distributes its contribution under this 
// license.
// "Licensed patents" are a contributor's patent claims that read directly on 
// its contribution.
//
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the 
// license conditions and limitations in section 3, each contributor grants 
// you a non-exclusive, worldwide, royalty-free copyright license to reproduce
// its contribution, prepare derivative works of its contribution, and 
// distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license
// conditions and limitations in section 3, each contributor grants you a 
// non-exclusive, worldwide, royalty-free license under its licensed patents to
// make, have made, use, sell, offer for sale, import, and/or otherwise dispose
// of its contribution in the software or derivative works of the contribution 
// in the software.
//
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any 
// contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that 
// you claim are infringed by the software, your patent license from such 
// contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all 
// copyright, patent, trademark, and attribution notices that are present in the
// software.
// (D) If you distribute any portion of the software in source code form, you 
// may do so only under this license by including a complete copy of this 
// license with your distribution. If you distribute any portion of the software
// in compiled or object code form, you may only do so under a license that 
// complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The
// contributors give no express warranties, guarantees or conditions. You may
// have additional consumer rights under your local laws which this license 
// cannot change. To the extent permitted under your local laws, the 
// contributors exclude the implied warranties of merchantability, fitness for a
// particular purpose and non-infringement.
//--------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace SharpDX.Toolkit.Graphics
{
    using System.Drawing;
    using System.Drawing.Imaging;

    // Extracts font glyphs from a specially marked 2D bitmap. Characters should be
    // arranged in a grid ordered from top left to bottom right. Monochrome characters
    // should use white for solid areas and black for transparent areas. To include
    // multicolored characters, add an alpha channel to the bitmap and use that to
    // control which parts of the character are solid. The spaces between characters
    // and around the edges of the grid should be filled with bright pink (red=255,
    // green=0, blue=255). It doesn't matter if your grid includes lots of wasted space,
    // because the converter will rearrange characters, packing as tightly as possible.
    internal class BitmapImporter : IFontImporter
    {
        // Properties hold the imported font data.
        public IEnumerable<Glyph> Glyphs { get; private set; }

        public float LineSpacing { get; private set; }


        public void Import(FontDescription options)
        {
            // Load the source bitmap.
            Bitmap bitmap;

            try
            {
                bitmap = new Bitmap(options.FontName);
            }
            catch
            {
                throw new FontException(string.Format("Unable to load '{0}'.", options.FontName));
            }

            // Convert to our desired pixel format.
            bitmap = BitmapUtils.ChangePixelFormat(bitmap, PixelFormat.Format32bppArgb);

            // What characters are included in this font?
            var characters = Utilities.ToArray(CharacterRegion.Flatten(options.CharacterRegions));
            int characterIndex = 0;
            char currentCharacter = '\0';

            // Split the source image into a list of individual glyphs.
            var glyphList = new List<Glyph>();

            Glyphs = glyphList;
            LineSpacing = 0;

            foreach (Rectangle rectangle in FindGlyphs(bitmap))
            {
                if (characterIndex < characters.Length)
                    currentCharacter = characters[characterIndex++];
                else
                    currentCharacter++;

                glyphList.Add(new Glyph(currentCharacter, bitmap, rectangle));

                LineSpacing = Math.Max(LineSpacing, rectangle.Height);
            }

            // If the bitmap doesn't already have an alpha channel, create one now.
            if (BitmapUtils.IsAlphaEntirely(255, bitmap))
            {
                BitmapUtils.ConvertGreyToAlpha(bitmap);
            }
        }


        // Searches a 2D bitmap for characters that are surrounded by a marker pink color.
        static IEnumerable<Rectangle> FindGlyphs(Bitmap bitmap)
        {
            using (var bitmapData = new BitmapUtils.PixelAccessor(bitmap, ImageLockMode.ReadOnly))
            {
                for (int y = 1; y < bitmap.Height; y++)
                {
                    for (int x = 1; x < bitmap.Width; x++)
                    {
                        // Look for the top left corner of a character (a pixel that is not pink, but was pink immediately to the left and above it)
                        if (!IsMarkerColor(bitmapData[x, y]) &&
                             IsMarkerColor(bitmapData[x - 1, y]) &&
                             IsMarkerColor(bitmapData[x, y - 1]))
                        {
                            // Measure the size of this character.
                            int w = 1, h = 1;

                            while ((x + w < bitmap.Width) && !IsMarkerColor(bitmapData[x + w, y]))
                            {
                                w++;
                            }

                            while ((y + h < bitmap.Height) && !IsMarkerColor(bitmapData[x, y + h]))
                            {
                                h++;
                            }

                            yield return new Rectangle(x, y, w, h);
                        }
                    }
                }
            }
        }


        // Checks whether a color is the magic magenta marker value.
        static bool IsMarkerColor(Color color)
        {
            return color.ToArgb() == Color.Magenta.ToArgb();
        }
    }
}
