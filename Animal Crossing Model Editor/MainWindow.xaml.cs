﻿using System;
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

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadModel(string Model_Location)
        {
            byte[] Data_Array = File.ReadAllBytes(Model_Location);
            
            short[] Data = new short[Data_Array.Length / 2];

            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] = (short)((Data_Array[i * 2] << 8) + Data_Array[i * 2 + 1]);
            }

            // Convert to AC_Vectors and Point3Ds
            AC_Vector[] Vectors = new AC_Vector[Data.Length / 8]; // 8 shorts per Vector
            List<Point3D> Points = new List<Point3D>();
            List<Point3D> Skipped_List = new List<Point3D>();
            Point3DCollection PointCollection = new Point3DCollection();
            for (int i = 0; i < Vectors.Length; i++)
            {
                Vectors[i] = new AC_Vector(Data.Skip(8 * i).Take(8).ToArray());
                Points.Add(Vectors[i].ToPoint3D());
                PointCollection.Add(Vectors[i].ToPoint3D());
            }

            ModelPoints.Color = Colors.White;
            ModelPoints.Points = PointCollection;
            ModelPoints.Size = 10;

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
                    byte[] Start_Face_Data = new byte[0];
                    int Model_Data_Start = 0x49;

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

                    int Data_Size = Model_Data[Model_Data_Start];

                    // Get start face data
                    for (int i = Model_Data.Length - 1; i >= 0; i--)
                    {
                        if (Model_Data[i] == 0xDF)
                        {
                            Start_Face_Data = Model_Data.Skip(i - 3).Take(3).ToArray();
                            break;
                        }
                    }

                    Model_Data = Model_Data.Skip(Model_Data_Start + 1).Take(Data_Size).ToArray();

                    ModelGroup = new Model3DGroup();
                    MeshBuilder Builder = new MeshBuilder(false, false);

                    byte[] Nibbles = new byte[Model_Data.Length * 2];
                    for (int i = 0; i < Model_Data.Length; i++)
                    {
                        int idx = i * 2;
                        Nibbles[idx] = (byte)((Model_Data[i] >> 4) & 0x0F);
                        Nibbles[idx + 1] = (byte)(Model_Data[i] & 0x0F);
                    }

                    int[] Actual_Values = new int[Nibbles.Length];

                    int Multiplier = 1;
                    int End_Value = Points.Count % 16 == 0 ? Points.Count : Points.Count + (16 - (Points.Count % 16));
                    int Current_Nibble = 0;
                    int Next_Nibble = 0;

                    string bytes = "";


                    // Add start face data (TODO: Scan linearly until we reach 0xFD (continue + 0x10 bytes later) OR until we reach 0xDF (end of model data)
                    //Array.Resize(ref Actual_Values, Actual_Values.Length + 3);
                    int A = Start_Face_Data[0] & 0x0F;
                    int B = (Start_Face_Data[1] >> 4) & 0x0F;
                    int C = Start_Face_Data[1] & 0x0F;
                    int D = (Start_Face_Data[2] >> 4) & 0x0F;

                    // NOTE: I *think* these multiplier values are correct. You should probably double check them.
                    int A_Value = (A * 4 + (B * 4) / 0x10) % End_Value;
                    int B_Value = (B * 8 + (C * 8) / 0x10) % End_Value;
                    int C_Value = (C * 16 + (D * 16) / 0x10) % End_Value;

                    Skipped_List.Add(Points[(D * 1 + (0 * 1) / 0x10) % End_Value]); // Is this the first skipped or last skipped?

                    Create_Triangle_Mesh(Points[A_Value], Points[B_Value], Points[C_Value]);

                    bytes = bytes + string.Format("Nibble Index: {0} | Current_Nibble: 0x{1} | Next_Nibble: 0x{2} | Multiplier: {3} | Actual_Index: 0x{4}\n", Actual_Values.Length - 3, Current_Nibble.ToString("X"), Next_Nibble.ToString("X"), 4, A_Value.ToString("X"));
                    bytes = bytes + string.Format("Nibble Index: {0} | Current_Nibble: 0x{1} | Next_Nibble: 0x{2} | Multiplier: {3} | Actual_Index: 0x{4}\n", Actual_Values.Length - 2, Current_Nibble.ToString("X"), Next_Nibble.ToString("X"), 8, B_Value.ToString("X"));
                    bytes = bytes + string.Format("Nibble Index: {0} | Current_Nibble: 0x{1} | Next_Nibble: 0x{2} | Multiplier: {3} | Actual_Index: 0x{4}\n", Actual_Values.Length - 1, Current_Nibble.ToString("X"), Next_Nibble.ToString("X"), 16, C_Value.ToString("X"));
                    bytes = bytes + string.Format("Nibble Index: {0} | Current_Nibble: 0x{1} | Next_Nibble: 0x{2} | Multiplier: {3} | Actual_Index: 0x{4}\n", Actual_Values.Length - 0, Current_Nibble.ToString("X"), Next_Nibble.ToString("X"), 1, ((D * 1 + (0 * 1) / 0x10) % End_Value).ToString("X"));

                    Triangle_Index++;

                    for (int i = 0; i < Nibbles.Length; i++)
                    {
                        Current_Nibble = Nibbles[i];
                        Next_Nibble = (i + 1) >= Nibbles.Length ? 0 : Nibbles[i + 1];
                        int Actual_Index = 0;

                        switch (i % 16)
                        {
                            case 11:
                                Actual_Index = (Current_Nibble - 1) / 2;
                                Actual_Values[i] = Actual_Index;
                                break;
                            default:
                                Multiplier = AC_Vector.Multipliers[i % 16];

                                Actual_Index = (Current_Nibble * Multiplier + (Next_Nibble * Multiplier) / 0x10) % End_Value;
                                if (Actual_Index >= Points.Count)
                                {
                                    Debug.WriteLine(string.Format("Actual_Index was greater than total array size. Subtracting 16 from modulus. Pre-sub value: {0}", Actual_Index.ToString("X")));
                                    Actual_Index = Actual_Index % (End_Value - 16);
                                }

                                Actual_Values[i] = Actual_Index;
                                break;
                        }

                        bytes = bytes + string.Format("Nibble Index: {0} | Current_Nibble: 0x{1} | Next_Nibble: 0x{2} | Multiplier: {3} | Actual_Index: 0x{4}\n", i, Current_Nibble.ToString("X"), Next_Nibble.ToString("X"), Multiplier, Actual_Index.ToString("X"));
                    }

                    Debug.WriteLine(bytes);

                    // NOTE: You should definitely check this part. I think this is wrong.
                    int Skip = 2;
                    for (int i = 0; i < Actual_Values.Length; i += 4)
                    {
                        if (i == 0)
                        {
                            //Builder.AddTriangle(Points[Actual_Values[0]], Points[Actual_Values[1]], Points[Actual_Values[2]]);
                            Create_Triangle_Mesh(Points[Actual_Values[0]], Points[Actual_Values[1]], Points[Actual_Values[2]], Triangle_Index);
                            i--; // Subtract one so we start at 3
                        }
                        else
                        {
                            try
                            {
                                if (Skip % 4 == 0)
                                {
                                    //Builder.AddTriangle(Points[Actual_Values[i + 1]], Points[Actual_Values[i + 2]], Points[Actual_Values[i + 3]]);
                                    Create_Triangle_Mesh(Points[Actual_Values[i + 1]], Points[Actual_Values[i + 2]], Points[Actual_Values[i + 3]], Triangle_Index);
                                    Skipped_List.Add(Points[Actual_Values[i]]);
                                    //Debug.WriteLine("Adding Skipped Point: " + Actual_Values[i].ToString("X2"));
                                }
                                else if (Skip % 4 == 1)
                                {
                                    //Builder.AddTriangle(Points[Actual_Values[i]], Points[Actual_Values[i + 2]], Points[Actual_Values[i + 3]]);
                                    Create_Triangle_Mesh(Points[Actual_Values[i]], Points[Actual_Values[i + 2]], Points[Actual_Values[i + 3]], Triangle_Index);
                                    Skipped_List.Add(Points[Actual_Values[i + 1]]);
                                    //Debug.WriteLine("Adding Skipped Point: " + Actual_Values[i + 1].ToString("X2"));
                                }
                                else if (Skip % 4 == 2)
                                {
                                    //Builder.AddTriangle(Points[Actual_Values[i]], Points[Actual_Values[i + 1]], Points[Actual_Values[i + 3]]);
                                    Create_Triangle_Mesh(Points[Actual_Values[i]], Points[Actual_Values[i + 1]], Points[Actual_Values[i + 3]], Triangle_Index);
                                    Skipped_List.Add(Points[Actual_Values[i + 2]]);
                                    //Debug.WriteLine("Adding Skipped Point: " + Actual_Values[i + 2].ToString("X2"));
                                }
                                else if (Skip % 4 == 3)
                                {
                                    Create_Triangle_Mesh(Points[Actual_Values[i]], Points[Actual_Values[i + 1]], Points[Actual_Values[i + 2]], Triangle_Index);

                                    //Builder.AddTriangle(Points[Actual_Values[i]], Points[Actual_Values[i + 1]], Points[Actual_Values[i + 2]]);
                                    //Create_Triangle_Mesh(Points[Actual_Values[i]], Points[Actual_Values[i + 1]], Points[Actual_Values[i + 2]], Triangle_Index);
                                    Skipped_List.Add(Points[Actual_Values[i + 3]]);
                                    Debug.WriteLine("Adding Skipped Point % = 3: " + Actual_Values[i + 3].ToString("X2"));
                                }
                                else
                                    throw new Exception("Skip modulus was invalid!");
                            }
                            catch { MessageBox.Show("Skip failed for index: " + i); }
                            Skip++;
                        }
                        Triangle_Index++;
                    }

                    Debug.WriteLine("Skipped_List Count: " + Skipped_List.Count);
                    
                    // Add Skipped Triangles
                    for (int i = 0; i < Skipped_List.Count; i += 3)
                    {
                        if (i + 2 >= Skipped_List.Count)
                            break;
                        //Builder.AddTriangle(Skipped_List[i], Skipped_List[i + 1], Skipped_List[i + 2]);
                        //Create_Triangle_Mesh(Skipped_List[i], Skipped_List[i + 1], Skipped_List[i + 2], Triangle_Index);
                        Triangle_Index++;
                    }

                    //MeshGeometry3D Mesh = Builder.ToMesh(true);

                    //ModelGroup.Children.Add(new GeometryModel3D { Geometry = Mesh, Material = MaterialHelper.CreateMaterial(Colors.Red), BackMaterial = MaterialHelper.CreateMaterial(Colors.Yellow) });

                    Model3D Model = ModelGroup;
                    ModelVisualizer.Content = Model;
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
        private void Create_Triangle_Mesh(Point3D A, Point3D B, Point3D C, int Index = 0)
        {
            if (Model_Colors[Color_Index].Equals(Colors.White))
                Color_Index++; // Attempt to skip any white colors for the face. Only skips the first so if there's more than one in a row it'll still be white.

            if (A.Equals(B) || B.Equals(C) || A.Equals(C))
                Debug.WriteLine(string.Format("One or more points have the same value! Triangle wont be formed! Index: {0} | Point A: {1} | Point B: {2} | Point C: {3}", Index, A, B, C));
            else
                Debug.WriteLine(string.Format("Creating triangle #{0} Vertex A: {1} | Vertex B: {2} | Vertex C: {3}", Index, A, B, C));
            Console.WriteLine(string.Format("Creating Triangle #{0} with Color of {1}", Index, Model_Colors[Color_Index]));
            MeshBuilder Builder = new MeshBuilder(false, false);
            Builder.AddTriangle(A, B, C);
            ModelGroup.Children.Add(new GeometryModel3D { Geometry = Builder.ToMesh(true), BackMaterial = MaterialHelper.CreateMaterial(Colors.White), Material = MaterialHelper.CreateMaterial(Model_Colors[Color_Index]) });
            Color_Index++;
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
    }
}
