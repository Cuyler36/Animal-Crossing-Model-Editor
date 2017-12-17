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

            uint FaceCount = (((Data[StartPoint] >> 16) & 0xFF) / 2) + 1;
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

                uint vIndex_0 = (Data[i + 1] >> 4) & 0x1F;
                uint vIndex_1 = (Data[i + 1] >> 14) & 0x1F;
                uint vIndex_2 = (Data[i + 1] >> 9) & 0x1F;

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
                if (FacesLeft == 0)
                    break;

                uint vIndex_3 = ((((Data[i] & 0xFF) << 8) | (Data[i + 1] >> 24)) >> 5) & 0x1F;
                uint vIndex_4 = (Data[i + 1] >> 24) & 0x1F;
                uint vIndex_5 = (Data[i + 1] >> 19) & 0x1F;

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

                uint vIndex_6 = (Data[i] >> 2) & 0x1F;
                uint vIndex_7 = (Data[i] >> 12) & 0x1F;
                uint vIndex_8 = (Data[i] >> 7) & 0x1F;

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

                if (FirstPassFinished)
                {
                    uint vIndex_9 = (Data[i] >> 17) & 0x1F;
                    uint vIndex_10 = (Data[i] >> 27) & 0x1F;
                    uint vIndex_11 = (Data[i] >> 22) & 0x1F;

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
