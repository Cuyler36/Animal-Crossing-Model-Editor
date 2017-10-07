using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Media.Media3D;

namespace Animal_Crossing_Model_Editor
{
    public enum OffsetIncrementType : byte
    {
        NoIncrement = 0xD9,
        Increment = 0xFD,
        End = 0xDF
    }

    class AC_Vector
    {
        public static int[] Multipliers = new int[16]
        {
            1, 2, 4, 8,
            16, 1, 2, 4,
            8, 16, 1, 0,
            2, 4, 8, 16
        };

        public static int[] New_Multipliers = new int[16]
        {
            2, 4, 8, 16, // 0A 0X
            1, 2, 4, 8,
            16, 1, 2, 4,
            8, 16, 1, 0
        };

        public static int[] REAL_Multipliers = new int[16]
        {
            4, 8, 16, 1,
            8, 16, 1, 2,
            16, 1, 2, 4,
            0, 2, 4, 8
        };

        // Probably not right
        public double Scale = 1; // Scales the model down for ToPoint3D

        public short X;
        public short Y;
        public short Z;
        public short Alpha_Modifier;
        public short Texture_X_Offset;
        public short Texture_Y_Offset;
        public short Light_Influence_D1;
        public short Light_Influence_D2;

        public AC_Vector(short[] Data)
        {
            /*if (Data.Length != 0x8)
                Debug.WriteLine(string.Format("AC Vector was not complete! Size: {0}", Data.Length));

            if (Data.Length < 8)
                return;*/

            X = Data[0];
            Y = Data[2];
            Z = Data[1];

            /*Alpha_Modifier = Data[3];

            Texture_X_Offset = Data[4];
            Texture_Y_Offset = Data[5];

            Light_Influence_D1 = Data[6];
            Light_Influence_D2 = Data[7];*/
        }

        public Point3D ToPoint3D()
        {
            return new Point3D(X * Scale, Y * Scale, Z * Scale);
        }
    }
}
