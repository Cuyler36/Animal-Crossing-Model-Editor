using System.Windows.Media.Media3D;

namespace Animal_Crossing_Model_Editor
{
    public class AC_Vector
    {
        public double Scale = 0.0001; // Scales the model down for ToPoint3D (1 = normal)

        public short X;
        public short Y;
        public short Z;
        public short Reserved; // Unknown or Unused. Usually 0x0001.
        public short TextureXCoordinate;
        public short TextureYCoordinate;

        // Vertex Normals \\
        public byte NormalX;
        public byte NormalY;
        public byte NormalZ;

        // Vertex Transparency \\
        public byte Alpha;

        public AC_Vector(short[] Data)
        {
            X = (short)-Data[0];
            Y = Data[2];
            Z = Data[1];

            Reserved = Data[3];

            TextureXCoordinate = Data[4];
            TextureYCoordinate = Data[5];

            NormalX = (byte)((Data[6] & 0xFF00) >> 8);
            NormalY = (byte)(Data[6] & 0x00FF);
            NormalZ = (byte)((Data[7] & 0xFF00) >> 8);

            Alpha = (byte)(Data[7] & 0x00FF);
        }

        public Point3D ToPoint3D()
        {
            return new Point3D(X * Scale, Y * Scale, Z * Scale);
        }
    }
}
