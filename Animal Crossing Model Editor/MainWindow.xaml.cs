using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.IO;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using System.Reflection;
using System.ComponentModel;
using System.Windows.Controls;

namespace Animal_Crossing_Model_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static System.Windows.Forms.OpenFileDialog Model_Select_Dialog = new System.Windows.Forms.OpenFileDialog();
        private static Model3DGroup ModelGroup;
        public Color PrimitiveColor = Colors.White;
        private List<Point3D> Points;
        private List<PointsVisual3D> PointsVisual3DList;
        private List<GeometryModel3D> Model3DList;
        private BackgroundWorker Parser_Worker = new BackgroundWorker();
        private PointsVisual3D LastPoint3D;
        private GeometryModel3D LastModel3D;
        private Material LastModel3DMaterial;
        private int FaceIndex = 0;
        private Material SelectedModel3DMaterial = MaterialHelper.CreateMaterial(Colors.LightBlue);
        private MeshBuilder Builder;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadModel(string Model_Location, bool New_File = true)
        {
            LastPoint3D = null;
            LastModel3D = null;
            FaceIndex = 0;

            FaceTreeView.Items.Clear();

            if (PointsVisual3DList != null)
            {
                foreach (PointsVisual3D p in PointsVisual3DList)
                {
                    if (viewPort3d.Children.Contains(p))
                    {
                        viewPort3d.Children.Remove(p);
                    }
                }
            }

            PointsVisual3DList = new List<PointsVisual3D>();
            Model3DList = new List<GeometryModel3D>();

            byte[] Data_Array = File.ReadAllBytes(Model_Location);
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

                /*PointsVisual3D point = new PointsVisual3D
                {
                    Color = Colors.White,
                    Size = 10,
                    Points = new Point3DCollection { Vectors[i].ToPoint3D() }
                };
                PointsVisual3DList.Add(point);
                viewPort3d.Children.Add(point);*/
            }

            /*ModelPoints.Color = Colors.White;
            ModelPoints.Points = PointCollection;
            ModelPoints.Size = 10;*/

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

                    ModelParser.ParseModel(Model_Data, Points, this);

                    ModelVisualizer.Content = ModelGroup;
                    viewPort3d.ZoomExtents();
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

        private void VertexTreeViewItemSelected(object sender, RoutedEventArgs e)
        {
            if (LastPoint3D != null)
            {
                LastPoint3D.Color = Colors.White;
            }

            LastPoint3D = ((sender as TreeViewItem).Tag as PointsVisual3D);
            LastPoint3D.Color = Colors.LightGreen;
        }

        private void FaceTreeViewitemSelected(object sender, RoutedEventArgs e)
        {
            if (LastModel3D != null)
            {
                LastModel3D.BackMaterial = LastModel3DMaterial;
                LastModel3D.Material = LastModel3DMaterial;
            }

            LastModel3D = ((sender as TreeViewItem).Tag as GeometryModel3D);
            LastModel3DMaterial = LastModel3D.Material;

            LastModel3D.Material = SelectedModel3DMaterial;
            LastModel3D.BackMaterial = SelectedModel3DMaterial;
        }

        private void AddFaceTreeViewItem(int[] PointIndices)
        {
            TreeViewItem[] VertexItems = new TreeViewItem[3];

            for (int i = 0; i < 3; i++)
            {
                VertexItems[i] = new TreeViewItem
                {
                    Foreground = new SolidColorBrush(Colors.White),
                    Header = "Vertex " + PointIndices[i].ToString(),
                    Tag = PointsVisual3DList[PointIndices[i]]
                };

                VertexItems[i].Selected += VertexTreeViewItemSelected;
            }

            TreeViewItem FaceItem = new TreeViewItem
            {
                Foreground = new SolidColorBrush(Colors.White),
                Header = "Face " + FaceIndex,
                Tag = Model3DList[FaceIndex]
            };

            FaceItem.Selected += FaceTreeViewitemSelected;

            for (int i = 0; i < 3; i++)
            {
                FaceItem.Items.Add(VertexItems[i]);
            }

            FaceTreeView.Items.Add(FaceItem);

            FaceIndex++;
        }

        public void CreateNewMeshBuilder()
        {
            Builder = new MeshBuilder(false, false);
        }

        // Creates a triangle mesh from 3 points.
        // TODO: Make a new ModelGroup3D for each face so it can be highlighted
        public void CreateTriangleFace(Point3D vertexA, Point3D vertexB, Point3D vertexC, int indexA, int indexB, int indexC)
        {
            Builder.AddTriangle(vertexA, vertexB, vertexC);
            //AddFaceTreeViewItem(new int[3] { indexA, indexB, indexC });
        }

        public void CreateCurrentModel()
        {
            var Model = new GeometryModel3D
            {
                Geometry = Builder.ToMesh(true),
                BackMaterial = MaterialHelper.CreateMaterial(PrimitiveColor),
                Material = MaterialHelper.CreateMaterial(PrimitiveColor)
            };

            ModelGroup.Children.Add(Model);
            Model3DList.Add(Model);
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

                    ModelParser.ParseModel(Model_Data, Points, this);

                    ModelVisualizer.Content = ModelGroup;
                    viewPort3d.ZoomExtents();
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
            var Save_File_Dialog = new System.Windows.Forms.SaveFileDialog
            {
                Filter = Exporters.Filter,
                DefaultExt = Exporters.DefaultExtension
            };
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

        private void viewPort3d_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var Hits = Viewport3DHelper.FindHits(viewPort3d.Viewport, e.GetPosition(viewPort3d.Viewport));
            if (Hits.Count > 0)
            {
                var FirstHit = Hits[0].Visual;
                {
                    if (FirstHit is PointsVisual3D)
                    {
                        if (FirstHit != LastPoint3D)
                        {
                            if (LastPoint3D != null)
                            {
                                LastPoint3D.Color = Colors.White;
                            }

                            LastPoint3D = FirstHit as PointsVisual3D;
                            LastPoint3D.Color = Colors.LightGreen;
                            return;
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }

            // If no valid points were found, set the last point's color to white
            if (LastPoint3D != null)
            {
                LastPoint3D.Color = Colors.White;
                LastPoint3D = null;
            }
        }

        private void RotateVertexPoints(Matrix3D Matrix)
        {
            if (PointsVisual3DList != null)
            {
                var Transform = new MatrixTransform3D(Matrix);
                foreach (PointsVisual3D p in PointsVisual3DList)
                {
                    p.Transform = Transform;
                }
            }
        }

        private int RotX = 0, RotY = 0, RotZ = 0;
        private void RotateXClick(object sender, RoutedEventArgs e)
        {
            var Axis = new Vector3D(0, 0, 1);
            RotX += 90;

            var Matrix = ModelGroup.Transform.Value;
            Matrix.Rotate(new Quaternion(Axis, RotX));

            ModelGroup.Transform = new MatrixTransform3D(Matrix);
            ModelPoints.Transform = new MatrixTransform3D(Matrix);
            RotateVertexPoints(Matrix);
        }

        private void RotateYClick(object sender, RoutedEventArgs e)
        {
            var Axis = new Vector3D(0, 1, 0);
            RotY += 90;

            var Matrix = ModelGroup.Transform.Value;
            Matrix.Rotate(new Quaternion(Axis, RotY));

            ModelGroup.Transform = new MatrixTransform3D(Matrix);
            ModelPoints.Transform = new MatrixTransform3D(Matrix);
            RotateVertexPoints(Matrix);
        }

        private void RotateZClick(object sender, RoutedEventArgs e)
        {
            var Axis = new Vector3D(1, 0, 0);
            RotZ += 90;

            var Matrix = ModelGroup.Transform.Value;
            Matrix.Rotate(new Quaternion(Axis, RotZ));

            ModelGroup.Transform = new MatrixTransform3D(Matrix);
            ModelPoints.Transform = new MatrixTransform3D(Matrix);
            RotateVertexPoints(Matrix);
        }
    }
}
