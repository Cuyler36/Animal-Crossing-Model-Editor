using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using System.Diagnostics;
using System.Reflection;

namespace Animal_Crossing_Model_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static System.Windows.Forms.OpenFileDialog Model_Select_Dialog = new System.Windows.Forms.OpenFileDialog();
        private Model3DGroup ModelGroup;
        private List<Color> Model_Colors = ColorStructToList();
        private int Color_Index = 10;
        private int Triangle_Index = 0;
        private int Section_Index = 0;
        private List<Point3D> Points;
        private int Last_End_Vertex = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private byte[][] Get_Model_Sections(byte[] Model_Data, ref List<OffsetIncrementType> Section_EndType)
        {
            List<byte[]> Sections = new List<byte[]>();
            for (int i = 0; i < Model_Data.Length; i++)
            {
                byte Current_Byte = Model_Data[i];
                if (Current_Byte == 0x0A) // Section start
                {
                    int Size = 0;
                    if (Model_Data[i + 1] > 6)
                        Size = Model_Data[i + 1] + 6; // Add 6 for the first "subsection" which only has 6 bytes (subsections are 8 bytes long)
                    else
                        Size = 8; // If we're <= 6 then we've only got that first 3 face section (6 bytes)

                    Size += Size % 8; // When we don't get 8 byte aligned

                    Sections.Add(Model_Data.Skip(i + 1).Take(Size - 1).ToArray()); // Size - 1 because otherwise we'll end up with an extra byte at the end
                    Debug.WriteLine(string.Format("Adding section #{0} with a length of 0x{1}", Sections.Count - 1, Sections[Sections.Count - 1].Length.ToString("X")));

                    byte End_Byte = Model_Data[i + Size];
                    if (End_Byte == 0xD9 || End_Byte == 0xFD || End_Byte == 0xDF)
                    {
                        Section_EndType.Add((OffsetIncrementType)End_Byte);
                        i += Size + 1; // Skip all the data we just when through
                    }
                    else
                    {
                        Section_EndType.Add(OffsetIncrementType.NoIncrement);
                        i += Size; // Skip all the data we just when through
                    }

                    Debug.WriteLine("i after skip: " + i + " | Value: " + Model_Data[i].ToString("X2"));
                }
            }
            return Sections.ToArray();
        }

        private byte[] Section_to_Nibbles(byte[] Section)
        {
            byte[] Nibbles = new byte[Section.Length * 2];
            for (int i = 0; i < Section.Length; i++)
            {
                int Idx = i * 2;
                Nibbles[Idx] = (byte)((Section[i] >> 4) & 0x0F);
                Nibbles[Idx + 1] = (byte)(Section[i] & 0x0F);
            }
            return Nibbles;
        }

        private void Decrypt_Vertex_Indices(List<Point3D> Vertices, byte[] Nibbles, byte Vertex_Offset = 0, OffsetIncrementType End_Type = OffsetIncrementType.NoIncrement)
        {
            List<Point3D> Decrypted_Vertices = new List<Point3D>();
            int Multiplier = 1;
            int End_Value = 32;//Vertices.Count % 16 == 0 ? Vertices.Count : Vertices.Count + (16 - (Vertices.Count % 16));
            int Current_Nibble = 0;
            int Next_Nibble = 0;
            string bytes = "";
            
            int Multiplier_Index = 3;
            int[] Actual_Values = new int[Nibbles.Length];
            for (int i = 1; i < Nibbles.Length; i++) // Skip the first one to avoid the 0
            {
                Current_Nibble = Nibbles[i];
                Next_Nibble = (i + 1) >= Nibbles.Length ? 0 : Nibbles[i + 1];
                int Actual_Index = 0;


                // TODO: We can just remove the "skip" algorithm by not adding values whose multiplier is equal to 1
                switch (Multiplier_Index % 16)
                {
                    case 15:
                        Actual_Index = (Current_Nibble - 1) / 2;
                        Actual_Index += Vertex_Offset;
                        Actual_Values[i] = Actual_Index;
                        Multiplier = 0;

                        if (End_Type == OffsetIncrementType.Increment && Last_End_Vertex < Actual_Index)
                            Last_End_Vertex = Actual_Index;
                        break;
                    default:
                        Multiplier = AC_Vector.New_Multipliers[Multiplier_Index % 16];

                        Actual_Index = (Current_Nibble * Multiplier + (Next_Nibble * Multiplier) / 0x10) % End_Value;
                        Actual_Index += Vertex_Offset;
                        //Actual_Index %= Vertices.Count;

                        if (Actual_Index >= Vertices.Count)
                        {
                            Debug.WriteLine(string.Format("Actual_Index was greater than total array size. Subtracting 16 from modulus. Index: {1} | Pre-sub value: {0}", Actual_Index.ToString("X"), i));
                            //Actual_Index = Actual_Index % (End_Value - 16);
                        }

                        if (Last_End_Vertex < Actual_Index)
                            Last_End_Vertex = Actual_Index;

                        Actual_Values[i] = Actual_Index;
                        break;
                }

                Multiplier_Index++;
                bytes = bytes + string.Format("Nibble Index: {0} | Current_Nibble: 0x{1} | Next_Nibble: 0x{2} | Multiplier: {3} | Actual_Index: 0x{4}\n", i, Current_Nibble.ToString("X"), Next_Nibble.ToString("X"), Multiplier, Actual_Index.ToString("X"));
            }

            // Increment Last_End_Vertex to start on the next value
            if (true || End_Type == OffsetIncrementType.Increment)
                Last_End_Vertex++;

            // Actual Values to Vertices (Using Skip Algorithm)
            int Skip = 1;
            for (int i = 1; i < Actual_Values.Length; i += 4)
            {
                var Value_A = i < Actual_Values.Length ? Actual_Values[i] : 0;
                var Value_B = (i + 1) < Actual_Values.Length ? Actual_Values[i + 1] : 0;
                var Value_C = (i + 2) < Actual_Values.Length ? Actual_Values[i + 2] : 0;
                var Value_D = (i + 3) < Actual_Values.Length ? Actual_Values[i + 3] : 0;

                try
                {
                    if (Skip % 4 == 0)
                    {
                        Create_Triangle_Mesh(Vertices[Value_B], Vertices[Value_C], Vertices[Value_D], Triangle_Index, Value_B, Value_C, Value_D);
                    }
                    else if (Skip % 4 == 1)
                    {
                        Create_Triangle_Mesh(Vertices[Value_A], Vertices[Value_C], Vertices[Value_D], Triangle_Index, Value_A, Value_C, Value_D);
                    }
                    else if (Skip % 4 == 2)
                    {
                        Create_Triangle_Mesh(Vertices[Value_A], Vertices[Value_B], Vertices[Value_D], Triangle_Index, Value_A, Value_B, Value_D);
                    }
                    else if (Skip % 4 == 3)
                    {
                        Create_Triangle_Mesh(Vertices[Value_A], Vertices[Value_B], Vertices[Value_C], Triangle_Index, Value_A, Value_B, Value_C);
                    }
                    else
                    {
                        throw new Exception("Skip modulus was invalid!");
                    }
                }
                catch { }

                Skip++;
                Triangle_Index++;
            }

            Debug.WriteLine("Output for section #" + Section_Index + " | Vertex Offset: 0x" + Last_End_Vertex.ToString("X2"));
            Debug.WriteLine(bytes);
            Section_Index++;
        }

        private void LoadModel(string Model_Location, bool New_File = true)
        {
            byte[] Data_Array = File.ReadAllBytes(Model_Location);
            Last_End_Vertex = 0;
            
            short[] Data = new short[Data_Array.Length / 2];

            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] = (short)((Data_Array[i * 2] << 8) + Data_Array[i * 2 + 1]);
            }

            // Convert to AC_Vectors and Point3Ds
            AC_Vector[] Vectors = new AC_Vector[Data.Length / 8]; // 8 shorts per Vector
            Points = new List<Point3D>();
            List<Point3D> Skipped_List = new List<Point3D>();
            Point3DCollection PointCollection = new Point3DCollection();
            for (int i = 0; i < Vectors.Length; i++)
            {
                Vectors[i] = new AC_Vector(Data.Skip(8 * i).Take(8).ToArray());
                Points.Add(Vectors[i].ToPoint3D());
                PointCollection.Add(Vectors[i].ToPoint3D());
            }

            //ModelPoints.Color = Colors.White;
            //ModelPoints.Points = PointCollection;
            //ModelPoints.Size = 10;

            // Generate 3D Model From Points
            string File_Name = Path.GetFileNameWithoutExtension(Model_Location);
            if (File_Name.Substring(File_Name.Length - 2, 2).Equals("_v") && Model_Select_Dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                File_Name = Model_Select_Dialog.FileName;
                if (!File_Name.ToLower().Contains("model"))
                    return;

                string Model_Path = File_Name;
                if (File.Exists(Model_Path))
                {
                    byte[] Model_Data = File.ReadAllBytes(Model_Path);
                    ModelGroup = new Model3DGroup();

                    List<OffsetIncrementType> Section_End_Types = new List<OffsetIncrementType>();
                    byte[][] Sections = Get_Model_Sections(Model_Data, ref Section_End_Types);
                    for (int i = 0; i < Sections.Length; i++)
                    {
                        byte[] Section_Nibbles = Section_to_Nibbles(Sections[i]);
                        Decrypt_Vertex_Indices(Points, Section_Nibbles, (byte)Last_End_Vertex, Section_End_Types[i]);
                    }

                    ModelVisualizer.Content = ModelGroup;
                }
                else
                {
                    MessageBox.Show(Model_Path);
                }
            }
            else
            {
                MessageBox.Show("Couldn't find the _model file!");
            }
        }

        // Creates a triangle from 3 points and gives it a (probably) unique color to help determine which triangle it is.
        // NOTE: White is the color of the back of the triangle. It is possible for the front to have it, but it's unlikely. Check the debug output for #FFFFFFFF if you think it might have been chosen.
        private void Create_Triangle_Mesh(Point3D A, Point3D B, Point3D C, int Index = 0, int A_Value = 0, int B_Value = 0, int C_Value = 0)
        {
            //if (Model_Colors[Color_Index].Equals(Colors.White))
                //Color_Index++; // Attempt to skip any white colors for the face. Only skips the first so if there's more than one in a row it'll still be white.

            if (A.Equals(B) || B.Equals(C) || A.Equals(C))
                Debug.WriteLine(string.Format("One or more points have the same value! Triangle wont be formed! Index: {0} | Point A: {1} | Point B: {2} | Point C: {3}", Index, A, B, C));
            else
                Debug.WriteLine(string.Format("Creating triangle #{0} Vertex A: {1} | Vertex B: {2} | Vertex C: {3} | Index A: {4} | Index B: {5} | Index C: {6}", Index, A, B, C, A_Value.ToString("X2"), B_Value.ToString("X2"), C_Value.ToString("X2")));

            //Console.WriteLine(string.Format("Creating Triangle #{0} with Color of {1}", Index, Model_Colors[Color_Index]));
            MeshBuilder Builder = new MeshBuilder(false, false);
            Builder.AddTriangle(A, B, C);
            ModelGroup.Children.Add(new GeometryModel3D { Geometry = Builder.ToMesh(true), BackMaterial = MaterialHelper.CreateMaterial(Colors.White), Material = MaterialHelper.CreateMaterial(Colors.White) });
            //Color_Index++;
        }

        private static List<Color> ColorStructToList()
        {
            return typeof(Colors).GetProperties(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public)
                                .Select(c => (Color)c.GetValue(null, null))
                                .ToList();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            if (Model_Select_Dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LoadModel(Model_Select_Dialog.FileName);
            }
        }

        private void Add_Model_Click(object sender, RoutedEventArgs e)
        {
            if (ModelGroup != null && Model_Select_Dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string File_Name = Model_Select_Dialog.FileName;
                if (!File_Name.ToLower().Contains("model"))
                    return;

                string Model_Path = File_Name;
                if (File.Exists(Model_Path))
                {
                    byte[] Model_Data = File.ReadAllBytes(Model_Path);
                    byte[] Start_Face_Data = new byte[0];
                    int Model_Data_Start = 0x49;
                    Last_End_Vertex = 0;

                    // Search for the first four 00's followed by 0A
                    for (int i = 0; i < Model_Data.Length; i++)
                    {
                        if (i + 4 >= Model_Data.Length)
                            MessageBox.Show("Couldn't find model data!");

                        if (Model_Data[i] == 0x00 && Model_Data[i + 1] == 0x00 && Model_Data[i + 2] == 0x00 && Model_Data[i + 3] == 0x00 && Model_Data[i + 4] == 0x0A)
                        {
                            Model_Data_Start = i + 5;
                            break;
                        }
                    }

                    List<OffsetIncrementType> Section_End_Types = new List<OffsetIncrementType>();
                    byte[][] Sections = Get_Model_Sections(Model_Data.Skip(Model_Data_Start).ToArray(), ref Section_End_Types);
                    for (int i = 0; i < Sections.Length; i++)
                    {
                        byte[] Section_Nibbles = Section_to_Nibbles(Sections[i]);
                        Decrypt_Vertex_Indices(Points, Section_Nibbles, 9, Section_End_Types[i]);
                    }

                    ModelVisualizer.Content = ModelGroup;
                }
                else
                {
                    MessageBox.Show(Model_Path);
                }
            }
            else
            {
                MessageBox.Show("Couldn't find the _model file!");
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var Save_File_Dialog = new System.Windows.Forms.SaveFileDialog();
            Save_File_Dialog.Filter = Exporters.Filter;
            Save_File_Dialog.DefaultExt = Exporters.DefaultExtension;
            var Exporter = new ObjExporter();

            if (Save_File_Dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                using (FileStream Stream = new FileStream(Save_File_Dialog.FileName, FileMode.OpenOrCreate))
                {
                    Exporter.MaterialsFile = Path.GetDirectoryName(Save_File_Dialog.FileName) + "\\" + Path.GetFileNameWithoutExtension(Save_File_Dialog.FileName) + ".mtl";
                    //var MP = ModelPoints.Points;
                    //ModelPoints.Points = null; // Don't export vertices
                    Exporter.Export(viewPort3d.Viewport, Stream);
                    //ModelPoints.Points = MP;
                }
            }
        }
    }
}
