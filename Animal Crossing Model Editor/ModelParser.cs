using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Animal_Crossing_Model_Editor
{
    public static class ModelParser
    {
        private static bool IsDEBUG = System.Diagnostics.Debugger.IsAttached;
        private static List<Point3D> Vertices;
        private static List<Point3D[]> Faces;
        private static int BaseIndex = 0;

        private static int RunModelRoutine(byte Value, int Index, byte[] Data)
        {
            switch (Value)
            {
                case 0x01:
                    return SetBaseVertex(Data, Index);
                case 0x0A:
                    return DrawFaces(Data, Index / 4) - Index;
                case 0xFD:
                    return TextureFaceGroup(); // Technically the one responsible for drawing it is 0xD2 (dl_G_SETTILE_DOLPHIN)
                default:
                    return 8;
            }
        }

        private static int GetEndVertexIndex(int CurrentData)
        {
            return (CurrentData >> 12) & 0xFF;
        }

        private static int GetOtherVertexIndex(int CurrentData)
        {
            return (CurrentData & 0xFF) >> 1;
        }

        private static int SetBaseVertex(byte[] Data, int Index)
        {
            BaseIndex = (Data[Index + 4] << 24) | (Data[Index + 5] << 16) | (Data[Index + 6] << 8) | Data[Index + 7];
            return 8;
        }

        private static int TextureFaceGroup()
        {
            return 8;
        }

        private static int DrawFaces(byte[] Model_Data, int StartPoint = 0)
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

            for (int i = StartPoint; i < Data.Length; i += 2)
            {
                EndIndex = i + 2;

                ulong CurrentFaceData = ((ulong)Data[i] << 32) | Data[i + 1]; // Combine the two sections into one 64 bit datatype

                uint vIndex_0 = (uint)((CurrentFaceData >> 4) & 0x1F);  // First vertex
                uint vIndex_1 = (uint)((CurrentFaceData >> 9) & 0x1F); // Second vertex
                uint vIndex_2 = (uint)((CurrentFaceData >> 14) & 0x1F); // Third vertex

                //Faces.Add(new Point3D[3] { Vertices[(BaseIndex + (int)vIndex_0)], Vertices[(BaseIndex + (int)vIndex_1)],
                //    Vertices[(BaseIndex + (int)vIndex_2)] });
                MainWindow.Create_Triangle_Mesh(Vertices[(BaseIndex + (int)vIndex_0)], Vertices[(BaseIndex + (int)vIndex_1)],
                    Vertices[(BaseIndex + (int)vIndex_2)]);

                FacesLeft--;
                if (FacesLeft == 0) // Check to see if we're done with the faces
                    break;

                uint vIndex_3 = (uint)((CurrentFaceData >> 19) & 0x1F); // Fourth vertex
                uint vIndex_4 = (uint)((CurrentFaceData >> 24) & 0x1F); // Fifth vertex
                uint vIndex_5 = (uint)((CurrentFaceData >> 29) & 0x1F); // Sixth vertex

                //Faces.Add(new Point3D[3] { Vertices[(BaseIndex + (int)vIndex_3)], Vertices[(BaseIndex + (int)vIndex_4)],
                //    Vertices[(BaseIndex + (int)vIndex_5)] });

                MainWindow.Create_Triangle_Mesh(Vertices[(BaseIndex + (int)vIndex_3)], Vertices[(BaseIndex + (int)vIndex_4)],
                    Vertices[(BaseIndex + (int)vIndex_5)]);

                FacesLeft--;
                if (FacesLeft == 0)
                    break;

                uint vIndex_6 = (uint)((CurrentFaceData >> 34) & 0x1F); // Seventh vertex
                uint vIndex_7 = (uint)((CurrentFaceData >> 39) & 0x1F); // Eighth vertex
                uint vIndex_8 = (uint)((CurrentFaceData >> 44) & 0x1F); // Ninth vertex

                //Faces.Add(new Point3D[3] { Vertices[(BaseIndex + (int)vIndex_6)], Vertices[(BaseIndex + (int)vIndex_7)],
                //    Vertices[(BaseIndex + (int)vIndex_8)] });

                MainWindow.Create_Triangle_Mesh(Vertices[(BaseIndex + (int)vIndex_6)], Vertices[(BaseIndex + (int)vIndex_7)],
                    Vertices[(BaseIndex + (int)vIndex_8)]);

                FacesLeft--;
                if (FacesLeft == 0)
                    break;

                if (FirstPassFinished) // Only do this after the first 64 bit section (since the first byte is the section identifer (0x0A) and the second byte is the number of faces * 2 - 1)
                {
                    uint vIndex_9 = (uint)((CurrentFaceData >> 49) & 0x1F); // Tenth vertex
                    uint vIndex_10 = (uint)((CurrentFaceData >> 54) & 0x1F); // Eleventh vertex
                    uint vIndex_11 = (uint)((CurrentFaceData >> 59) & 0x1F); // Twelth vertex

                    //Faces.Add(new Point3D[3] { Vertices[(BaseIndex + (int)vIndex_9)], Vertices[(BaseIndex + (int)vIndex_10)],
                    //    Vertices[(BaseIndex + (int)vIndex_11)] });

                    MainWindow.Create_Triangle_Mesh(Vertices[(BaseIndex + (int)vIndex_9)], Vertices[(BaseIndex + (int)vIndex_10)],
                        Vertices[(BaseIndex + (int)vIndex_11)]);

                    FacesLeft--;
                    if (FacesLeft == 0)
                        break;
                }
                else
                {
                    FirstPassFinished = true;
                }
            }

            return EndIndex * 4;
        }

        public static void ParseModel(byte[] Model_Data, List<Point3D> VertexList)
        {
            Vertices = VertexList;
            for (int i = 0; i < Model_Data.Length; i += 8)
            {
                i += RunModelRoutine(Model_Data[i], i, Model_Data) - 8;
            }
        }
    }
}
