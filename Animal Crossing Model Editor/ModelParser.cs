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
        private static int CurrentModelSectionIndex = 0;

        private static List<Point3D[]> GetModelSections(byte[] Model_Data, List<Point3D> Vertices, List<Point3D[]> Faces = null, int StartPoint = 0, int BaseIndex = 0)
        {
            CurrentModelSectionIndex++;
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
                if ((Data[i] & 0xFF000000) == 0x0A000000)
                {
                    StartPoint = i;
                    Found_StartPoint = true;
                    break;
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
            int ThisBaseIndex = 0;

            for (int i = StartPoint; i < Data.Length; i += 2)
            {
                EndIndex = i + 2;

                ulong CurrentFaceData = ((ulong)Data[i] << 32) | Data[i + 1]; // Combine the two sections into one 64 bit datatype

                uint vIndex_0 = (uint)((CurrentFaceData >> 4) & 0x1F);  // First vertex
                uint vIndex_1 = (uint)((CurrentFaceData >> 9) & 0x1F); // Second vertex
                uint vIndex_2 = (uint)((CurrentFaceData >> 14) & 0x1F); // Third vertex

                if (vIndex_0 > ThisBaseIndex)
                    ThisBaseIndex = (int)vIndex_0;

                if (vIndex_1 > ThisBaseIndex)
                    ThisBaseIndex = (int)vIndex_1;

                if (vIndex_2 > ThisBaseIndex)
                    ThisBaseIndex = (int)vIndex_2;

                if (IsDEBUG)
                {
                    Console.WriteLine("vIndex_0: 0x" + vIndex_0.ToString("X"));
                    Console.WriteLine("vIndex_1: 0x" + vIndex_1.ToString("X"));
                    Console.WriteLine("vIndex_2: 0x" + vIndex_2.ToString("X"));
                }

                Faces.Add(new Point3D[3] { Vertices[(BaseIndex + (int)vIndex_0) % Vertices.Count], Vertices[(BaseIndex + (int)vIndex_1) % Vertices.Count],
                    Vertices[(BaseIndex + (int)vIndex_2)  % Vertices.Count] });

                FacesLeft--;
                if (FacesLeft == 0) // Check to see if we're done with the faces
                    break;

                uint vIndex_3 = (uint)((CurrentFaceData >> 19) & 0x1F); // Fourth vertex
                uint vIndex_4 = (uint)((CurrentFaceData >> 24) & 0x1F); // Fifth vertex
                uint vIndex_5 = (uint)((CurrentFaceData >> 29) & 0x1F); // Sixth vertex

                if (vIndex_3 > ThisBaseIndex)
                    ThisBaseIndex = (int)vIndex_3;

                if (vIndex_4 > ThisBaseIndex)
                    ThisBaseIndex = (int)vIndex_4;

                if (vIndex_5 > ThisBaseIndex)
                    ThisBaseIndex = (int)vIndex_5;

                if (IsDEBUG)
                {
                    Console.WriteLine("vIndex_3: 0x" + vIndex_3.ToString("X"));
                    Console.WriteLine("vIndex_4: 0x" + vIndex_4.ToString("X"));
                    Console.WriteLine("vIndex_5: 0x" + vIndex_5.ToString("X"));
                }

                Faces.Add(new Point3D[3] { Vertices[(BaseIndex + (int)vIndex_3) % Vertices.Count], Vertices[(BaseIndex + (int)vIndex_4) % Vertices.Count],
                    Vertices[(BaseIndex + (int)vIndex_5) % Vertices.Count] });

                FacesLeft--;
                if (FacesLeft == 0)
                    break;

                uint vIndex_6 = (uint)((CurrentFaceData >> 34) & 0x1F); // Seventh vertex
                uint vIndex_7 = (uint)((CurrentFaceData >> 39) & 0x1F); // Eighth vertex
                uint vIndex_8 = (uint)((CurrentFaceData >> 44) & 0x1F); // Ninth vertex

                if (vIndex_6 > ThisBaseIndex)
                    ThisBaseIndex = (int)vIndex_6;

                if (vIndex_7 > ThisBaseIndex)
                    ThisBaseIndex = (int)vIndex_7;

                if (vIndex_8 > ThisBaseIndex)
                    ThisBaseIndex = (int)vIndex_8;

                if (IsDEBUG)
                {
                    Console.WriteLine("vIndex_6: 0x" + vIndex_6.ToString("X"));
                    Console.WriteLine("vIndex_7: 0x" + vIndex_7.ToString("X"));
                    Console.WriteLine("vIndex_8: 0x" + vIndex_8.ToString("X"));
                }

                Faces.Add(new Point3D[3] { Vertices[(BaseIndex + (int)vIndex_6) % Vertices.Count], Vertices[(BaseIndex + (int)vIndex_7) % Vertices.Count],
                    Vertices[(BaseIndex + (int)vIndex_8) % Vertices.Count] });

                FacesLeft--;
                if (FacesLeft == 0)
                    break;

                if (FirstPassFinished) // Only do this after the first 64 bit section (since the first byte is the section identifer (0x0A) and the second byte is the number of faces * 2 - 1)
                {
                    uint vIndex_9 = (uint)((CurrentFaceData >> 49) & 0x1F); // Tenth vertex
                    uint vIndex_10 = (uint)((CurrentFaceData >> 54) & 0x1F); // Eleventh vertex
                    uint vIndex_11 = (uint)((CurrentFaceData >> 59) & 0x1F); // Twelth vertex

                    if (vIndex_9 > ThisBaseIndex)
                        ThisBaseIndex = (int)vIndex_9;

                    if (vIndex_10 > ThisBaseIndex)
                        ThisBaseIndex = (int)vIndex_10;

                    if (vIndex_11 > ThisBaseIndex)
                        ThisBaseIndex = (int)vIndex_11;

                    if (IsDEBUG)
                    {
                        Console.WriteLine("vIndex_9: 0x" + vIndex_9.ToString("X"));
                        Console.WriteLine("vIndex_10: 0x" + vIndex_10.ToString("X"));
                        Console.WriteLine("vIndex_11: 0x" + vIndex_11.ToString("X"));
                    }

                    Faces.Add(new Point3D[3] { Vertices[(BaseIndex + (int)vIndex_9) % Vertices.Count], Vertices[(BaseIndex + (int)vIndex_10) % Vertices.Count],
                        Vertices[(BaseIndex + (int)vIndex_11) % Vertices.Count] });

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
            Console.WriteLine("Highest Vertex Index: 0x" + ThisBaseIndex.ToString("X"));
            Console.WriteLine("Current Base Index: 0x" + BaseIndex.ToString("X"));
            Console.WriteLine("===== Section End =====");

            if (EndIndex < Data.Length)
            {
                if (CurrentModelSectionIndex % 2 == 0)
                    BaseIndex = ThisBaseIndex + 1;
                else
                    BaseIndex += ThisBaseIndex + 1;

                //CurrentModelSectionIndex++;
                return GetModelSections(Model_Data, Vertices, Faces, EndIndex, BaseIndex);
            }

            return Faces;
        }

        public static Point3D[][] ParseModel(byte[] Model_Data, List<Point3D> Vertices)
        {
            return GetModelSections(Model_Data, Vertices).ToArray();
        }
    }
}
