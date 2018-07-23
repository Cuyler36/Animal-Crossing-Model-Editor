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
using System.Windows.Media.Imaging;
using System;

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
        public BitmapSource Texture;
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
        private FileStream RAMStream;
        private BinaryReader RAMReader;
        public int TextureIndex = 0;
        public List<KeyValuePair<string, BitmapSource>> TextureList = new List<KeyValuePair<string, BitmapSource>>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadModelFromRAMDump(string Dump_Location)
        {
            if (File.Exists(Dump_Location))
            {
                var DLAddressBox = new DrawListAddressBox();
                if (DLAddressBox.ShowDialog().Value)
                {
                    if (RAMReader != null)
                    {
                        RAMReader.Close();
                        RAMStream.Dispose();
                        RAMReader.Dispose();
                    }

                    PointsVisual3DList = new List<PointsVisual3D>();
                    Model3DList = new List<GeometryModel3D>();
                    uint DrawListModelAddress = DLAddressBox.Address & ~0x80000000;
                    var FStream = new FileStream(Dump_Location, FileMode.Open);
                    var Reader = new BinaryReader(FStream);

                    RAMStream = FStream;
                    RAMReader = Reader;

                    Reader.BaseStream.Seek(DrawListModelAddress, SeekOrigin.Begin);

                    bool SetModel = ModelGroup == null;

                    if (ModelGroup == null)
                        ModelGroup = new Model3DGroup();
                    else
                        ModelGroup.Children.Clear();

                    ModelParser.ParseModel(Reader, this);

                    if (SetModel)
                        ModelVisualizer.Content = ModelGroup;
                    viewPort3d.ZoomExtents();
                }
            }
        }

        public Tuple<List<Point3D>, List<AC_Vector>> LoadVertices(byte[] Data_Array)
        {
            short[] Data = new short[Data_Array.Length / 2];
            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] = (short)((Data_Array[i * 2] << 8) + Data_Array[i * 2 + 1]);
            }

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

            return new Tuple<List<Point3D>, List<AC_Vector>>(Points, Vectors.ToList());
        }

        private void LoadModel(string Model_Location, bool New_File = true)
        {
            LastPoint3D = null;
            LastModel3D = null;
            FaceIndex = 0;

            if (RAMReader != null)
            {
                RAMReader.Close();
                RAMReader = null;
            }

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
            
            // Convert to AC_Vectors and Point3Ds
            LoadVertices(Data_Array);

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
            Builder = new MeshBuilder();
        }

        // Creates a triangle mesh from 3 points.
        // TODO: Make a new ModelGroup3D for each face so it can be highlighted
        public void CreateTriangleFace(Point3D vertexA, Point3D vertexB, Point3D vertexC, int indexA, int indexB, int indexC, Tuple<short[], short[]> texCoords)
        {
            short[] texCoordX = texCoords.Item1;
            short[] texCoordY = texCoords.Item2;
            double Scale = 0.03125; //1 / (Texture == null ? 1 : Texture.PixelWidth); // 0.0312;
            Builder.AddTriangle(vertexA, vertexB, vertexC,
                new Point(texCoordX[0] * Scale, texCoordY[0] * Scale),
                new Point(texCoordX[1] * Scale, texCoordY[1] * Scale),
                new Point(texCoordX[2] * Scale, texCoordY[2] * Scale));
            //AddFaceTreeViewItem(new int[3] { indexA, indexB, indexC });
        }

        public void CreateCurrentModel()
        {
            var ImgBrush = new ImageBrush(Texture)
            {
                TileMode = TileMode.Tile,
                Stretch = Stretch.None,
                ViewportUnits = BrushMappingMode.Absolute,
                ViewboxUnits = BrushMappingMode.Absolute,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
            };

            if (Texture != null)
            {
                ImgBrush.Viewport = new Rect(0, 0, Texture.PixelWidth, Texture.PixelHeight);
                ImgBrush.Viewbox = new Rect(0, 0, Texture.PixelWidth, Texture.PixelHeight);
            }

            var Model = new GeometryModel3D
            {
                Geometry = Builder.ToMesh(true),
                //BackMaterial = MaterialHelper.CreateMaterial(PrimitiveColor)
            };

            Model.Material = Texture != null ? new DiffuseMaterial(ImgBrush) : MaterialHelper.CreateMaterial(PrimitiveColor);
            Model.Material.SetName("Texture_" + TextureIndex);

            TextureIndex++;
            if (Texture != null)
            {
                TextureList.Add(new KeyValuePair<string, BitmapSource>("mat" + TextureIndex, Texture));
            }


            ModelGroup.Children.Add(Model);
            Model3DList.Add(Model);
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            if (Model_Select_Dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FileInfo ModelFileInfo = new FileInfo(Model_Select_Dialog.FileName);
                if (ModelFileInfo.Length == 0x01800000)
                {
                    LoadModelFromRAMDump(Model_Select_Dialog.FileName);
                }
                else
                {
                    LoadModel(Model_Select_Dialog.FileName);
                }
            }
        }

        private void Add_Model_Click(object sender, RoutedEventArgs e)
        {
            if (ModelGroup != null)
            {
                if (RAMReader == null)
                {
                    if (Model_Select_Dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
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
                }
                else
                {
                    var DLAddressBox = new DrawListAddressBox();
                    if (DLAddressBox.ShowDialog().Value)
                    {
                        uint DrawListModelAddress = DLAddressBox.Address & ~0x80000000;

                        RAMReader.BaseStream.Seek(DrawListModelAddress, SeekOrigin.Begin);

                        ModelParser.ParseModel(RAMReader, this);

                        ModelVisualizer.Content = ModelGroup;
                        viewPort3d.ZoomExtents();
                    }
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

            if (Save_File_Dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string SaveFolder = Path.GetDirectoryName(Save_File_Dialog.FileName);

                // Save Textures
                foreach (KeyValuePair<string, BitmapSource> Image in TextureList)
                {
                    using (var FStream = new FileStream(SaveFolder + "\\" + Image.Key + ".png", FileMode.Create))
                    {
                        var Encoder = new PngBitmapEncoder();
                        Encoder.Frames.Add(BitmapFrame.Create(Image.Value));
                        Encoder.Save(FStream);
                    }
                }

                // Save Models
                using (FileStream Stream = new FileStream(Save_File_Dialog.FileName, FileMode.Create))
                {
                    //var MP = ModelPoints.Points;
                    //ModelPoints.Points = null; // Don't export vertices
                    var ModelExporter = Exporters.Create(Save_File_Dialog.FileName);
                    if (ModelExporter is ObjExporter)
                    {
                        var OExporter = ModelExporter as ObjExporter;
                        OExporter.MaterialsFile = Path.GetDirectoryName(Save_File_Dialog.FileName) + Path.DirectorySeparatorChar
                            + Path.GetFileNameWithoutExtension(Save_File_Dialog.FileName) + "_Material.mtl";
                        
                        foreach (var Mat in ModelGroup.Children)
                        {
                            Console.WriteLine("Material: " + (Mat as GeometryModel3D).Material.GetName());
                        }
                    }

                    ModelExporter.Export(ModelGroup, Stream);
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
