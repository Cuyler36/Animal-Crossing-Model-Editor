using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using GCNToolKit.Formats.Colors;
using GCNToolKit.Formats.Images;

namespace Animal_Crossing_Model_Editor
{
    public static class ModelParser
    {
        private static readonly bool IsDEBUG = Debugger.IsAttached;
        private static List<Point3D> Vertices;
        private static List<AC_Vector> ACVectors;
        private static MainWindow MainWindowReference;
        private static int BaseIndex = 0;
        private static bool EndList = false;
        private static uint VtxOffset = 0;

        // Temporary
        private static List<ushort> Palette = new List<ushort>
        {
            RGB5A3.ToRGB5A3(0xFF000000),
            RGB5A3.ToRGB5A3(0xFF111111),
            RGB5A3.ToRGB5A3(0xFF222222),
            RGB5A3.ToRGB5A3(0xFF333333),
            RGB5A3.ToRGB5A3(0xFF444444),
            RGB5A3.ToRGB5A3(0xFF555555),
            RGB5A3.ToRGB5A3(0xFF666666),
            RGB5A3.ToRGB5A3(0xFF777777),
            RGB5A3.ToRGB5A3(0xFF888888),
            RGB5A3.ToRGB5A3(0xFF999999),
            RGB5A3.ToRGB5A3(0xFFAAAAAA),
            RGB5A3.ToRGB5A3(0xFFBBBBBB),
            RGB5A3.ToRGB5A3(0xFFCCCCCC),
            RGB5A3.ToRGB5A3(0xFFDDDDDD),
            RGB5A3.ToRGB5A3(0xFFEEEEEE),
            RGB5A3.ToRGB5A3(0xFFFFFFFF)
        };

        private static readonly string ImageOutputDirectory = Directory.CreateDirectory("C:\\Users\\olsen\\Documents\\Animal Crossing Model Images").FullName;

        private static readonly Dictionary<byte, string> uCodeOpCodes = new Dictionary<byte, string>
        {
            { 0x00, "NOOP" },
            { 0x01, "VTX" },
            { 0x02, "MODIFYVTX" },
            { 0x03, "CULLDL" },
            { 0x04, "BRANCH" },
            { 0x05, "TRI1" },
            { 0x06, "TRI2" },
            { 0x07, "QUAD" },
            { 0x08, "LINE3D" },
            { 0x09, "TRIN" },
            { 0x0A, "TRIN" },
            { 0x0B, "NOOP" },
            { 0x0C, "NOOP" },
            { 0x0D, "QUADN" },
            { 0xCE, "SETTEXEDGEALPHA" },
            { 0xCF, "SETCOMBINE" },
            { 0xD0, "SETCOMBINE" },
            { 0xD1, "NOOP" },
            { 0xD2, "SETTILE_DOLPHIN" },
            { 0xD3, "NOOP" },
            { 0xD4, "NOOP" },
            { 0xD5, "SPECIAL" },
            { 0xD6, "NOOP" },
            { 0xD7, "TEXTURE" },
            { 0xD8, "POPMTX" },
            { 0xD9, "GEOMETRY" },
            { 0xDA, "MTX" },
            { 0xDB, "MOVEWORD" },
            { 0xDC, "MOVEMEM" },
            { 0xDD, "LOAD" },
            { 0xDE, "DL" },
            { 0xDF, "ENDDL" },
            { 0xE0, "SPNOOP" },
            { 0xE1, "RDPHALF" },
            { 0xE2, "SETOTHERMODE" },
            { 0xE3, "SETOTHERMODE" },
            { 0xE4, "TEXRECT" },
            { 0xE5, "NOOP" },
            { 0xE6, "RDPLOADSYNC" },
            { 0xE7, "RDPPIPESYNC" },
            { 0xE8, "RDPTILESYNC" },
            { 0xE9, "RDPFULLSYNC" },
            { 0xEA, "NOOP" },
            { 0xEB, "NOOP" },
            { 0xEC, "NOOP" },
            { 0xED, "SETSCISSOR" },
            { 0xEE, "SETPRIMDEPTH" },
            { 0xEF, "RDPSETOTHERMODE" },
            { 0xF0, "LOADTLUT" },
            { 0xF1, "NOOP" },
            { 0xF2, "SETTILESIZE" },
            { 0xF3, "LOADBLOCK" },
            { 0xF4, "LOADTILE" },
            { 0xF5, "SETTILE" },
            { 0xF6, "FILLRECT" },
            { 0xF7, "SETFILLCOLOR" },
            { 0xF8, "SETFOGCOLOR" },
            { 0xF9, "SETBLENDCOLOR" },
            { 0xFA, "SETPRIMCOLOR" },
            { 0xFB, "SETENVCOLOR" },
            { 0xFC, "SETCOMBINE" },
            { 0xFD, "SETTIMG" },
            { 0xFE, "SETZIMG" },
            { 0xFF, "SETCIMG" }
        };

        private static int RunModelRoutine(byte uCode, int Index, byte[] Data, BinaryReader Reader = null)
        {
            Debug.WriteLine("uCode: " + uCodeOpCodes[uCode]);
            switch (uCode)
            {
                case 0x00:
                    return NoOp();
                case 0x01:
                    return SetVertices(Data, Index, Reader);
                case 0x02:
                    return ModifyVertex(Data, Index);
                case 0x09:
                    return DrawTriangle(Data, Index / 4, Reader) - Index;
                case 0x0A:
                    return DrawTriangleIndependent(Data, Index / 4, Reader) - Index;
                case 0xD7:
                    return SetTextureInfo(Data, Index);
                case 0xD9:
                    return SetGeometryMode(Data, Index);
                case 0xDE:
                    return DrawList(Data, Index);
                case 0xDF:
                    return EndDisplayList();
                case 0xE2:
                    return SetOtherModeLow(Data, Index);
                case 0xF0:
                    return LoadTextureLookUpTable(Data, Index, Reader);
                case 0xFA:
                    return SetPrimativeColor(Data, Index);
                case 0xFC:
                    return SetColorCombinerMode(Data, Index);
                case 0xFD:
                    return SetTextureImage(Data, Index, Reader); // Technically the one responsible for drawing it is 0xD2 (dl_G_SETTILE_DOLPHIN)
                default:
                    return 8;
            }
        }

        private static int NoOp() => 8;

        private static int ModifyVertex(byte[] Data, int Index)
        {
            int VertexToModify = Data[Index + 1];
            int VertexBufferIndex = ((Data[Index + 2] << 8) | Data[Index + 3]) / 2;
            int NewValue = (Data[Index + 4] << 24) | (Data[Index + 5] << 16) | (Data[Index + 6] << 8) | Data[Index + 7];

            return 8;
        }

        private static int SetVertices(byte[] Data, int Index, BinaryReader Reader = null)
        {
            int NumVertices = ((Data[Index + 1] & 0x0F) << 4) | ((Data[Index + 2] & 0xF0) >> 4); // The total number of vertices loaded (past the specified start vertex)
            int VertexBufferIndex = Data[Index + 3]; // To Decode it: (Data[Index +3] >> 1) - NumVertices; (This is useless for decoding)
            if (Reader != null)
            {
                VtxOffset = BitConverter.ToUInt32(Data, 4).Reverse() & ~0x80000000;
                BaseIndex = 0;
                Reader.BaseStream.Seek(VtxOffset, SeekOrigin.Begin);
                var VertexData = MainWindowReference.LoadVertices(Reader.ReadBytes(0x20 * 0x10));
                Vertices = VertexData.Item1;
                ACVectors = VertexData.Item2;
            }
            else
            {
                BaseIndex = (Data[Index + 4] << 24) | (Data[Index + 5] << 16) | (Data[Index + 6] << 8) | Data[Index + 7]; // The offset into the vertex table of the start vertex
            }
            return 8;
        }

        private static int SetTileDolphin(byte[] Data, int Index)
        {
            int Unknown1 = Data[Index + 1] & 0x07;
            return 8;
        }

        private static int SetTextureInfo(byte[] Data, int Index)
        {
            int MaximumMipmapLevels = (Data[Index + 2] >> 3) & 0x07; // Excludes the actual texture
            int TileDescriptorNumber = Data[Index + 2] & 0x07;
            int Enabled = Data[Index + 3]; // "on" or "off"
            int XScaleFactor = (Data[Index + 4] << 8) | Data[Index + 5];
            int YScaleFactor = (Data[Index + 6] << 8) | Data[Index + 7];

            Debug.WriteLine(string.Format("Texture: Mipmap Levels: {0} | Tile Descriptor: {1} | Enabled: {2} | Scale X: {3} | Scale Y: {4}",
                MaximumMipmapLevels, TileDescriptorNumber, Enabled, XScaleFactor, YScaleFactor));

            return 8;
        }

        private static int SetGeometryMode(byte[] Data, int Index)
        {
            int ClearBits = ~((Data[Index + 1] << 16) | (Data[Index + 2] << 8) | Data[Index + 3]);
            int SetBits = (Data[Index + 4] << 24) | (Data[Index + 5] << 16) | (Data[Index + 6] << 8) | Data[Index + 7];

            return 8;
        }

        private static int DrawList(byte[] Data, int Index)
        {
            byte ProcessType = Data[Index + 1];
            int Address = (Data[Index + 4] << 24) | (Data[Index + 5] << 16) | (Data[Index + 6] << 8) | Data[Index + 7];

            Debug.WriteLine(string.Format("Draw List: Type: {0} | Draw List Address: {1}", ProcessType, Address.ToString("X8")));

            return 8;
        }

        private static int EndDisplayList()
        {
            EndList = true;
            return 8;
        }

        private static int SetOtherModeLow(byte[] Data, int Index)
        {
            int Length = Data[Index + 3] + 1;
            int Shift = 32 - Length - Data[Index + 2];
            int Bits = (Data[Index + 4] << 24) | (Data[Index + 5] << 16) | (Data[Index + 6] << 8) | Data[Index + 7];

            return 8;
        }

        private static int LoadTextureLookUpTable(byte[] Data, int Index, BinaryReader Reader = null)
        {
            int Type = (Data[1] >> 6) & 3;
            int Slot = Data[1] & 0xF; // Unsure about if this is a "slot"
            int PaletteCount = BitConverter.ToInt16(Data, 2).Reverse() & 0x3FF;
            int PaletteAddress = BitConverter.ToInt32(Data, 4).Reverse();

            Debug.WriteLine(string.Format("Load Texture Lookup Table: | Type: {0} | Slot: {1} | Palette Count: {2} | Palette Address: {3}",
                Type, Slot, PaletteCount, PaletteAddress.ToString("X8")));

            if (Reader != null && (PaletteAddress & 0x80000000) != 0)
            {
                Reader.BaseStream.Seek(PaletteAddress & ~0x80000000, SeekOrigin.Begin);
                Palette = new List<ushort>();
                for (int i = 0; i < PaletteCount; i++)
                {
                    Palette.Add(Reader.ReadUInt16().Reverse());
                }
            }

            return 8;
        }

        private static int SetPrimativeColor(byte[] Data, int Index)
        {
            int MinimumLevelOfDetail = Data[Index + 2];
            int LevelOfDetailFraction = Data[Index + 3];
            int PrimitiveColor = (Data[Index + 7] << 24) | (Data[Index + 4] << 16) | (Data[Index + 5] << 8) | Data[Index + 6]; // R->G->B->A

            MainWindowReference.PrimitiveColor = Color.FromArgb(Data[Index + 7], Data[Index + 4], Data[Index + 5], Data[Index + 6]);
            Debug.WriteLine("Set Primitive Color to: 0x" + PrimitiveColor.ToString("X8"));

            return 8;
        }

        private static int SetColorCombinerMode(byte[] Data, int Index)
        {
            // a0, c0, Aa0, Ac0, a1, c1, b0, b1, Aa1, Ac1, d0, Ab0, Ad0, d1, Ab1, Ad1
            return 8;
        }

        private static int SetTextureImage(byte[] Data, int Index, BinaryReader Reader = null)
        {
            int TextureFormat = (Data[Index + 1] & 0xE0) >> 5;
            int BitsPerPixel = (Data[Index + 1] & 0x18) >> 3;
            int Width = (((Data[Index + 2] << 8) | Data[Index + 3]) & 0x3FF) + 1;
            int Height = (((BitConverter.ToInt32(Data, 0).Reverse() >> 10) & 0xFF) + 1) * 4;
            int ImageAddress = (Data[Index + 4] << 24) | (Data[Index + 5] << 16) | (Data[Index + 6] << 8) | Data[Index + 7];

            Debug.WriteLine(string.Format("Set Texture | Address: {0} | Texture Format: {1} | Bits Per Pixel: {2} | Width: {3} | Height: {4}",
                ImageAddress.ToString("X8"), TextureFormat, BitsPerPixel, Width, Height));

            if (Reader != null && (ImageAddress & 0x80000000) != 0)
            {
                Reader.BaseStream.Seek(ImageAddress & ~0x80000000, SeekOrigin.Begin);
                int[] PixelData = C4.DecodeC4(Reader.ReadBytes((Width * Height) / 2), Palette.ToArray(), Width, Height);
                byte[] ImgData = new byte[PixelData.Length * 4];
                Buffer.BlockCopy(PixelData, 0, ImgData, 0, ImgData.Length);
                BitmapSource Img = Util.ToImage(ImgData, Width, Height);
                MainWindowReference.Texture = Img;
                using (var FStream = new FileStream(ImageOutputDirectory + "\\Image_" + ImageAddress.ToString("X8") + ".png", FileMode.Create))
                {
                    var Encoder = new PngBitmapEncoder();
                    Encoder.Frames.Add(BitmapFrame.Create(Img));
                    Encoder.Save(FStream);
                }
            }

            return 8;
        }

        private static int DrawTriangle(byte[] Model_Data, int StartPoint = 0, BinaryReader Reader = null, bool FirstPassDone = false, int InitialFacesLeft = 0)
        {
            if (Vertices == null)
                return 0;

            if (StartPoint >= Model_Data.Length)
                return 0;

            int FaceCount = FirstPassDone ? InitialFacesLeft : (Model_Data[StartPoint * 4 + 1] / 2) + 1; // Get the total number of faces in the model
            int FacesLeft = FaceCount;
            int EndIndex = StartPoint;

            List<uint> ConvertedData = new List<uint>();

            // Convert all data into uint types for handling
            for (int i = 0; i < Model_Data.Length; i += 4)
            {
                ConvertedData.Add((uint)((Model_Data[i] << 24) | (Model_Data[i + 1] << 16) | (Model_Data[i + 2] << 8) | Model_Data[i + 3]));
            }

            uint[] Data = ConvertedData.ToArray();
            bool FirstPassFinished = FirstPassDone;

            // Create a new mesh builder for this section
            if (!FirstPassDone)
                MainWindowReference.CreateNewMeshBuilder();

            for (int i = StartPoint; i < Data.Length; i += 2)
            {
                EndIndex = i + 2;

                ulong CurrentFaceData = ((ulong)Data[i] << 32) | Data[i + 1]; // Combine the two sections into one 64 bit datatype

                GetFaceVertexSet(CurrentFaceData, 0, out uint vIndex_0, out uint vIndex_1, out uint vIndex_2);

                MainWindowReference.CreateTriangleFace(Vertices[(BaseIndex + (int)vIndex_0)], Vertices[(BaseIndex + (int)vIndex_1)],
                    Vertices[(BaseIndex + (int)vIndex_2)], BaseIndex + (int)vIndex_0, BaseIndex + (int)vIndex_1, BaseIndex + (int)vIndex_2,
                    GetTextureCoordinates(vIndex_0, vIndex_1, vIndex_2));

                FacesLeft--;
                if (FacesLeft == 0) // Check to see if we're done with the faces
                    break;

                GetFaceVertexSet(CurrentFaceData, 1, out uint vIndex_3, out uint vIndex_4, out uint vIndex_5);

                MainWindowReference.CreateTriangleFace(Vertices[(BaseIndex + (int)vIndex_3)], Vertices[(BaseIndex + (int)vIndex_4)],
                    Vertices[(BaseIndex + (int)vIndex_5)], BaseIndex + (int)vIndex_3, BaseIndex + (int)vIndex_4, BaseIndex + (int)vIndex_5,
                    GetTextureCoordinates(vIndex_3, vIndex_4, vIndex_5));

                FacesLeft--;
                if (FacesLeft == 0)
                    break;

                GetFaceVertexSet(CurrentFaceData, 2, out uint vIndex_6, out uint vIndex_7, out uint vIndex_8);

                MainWindowReference.CreateTriangleFace(Vertices[(BaseIndex + (int)vIndex_6)], Vertices[(BaseIndex + (int)vIndex_7)],
                    Vertices[(BaseIndex + (int)vIndex_8)], BaseIndex + (int)vIndex_6, BaseIndex + (int)vIndex_7, BaseIndex + (int)vIndex_8,
                    GetTextureCoordinates(vIndex_6, vIndex_7, vIndex_8));

                FacesLeft--;
                if (FacesLeft == 0)
                    break;

                if (FirstPassFinished) // Only do this after the first 64 bit section (since the first byte is the section identifer (0x0A) and the second byte is the number of faces * 2 - 1)
                {
                    GetFaceVertexSet(CurrentFaceData, 3, out uint vIndex_9, out uint vIndex_10, out uint vIndex_11);

                    MainWindowReference.CreateTriangleFace(Vertices[(BaseIndex + (int)vIndex_9)], Vertices[(BaseIndex + (int)vIndex_10)],
                        Vertices[(BaseIndex + (int)vIndex_11)], BaseIndex + (int)vIndex_9, BaseIndex + (int)vIndex_10, BaseIndex + (int)vIndex_11,
                    GetTextureCoordinates(vIndex_9, vIndex_10, vIndex_11));

                    FacesLeft--;
                    if (FacesLeft == 0)
                        break;
                }
                else
                {
                    FirstPassFinished = true;
                }

                if (Reader != null && FacesLeft > 0)
                {
                    return EndIndex * 4 + DrawTriangle(Reader.ReadBytes(8), 0, Reader, true, FacesLeft);
                }
            }

            // Create the new model
            MainWindowReference.CreateCurrentModel();

            return EndIndex * 4;
        }

        private static int DrawTriangleIndependent(byte[] Model_Data, int StartPoint = 0, BinaryReader Reader = null)
        {
            return DrawTriangle(Model_Data, StartPoint, Reader);
        }

        // Non-Emulated code
        private static void GetFaceVertexSet(ulong Data, int Index, out uint VertexA, out uint VertexB, out uint VertexC)
        {
            int BaseShiftCount = 4 + Index * 15;
            VertexA = (uint)(Data >> BaseShiftCount) & 0x1F;
            VertexB = (uint)(Data >> (BaseShiftCount + 5)) & 0x1F;
            VertexC = (uint)(Data >> (BaseShiftCount + 10)) & 0x1F;
        }

        private static Tuple<short[], short[]> GetTextureCoordinates(uint VertexA, uint VertexB, uint VertexC)
            => new Tuple<short[], short[]>(
                new short[3] { ACVectors[(int)VertexA].TextureXCoordinate, ACVectors[(int)VertexB].TextureXCoordinate, ACVectors[(int)VertexC].TextureXCoordinate },
                new short[3] { ACVectors[(int)VertexA].TextureYCoordinate, ACVectors[(int)VertexB].TextureYCoordinate, ACVectors[(int)VertexC].TextureYCoordinate });

        public static void ParseModel(byte[] Model_Data, List<Point3D> VertexList, MainWindow mainWindowReference)
        {
            BaseIndex = 0;
            VtxOffset = 0;
            MainWindowReference = mainWindowReference;
            Vertices = VertexList;
            EndList = false;
            for (int i = 0; i < Model_Data.Length; i += 8)
            {
                if (EndList)
                {
                    break;
                }

                i += RunModelRoutine(Model_Data[i], i, Model_Data) - 8;
            }
        }

        public static void ParseModel(BinaryReader Reader, MainWindow mainWindowReference)
        {
            MainWindowReference = mainWindowReference;
            BaseIndex = 0;
            VtxOffset = 0;
            EndList = false;
            //Vertices = VertexList;
            while (!EndList) {
                byte[] Data = Reader.ReadBytes(8);
                var CurrentAddress = Reader.BaseStream.Position;
                uint SkipAmount = (uint)RunModelRoutine(Data[0], 0, Data, Reader) - 8;
                Reader.BaseStream.Seek(CurrentAddress + SkipAmount, SeekOrigin.Begin);
            }
        }
    }
}
