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

        private static List<Point3D[]> GetModelSections(byte[] Model_Data, List<Point3D> Vertices, List<Point3D[]> Faces = null, int StartPoint = 0, int BaseIndex = 0)
        {
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

            uint FaceCount = GekkoInstructions.extrwi((Data[StartPoint] >> 16) & 0xFF, 7, 24) + 1;
            uint FacesLeft = FaceCount;

            if (IsDEBUG)
            {
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
                uint vRealValue_0 = Data[i + 1] & 0xFFFF;
                uint vRealValue_1 = Data[i + 1];
                uint vRealValue_2 = (Data[i + 1] >> 8) & 0xFF;

                uint vIndex_0 = GekkoInstructions.extrwi(vRealValue_0, 5, 23);
                uint vIndex_1 = GekkoInstructions.extrwi(vRealValue_1, 5, 13);
                uint vIndex_2 = GekkoInstructions.extrwi(vRealValue_2, 5, 26);

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
                    Console.WriteLine("vRealValue_0: 0x" + vRealValue_0.ToString("X"));
                    Console.WriteLine("vRealValue_1: 0x" + vRealValue_1.ToString("X"));
                    Console.WriteLine("vRealValue_2: 0x" + vRealValue_2.ToString("X"));
                }

                Faces.Add(new Point3D[3] { Vertices[BaseIndex + (int)vIndex_0], Vertices[BaseIndex + (int)vIndex_1], Vertices[BaseIndex + (int)vIndex_2] });

                FacesLeft--;
                if (FacesLeft == 0)
                    break;

                uint vRealValue_3 = (Data[i + 1] >> 24) & 0xFF;
                uint vRealValue_4 = Data[i] & 0xFF;
                uint vRealValue_5 = (Data[i + 1] >> 16) & 0xFF;

                uint vIndex_3Pre = GekkoInstructions.rlwinm(vRealValue_3, 27, 29, 31);

                uint vIndex_3 = GekkoInstructions.rlwimi(vIndex_3Pre, vRealValue_4, 3, 27, 28);
                uint vIndex_4 = GekkoInstructions.rlwinm(vRealValue_3, 0, 27, 31);
                uint vIndex_5 = GekkoInstructions.rlwinm(vRealValue_5, 29, 27, 31);

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
                    Console.WriteLine("vRealValue_3: 0x" + vRealValue_3.ToString("X"));
                    Console.WriteLine("vRealValue_4: 0x" + vRealValue_4.ToString("X"));
                    Console.WriteLine("vRealValue_5: 0x" + vRealValue_5.ToString("X"));
                }

                Faces.Add(new Point3D[3] { Vertices[BaseIndex + (int)vIndex_3], Vertices[BaseIndex + (int)vIndex_4], Vertices[BaseIndex + (int)vIndex_5] });

                FacesLeft--;
                if (FacesLeft == 0)
                    break;

                uint vRealValue_6 = Data[i] & 0xFF;
                uint vRealValue_7 = Data[i];
                uint vRealValue_8 = Data[i] & 0xFFFF;

                uint vIndex_6 = GekkoInstructions.extrwi(vRealValue_6, 5, 25);
                uint vIndex_7 = GekkoInstructions.extrwi(vRealValue_7, 5, 15);
                uint vIndex_8 = GekkoInstructions.extrwi(vRealValue_8, 5, 20);

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
                    Console.WriteLine("vRealValue_6: 0x" + vRealValue_6.ToString("X"));
                    Console.WriteLine("vRealValue_7: 0x" + vRealValue_7.ToString("X"));
                    Console.WriteLine("vRealValue_8: 0x" + vRealValue_8.ToString("X"));
                }

                Faces.Add(new Point3D[3] { Vertices[BaseIndex + (int)vIndex_6], Vertices[BaseIndex + (int)vIndex_7], Vertices[BaseIndex + (int)vIndex_8] });

                FacesLeft--;
                if (FacesLeft == 0)
                    break;

                if (FirstPassFinished)
                {
                    uint vRealValue_9 = (Data[i] >> 16) & 0xFF;
                    uint vRealValue_10 = (Data[i] >> 24) & 0xFF;
                    uint vRealValue_11 = (Data[i] >> 16) & 0xFFFF;

                    uint vIndex_9 = GekkoInstructions.extrwi(vRealValue_9, 5, 26);
                    uint vIndex_10 = GekkoInstructions.extrwi(vRealValue_10, 5, 24);
                    uint vIndex_11 = GekkoInstructions.extrwi(vRealValue_11, 5, 21);

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
                        Console.WriteLine("vRealValue_9: 0x" + vRealValue_9.ToString("X"));
                        Console.WriteLine("vRealValue_10: 0x" + vRealValue_10.ToString("X"));
                        Console.WriteLine("vRealValue_11: 0x" + vRealValue_11.ToString("X"));
                    }

                    Faces.Add(new Point3D[3] { Vertices[BaseIndex + (int)vIndex_9], Vertices[BaseIndex + (int)vIndex_10], Vertices[BaseIndex + (int)vIndex_11] });

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

            if (EndIndex < Data.Length)
            {
                Console.WriteLine("Increment BaseId: 0x" + Data[EndIndex].ToString("X8"));
                if ((Data[EndIndex] & 0xFF000000) == 0xFD000000)
                {
                    BaseIndex += ThisBaseIndex;
                    Console.WriteLine("Incremented BaseId: 0x" + BaseIndex.ToString("X"));
                }
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
