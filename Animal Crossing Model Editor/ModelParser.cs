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

        private static dynamic RunModelRoutine(byte Value, int Index, byte[] Data)
        {
            switch (Value)
            {
                case 0x0A:
                    return null;
                default:
                    return null;
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

        private static List<Point3D[]> GetModelSections(byte[] Model_Data, List<Point3D> Vertices, List<Point3D[]> Faces = null, int StartPoint = 0, int NumSections = 0, int BaseIndex = 0, int AdditiveIndex = 0, int Section = 0)
        {
            if (StartPoint >= Model_Data.Length)
                return Faces;

            List<uint> ConvertedData = new List<uint>();

            // Convert all data into uint types for handling
            for (int i = 0; i < Model_Data.Length; i += 4)
            {
                ConvertedData.Add((uint)((Model_Data[i] << 24) | (Model_Data[i + 1] << 16) | (Model_Data[i + 2] << 8) | Model_Data[i + 3]));
            }

            uint[] Data = ConvertedData.ToArray();

            // Find start point
            bool Found_StartPoint = false;
            for (int i = StartPoint; i < Data.Length; i += 2)
            {
                if ((Data[i] & 0xFF000000) == 0x01000000) // Set vertices
                {
                    BaseIndex = (int)Data[i + 1];
                    //AdditiveIndex = (int)(Data[i] >> 1) & 0xFF;
                }
                else if ((Data[i] & 0xFF000000) == 0x0A000000)
                {
                    StartPoint = i;
                    Found_StartPoint = true;
                    break;
                }
            }

            if (NumSections == 0)
            {
                for (int i = 0; i < Data.Length; i += 2)
                {
                    if ((Data[i] & 0xFF000000) == 0x0A000000)
                    {
                        NumSections++;
                    }
                }
            }

            if (!Found_StartPoint)
                return Faces;

            uint FaceCount = (((Data[StartPoint] >> 16) & 0xFF) / 2) + 1; // Get the total number of faces in the model
            uint FacesLeft = FaceCount;

            if (IsDEBUG)
            {
                Console.WriteLine("===== Section Start =====");
                Console.WriteLine("Start Point: 0x" + (StartPoint * 4).ToString("X"));
                Console.WriteLine("Faces: " + FaceCount);
            }

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

                if (IsDEBUG)
                {
                    Console.WriteLine("vIndex_0: 0x" + vIndex_0.ToString("X"));
                    Console.WriteLine("vIndex_1: 0x" + vIndex_1.ToString("X"));
                    Console.WriteLine("vIndex_2: 0x" + vIndex_2.ToString("X"));
                }

                //Faces.Add(new Point3D[3] { Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_0)], Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_1)],
                //    Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_2)] });
                MainWindow.Create_Triangle_Mesh(Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_0)], Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_1)],
                    Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_2)]);

                FacesLeft--;
                if (FacesLeft == 0) // Check to see if we're done with the faces
                    break;

                uint vIndex_3 = (uint)((CurrentFaceData >> 19) & 0x1F); // Fourth vertex
                uint vIndex_4 = (uint)((CurrentFaceData >> 24) & 0x1F); // Fifth vertex
                uint vIndex_5 = (uint)((CurrentFaceData >> 29) & 0x1F); // Sixth vertex

                if (IsDEBUG)
                {
                    Console.WriteLine("vIndex_3: 0x" + vIndex_3.ToString("X"));
                    Console.WriteLine("vIndex_4: 0x" + vIndex_4.ToString("X"));
                    Console.WriteLine("vIndex_5: 0x" + vIndex_5.ToString("X"));
                }

                //Faces.Add(new Point3D[3] { Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_3)], Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_4)],
                //    Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_5)] });

                MainWindow.Create_Triangle_Mesh(Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_3)], Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_4)],
                    Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_5)]);

                FacesLeft--;
                if (FacesLeft == 0)
                    break;

                uint vIndex_6 = (uint)((CurrentFaceData >> 34) & 0x1F); // Seventh vertex
                uint vIndex_7 = (uint)((CurrentFaceData >> 39) & 0x1F); // Eighth vertex
                uint vIndex_8 = (uint)((CurrentFaceData >> 44) & 0x1F); // Ninth vertex

                if (IsDEBUG)
                {
                    Console.WriteLine("vIndex_6: 0x" + vIndex_6.ToString("X"));
                    Console.WriteLine("vIndex_7: 0x" + vIndex_7.ToString("X"));
                    Console.WriteLine("vIndex_8: 0x" + vIndex_8.ToString("X"));
                }

                //Faces.Add(new Point3D[3] { Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_6)], Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_7)],
                //    Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_8)] });

                MainWindow.Create_Triangle_Mesh(Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_6)], Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_7)],
                    Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_8)]);

                FacesLeft--;
                if (FacesLeft == 0)
                    break;

                if (FirstPassFinished) // Only do this after the first 64 bit section (since the first byte is the section identifer (0x0A) and the second byte is the number of faces * 2 - 1)
                {
                    uint vIndex_9 = (uint)((CurrentFaceData >> 49) & 0x1F); // Tenth vertex
                    uint vIndex_10 = (uint)((CurrentFaceData >> 54) & 0x1F); // Eleventh vertex
                    uint vIndex_11 = (uint)((CurrentFaceData >> 59) & 0x1F); // Twelth vertex

                    if (IsDEBUG)
                    {
                        Console.WriteLine("vIndex_9: 0x" + vIndex_9.ToString("X"));
                        Console.WriteLine("vIndex_10: 0x" + vIndex_10.ToString("X"));
                        Console.WriteLine("vIndex_11: 0x" + vIndex_11.ToString("X"));
                    }

                    //Faces.Add(new Point3D[3] { Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_9)], Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_10)],
                    //    Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_11)] });

                    MainWindow.Create_Triangle_Mesh(Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_9)], Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_10)],
                        Vertices[(BaseIndex + AdditiveIndex + (int)vIndex_11)]);

                    FacesLeft--;
                    if (FacesLeft == 0)
                        break;
                }
                else
                {
                    FirstPassFinished = true;
                }
            }

            if (FacesLeft == 0)
                Console.WriteLine("Constructed all faces!");

            Console.WriteLine("Section Information");
            Console.WriteLine("Current Section: " + Section);
            Console.WriteLine("Current Base Index: 0x" + BaseIndex.ToString("X"));
            Console.WriteLine("Current Additive Index: 0x" + AdditiveIndex.ToString("X"));
            Console.WriteLine("Current Vertex Offset Index: 0x" + (BaseIndex + AdditiveIndex).ToString("X"));
            Console.WriteLine("Current Section Color: " + MainWindow.Model_Colors[MainWindow.Color_Index].ToString());
            Console.WriteLine("===== Section End =====");


            MainWindow.Color_Index++;

            if (EndIndex < Data.Length)
            {
                return GetModelSections(Model_Data, Vertices, Faces, EndIndex, NumSections, BaseIndex, AdditiveIndex, ++Section);
            }

            return Faces;
        }

        public static Point3D[][] ParseModel(byte[] Model_Data, List<Point3D> Vertices)
        {
            return GetModelSections(Model_Data, Vertices).ToArray();
        }
    }
}
