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
using System.ComponentModel;

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
        private BackgroundWorker Parser_Worker = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadModel(string Model_Location, bool New_File = true)
        {
            byte[] Data_Array = File.ReadAllBytes(Model_Location);
            Last_End_Vertex = 0;
            Section_Index = 0;
            Triangle_Index = 0;
            
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

                    var Faces = ModelParser.ParseModel(Model_Data, Points);
                    foreach (Point3D[] Face in Faces)
                    {
                        Create_Triangle_Mesh(Face[0], Face[1], Face[2], 0, 0, 0, 0);
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
                    Section_Index = 0;

                    var Faces = ModelParser.ParseModel(Model_Data, Points);
                    foreach (Point3D[] Face in Faces)
                    {
                        Create_Triangle_Mesh(Face[0], Face[1], Face[2], 0, 0, 0, 0);
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

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Import texture files (this means converting it from the AC format to a bitmap)
        }
    }
}
