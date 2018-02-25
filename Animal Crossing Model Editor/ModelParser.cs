using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Animal_Crossing_Model_Editor
{
    public static class ModelParser
    {
        private static bool IsDEBUG = Debugger.IsAttached;
        private static List<Point3D> Vertices;
        private static List<Point3D[]> Faces;
        private static MainWindow MainWindowReference;
        private static int BaseIndex = 0;

        private static int RunModelRoutine(byte uCode, int Index, byte[] Data)
        {
            switch (uCode)
            {
                case 0x00:
                    return NoOp();
                case 0x01:
                    return SetVertices(Data, Index);
                case 0x02:
                    return ModifyVertex(Data, Index);
                case 0x09:
                    return DrawTriangle(Data, Index / 4) - Index;
                case 0x0A:
                    return DrawTriangleIndependent(Data, Index / 4) - Index;
                case 0xD7:
                    return SetTextureInfo(Data, Index);
                case 0xD9:
                    return SetGeometryMode(Data, Index);
                case 0xDF:
                    return EndDisplayList();
                case 0xE2:
                    return SetOtherModeLow(Data, Index);
                case 0xFA:
                    return SetPrimativeColor(Data, Index);
                case 0xFC:
                    return SetColorCombinerMode(Data, Index);
                case 0xFD:
                    return SetTextureImage(Data, Index); // Technically the one responsible for drawing it is 0xD2 (dl_G_SETTILE_DOLPHIN)
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

        private static int SetVertices(byte[] Data, int Index)
        {
            int NumVertices = ((Data[Index + 1] & 0x0F) << 4) | ((Data[Index + 2] & 0xF0) >> 4); // The total number of vertices loaded (past the specified start vertex)
            int VertexBufferIndex = Data[Index + 3]; // To Decode it: (Data[Index +3] >> 1) - NumVertices; (This is useless for decoding)
            BaseIndex = (Data[Index + 4] << 24) | (Data[Index + 5] << 16) | (Data[Index + 6] << 8) | Data[Index + 7]; // The offset into the vertex table of the start vertex
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

            return 8;
        }

        private static int SetGeometryMode(byte[] Data, int Index)
        {
            int ClearBits = ~((Data[Index + 1] << 16) | (Data[Index + 2] << 8) | Data[Index + 3]);
            int SetBits = (Data[Index + 4] << 24) | (Data[Index + 5] << 16) | (Data[Index + 6] << 8) | Data[Index + 7];

            return 8;
        }

        private static int EndDisplayList() => 8;

        private static int SetOtherModeLow(byte[] Data, int Index)
        {
            int Length = Data[Index + 3] + 1;
            int Shift = 32 - Length - Data[Index + 2];
            int Bits = (Data[Index + 4] << 24) | (Data[Index + 5] << 16) | (Data[Index + 6] << 8) | Data[Index + 7];

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

        private static int SetTextureImage(byte[] Data, int Index)
        {
            int TextureFormat = (Data[Index + 1] & 0xE0) >> 5;
            int BitsPerPixel = (Data[Index + 1] & 0x18) >> 3;
            int Width = (((Data[Index + 2] & 0x0F) << 12) | Data[Index + 3]) + 1;
            int ImageAddress = (Data[Index + 4] << 24) | (Data[Index + 5] << 16) | (Data[Index + 6] << 8) | Data[Index + 7];

            return 8;
        }

        private static int DrawTriangle(byte[] Model_Data, int StartPoint = 0)
        {
            if (Vertices == null)
                return 0;

            if (StartPoint >= Model_Data.Length)
                return 0;

            List<uint> ConvertedData = new List<uint>();

            // Convert all data into uint types for handling
            for (int i = 0; i < Model_Data.Length; i += 4)
            {
                ConvertedData.Add((uint)((Model_Data[i] << 24) | (Model_Data[i + 1] << 16) | (Model_Data[i + 2] << 8) | Model_Data[i + 3]));
            }

            uint[] Data = ConvertedData.ToArray();

            uint FaceCount = (((Data[StartPoint] >> 16) & 0xFF) / 2) + 1; // Get the total number of faces in the model
            uint FacesLeft = FaceCount;

            Faces = Faces ?? new List<Point3D[]>();

            bool FirstPassFinished = false;
            int EndIndex = StartPoint;

            // Create a new mesh builder for this section
            MainWindowReference.CreateNewMeshBuilder();

            for (int i = StartPoint; i < Data.Length; i += 2)
            {
                EndIndex = i + 2;

                ulong CurrentFaceData = ((ulong)Data[i] << 32) | Data[i + 1]; // Combine the two sections into one 64 bit datatype

                GetFaceVertexSet(CurrentFaceData, 0, out uint vIndex_0, out uint vIndex_1, out uint vIndex_2);

                MainWindowReference.CreateTriangleFace(Vertices[(BaseIndex + (int)vIndex_0)], Vertices[(BaseIndex + (int)vIndex_1)],
                    Vertices[(BaseIndex + (int)vIndex_2)], BaseIndex + (int)vIndex_0, BaseIndex + (int)vIndex_1, BaseIndex + (int)vIndex_2);

                FacesLeft--;
                if (FacesLeft == 0) // Check to see if we're done with the faces
                    break;

                GetFaceVertexSet(CurrentFaceData, 1, out uint vIndex_3, out uint vIndex_4, out uint vIndex_5);

                MainWindowReference.CreateTriangleFace(Vertices[(BaseIndex + (int)vIndex_3)], Vertices[(BaseIndex + (int)vIndex_4)],
                    Vertices[(BaseIndex + (int)vIndex_5)], BaseIndex + (int)vIndex_3, BaseIndex + (int)vIndex_4, BaseIndex + (int)vIndex_5);

                FacesLeft--;
                if (FacesLeft == 0)
                    break;

                GetFaceVertexSet(CurrentFaceData, 2, out uint vIndex_6, out uint vIndex_7, out uint vIndex_8);

                MainWindowReference.CreateTriangleFace(Vertices[(BaseIndex + (int)vIndex_6)], Vertices[(BaseIndex + (int)vIndex_7)],
                    Vertices[(BaseIndex + (int)vIndex_8)], BaseIndex + (int)vIndex_6, BaseIndex + (int)vIndex_7, BaseIndex + (int)vIndex_8);

                FacesLeft--;
                if (FacesLeft == 0)
                    break;

                if (FirstPassFinished) // Only do this after the first 64 bit section (since the first byte is the section identifer (0x0A) and the second byte is the number of faces * 2 - 1)
                {
                    GetFaceVertexSet(CurrentFaceData, 3, out uint vIndex_9, out uint vIndex_10, out uint vIndex_11);

                    MainWindowReference.CreateTriangleFace(Vertices[(BaseIndex + (int)vIndex_9)], Vertices[(BaseIndex + (int)vIndex_10)],
                        Vertices[(BaseIndex + (int)vIndex_11)], BaseIndex + (int)vIndex_9, BaseIndex + (int)vIndex_10, BaseIndex + (int)vIndex_11);

                    FacesLeft--;
                    if (FacesLeft == 0)
                        break;
                }
                else
                {
                    FirstPassFinished = true;
                }
            }

            // Create the new model
            MainWindowReference.CreateCurrentModel();

            return EndIndex * 4;
        }

        private static int DrawTriangleIndependent(byte[] Model_Data, int StartPoint = 0)
        {
            return DrawTriangle(Model_Data, StartPoint);
        }

        // Non-Emulated code
        private static void GetFaceVertexSet(ulong Data, int Index, out uint VertexA, out uint VertexB, out uint VertexC)
        {
            int BaseShiftCount = 4 + Index * 15;
            VertexA = (uint)(Data >> BaseShiftCount) & 0x1F;
            VertexB = (uint)(Data >> (BaseShiftCount + 5)) & 0x1F;
            VertexC = (uint)(Data >> (BaseShiftCount + 10)) & 0x1F;
        }

        public static void ParseModel(byte[] Model_Data, List<Point3D> VertexList, MainWindow mainWindowReference)
        {
            MainWindowReference = mainWindowReference;
            Vertices = VertexList;
            for (int i = 0; i < Model_Data.Length; i += 8)
            {
                i += RunModelRoutine(Model_Data[i], i, Model_Data) - 8;
            }
        }
    }
}
