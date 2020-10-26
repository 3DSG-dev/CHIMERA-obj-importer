using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Chimera_Importer.Utility;

using ITinnovationsLibrary.Functions;

namespace Chimera_Importer
{
    internal struct MeshFace
    {
        public int A, B, C;

        public MeshFace(int a, int b, int c)
        {
            A = a;
            B = b;
            C = c;
        }

        /// <exception cref="ArgumentNullException"><paramref /> is null. </exception>
        /// <exception cref="OverflowException">
        ///     <paramref /> represents a number less than
        ///     <see cref="F:System.Int32.MinValue" /> or greater than <see cref="F:System.Int32.MaxValue" />.
        /// </exception>
        /// <exception cref="FormatException"><paramref /> is not in the correct format. </exception>
        public MeshFace(string a, string b, string c)
        {
            A = int.Parse(a);
            B = int.Parse(b);
            C = int.Parse(c);
        }
    }

    internal class ExportFile
    {
        private readonly string _tempPath = Properties.Settings.Default.TempPath + "\\";
        private const int MaxDBbyte = 1 * 1024 * 1024 * 8;

        private DB _db;

        private double _dbX;
        private double _dbY;
        private double _dbZ;
        private double _xTranslation;
        private double _yTranslation;
        private double _zTranslation;
        private string _srs;

        private readonly ExportElement _exportElement;
        private string _exportFile;
        private string _exportFilePath;

        private string _mtllibFile;
        private Dictionary<int, string[]> _usemtl = new Dictionary<int, string[]>();

        private List<Point3D> _originalVertex = new List<Point3D>();
        private List<Point3D> _originalTextureCoordinates = new List<Point3D>();
        private List<Point3D> _originalNormal = new List<Point3D>();

        private Dictionary<string, List<MeshFace>> _originalFacesVertex = new Dictionary<string, List<MeshFace>>();
        private readonly Dictionary<string, List<MeshFace>> _originalFacesTextureCoordinates = new Dictionary<string, List<MeshFace>>();
        private readonly Dictionary<string, List<MeshFace>> _originalFacesNormal = new Dictionary<string, List<MeshFace>>();

        private readonly List<bool> _originalVertexValid = new List<bool>();
        private readonly List<bool> _originalTextureCoordinatesValid = new List<bool>();
        private readonly List<bool> _originalNormalValid = new List<bool>();

        private List<Color> _originalVertexColor = new List<Color>();

        private int _vertexCount;
        private int _textureCount;
        private int _textureResolution;

        private bool _exportTexture;
        private Dictionary<string, string> _textures = new Dictionary<string, string>();

        ////private readonly Dictionary<int, List<int>> _originalVertexFacesList = new Dictionary<int, List<int>>();

        private readonly Dictionary<string, List<List<Point3D>>> _newVertexList = new Dictionary<string, List<List<Point3D>>>();
        private readonly Dictionary<string, List<List<Point3D>>> _newTextureCoordinatesList = new Dictionary<string, List<List<Point3D>>>();
        private readonly Dictionary<string, List<List<Point3D>>> _newNormalList = new Dictionary<string, List<List<Point3D>>>();
        private readonly Dictionary<string, List<List<MeshFace>>> _newFacesList = new Dictionary<string, List<List<MeshFace>>>();
        private List<List<Color>> _newVertexColor = new List<List<Color>>();
        private List<List<string>> _newVertexColorString = new List<List<string>>();

        private readonly OdbcParameter _dbLayer0 = new OdbcParameter("@Layer0", OdbcType.VarChar, 255);
        private readonly OdbcParameter _dbLayer1 = new OdbcParameter("@Layer1", OdbcType.VarChar, 255);
        private readonly OdbcParameter _dbLayer2 = new OdbcParameter("@Layer2", OdbcType.VarChar, 255);
        private readonly OdbcParameter _dbLayer3 = new OdbcParameter("@Layer3", OdbcType.VarChar, 255);
        private readonly OdbcParameter _dbName = new OdbcParameter("@Name", OdbcType.VarChar, 255);
        private readonly OdbcParameter _dbVersione = new OdbcParameter("@Versione", OdbcType.Int);
        private readonly OdbcParameter _dbTipoModello = new OdbcParameter("@TipoModello", OdbcType.Int);
        private readonly OdbcParameter _dbUser = new OdbcParameter("@User", OdbcType.VarChar, 255);

        public bool Success { get; private set; }

        private static StringBuilder _processError;
        private static StringBuilder _processOutput;

        public object LockProgressObject = new object();

        public float Progress { get; private set; }

        public Point3D Center { get; private set; }

        public double Radius { get; private set; }

        public ExportFile(ExportElement element, bool localCoordinates, double xTranslation, double yTranslation, double zTranslation, string srs)
        {
            _exportElement = element;
            _exportFile = element.Filename;
            _dbX = -xTranslation;
            _dbY = -yTranslation;
            _dbZ = -zTranslation;
            if (localCoordinates)
            {
                _xTranslation = 0;
                _yTranslation = 0;
                _zTranslation = 0;
            }
            else
            {
                _xTranslation = -xTranslation;
                _yTranslation = -yTranslation;
                _zTranslation = -zTranslation;
            }

            _srs = srs;

            Success = false;
            lock (LockProgressObject)
            {
                Progress = 0;
            }
        }

        public void Export()
        {
            _exportFilePath = Path.GetDirectoryName(_exportFile) + "\\";

            switch (_exportElement.Type)
            {
                case "Mesh":
                    _dbTipoModello.Value = 0;
                    ExportOBJ();
                    break;
                case "PointCloud":
                    _dbTipoModello.Value = 1;
                    ExportPointCloudTxt();
                    break;
            }
        }

        private void ExportPointCloudTxt()
        {
            try
            {
                Success = true;

                PreinitializeDB();

                CreateJSONFileFromTxt();

                UpdateLodDB(0);
                UploadJSONThread(0);

                if (Success)
                {
                    RemoveUpdating();
                }
            }
            catch (Exception ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while exporting " + _exportElement.Filename + "!\n\n" + ex.Message);
            }
        }

        private void ExportOBJ()
        {
            try
            {
                Thread uploadOBJ = null;
                Thread[] uploadTexture = new Thread[8];
                Thread[] uploadJSON = new Thread[8];

                Success = true;

                PreinitializeDB();

                lock (LockProgressObject)
                {
                    Progress = 0.5f;
                }

                for (int lod = 0; lod < 8 && Success; lod++)
                {
                    float progressScale = GetProgressScale(lod);

                    if (lod != 0)
                    {
                        ClearLists();

                        CreateTempLod(lod);

                        lock (LockProgressObject)
                        {
                            Progress = Progress + 6;
                        }
                    }

                    CreateJSONFile(lod);
                    UpdateLodDB(lod);

                    uploadJSON[lod] = ThreadTools.StartNewBackgroudThread(UploadJSONThread, lod);

                    if (_exportTexture)
                    {
                        uploadTexture[lod] = ThreadTools.StartNewBackgroudThread(UploadTextureThread, lod);
                    }
                    else
                    {
                        lock (LockProgressObject)
                        {
                            Progress = Progress + progressScale / 6;
                        }
                    }

                    if (lod == 0)
                    {
                        uploadOBJ = ThreadTools.StartNewBackgroudThread(UploadOBJ);
                    }

                    if (_exportFile != _exportElement.Filename)
                    {
                        File.Delete(_exportFile);
                        File.Delete(_exportFile + ".mtl");
                    }
                }

                while (uploadOBJ != null && uploadOBJ.IsAlive)
                {
                    uploadOBJ.Join(500);
                }
                for (int lod = 0; lod < 8; lod++)
                {
                    while (uploadTexture[lod] != null && uploadTexture[lod].IsAlive)
                    {
                        uploadTexture[lod].Join(500);
                    }
                    while (uploadJSON[lod] != null && uploadJSON[lod].IsAlive)
                    {
                        uploadJSON[lod].Join(500);
                    }
                }

                if (Success)
                {
                    RemoveUpdating();

                    lock (LockProgressObject)
                    {
                        Progress = Progress + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while exporting " + _exportElement.Filename + "!\n\n" + ex.Message);
            }
        }

        private float GetProgressScale(int lod)
        {
            switch (lod)
            {
                case 0: return 13;
                case 1: return 12;
                case 2: return 10.5f;
                case 3: return 8;
                case 4: return 5;
                case 5: return 2.5f;
                case 6: return 1.5f;
                //// ReSharper disable once RedundantCaseLabel
                case 7:
                default: return 1;
            }
        }

        ////private void CreateTempOBJ()
        ////{
        ////    string outputFile = TempPath + Path.GetFileNameWithoutExtension(_exportFile) + ".obj";
        ////    Directory.CreateDirectory(TempPath);

        ////    StreamWriter writer = new StreamWriter(outputFile);

        ////    foreach (Point3D vertex in _originalVertex)
        ////    {
        ////        writer.WriteLine("v " + vertex.X.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + vertex.Y.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + vertex.Z.ToString(System.Globalization.CultureInfo.InvariantCulture));
        ////    }
        ////    foreach (Point3D textureCoordinate in _originalTextureCoordinates)
        ////    {
        ////        writer.WriteLine("vt " + textureCoordinate.X.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + textureCoordinate.Y.ToString(System.Globalization.CultureInfo.InvariantCulture));

        ////    }
        ////    foreach (Point3D normal in _originalNormal)
        ////    {
        ////        writer.WriteLine("vn " + normal.X.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + normal.Y.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + normal.Z.ToString(System.Globalization.CultureInfo.InvariantCulture));
        ////    }
        ////    foreach (MeshFace face in _originalFacesVertex)
        ////    {
        ////        writer.WriteLine("f " + face.A + "/" + face.A + "/" + face.A + " " + face.B + "/" + face.B + "/" + face.B + " " + face.C + "/" + face.C + "/" + face.C + " ");
        ////    }

        ////    writer.Close();

        ////    _exportFile = outputFile;
        ////}

        private void ClearLists()
        {
            _newVertexList.Clear();

            _newTextureCoordinatesList.Clear();

            _newNormalList.Clear();

            _newFacesList.Clear();

            _originalVertex.Clear();

            _originalTextureCoordinates.Clear();

            _originalNormal.Clear();

            _originalFacesVertex.Clear();

            _originalFacesTextureCoordinates.Clear();

            _originalFacesNormal.Clear();

            _originalVertexValid.Clear();

            _originalTextureCoordinatesValid.Clear();

            _originalNormalValid.Clear();
        }

        #region CreateLoD
        private void CreateTempLod(int lod)
        {
            try
            {
                string outputFile = (lod == 1 ? _tempPath + Path.GetFileNameWithoutExtension(_exportFile) + "LOD" : _exportFile.Substring(0, _exportFile.Length - 5)) + lod + ".obj";

                string script = null;

                switch (lod)
                {
                    case 1:
                        script = "testscript-1.mlx";
                        break;
                    case 2:
                        script = "testscript-2.mlx";
                        break;
                    case 3:
                        script = "testscript-3.mlx";
                        break;
                    case 4:
                        script = "testscript-4.mlx";
                        break;
                    case 5:
                        script = "testscript-5.mlx";
                        break;
                    case 6:
                        script = "testscript-6.mlx";
                        break;
                    case 7:
                        script = "testscript-7.mlx";
                        break;
                }

                _processError = new StringBuilder();
                _processOutput = new StringBuilder();

                ProcessStartInfo processinfo = new ProcessStartInfo
                                                   {
                                                       FileName = Properties.Settings.Default.MeshLabServer,
                                                       Arguments = ("-i \"" + _exportElement.Filename + "\" -o \"" + outputFile + "\" -m vc vn  fc fn wt -s " + script).Replace('à', '\u00E0'),
                                                       RedirectStandardOutput = true,
                                                       RedirectStandardError = true,
                                                       WindowStyle = ProcessWindowStyle.Hidden,
                                                       UseShellExecute = false,
                                                       LoadUserProfile = false,
                                                       CreateNoWindow = true
                                                   };

                Process process = new Process
                                      {
                                          StartInfo = processinfo
                                      };
                process.Start();

                process.OutputDataReceived += ProcessOutputDataReceived;
                process.ErrorDataReceived += ProcessErrorDataReceived;

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    if (lod > 1)
                    {
                        File.Delete(_exportFile);
                        File.Delete(_exportFile + ".mtl");
                    }
                    _exportFile = outputFile;
                }
                else
                {
                    //// ReSharper disable once ThrowingSystemException
                    throw new Exception(_processError + "\n\n" + _processOutput);
                }
            }
            catch (Exception ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while creating temp LoD!\n\n" + ex.Message);
            }
        }

        ////private void CreateTempLod(int lod)
        ////{
        ////    try
        ////    {
        ////        string outputFile = (lod == 1 ? TempPath + Path.GetFileNameWithoutExtension(_exportFile) + "LOD" : _exportFile.Substring(0, _exportFile.Length - 5)) + lod + ".obj";

        ////        string script = null;

        ////        if (string.IsNullOrWhiteSpace(_exportElement.Texture))
        ////        {
        ////            switch (lod)
        ////            {
        ////                case 1:
        ////                case 2:
        ////                    script = "testscript-09.mlx";
        ////                    break;
        ////                case 3:
        ////                case 4:
        ////                    script = "testscript-08.mlx";
        ////                    break;
        ////                case 5:
        ////                case 6:
        ////                case 7:
        ////                    script = "testscript-07.mlx";
        ////                    break;
        ////            }
        ////        }
        ////        else
        ////        {
        ////            switch (lod)
        ////            {
        ////                case 1:
        ////                case 2:
        ////                    script = "testscript-texture-09.mlx";
        ////                    break;
        ////                case 3:
        ////                case 4:
        ////                    script = "testscript-texture-08.mlx";
        ////                    break;
        ////                case 5:
        ////                case 6:
        ////                case 7:
        ////                    script = "testscript-texture-07.mlx";
        ////                    break;
        ////            }
        ////        }

        ////        _processError = new StringBuilder();
        ////        _processOutput = new StringBuilder();

        ////        ProcessStartInfo processinfo = new ProcessStartInfo
        ////        {
        ////            FileName = @"C:\Program Files\VCG\MeshLab\meshlabserver.exe",
        ////            Arguments = ("-i \"" + _exportFile + "\" -o \"" + outputFile + "\" -s " + script + " -om vc vn vt  fc fn").Replace('à', '\u00E0'),
        ////            RedirectStandardOutput = true,
        ////            RedirectStandardError = true,
        ////            WindowStyle = ProcessWindowStyle.Hidden,
        ////            UseShellExecute = false,
        ////            LoadUserProfile = false,
        ////            CreateNoWindow = true
        ////        };

        ////        Process process = new Process
        ////        {
        ////            StartInfo = processinfo
        ////        };
        ////        process.Start();

        ////        process.OutputDataReceived += ProcessOutputDataReceived;
        ////        process.ErrorDataReceived += ProcessErrorDataReceived;

        ////        process.BeginOutputReadLine();
        ////        process.BeginErrorReadLine();

        ////        process.WaitForExit();

        ////        if (process.ExitCode == 0)
        ////        {
        ////            if (lod > 1)
        ////            {
        ////                File.Delete(_exportFile);
        ////                File.Delete(_exportFile + ".mtl");
        ////            }
        ////            _exportFile = outputFile;
        ////        }
        ////        else
        ////        {
        ////            //// ReSharper disable once ThrowingSystemException
        ////            throw new Exception(_processError + "\n\n" + _processOutput);
        ////        }
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        Message.ErrorMessage("Unexpected error while creating temp LoD!\n\n" + ex.Message);
        ////    }
        ////}

        private static void ProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (_processOutput != null && !string.IsNullOrEmpty(e.Data))
                {
                    _processOutput.Append(Environment.NewLine + "  " + e.Data);
                }
            }
            catch
            {
                // ignored
            }
        }

        private static void ProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (_processError != null && !string.IsNullOrEmpty(e.Data))
                {
                    _processError.Append(Environment.NewLine + "  " + e.Data);
                }
            }
            catch
            {
                // ignored
            }
        }
        #endregion

        #region CreateJSON from OBJ
        #region ResetOriginalValid
        private void ResetOriginalVertexValid()
        {
            _originalVertexValid.Clear();
            _originalVertexValid.Capacity = _originalVertex.Count;
            for (int i = 0; i < _originalVertex.Count; i++)
            {
                _originalVertexValid.Add(true);
            }
        }

        private void ResetOriginalTextureCoordinatesValid()
        {
            _originalTextureCoordinatesValid.Clear();
            _originalTextureCoordinatesValid.Capacity = _originalTextureCoordinates.Count;
            for (int i = 0; i < _originalTextureCoordinates.Count; i++)
            {
                _originalTextureCoordinatesValid.Add(true);
            }
        }

        private void ResetOriginalNormalValid()
        {
            _originalNormalValid.Clear();
            _originalNormalValid.Capacity = _originalNormal.Count;
            for (int i = 0; i < _originalNormal.Count; i++)
            {
                _originalNormalValid.Add(true);
            }
        }
        #endregion

        public void CreateJSONFile(int lod)
        {
            float progressScale = GetProgressScale(lod);

            ParseOBJ(lod);
            lock (LockProgressObject)
            {
                Progress = Progress + progressScale / 18;
            }

            ////if (HasDuplicate(_originalVertex))
            ////{
            ////    RemoveDuplicateVertex();
            ////}

            BoundingBox();

            SplitMesh();
            lock (LockProgressObject)
            {
                Progress = Progress + progressScale / 18;
            }

            _usemtl.Add(lod, new string[_newVertexList.Count]);
            _newVertexList.Keys.CopyTo(_usemtl[lod], 0);

            SaveFile(lod);
            lock (LockProgressObject)
            {
                Progress = Progress + progressScale / 18;
            }

            ////ClearLists();
        }

        private void ParseOBJ(int lod)
        {
            StreamReader reader = null;
            string line = "";

            try
            {
                string usemtl = null;
                Point3D point;

                reader = new StreamReader(_exportFile);

                while ((line = reader.ReadLine()) != null)
                {
                    string[] values = line.Split(' ');
                    switch (values[0])
                    {
                        case "mtllib":
                            _mtllibFile = values[1];
                            break;
                        case "usemtl":
                            usemtl = values[1];
                            _originalFacesVertex.Add(usemtl, new List<MeshFace>());
                            _originalFacesTextureCoordinates.Add(usemtl, new List<MeshFace>());
                            _originalFacesNormal.Add(usemtl, new List<MeshFace>());
                            break;
                        case "v":
                            point = Point3D.Parse(values[1] + "," + values[2] + "," + values[3]);
                            point.Offset(_xTranslation, _yTranslation, _zTranslation);
                            _originalVertex.Add(point);
                            _originalVertexValid.Add(true);
                            break;
                        case "vt":
                            _originalTextureCoordinates.Add(Point3D.Parse(values[1] + "," + values[2] + ",0"));
                            break;
                        case "vn":
                            _originalNormal.Add(Point3D.Parse(values[1] + "," + values[2] + "," + values[3]));
                            _originalNormalValid.Add(true);
                            break;
                        case "f":
                            string[] a = values[1].Split('/');
                            string[] b = values[2].Split('/');
                            string[] c = values[3].Split('/');

                            if (a.Length != 3 || b.Length != 3 || c.Length != 3)
                            {
                                throw new Exception("OBJ file format is not valid or unsupported!");
                            }

                            MeshFace face = new MeshFace(a[0], b[0], c[0]);
                            if (usemtl != null)
                            {
                                _originalFacesVertex[usemtl].Add(face);
                                _originalFacesTextureCoordinates[usemtl].Add(new MeshFace(a[1], b[1], c[1]));
                                _originalFacesNormal[usemtl].Add(new MeshFace(a[2], b[2], c[2]));
                            }
                            else
                            {
                                usemtl = "noMtl" + Path.GetFileNameWithoutExtension(_exportFile);
                                _originalFacesVertex.Add(usemtl, new List<MeshFace>());
                                _originalFacesTextureCoordinates.Add(usemtl, new List<MeshFace>());
                                _originalFacesNormal.Add(usemtl, new List<MeshFace>());
                                _originalFacesVertex[usemtl].Add(face);
                                _originalFacesTextureCoordinates[usemtl].Add(new MeshFace(a[1], b[1], c[1]));
                                _originalFacesNormal[usemtl].Add(new MeshFace(a[2], b[2], c[2]));
                                ////throw new Exception("OBJ file format is not valid or unsupported!");
                            }

                            break;
                    }
                }

                if (lod == 0 && _originalFacesTextureCoordinates.Count > 1)
                {
                    _db.NewCommand("UPDATE \"Modelli3D\" SET \"Type\" = 3 WHERE \"Codice\" = (SELECT \"CodiceModello\" FROM \"OggettiVersion\" WHERE \"CodiceOggetto\" = (SELECT \"Codice\" FROM \"Oggetti\" WHERE \"Layer0\" = ? AND \"Layer1\" = ? AND \"Layer2\" = ? AND \"Layer3\" = ? AND \"Name\" = ?) AND \"Versione\" = ?)");
                    _db.ParametersAdd(_dbLayer0);
                    _db.ParametersAdd(_dbLayer1);
                    _db.ParametersAdd(_dbLayer2);
                    _db.ParametersAdd(_dbLayer3);
                    _db.ParametersAdd(_dbName);
                    _db.ParametersAdd(_dbVersione);

                    _db.SafeExecuteNonQuery();
                }

                ResetOriginalVertexValid();
                ResetOriginalNormalValid();
                ResetOriginalTextureCoordinatesValid();

                if (usemtl != "noMtl" + Path.GetFileNameWithoutExtension(_exportFile))
                {
                    ParseMtl(lod);
                }
            }
            catch (Exception ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while parsing OBJ file!\n\n" + line + " is not supported!\n\n" + ex.Message);
            }
            finally
            {
                reader?.Close();
            }
        }

        private void ParseMtl(int lod)
        {
            StreamReader reader = null;

            try
            {
                string newMtl = null;

                reader = new StreamReader(Path.GetDirectoryName(_exportFile) + "\\" + _mtllibFile);

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] values = line.Split(' ');
                    switch (values[0])
                    {
                        case "newmtl":
                            newMtl = values[1];
                            break;
                        case "map_Kd":
                            if (!string.IsNullOrWhiteSpace(newMtl) && _originalFacesVertex.ContainsKey(newMtl))
                            {
                                _textures.Add(lod + "-" + newMtl, _exportFilePath + values[1]);
                            }
                            break;
                    }
                }

                _exportTexture = _textures.Count > 0;
            }
            catch (Exception ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while parsing MTL file!\n\n" + ex.Message);
            }
            finally
            {
                reader?.Close();
            }
        }

        ////private bool HasDuplicate<T>(List<T> originalVertex)
        ////{
        ////    HashSet<T> hashSet = new HashSet<T>();
        ////    return originalVertex.Any(r => !hashSet.Add(r));
        ////}

        ////private void RemoveDuplicateVertex()
        ////{
        ////    List<Point3D> vertex = new List<Point3D>
        ////    {
        ////        Capacity = _originalVertex.Count
        ////    };
        ////    Dictionary<int, int> vertexRel = new Dictionary<int, int>(_originalVertex.Count);
        ////    List<Point3D> normal = new List<Point3D>
        ////    {
        ////        Capacity = _originalNormal.Count
        ////    };

        ////    List<MeshFace> faces = new List<MeshFace>
        ////    {
        ////        Capacity = _originalFacesVertex.Count
        ////    };

        ////    for (int i = 0; i < _originalFacesVertex.Count; i++)
        ////    {
        ////        MeshFace face = _originalFacesVertex[i];
        ////        MeshFace normalFace = _originalFacesNormal[i];

        ////        int indexA = AddVertex(face.A, ref vertexRel, ref vertex);
        ////        int indexB = AddVertex(face.B, ref vertexRel, ref vertex);
        ////        int indexC = AddVertex(face.C, ref vertexRel, ref vertex);

        ////        AddNormal(normalFace.A, ref normal);
        ////        AddNormal(normalFace.B, ref normal);
        ////        AddNormal(normalFace.C, ref normal);

        ////        faces.Add(new MeshFace(indexA, indexB, indexC));
        ////    }

        ////    _originalVertex.Clear();
        ////    _originalNormal.Clear();
        ////    _originalFacesVertex.Clear();
        ////    _originalFacesNormal.Clear();

        ////    _originalVertex = vertex;
        ////    _originalNormal = normal;
        ////    _originalFacesVertex = faces;
        ////    _originalFacesNormal = faces;
        ////}

        private void BoundingBox()
        {
            double xMin = _originalVertex[0].X;
            double yMin = _originalVertex[0].Y;
            double zMin = _originalVertex[0].Z;
            double xMax = xMin;
            double yMax = yMin;
            double zMax = zMin;

            foreach (Point3D point in _originalVertex)
            {
                if (xMin > point.X)
                {
                    xMin = point.X;
                }
                if (xMax < point.X)
                {
                    xMax = point.X;
                }
                if (yMin > point.Y)
                {
                    yMin = point.Y;
                }
                if (yMax < point.Y)
                {
                    yMax = point.Y;
                }
                if (zMin > point.Z)
                {
                    zMin = point.Z;
                }
                if (zMax < point.Z)
                {
                    zMax = point.Z;
                }
            }

            Center = new Point3D((xMax + xMin) / 2, (yMax + yMin) / 2, (zMax + zMin) / 2);
            Radius = Math.Sqrt(Math.Pow(xMax - Center.X, 2) + Math.Pow(yMax - Center.Y, 2) + Math.Pow(zMax - Center.Z, 2));
        }

        #region SplitMesh
        private int AddVertex(int vertexId, ref Dictionary<int, int> vertexRel, ref List<Point3D> vertex)
        {
            int index = vertexId - 1;
            if (_originalVertexValid[index])
            {
                vertex.Add(_originalVertex[index]);
                vertexRel.Add(vertexId, vertexRel.Count);
                _originalVertexValid[index] = false;
                return vertexRel.Count - 1;
            }
            else
            {
                return vertexRel[vertexId];
            }
        }

        private int DuplicateVertex(int vertexId, ref Dictionary<int, int> vertexRel, ref List<Point3D> vertex)
        {
            vertex.Add(_originalVertex[vertexId - 1]);
            vertexRel.Add(_vertexCount, vertexRel.Count);
            _vertexCount++;
            return vertexRel.Count - 1;
        }

        private void AddTextureCoordinates(int textureCoordinatesId, ref List<Point3D> textureCoordinates)
        {
            int index = textureCoordinatesId - 1;
            if (_originalTextureCoordinatesValid[index])
            {
                textureCoordinates.Add(_originalTextureCoordinates[index]);
                _originalTextureCoordinatesValid[index] = false;
            }
        }

        private int AddTextureCoordinates(int textureCoordinatesId, ref Dictionary<int, int> textureRel, ref List<Point3D> textureCoordinates)
        {
            int index = textureCoordinatesId - 1;
            if (_originalTextureCoordinatesValid[index])
            {
                textureCoordinates.Add(_originalTextureCoordinates[index]);
                textureRel.Add(textureCoordinatesId, textureRel.Count);
                _originalTextureCoordinatesValid[index] = false;
                return textureRel.Count - 1;
            }
            else
            {
                return textureRel[textureCoordinatesId];
            }
        }

        private int DuplicateTextureCoordinates(int textureCoordinatesId, ref Dictionary<int, int> textureRel, ref List<Point3D> textureCoordinates)
        {
            textureCoordinates.Add(_originalTextureCoordinates[textureCoordinatesId - 1]);
            textureRel.Add(_textureCount, textureRel.Count);
            _textureCount++;
            return textureRel.Count - 1;
        }

        private void AddNormal(int normalId, ref List<Point3D> normal)
        {
            try
            {
                int index = normalId - 1;
                if (_originalNormalValid[index])
                {
                    normal.Add(_originalNormal[index]);
                    _originalNormalValid[index] = false;
                }
            }
            catch (Exception ex)
            {
                Message.ErrorMessage("Unexpected error while !\n\n" + ex.Message);
            }
        }

        private void DuplicateNormal(int normalId, ref List<Point3D> normal)
        {
            normal.Add(_originalNormal[normalId - 1]);
        }

        private void DuplicateVertexUV()
        {
            List<Point3D> vertex = new List<Point3D>
                                       {
                                           Capacity = _originalVertex.Count
                                       };
            Dictionary<int, int> vertexRel = new Dictionary<int, int>(_originalVertex.Count);

            List<Point3D> textureCoordinates = new List<Point3D>
                                                   {
                                                       Capacity = _originalTextureCoordinates.Count
                                                   };
            Dictionary<int, int> textureRel = new Dictionary<int, int>(_originalTextureCoordinates.Count);

            List<Point3D> normal = new List<Point3D>
                                       {
                                           Capacity = _originalNormal.Count
                                       };

            Dictionary<string, List<MeshFace>> faces = new Dictionary<string, List<MeshFace>>();

            _vertexCount = _originalVertex.Count + 1;
            _textureCount = _originalTextureCoordinates.Count + 1;

            foreach (string usemtl in _originalFacesVertex.Keys)
            {
                faces.Add(usemtl, new List<MeshFace>
                                      {
                                          Capacity = _originalFacesVertex.Count
                                      });

                for (int i = 0; i < _originalFacesVertex[usemtl].Count; i++)
                {
                    MeshFace face = _originalFacesVertex[usemtl][i];
                    MeshFace textureCoordinatesFace = _originalFacesTextureCoordinates[usemtl][i];
                    MeshFace normalFace = _originalFacesNormal[usemtl][i];

                    int vertexA = AddVertex(face.A, ref vertexRel, ref vertex);
                    int textureA = AddTextureCoordinates(textureCoordinatesFace.A, ref textureRel, ref textureCoordinates);
                    if (vertex.Count < textureCoordinates.Count)
                    {
                        vertexA = DuplicateVertex(face.A, ref vertexRel, ref vertex);
                        DuplicateNormal(normalFace.A, ref normal);
                    }
                    else if (vertex.Count > textureCoordinates.Count)
                    {
                        textureA = DuplicateTextureCoordinates(textureCoordinatesFace.A, ref textureRel, ref textureCoordinates);
                        AddNormal(normalFace.A, ref normal);
                        if (vertex.Count != normal.Count)
                        {
                            DuplicateNormal(normalFace.A, ref normal);
                        }
                    }
                    else
                    {
                        AddNormal(normalFace.A, ref normal);
                    }
                    if (vertexA != textureA)
                    {
                        if (vertex[vertexA] != vertex[textureA])
                        {
                            if (textureCoordinates[vertexA] == textureCoordinates[textureA])
                            {
                                textureA = vertexA;
                            }
                            else
                            {
                                vertexA = DuplicateVertex(face.A, ref vertexRel, ref vertex);
                                textureA = DuplicateTextureCoordinates(textureCoordinatesFace.A, ref textureRel, ref textureCoordinates);
                                DuplicateNormal(normalFace.A, ref normal);
                            }
                        }

                        if (vertex[vertexA] != vertex[textureA] || normal[vertexA] != normal[textureA] || textureCoordinates[textureA] != _originalTextureCoordinates[textureCoordinatesFace.A - 1])
                        {
                            Message.WarningMessage("Unexpected error while duplicating vertex!");
                        }
                    }

                    int vertexB = AddVertex(face.B, ref vertexRel, ref vertex);
                    int textureB = AddTextureCoordinates(textureCoordinatesFace.B, ref textureRel, ref textureCoordinates);
                    if (vertex.Count < textureCoordinates.Count)
                    {
                        vertexB = DuplicateVertex(face.B, ref vertexRel, ref vertex);
                        DuplicateNormal(normalFace.B, ref normal);
                    }
                    else if (vertex.Count > textureCoordinates.Count)
                    {
                        textureB = DuplicateTextureCoordinates(textureCoordinatesFace.B, ref textureRel, ref textureCoordinates);
                        AddNormal(normalFace.B, ref normal);
                        if (vertex.Count != normal.Count)
                        {
                            DuplicateNormal(normalFace.B, ref normal);
                        }
                    }
                    else
                    {
                        AddNormal(normalFace.B, ref normal);
                    }
                    if (vertexB != textureB)
                    {
                        if (vertex[vertexB] != vertex[textureB])
                        {
                            if (textureCoordinates[vertexB] == textureCoordinates[textureB])
                            {
                                textureB = vertexB;
                            }
                            else
                            {
                                vertexB = DuplicateVertex(face.B, ref vertexRel, ref vertex);
                                textureB = DuplicateTextureCoordinates(textureCoordinatesFace.B, ref textureRel, ref textureCoordinates);
                                DuplicateNormal(normalFace.B, ref normal);
                            }
                        }
                        if (vertex[vertexB] != vertex[textureB] || normal[vertexB] != normal[textureB] || textureCoordinates[textureB] != _originalTextureCoordinates[textureCoordinatesFace.B - 1])
                        {
                            Message.WarningMessage("Unexpected error while duplicating vertex!");
                        }
                    }

                    int vertexC = AddVertex(face.C, ref vertexRel, ref vertex);
                    int textureC = AddTextureCoordinates(textureCoordinatesFace.C, ref textureRel, ref textureCoordinates);
                    if (vertex.Count < textureCoordinates.Count)
                    {
                        vertexC = DuplicateVertex(face.C, ref vertexRel, ref vertex);
                        DuplicateNormal(normalFace.C, ref normal);
                    }
                    else if (vertex.Count > textureCoordinates.Count)
                    {
                        textureC = DuplicateTextureCoordinates(textureCoordinatesFace.C, ref textureRel, ref textureCoordinates);
                        AddNormal(normalFace.C, ref normal);
                        if (vertex.Count != normal.Count)
                        {
                            DuplicateNormal(normalFace.C, ref normal);
                        }
                    }
                    else
                    {
                        AddNormal(normalFace.C, ref normal);
                    }
                    if (vertexC != textureC)
                    {
                        if (vertex[vertexC] != vertex[textureC])
                        {
                            if (textureCoordinates[vertexC] == textureCoordinates[textureC])
                            {
                                textureC = vertexC;
                            }
                            else
                            {
                                vertexC = DuplicateVertex(face.C, ref vertexRel, ref vertex);
                                textureC = DuplicateTextureCoordinates(textureCoordinatesFace.C, ref textureRel, ref textureCoordinates);
                                DuplicateNormal(normalFace.C, ref normal);
                            }
                        }
                        if (vertex[vertexC] != vertex[textureC] || normal[vertexC] != normal[textureC] || textureCoordinates[textureC] != _originalTextureCoordinates[textureCoordinatesFace.C - 1])
                        {
                            Message.WarningMessage("Unexpected error while duplicating vertex!");
                        }
                    }

                    if (vertex.Count != textureCoordinates.Count || normal.Count != vertex.Count)
                    {
                        //Message.WarningMessage("Unexpected error while duplicating vertex!");
                    }

                    faces[usemtl].Add(new MeshFace(textureA + 1, textureB + 1, textureC + 1));
                }
            }

            ClearLists();

            _originalVertex = vertex;
            _originalTextureCoordinates = textureCoordinates;
            _originalNormal = normal;
            _originalFacesVertex = faces;
            ////_originalFacesTextureCoordinates = faces;
            ////_originalFacesNormal = faces;

            ResetOriginalVertexValid();
            ResetOriginalTextureCoordinatesValid();
            ResetOriginalNormalValid();
        }

        private void SplitMesh()
        {
            try
            {
                DuplicateVertexUV();

                List<Point3D> vertex = new List<Point3D>
                                           {
                                               Capacity = Math.Min(65536, _originalVertex.Count)
                                           };
                Dictionary<int, int> vertexRel = new Dictionary<int, int>(Math.Min(65536, _originalVertex.Count));

                List<Point3D> textureCoordinates = new List<Point3D>
                                                       {
                                                           Capacity = Math.Min(65536, _originalTextureCoordinates.Count)
                                                       };

                List<Point3D> normal = new List<Point3D>
                                           {
                                               Capacity = Math.Min(65536, _originalNormal.Count)
                                           };

                List<MeshFace> faces = new List<MeshFace>
                                           {
                                               Capacity = Math.Min(65536, _originalFacesVertex.Count)
                                           };
                foreach (string usemtl in _originalFacesVertex.Keys)
                {
                    _newVertexList.Add(usemtl, new List<List<Point3D>>());
                    _newTextureCoordinatesList.Add(usemtl, new List<List<Point3D>>());
                    _newNormalList.Add(usemtl, new List<List<Point3D>>());
                    _newFacesList.Add(usemtl, new List<List<MeshFace>>());
                    //// ReSharper disable once ForCanBeConvertedToForeach
                    for (int i = 0; i < _originalFacesVertex[usemtl].Count; i++)
                    {
                        MeshFace face = _originalFacesVertex[usemtl][i];
                        MeshFace textureCoordinatesFace = _originalFacesVertex[usemtl][i];
                        MeshFace normalFace = _originalFacesVertex[usemtl][i];
                        ////MeshFace textureCoordinatesFace = _originalFacesTextureCoordinates[i];
                        ////MeshFace normalFace = _originalFacesNormal[i];

                        if (vertex.Count > 65533 || faces.Count > 65535)
                        {
                            _newVertexList[usemtl].Add(vertex);
                            _newTextureCoordinatesList[usemtl].Add(textureCoordinates);
                            _newNormalList[usemtl].Add(normal);
                            _newFacesList[usemtl].Add(faces);

                            ResetOriginalVertexValid();
                            ResetOriginalTextureCoordinatesValid();
                            ResetOriginalNormalValid();

                            vertex = new List<Point3D>
                                         {
                                             Capacity = Math.Min(65536, _originalVertex.Count)
                                         };
                            vertexRel = new Dictionary<int, int>(Math.Min(65536, _originalVertex.Count));
                            textureCoordinates = new List<Point3D>
                                                     {
                                                         Capacity = Math.Min(65536, _originalTextureCoordinates.Count)
                                                     };
                            normal = new List<Point3D>
                                         {
                                             Capacity = Math.Min(65536, _originalNormal.Count)
                                         };
                            faces = new List<MeshFace>
                                        {
                                            Capacity = Math.Min(65536, _originalFacesVertex[usemtl].Count)
                                        };
                        }

                        int indexA = AddVertex(face.A, ref vertexRel, ref vertex);
                        int indexB = AddVertex(face.B, ref vertexRel, ref vertex);
                        int indexC = AddVertex(face.C, ref vertexRel, ref vertex);

                        AddTextureCoordinates(textureCoordinatesFace.A, ref textureCoordinates);
                        AddTextureCoordinates(textureCoordinatesFace.B, ref textureCoordinates);
                        AddTextureCoordinates(textureCoordinatesFace.C, ref textureCoordinates);

                        AddNormal(normalFace.A, ref normal);
                        AddNormal(normalFace.B, ref normal);
                        AddNormal(normalFace.C, ref normal);

                        faces.Add(new MeshFace(indexA, indexB, indexC));
                    }

                    _newVertexList[usemtl].Add(vertex);
                    _newTextureCoordinatesList[usemtl].Add(textureCoordinates);
                    _newNormalList[usemtl].Add(normal);
                    _newFacesList[usemtl].Add(faces);

                    ResetOriginalVertexValid();
                    ResetOriginalTextureCoordinatesValid();
                    ResetOriginalNormalValid();

                    vertex = new List<Point3D>
                                 {
                                     Capacity = Math.Min(65536, _originalVertex.Count)
                                 };
                    vertexRel = new Dictionary<int, int>(Math.Min(65536, _originalVertex.Count));
                    textureCoordinates = new List<Point3D>
                                             {
                                                 Capacity = Math.Min(65536, _originalTextureCoordinates.Count)
                                             };
                    normal = new List<Point3D>
                                 {
                                     Capacity = Math.Min(65536, _originalNormal.Count)
                                 };
                    faces = new List<MeshFace>
                                {
                                    Capacity = Math.Min(65536, _originalFacesVertex[usemtl].Count)
                                };
                }
            }
            catch (Exception ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while splitting mesh!\n\n" + ex.Message);
            }
        }

        ////private void SplitMeshOctree()
        ////{
        ////    try
        ////    {
        ////        List<Point3D> vertex = new List<Point3D>
        ////                                   {
        ////                                       Capacity = Math.Min(65536, _originalVertex.Count),
        ////                                   };
        ////        vertex.Add(_originalVertex[0]);

        ////        Dictionary<int, int> vertexRel = new Dictionary<int, int>(Math.Min(65536, _originalVertex.Count))
        ////                                             {
        ////                                                 { 1, 1 }
        ////                                             };
        ////        _originalVertexValid[0] = false;

        ////        List<MeshFace> faces = new List<MeshFace>
        ////                                   {
        ////                                       Capacity = Math.Min(65536, _originalFacesVertex.Count)
        ////                                   };

        ////        Dictionary<int, bool> originalFaceValid = new Dictionary<int, bool>(_originalFacesVertex.Count);
        ////        for (int i = 0; i < _originalFacesVertex.Count; i++)
        ////        {
        ////            originalFaceValid.Add(i, true);
        ////        }

        ////        int index = 1;
        ////        int j = 0;
        ////        int l = 0;

        ////        while (_originalFacesVertex.Count > l)
        ////        {
        ////            foreach (int faceIndex in _originalVertexFacesList[index])
        ////            {
        ////                if (originalFaceValid[faceIndex])
        ////                {
        ////                    MeshFace face = _originalFacesVertex[faceIndex];

        ////                    if (vertex.Count > 65534 || faces.Count > 65535)
        ////                    {
        ////                        _newVertexList.Add(vertex);
        ////                        _newFacesList.Add(faces);

        ////                        _originalVertexValid.Clear();
        ////                        _originalVertex.Capacity = _originalVertex.Count;
        ////                        for (int i = 0; i < _originalVertex.Count; i++)
        ////                        {
        ////                            _originalVertexValid.Add(true);
        ////                        }
        ////                        vertex.Clear();
        ////                        vertexRel.Clear();
        ////                        faces.Clear();

        ////                        vertex.Add(_originalVertex[index - 1]);
        ////                        vertexRel.Add(index, 1);
        ////                        _originalVertexValid[index - 1] = false;
        ////                        j = 0;
        ////                    }

        ////                    int indexA = AddVertex(face.A, ref vertexRel, ref vertex);
        ////                    int indexB = AddVertex(face.B, ref vertexRel, ref vertex);
        ////                    int indexC = AddVertex(face.C, ref vertexRel, ref vertex);

        ////                    faces.Add(new MeshFace(indexA, indexB, indexC));
        ////                    originalFaceValid[faceIndex] = false;

        ////                    l++;
        ////                }
        ////            }

        ////            j++;

        ////            if (j > vertexRel.Count)
        ////            {
        ////                for (int i = 0; i < _originalFacesVertex.Count; i++)
        ////                {
        ////                    if (originalFaceValid[i])
        ////                    {
        ////                        index = _originalFacesVertex[i].A;
        ////                        int index1 = index - 1;
        ////                        if (_originalVertexValid[index1])
        ////                        {
        ////                            vertex.Add(_originalVertex[index1]);
        ////                            vertexRel.Add(index, vertexRel.Count + 1);
        ////                            _originalVertexValid[index1] = false;
        ////                            break;
        ////                        }

        ////                        index = _originalFacesVertex[i].B;
        ////                        index1 = index - 1;
        ////                        if (_originalVertexValid[index1])
        ////                        {
        ////                            vertex.Add(_originalVertex[index1]);
        ////                            vertexRel.Add(index, vertexRel.Count + 1);
        ////                            _originalVertexValid[index1] = false;
        ////                            break;
        ////                        }

        ////                        index = _originalFacesVertex[i].C;
        ////                        index1 = index - 1;
        ////                        if (_originalVertexValid[index1])
        ////                        {
        ////                            vertex.Add(_originalVertex[index1]);
        ////                            vertexRel.Add(index, vertexRel.Count + 1);
        ////                            _originalVertexValid[index1] = false;
        ////                        }
        ////                    }
        ////                }
        ////            }

        ////            int k = 0;
        ////            foreach (int key in vertexRel.Keys)
        ////            {
        ////                k++;
        ////                if (k == j)
        ////                {
        ////                    index = key;
        ////                    break;
        ////                }
        ////            }

        ////            //index = vertexRel.Keys.;
        ////        }

        ////        _newVertexList.Add(vertex);
        ////        _newFacesList.Add(faces);
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        Message.ErrorMessage("Unexpected error while splitting mesh!\n\n" + ex.Message);
        ////    }
        ////}
        #endregion

        private void SaveFile(int lod)
        {
            string filename = Path.GetFileNameWithoutExtension(_exportFile);
            Directory.CreateDirectory(_tempPath);

            int j = 0;
            foreach (string usemtl in _usemtl[lod])
            {
                for (int i = 0; i < _newVertexList[usemtl].Count; i++)
                {
                    string fullname = _tempPath + filename + "#" + j * 100 + "-" + i + ".json";

                    StreamWriter writer = new StreamWriter(fullname);

                    writer.Write(@"{
    ""primitive"": ""triangles"",
    ""positions"": [
        ");
                    bool more = false;
                    foreach (Point3D vertex in _newVertexList[usemtl][i])
                    {
                        writer.Write((more ? ", " : "") + vertex.X.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + vertex.Y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + vertex.Z.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        more = true;
                    }

                    writer.Write(@"
    ],
    ""normals"": [
        ");
                    more = false;
                    foreach (Point3D normal in _newNormalList[usemtl][i])
                    {
                        writer.Write((more ? ", " : "") + normal.X.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + normal.Y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + normal.Z.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        more = true;
                    }

                    writer.Write(@"
    ],
    ""uv"": [
        ");
                    more = false;
                    foreach (Point3D textureCoordinate in _newTextureCoordinatesList[usemtl][i])
                    {
                        writer.Write((more ? ", " : "") + textureCoordinate.X.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + textureCoordinate.Y.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        more = true;
                    }

                    writer.Write(@"
    ],
    ""uv2"":null,
    ""indices"": [
        ");
                    more = false;
                    foreach (MeshFace face in _newFacesList[usemtl][i])
                    {
                        writer.Write((more ? ", " : "") + face.A + ", " + face.B + ", " + face.C);
                        more = true;
                    }
                    writer.Write(@"
    ]
}");
                    writer.Close();
                }

                j++;
            }
        }
        #endregion

        #region InsertDB
        public void PreinitializeDB()
        {
            try
            {
                //// ReSharper disable once ExceptionNotDocumented
                _db = new DB();

                _dbLayer0.Value = _exportElement.Layer0;
                _dbLayer1.Value = _exportElement.Layer1;
                _dbLayer2.Value = _exportElement.Layer2;
                _dbLayer3.Value = _exportElement.Layer3;
                _dbName.Value = _exportElement.Name;
                _dbVersione.Value = _exportElement.Version;
                _dbUser.Value = Authentication.AuthenticationUtility.User;

                _db.NewCommand("{call " + (_exportElement.IsNew ? "preinitializenewobject" : "preinitializemodifiedobjectonlyweb") + " (?, ?, ?, ?, ?, ?, ?, ?)};");
                _db.ParametersAdd(_dbLayer0);
                _db.ParametersAdd(_dbLayer1);
                _db.ParametersAdd(_dbLayer2);
                _db.ParametersAdd(_dbLayer3);
                _db.ParametersAdd(_dbName);
                _db.ParametersAdd(_dbVersione);
                _db.ParametersAdd(_dbTipoModello);
                _db.ParametersAdd(_dbUser);

                _db.SafeExecuteNonQuery();
            }
            catch (DB.DBConnectionErrorException ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while connecting to DB for exporting objects!\n\n" + ex.Message);
            }
            catch (DB.DBErrorException ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while executing DB query for exporting objects!\n\n" + ex.Message);
            }
            catch (Exception ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while updating DB for " + _exportFile + "!\n\n" + ex.Message);
            }
        }

        public void UpdateLodDB(int loD)
        {
            try
            {
                int j = 0;

                foreach (string usemtl in _newVertexList.Keys)
                {
                    OdbcParameter dbLoD = new OdbcParameter("@LoD", OdbcType.Int)
                                              {
                                                  Value = loD + 100 * j
                                              };

                    OdbcParameter dbxc = new OdbcParameter("@xc", OdbcType.Real)
                                             {
                                                 Value = Center.X
                                             };
                    OdbcParameter dbyc = new OdbcParameter("@yc", OdbcType.Real)
                                             {
                                                 Value = Center.Y
                                             };
                    OdbcParameter dbzc = new OdbcParameter("@zc", OdbcType.Real)
                                             {
                                                 Value = Center.Z
                                             };
                    OdbcParameter dbRadius = new OdbcParameter("@Radius", OdbcType.Real)
                                                 {
                                                     Value = Radius
                                                 };

                    OdbcParameter dbParti = new OdbcParameter("@Parti", OdbcType.Int)
                                                {
                                                    Value = _newVertexList[usemtl].Count
                                                };

                    OdbcParameter dbVolume = new OdbcParameter("@Volume", OdbcType.Double)
                                                 {
                                                     Value = null
                                                 };
                    OdbcParameter dbSuperficie = new OdbcParameter("@Superficie", OdbcType.Double)
                                                     {
                                                         Value = null
                                                     };

                    OdbcParameter dbTranslationX = new OdbcParameter("@TranslationX", OdbcType.Double)
                    {
                        Value = _dbX
                    };
                    OdbcParameter dbTranslationY = new OdbcParameter("@TranslationY", OdbcType.Double)
                    {
                        Value = _dbY
                    };
                    OdbcParameter dbTranslationZ = new OdbcParameter("@TranslationZ", OdbcType.Double)
                    {
                        Value = _dbZ
                    };
                    OdbcParameter dbSrs = new OdbcParameter("@SRS", OdbcType.Text)
                    {
                        Value = _srs
                    };

                    OdbcParameter db3DMTexture = new OdbcParameter("@3dmTexture", OdbcType.Bit)
                                                     {
                                                         Value = false
                                                     };
                    OdbcParameter dbJSONTexture = new OdbcParameter("@JSONTexture", OdbcType.Bit)
                                                      {
                                                          Value = _exportTexture
                                                      };

                    OdbcParameter dbExportJSON = new OdbcParameter("@ExportJSON", OdbcType.Bit)
                                                     {
                                                         Value = true
                                                     };

                    _db.NewCommand("{call updateobject (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)};");
                    _db.ParametersAdd(_dbLayer0);
                    _db.ParametersAdd(_dbLayer1);
                    _db.ParametersAdd(_dbLayer2);
                    _db.ParametersAdd(_dbLayer3);
                    _db.ParametersAdd(_dbName);
                    _db.ParametersAdd(_dbVersione);
                    _db.ParametersAdd(dbLoD);
                    _db.ParametersAdd(dbVolume);
                    _db.ParametersAdd(dbSuperficie);
                    _db.ParametersAdd(dbTranslationX);
                    _db.ParametersAdd(dbTranslationY);
                    _db.ParametersAdd(dbTranslationZ);
                    _db.ParametersAdd(dbSrs);
                    _db.ParametersAdd(dbxc);
                    _db.ParametersAdd(dbyc);
                    _db.ParametersAdd(dbzc);
                    _db.ParametersAdd(dbRadius);
                    _db.ParametersAdd(dbParti);
                    _db.ParametersAdd(db3DMTexture);
                    _db.ParametersAdd(dbJSONTexture);
                    _db.ParametersAdd(dbExportJSON);
                    _db.ParametersAdd(_dbUser);

                    _db.SafeExecuteNonQuery();

                    j++;
                }
            }
            catch (DB.DBConnectionErrorException ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while connecting to DB for exporting objects!\n\n" + ex.Message);
            }
            catch (DB.DBErrorException ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while executing DB query for exporting objects!\n\n" + ex.Message);
            }
            catch (Exception ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while updating DB for " + _exportFile + "!\n\n" + ex.Message);
            }
        }

        private void RemoveUpdating()
        {
            _db.NewCommand("UPDATE \"OggettiVersion\" SET \"Updating\" = FALSE WHERE \"CodiceOggetto\" = (SELECT \"Codice\" FROM \"Oggetti\" WHERE \"Layer0\" = ? AND \"Layer1\" = ? AND \"Layer2\" = ? AND \"Layer3\" = ? AND \"Name\" = ?) AND \"Versione\" = ? AND \"Lock\" = ?");
            _db.ParametersAdd(_dbLayer0);
            _db.ParametersAdd(_dbLayer1);
            _db.ParametersAdd(_dbLayer2);
            _db.ParametersAdd(_dbLayer3);
            _db.ParametersAdd(_dbName);
            _db.ParametersAdd(_dbVersione);
            _db.ParametersAdd(_dbUser);

            _db.SafeExecuteNonQuery();
        }

        private void UploadJSONThread(object data)
        {
            DB db = null;

            try
            {
                int lod = (int)data;

                OdbcParameter dbLayer0 = new OdbcParameter("@Layer0", OdbcType.VarChar, 255)
                                           {
                                               Value = _exportElement.Layer0
                                           };
                OdbcParameter dbLayer1 = new OdbcParameter("@Layer1", OdbcType.VarChar, 255)
                                           {
                                               Value = _exportElement.Layer1
                                           };
                OdbcParameter dbLayer2 = new OdbcParameter("@Layer2", OdbcType.VarChar, 255)
                                           {
                                               Value = _exportElement.Layer2
                                           };
                OdbcParameter dbLayer3 = new OdbcParameter("@Layer3", OdbcType.VarChar, 255)
                                             {
                                                 Value = _exportElement.Layer3
                                             };
                OdbcParameter dbName = new OdbcParameter("@Name", OdbcType.VarChar, 255)
                                           {
                                               Value = _exportElement.Name
                                           };
                OdbcParameter dbVersione = new OdbcParameter("@Versione", OdbcType.Int)
                                               {
                                                   Value = _exportElement.Version
                                               };
                OdbcParameter dbUser = new OdbcParameter("@User", OdbcType.VarChar, 255)
                                           {
                                               Value = Authentication.AuthenticationUtility.User
                                           };

                OdbcParameter dbParte = new OdbcParameter("@parte", OdbcType.Int);

                OdbcParameter dbJSON = new OdbcParameter("@JSON", OdbcType.Binary);

                string fullpath = _tempPath;
                string filename = Path.GetFileNameWithoutExtension(_exportFile);

                db = new DB();

                int j = 0;

                foreach (string usemtl in _usemtl[lod])
                {
                    OdbcParameter dbLoD = new OdbcParameter("@LoD", OdbcType.Int)
                                              {
                                                  Value = lod + 100 * j
                                              };

                    db.NewCommand("SELECT \"JSON_NumeroParti\" FROM \"Modelli3D_LoD\" JOIN \"OggettiVersion\" ON \"Modelli3D_LoD\".\"CodiceModello\" = \"OggettiVersion\".\"CodiceModello\" JOIN \"Oggetti\" ON \"OggettiVersion\".\"CodiceOggetto\" = \"Oggetti\".\"Codice\" WHERE \"Layer0\" = ? AND \"Layer1\" = ? AND \"Layer2\" = ? AND \"Layer3\" = ? AND \"Name\" = ? AND \"Versione\" = ? AND \"LoD\" = ?");
                    db.ParametersAdd(dbLayer0);
                    db.ParametersAdd(dbLayer1);
                    db.ParametersAdd(dbLayer2);
                    db.ParametersAdd(dbLayer3);
                    db.ParametersAdd(dbName);
                    db.ParametersAdd(dbVersione);
                    db.ParametersAdd(dbLoD);

                    int parti = (int)db.SafeExecuteScalar();
                    float progressScale = GetProgressScale(lod) * 2 / (3 * parti * _usemtl[lod].Length);

                    for (int i = 0; i < parti; i++)
                    {
                        dbParte.Value = i;

                        string fullname = _tempPath + filename + "#" + j * 100 + "-" + i + ".json";

                        if (File.Exists(fullname))
                        {
                            FileStream fs = new FileStream(fullname, FileMode.Open, FileAccess.Read);
                            byte[] fileJSON = new byte[fs.Length];
                            fs.Read(fileJSON, 0, Convert.ToInt32(fs.Length));
                            fs.Close();
                            dbJSON.Value = fileJSON;

                            //// ReSharper disable once SpliceString
                            db.NewCommand("{call uploadjsonfile (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)};");
                            db.ParametersAdd(dbLayer0);
                            db.ParametersAdd(dbLayer1);
                            db.ParametersAdd(dbLayer2);
                            db.ParametersAdd(dbLayer3);
                            db.ParametersAdd(dbName);
                            db.ParametersAdd(dbVersione);
                            db.ParametersAdd(dbLoD);
                            db.ParametersAdd(dbParte);
                            db.ParametersAdd(dbJSON);
                            db.ParametersAdd(dbUser);

                            db.SafeExecuteNonQuery();

                            File.Delete(fullname);

                            lock (LockProgressObject)
                            {
                                Progress = Progress + progressScale;
                            }
                        }
                        else
                        {
                            Success = false;
                            Message.ErrorMessage("Can't find temp JSON file (" + fullname + "): can't export this object!\n\nDatabase can be in inconsistent state; original objects are still present into current file, we suggest you to export them again to fix it.");

                            return;
                        }
                    }

                    j++;
                }
            }
            catch (DB.DBConnectionErrorException ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while connecting to DB for uploading JSON files!\n\nDatabase can be in inconsistent state; original objects are still present into current file, we suggest you to export them again to fix it.\n\n" + ex.Message);
            }
            catch (DB.DBErrorException ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while executing DB query for uploading JSON files!\n\nDatabase can be in inconsistent state; original objects are still present into current file, we suggest you to export them again to fix it.\n\n" + ex.Message);
            }
            catch (Exception ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while uploading JSON files!\n\nDatabase can be in inconsistent state; original objects are still present into current file, we suggest you to export them again to fix it.\n\n" + ex.Message);
            }
            finally
            {
                db?.CloseConnection();
            }
        }

        private void UploadTextureThread(object data)
        {
            DB db = null;

            try
            {
                int lod = (int)data;

                int j = 0;
                float progressIncrement = GetProgressScale(lod) / (6 * _usemtl[lod].Length);

                _textureResolution = lod == 0 ? 4096 : _textureResolution / 2;

                int textureResolution = _textureResolution;

                foreach (string usemtl in _usemtl[lod])
                {
                    string textureFile = _textures[lod + "-" + usemtl];

                    if (File.Exists(textureFile))
                    {
                        db = new DB();

                        string fullpath = _tempPath;
                        string name = Path.GetFileNameWithoutExtension(textureFile);
                        string extension = Path.GetExtension(textureFile);
                        string filename = name + extension;

                        Directory.CreateDirectory(_tempPath);

                        OdbcParameter dbLayer0 = new OdbcParameter("@Layer0", OdbcType.VarChar, 255)
                                                   {
                                                       Value = _exportElement.Layer0
                                                   };
                        OdbcParameter dbLayer1 = new OdbcParameter("@Layer1", OdbcType.VarChar, 255)
                                                   {
                                                       Value = _exportElement.Layer1
                                                   };
                        OdbcParameter dbLayer2 = new OdbcParameter("@Layer2", OdbcType.VarChar, 255)
                                                   {
                                                       Value = _exportElement.Layer2
                                                   };
                        OdbcParameter dbLayer3 = new OdbcParameter("@Layer3", OdbcType.VarChar, 255)
                                                     {
                                                         Value = _exportElement.Layer3
                                                     };
                        OdbcParameter dbName = new OdbcParameter("@Name", OdbcType.VarChar, 255)
                                                   {
                                                       Value = _exportElement.Name
                                                   };
                        OdbcParameter dbVersione = new OdbcParameter("@Versione", OdbcType.Int)
                                                       {
                                                           Value = _exportElement.Version
                                                       };
                        OdbcParameter dbUser = new OdbcParameter("@User", OdbcType.VarChar, 255)
                                                   {
                                                       Value = Authentication.AuthenticationUtility.User
                                                   };

                        OdbcParameter dbLoD = new OdbcParameter("@LoD", OdbcType.Int)
                                                  {
                                                      Value = lod + 100 * j
                                                  };

                        OdbcParameter dbTextureIndex = new OdbcParameter("@TextureIndex", OdbcType.Int)
                                                           {
                                                               Value = 0
                                                           };
                        OdbcParameter dbTextureFileName = new OdbcParameter("@TextureFileName", OdbcType.VarChar, 255)
                                                              {
                                                                  Value = filename
                                                              };

                        string destName = fullpath + name + "-" + textureResolution + extension;
                        Size? newResolution = ImageTools.ResizeImage(textureFile, destName, new Size(textureResolution, textureResolution));

                        if (newResolution != null && _textureResolution == 4096)
                        {
                            _textureResolution = (int)((Size)newResolution).Width;
                        }

                        OdbcParameter dbMimeType = new OdbcParameter("@MimeType", OdbcType.VarChar, 255)
                                                       {
                                                           Value = ImageTools.GetMimeType(destName)
                                                       };

                        FileStream fs = new FileStream(destName, FileMode.Open, FileAccess.Read);
                        byte[] fileTexture = new byte[fs.Length];
                        fs.Read(fileTexture, 0, Convert.ToInt32(fs.Length));
                        fs.Close();
                        OdbcParameter dbTextureFile = new OdbcParameter("@Texture", OdbcType.Binary)
                                                          {
                                                              Value = fileTexture
                                                          };

                        //// ReSharper disable once SpliceString
                        db.NewCommand("{call uploadtexturefile (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)}");
                        db.ParametersAdd(dbLayer0);
                        db.ParametersAdd(dbLayer1);
                        db.ParametersAdd(dbLayer2);
                        db.ParametersAdd(dbLayer3);
                        db.ParametersAdd(dbName);
                        db.ParametersAdd(dbVersione);
                        db.ParametersAdd(dbTextureIndex);
                        db.ParametersAdd(dbLoD);
                        db.ParametersAdd(dbTextureFileName);
                        db.ParametersAdd(dbTextureFile);
                        db.ParametersAdd(dbMimeType);
                        db.ParametersAdd(dbUser);

                        db.SafeExecuteNonQuery();
                        //File.Delete(destName);
                    }
                    else
                    {
                        Success = false;
                        Message.ErrorMessage("Can't find texture file (" + textureFile + "): can't export this object!\n\nDatabase can be in inconsistent state; original objects are still present into current file, we suggest you to export them again to fix it.");
                    }

                    j++;

                    lock (LockProgressObject)
                    {
                        Progress = Progress + progressIncrement;
                    }
                }
            }
            catch (DB.DBConnectionErrorException ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while connecting to DB for uploading texture files!\n\nDatabase can be in inconsistent state; original objects are still present into current file, we suggest you to export them again to fix it.\n\n" + ex.Message);
            }
            catch (DB.DBErrorException ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while executing DB query for uploading texture files!\n\nDatabase can be in inconsistent state; original objects are still present into current file, we suggest you to export them again to fix it.\n\n" + ex.Message);
            }
            catch (Exception ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while uploading texture files!\n\nDatabase can be in inconsistent state; original objects are still present into current file, we suggest you to export them again to fix it.\n\n" + ex.Message);
            }
            finally
            {
                db?.CloseConnection();
            }
        }

        private void UploadOBJ()
        {
            DB db = null;
            FileStream fs = null;

            try
            {
                OdbcParameter dbLayer0 = new OdbcParameter("@Layer0", OdbcType.VarChar, 255)
                                           {
                                               Value = _exportElement.Layer0
                                           };
                OdbcParameter dbLayer1 = new OdbcParameter("@Layer1", OdbcType.VarChar, 255)
                                           {
                                               Value = _exportElement.Layer1
                                           };
                OdbcParameter dbLayer2 = new OdbcParameter("@Layer2", OdbcType.VarChar, 255)
                                           {
                                               Value = _exportElement.Layer2
                                           };
                OdbcParameter dbLayer3 = new OdbcParameter("@Layer3", OdbcType.VarChar, 255)
                                             {
                                                 Value = _exportElement.Layer3
                                             };
                OdbcParameter dbName = new OdbcParameter("@Name", OdbcType.VarChar, 255)
                                           {
                                               Value = _exportElement.Name
                                           };
                OdbcParameter dbVersione = new OdbcParameter("@Versione", OdbcType.Int)
                                               {
                                                   Value = _exportElement.Version
                                               };
                OdbcParameter dbUser = new OdbcParameter("@User", OdbcType.VarChar, 255)
                                           {
                                               Value = Authentication.AuthenticationUtility.User
                                           };

                OdbcParameter dbLoD = new OdbcParameter("@LoD", OdbcType.Int)
                                          {
                                              Value = 0
                                          };

                OdbcParameter dbParte = new OdbcParameter("@parte", OdbcType.Int);

                OdbcParameter dbOBJ = new OdbcParameter("@OBJ", OdbcType.Binary);

                db = new DB();

                fs = new FileStream(_exportElement.Filename, FileMode.Open, FileAccess.Read);

                int leftByte = (int)fs.Length;
                int startByte = 0;

                int parti = leftByte / MaxDBbyte;
                if (leftByte % MaxDBbyte > 0)
                {
                    parti = parti + 1;
                }

                for (int i = 0; leftByte > 0; i++)
                {
                    dbParte.Value = i;
                    int readByte = Math.Min(leftByte, MaxDBbyte);

                    byte[] fileOBJ = new byte[readByte];
                    fs.Read(fileOBJ, 0, readByte);
                    startByte = startByte + readByte;
                    leftByte = leftByte - readByte;
                    dbOBJ.Value = fileOBJ;

                    //// ReSharper disable once SpliceString
                    db.NewCommand("{call uploadOBJfile (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)};");
                    db.ParametersAdd(dbLayer0);
                    db.ParametersAdd(dbLayer1);
                    db.ParametersAdd(dbLayer2);
                    db.ParametersAdd(dbLayer3);
                    db.ParametersAdd(dbName);
                    db.ParametersAdd(dbVersione);
                    db.ParametersAdd(dbLoD);
                    db.ParametersAdd(dbParte);
                    db.ParametersAdd(dbOBJ);
                    db.ParametersAdd(dbUser);

                    db.SafeExecuteNonQuery();

                    lock (LockProgressObject)
                    {
                        Progress = Progress + 3.0f / parti;
                    }
                }
            }
            catch (DB.DBConnectionErrorException ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while connecting to DB for uploading OBJ files!\n\nDatabase can be in inconsistent state; original objects are still present into current file, we suggest you to export them again to fix it.\n\n" + ex.Message);
            }
            catch (DB.DBErrorException ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while executing DB query for uploading OBJ files!\n\nDatabase can be in inconsistent state; original objects are still present into current file, we suggest you to export them again to fix it.\n\n" + ex.Message);
            }
            catch (Exception ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while uploading OBJ files!\n\nDatabase can be in inconsistent state; original objects are still present into current file, we suggest you to export them again to fix it.\n\n" + ex.Message);
            }
            finally
            {
                fs?.Close();
                db?.CloseConnection();
            }
        }
        #endregion

        #region CreateJSON from TXT
        private void CreateJSONFileFromTxt()
        {
            ////ParseTxt();

            ////BoundingBox();

            ////SplitPointCloud();

            ParseAndSplit();

            SavePointCloudFile();
        }

        private void ParseAndSplit()
        {
            StreamReader reader = null;
            try
            {
                List<Point3D> vertex = new List<Point3D>
                                           {
                                               Capacity = 65536
                                           };
                ////List<Color> vertexColor = new List<Color>
                ////{
                ////    Capacity = 65536
                ////};
                List<string> vertexColorString = new List<string>
                                                     {
                                                         Capacity = 65536
                                                     };

                reader = new StreamReader(_exportFile);

                double xMin = double.MaxValue;
                double yMin = double.MaxValue;
                double zMin = double.MaxValue;
                double xMax = double.MinValue;
                double yMax = double.MinValue;
                double zMax = double.MinValue;

                _newVertexList.Add("0", new List<List<Point3D>>());

                int i = 1;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    i++;
                    if (vertex.Count > 65535)
                    {
                        _newVertexList["0"].Add(vertex);
                        //_newVertexColor.Add(vertexColor);
                        _newVertexColorString.Add(vertexColorString);

                        vertex = new List<Point3D>
                                     {
                                         Capacity = Math.Min(65536, _originalVertex.Count)
                                     };
                        ////vertexColor = new List<Color>
                        ////{
                        ////    Capacity = 65536
                        ////};
                        vertexColorString = new List<string>
                                                {
                                                    Capacity = 65536
                                                };
                    }
                    string[] values = line.Split(' ');

                    Point3D point = Point3D.Parse(values[0] + "," + values[1] + "," + values[2]);
                    point.Offset(_xTranslation, _yTranslation, _zTranslation);
                    if (xMin > point.X)
                    {
                        xMin = point.X;
                    }
                    if (xMax < point.X)
                    {
                        xMax = point.X;
                    }
                    if (yMin > point.Y)
                    {
                        yMin = point.Y;
                    }
                    if (yMax < point.Y)
                    {
                        yMax = point.Y;
                    }
                    if (zMin > point.Z)
                    {
                        zMin = point.Z;
                    }
                    if (zMax < point.Z)
                    {
                        zMax = point.Z;
                    }
                    vertex.Add(point);
                    ////vertexColor.Add(Color.FromRgb(byte.Parse(values[3]), byte.Parse(values[4]), byte.Parse(values[5])));

                    if (values[3].Length > 2 && values[3].Substring(1, 1) == "." && values[4].Length > 2 && values[4].Substring(1, 1) == "." && values[5].Length > 2 && values[5].Substring(1, 1) == ".")
                    {
                        vertexColorString.Add(values[3] + ", " + values[4] + ", " + values[5]);
                    }
                    else
                    {
                        Color color = Color.FromRgb(byte.Parse(values[3]), byte.Parse(values[4]), byte.Parse(values[5]));
                        vertexColorString.Add((color.R / 255.0).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ", " + (color.G / 255.0).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ", " + (color.B / 255.0).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ", 1.0");
                    }

                    //if (i > 65535)
                    //    break;
                }

                _newVertexList["0"].Add(vertex);
                ////_newVertexColor.Add(vertexColor);
                _newVertexColorString.Add(vertexColorString);

                Center = new Point3D((xMax + xMin) / 2, (yMax + yMin) / 2, (zMax + zMin) / 2);
                Radius = Math.Sqrt(Math.Pow(xMax - Center.X, 2) + Math.Pow(yMax - Center.Y, 2) + Math.Pow(zMax - Center.Z, 2));

                _usemtl.Add(0, new string[_newVertexList.Count]);
                _newVertexList.Keys.CopyTo(_usemtl[0], 0);
            }
            catch (Exception ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while parsing OBJ file!\n\n" + ex.Message);
            }
            finally
            {
                reader?.Close();
            }
        }

        private void ParseTxt()
        {
            StreamReader reader = null;
            try
            {
                reader = new StreamReader(_exportFile);
                ////int i = 1;
                string line;
                Point3D point;
                while ((line = reader.ReadLine()) != null)
                {
                    ////i++;
                    string[] values = line.Split(' ');

                    point = Point3D.Parse(values[0] + "," + values[1] + "," + values[2]);
                    point.Offset(_xTranslation, _yTranslation, _zTranslation);
                    _originalVertex.Add(point);
                    _originalVertexValid.Add(true);
                    _originalVertexColor.Add(Color.FromRgb(byte.Parse(values[3]), byte.Parse(values[4]), byte.Parse(values[5])));
                    //if (i > 65535)
                    //    break;
                }
            }
            catch (Exception ex)
            {
                Success = false;
                Message.ErrorMessage("Unexpected error while parsing OBJ file!\n\n" + ex.Message);
            }
            finally
            {
                reader?.Close();
            }
        }

        private void SplitPointCloud()
        {
            List<Point3D> vertex = new List<Point3D>
                                       {
                                           Capacity = Math.Min(65536, _originalVertex.Count)
                                       };
            List<Color> vertexColor = new List<Color>
                                          {
                                              Capacity = Math.Min(65536, _originalVertexColor.Count)
                                          };

            _newVertexList.Add("0", new List<List<Point3D>>());

            for (int i = 0; i < _originalVertex.Count; i++)
            {
                if (vertex.Count > 65535)
                {
                    _newVertexList["0"].Add(vertex);
                    _newVertexColor.Add(vertexColor);

                    vertex = new List<Point3D>
                                 {
                                     Capacity = Math.Min(65536, _originalVertex.Count)
                                 };
                    vertexColor = new List<Color>
                                      {
                                          Capacity = Math.Min(65536, _originalVertexColor.Count)
                                      };
                }

                vertex.Add(_originalVertex[i]);
                vertexColor.Add(_originalVertexColor[i]);
            }

            _newVertexList["0"].Add(vertex);
            _newVertexColor.Add(vertexColor);
        }

        private void SavePointCloudFile()
        {
            string filename = Path.GetFileNameWithoutExtension(_exportFile);
            Directory.CreateDirectory(_tempPath);

            for (int i = 0; i < _newVertexList["0"].Count; i++)
            {
                string fullname = _tempPath + filename + "#0-" + i + ".json";

                StreamWriter writer = new StreamWriter(fullname);

                writer.Write(@"{
            ""primitive"": ""points"",
            ""pointSize"": 1,
            ""positions"": [
                ");
                bool more = false;
                foreach (Point3D vertex in _newVertexList["0"][i])
                {
                    writer.Write((more ? ", " : "") + vertex.X.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + vertex.Y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + vertex.Z.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    more = true;
                }

                writer.Write(@"
            ],
            ""colors"": [
                ");
                more = false;
                ////foreach (Color color in _newVertexColor[i])
                ////{
                ////    writer.Write((more ? ", " : "") + (color.R / 255.0).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ", " + (color.G / 255.0).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ", " + (color.B / 255.0).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ", 1.0");
                ////    more = true;
                ////}
                foreach (string colore in _newVertexColorString[i])
                {
                    writer.Write((more ? ", " : "") + colore);
                    more = true;
                }

                writer.Write(@"
            ]
        }
            ");
                writer.Close();
            }
        }

        ////        private void SavePointCloudFile()
        ////        {
        ////            string filename = Path.GetFileNameWithoutExtension(_exportFile);
        ////            Directory.CreateDirectory(_tempPath);

        ////            for (int i = 0; i < _newVertexList.Count; i++)
        ////            {
        ////                string fullname = _tempPath + filename + "#" + i + ".json";

        ////                StreamWriter writer = new StreamWriter(fullname);

        ////                writer.Write(@"{
        ////    ""primitive"": ""points"",
        ////    ""pointSize"": 1,
        ////    ""positions"": [
        ////        ");
        ////                bool more = false;
        ////                foreach (Point3D vertex in _newVertexList[i])
        ////                {
        ////                    writer.Write((more ? ", " : "") + vertex.X.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + vertex.Y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + vertex.Z.ToString(System.Globalization.CultureInfo.InvariantCulture));
        ////                    more = true;
        ////                }

        ////                writer.Write(@"
        ////    ]
        ////}");
        ////                writer.Close();
        ////            }
        ////        }

        ////        private void SavePointCloudFile()
        ////        {
        ////            string filename = Path.GetFileNameWithoutExtension(_exportFile);
        ////            Directory.CreateDirectory(_tempPath);

        ////            for (int i = 0; i < _newVertexList.Count; i++)
        ////            {
        ////                string fullname = _tempPath + filename + "#" + i + ".json";

        ////                StreamWriter writer = new StreamWriter(fullname);

        ////                writer.Write(@"{
        ////    ""primitive"": ""points"",
        ////    ""pointSize"": 1,
        ////    ""pointList"": [");
        ////                bool more = false;
        ////                for (int j = 0; j < _newVertexList[i].Count; j++)
        ////                {
        ////                    Point3D vertex = _newVertexList[i][j];
        ////                    Color color = _newVertexColor[i][j];
        ////                    writer.Write(more ? ", " : "");

        ////                    writer.Write(@"
        ////{ 
        ////    ""positions"" : [" + vertex.X.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + vertex.Y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + vertex.Z.ToString(System.Globalization.CultureInfo.InvariantCulture) + @"],
        ////    ""color"" : [" + color.R / 255.0 + ", " + color.G / 255.0 + ", " + color.B / 255.0 + @"]
        ////}");
        ////                    more = true;
        ////                }

        ////                writer.Write(@"
        ////    ]
        ////}");
        ////                writer.Close();
        ////            }
        ////        }
        #endregion
    }
}
