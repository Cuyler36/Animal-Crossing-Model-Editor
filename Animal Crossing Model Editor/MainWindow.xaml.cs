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

namespace Animal_Crossing_Model_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static System.Windows.Forms.OpenFileDialog Model_Select_Dialog = new System.Windows.Forms.OpenFileDialog();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadModel(string Model_Location)
        {
            // Vecs for now
            byte[] Data_Array = File.ReadAllBytes(Model_Location);
            // TEMP
            //Array.Resize(ref Data_Array, Data_Array.Length - 0x40); // I don't know what the last bit of stuff is
            short[] Data = new short[Data_Array.Length / 2];

            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] = (short)((Data_Array[i * 2] << 8) + Data_Array[i * 2 + 1]);
            }

            // Convert to AC_Vectors and Point3Ds
            AC_Vector[] Vectors = new AC_Vector[Data.Length / 8]; // 8 shorts per Vector
            List<Point3D> Points = new List<Point3D>();
            for (int i = 0; i < Vectors.Length; i++)
            {
                Vectors[i] = new AC_Vector(Data.Skip(8 * i).Take(8).ToArray());
                Points.Add(Vectors[i].ToPoint3D());
            }

            //ModelPoints.Color = Colors.White;
            //ModelPoints.Points = Points;
            //ModelPoints.Size = 10;

            // Generate 3D Model From Points
            string File_Name = Path.GetFileNameWithoutExtension(Model_Location);
            if (File_Name.Substring(File_Name.Length - 2, 2).Equals("_v"))
            {
                File_Name = File_Name.Substring(0, File_Name.Length - 2) + "_model.bin"; // Temp. Implement a way to select the model
                string Model_Path = Path.GetDirectoryName(Model_Location) + "\\" + File_Name;
                if (File.Exists(Model_Path))
                {
                    byte[] Model_Data = File.ReadAllBytes(Model_Path);
                    byte[] Start_Face_Data = new byte[0];
                    int Data_Size = Model_Data[0x49];

                    for (int i = Model_Data.Length - 1; i >= 0; i--)
                    {
                        if (Model_Data[i] == 0xDF)
                        {
                            Start_Face_Data = Model_Data.Skip(i - 4).Take(3).ToArray();
                            break;
                        }
                    }

                    Model_Data = Model_Data.Skip(0x4A).Take(Data_Size).ToArray();
                    Array.Resize(ref Model_Data, Model_Data.Length + 3);
                    Start_Face_Data.CopyTo(Model_Data, Model_Data.Length - 3);

                    Model3DGroup ModelGroup = new Model3DGroup();
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
                   /* }
                    catch (Exception e)
                    {
                        MessageBox.Show("Error: " + e.Message);
                        MessageBox.Show("Line Number: " + e.StackTrace);
                    }*/

                    Debug.WriteLine(bytes);

                    int Skip = 1;

                    List<Point3D> Skipped_List = new List<Point3D>();

                    for (int i = 0; i < Actual_Values.Length; i += 4)
                    {
                        if (i == 0)
                        {
                            Builder.AddTriangle(Points[Actual_Values[0]], Points[Actual_Values[1]], Points[Actual_Values[2]]);
                        }
                        else
                        {
                            try
                            {
                                if (Skip % 4 == 0)
                                {
                                    Builder.AddTriangle(Points[Actual_Values[i + 1]], Points[Actual_Values[i + 2]], Points[Actual_Values[i + 3]]);
                                    Skipped_List.Add(Points[Actual_Values[i]]);
                                }
                                else if (Skip % 4 == 1)
                                {
                                    Builder.AddTriangle(Points[Actual_Values[i]], Points[Actual_Values[i + 2]], Points[Actual_Values[i + 3]]);
                                    Skipped_List.Add(Points[Actual_Values[i + 1]]);
                                }
                                else if (Skip % 4 == 2)
                                {
                                    Builder.AddTriangle(Points[Actual_Values[i]], Points[Actual_Values[i + 1]], Points[Actual_Values[i + 3]]);
                                    Skipped_List.Add(Points[Actual_Values[i + 2]]);
                                }
                                else if (Skip % 4 == 3)
                                {
                                    Builder.AddTriangle(Points[Actual_Values[i]], Points[Actual_Values[i + 1]], Points[Actual_Values[i + 2]]);
                                    // Always 0 so we don't add
                                }
                                else
                                    throw new Exception("Skip modulus was invalid!");
                            }
                            catch { }
                            Skip++;
                        }
                    }

                    Debug.WriteLine("Skipped_List Count: " + Skipped_List.Count);
                    
                    // Add Skipped Triangles
                    for (int i = 1; i < Skipped_List.Count; i += 3)
                    {
                        if (i + 2 >= Skipped_List.Count)
                            break;
                        Builder.AddTriangle(Skipped_List[i], Skipped_List[i + 1], Skipped_List[i + 2]);
                    }

                    MeshGeometry3D Mesh = Builder.ToMesh(true);

                    ModelGroup.Children.Add(new GeometryModel3D { Geometry = Mesh, Material = MaterialHelper.CreateMaterial(Colors.Red), BackMaterial = MaterialHelper.CreateMaterial(Colors.Yellow) });

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

        private double Magnitude(Point3D A, Point3D B)
        {
            double Dx = B.X - A.X;
            double Dy = B.Y - A.Y;
            double Dz = B.Z - A.Z;

            return Math.Sqrt(Dx * Dx + Dy * Dy + Dz * Dz);
        }

        private Point3D[] GetClosestPoints(Point3D Point, Point3DCollection Points)
        {
            Point3D ClosestA = Point;
            Point3D ClosestB = Point;
            double ClosestDistance = double.MaxValue;

            for (int i = 0; i < Points.Count; i++)
            {
                if (Points[i] != Point)
                {
                    double Distance = Magnitude(Point, Points[i]);
                    if (ClosestA == Point || Distance < ClosestDistance)
                    {
                        ClosestA = Points[i];
                        ClosestDistance = Distance;
                    }
                }

                ClosestDistance = double.MaxValue;

                if (Points[i] != Point && Points[i] != ClosestA)
                {
                    double Distance = Magnitude(Point, Points[i]);
                    if (ClosestA == Point || Distance < ClosestDistance)
                    {
                        ClosestB = Points[i];
                        ClosestDistance = Distance;
                    }
                }
            }

            return new Point3D[2] { ClosestA, ClosestB };
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