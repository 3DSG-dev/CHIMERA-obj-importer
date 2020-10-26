using System;
using System.Data.Odbc;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

using Chimera_Importer.Authentication;
using Chimera_Importer.Utility;

using ITinnovationsLibrary.Functions;

namespace Chimera_Importer
{
    public class ExportElement
    {
        public string Layer0 { get; set; }
        public string Layer1 { get; set; }
        public string Layer2 { get; set; }
        public string Layer3 { get; set; }
        public string Name { get; set; }
        public int? Version { get; set; }
        public string Type { get; set; }
        public string Filename { get; set; }
        public bool IsNew { get; set; }
    }

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly string _appName = Assembly.GetExecutingAssembly().GetName().Name;

        private DB _db;

        private ExportFile _export;

        ////private const double TickToSec = 10000000;
        ////private const double SecToMs = 1000;
        private DispatcherTimer _dispatcherTimer;
        private Stopwatch _stopwatch;
        private TimeSpan _left;
        private readonly TimeSpan _secTimespan = TimeSpan.FromSeconds(1);
        private TextBlock _currentElapsedTextBlock;
        private Grid _currentProgressGrid;
        private ProgressBar _currentProgressBar;
        private Brush _defaultProgressBrush;
        private TextBlock _currentLeftTextBlock;
        private TextBlock _progressText;

        public MainWindow()
        {
            InitializeComponent();
        }

        #region Events
        private void BordlessWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AuthenticationUtility.Check())
                {
                    _db = new DB();

                    if (Properties.Settings.Default.First)
                    {
                        OpenSettings();

                        Properties.Settings.Default.First = false;
                        Properties.Settings.Default.Save();
                    }
                }
                else
                {
                    Close();
                }
            }
            catch (DB.DBConnectionErrorException ex)
            {
                Message.ErrorMessage("Unexpected error while connecting to DB for initializing application!\n\n" + ex.Message);
            }
            catch (DB.DBErrorException ex)
            {
                Message.ErrorMessage("Unexpected error while executing DB query for initializing application!\n\n" + ex.Message);
            }
            catch (Exception ex)
            {
                Message.ErrorMessage("Unexpected error while initializing application!\n\n" + ex.Message);
            }
        }

        private void BordlessWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (AuthenticationUtility.IsAuthenticated)
                {
                    MessageBoxResult exit = MessageBox.Show("Are you sure to exit?", _appName, MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                    if (exit == MessageBoxResult.OK)
                    {
                        AuthenticationUtility.Logout();

                        _db.CloseConnection();
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Message.ErrorMessage("Unexpected error while closing " + _appName + "!\n\n" + ex.Message);
            }
        }
        #endregion

        #region ExportList
        private void AddObject()
        {
            AddObjectWindow addObjectWindow = new AddObjectWindow(_db);

            if (addObjectWindow.ShowDialog() == true)
            {
                ExportList.Items.Add(new ExportElement
                                         {
                                             Layer0 = addObjectWindow.Layer0,
                                             Layer1 = addObjectWindow.Layer1,
                                             Layer2 = addObjectWindow.Layer2,
                                             Layer3 = addObjectWindow.Layer3,
                                             Name = addObjectWindow.Nome,
                                             Version = addObjectWindow.Version,
                                             Type = addObjectWindow.Type,
                                             Filename = addObjectWindow.Filename,
                                             IsNew = addObjectWindow.IsNew
                                         });
            }
        }

        #region AddFolder
        private string[] ValidateOBJ(string file, bool isNew)
        {
            StreamReader reader = null;
            string line = "";

            try
            {
                string[] objectsIds = new string[5];
                bool search = true;

                reader = new StreamReader(file);

                while (search && (line = reader.ReadLine()) != null)
                {
                    string[] values = line.Split(new[] { ' ' }, 2);
                    switch (values[0])
                    {
                        case "g":
                            string[] layers = values[1].Split(new[] { "__" }, StringSplitOptions.None);
                            objectsIds[0] = layers[0];
                            objectsIds[1] = layers[1];
                            objectsIds[2] = layers[2];
                            objectsIds[3] = layers[3];
                            break;
                        case "o":
                            objectsIds[4] = values[1];
                            break;
                        case "v":
                        case "vt":
                        case "vn":
                        case "f":
                            search = false;
                            break;
                    }
                }

                foreach (string id in objectsIds)
                {
                    if (id == null)
                    {
                        return null;
                    }
                }

                if (isNew && !GetDBIsNew(objectsIds[0], objectsIds[1], objectsIds[2], objectsIds[3], objectsIds[4], 0))
                {
                    Message.ErrorMessage("Object already exist!");
                }
                else if (!isNew && GetDBIsNew(objectsIds[0], objectsIds[1], objectsIds[2], objectsIds[3], objectsIds[4], 0))
                {
                    Message.ErrorMessage("Object doesn't exits!\n\n" + string.Join(", ", objectsIds));
                }
                else if (!isNew && !GetDBWrite(objectsIds[0], objectsIds[1], objectsIds[2], objectsIds[3], objectsIds[4], 0))
                {
                    Message.ErrorMessage("Object isn't imported in write mode!");
                }
                else
                {
                    return objectsIds;
                }

                return null;
            }
            catch (Exception ex)
            {
                Message.ErrorMessage("Unexpected error while parsing OBJ file!\n\n" + line + " is not supported!\n\n" + ex.Message);
                return null;

            }
            finally
            {
                reader?.Close();
            }
        }

        private void AddFolder()
        {
            Thread waitIndicator = null;

            try
            {
                AddFolderWindow addFolderWindow = new AddFolderWindow();

                if (addFolderWindow.ShowDialog() == true)
                {
                    ITinnovationsAddForXceedToolkit.WaitIndicator.Start(ref waitIndicator);

                    if (addFolderWindow.Type == "Mesh")
                    {
                        string[] files = Directory.GetFiles(addFolderWindow.Folder, "*.obj");
                        foreach (string filename in files)
                        {
                            string[] objectsIds = ValidateOBJ(filename, addFolderWindow.IsNew);

                            if (objectsIds != null)
                            {
                                ExportList.Items.Add(new ExportElement
                                                         {
                                                             Layer0 = objectsIds[0],
                                                             Layer1 = objectsIds[1],
                                                             Layer2 = objectsIds[2],
                                                             Layer3 = objectsIds[3],
                                                             Name = objectsIds[4],
                                                             Version = 0,
                                                             Type = addFolderWindow.Type,
                                                             Filename = filename,
                                                             IsNew = addFolderWindow.IsNew
                                                         });
                            }
                        }
                    }

                    ITinnovationsAddForXceedToolkit.WaitIndicator.Stop(ref waitIndicator);
                }
            }
            catch (Exception ex)
            {
                ITinnovationsAddForXceedToolkit.WaitIndicator.Stop(ref waitIndicator);
                Message.ErrorMessage("Unexpected error while adding folder to export list!\n\n" + ex.Message);
            }
        }
        #endregion

        #region Remove Objects
        private void RemoveSelected()
        {
            try
            {
                for (int i = ExportList.SelectedItems.Count - 1; i >= 0; i--)
                {
                    ExportList.Items.Remove(ExportList.SelectedItems[i]);
                }
            }
            catch (Exception ex)
            {
                Message.ErrorMessage("Unexpected error while removing selected objects!\n\n" + ex.Message);
            }
        }

        private void RemoveAll()
        {
            try
            {
                ExportList.Items.Clear();
            }
            catch (Exception ex)
            {
                Message.ErrorMessage("Unexpected error while empty export list!\n\n" + ex.Message);
            }
        }
        #endregion

        #region ContextMenu
        private void RemoveSelectedContext_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelected();
        }

        private void RemoveAllContext_Click(object sender, RoutedEventArgs e)
        {
            RemoveAll();
        }
        #endregion

        #endregion

        #region Buttons
        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            AddObject();
        }

        private void AddFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            AddFolder();
        }

        private void RemoveSelectedBtn_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelected();
        }

        private void RemoveAllBtn_Click(object sender, RoutedEventArgs e)
        {
            RemoveAll();
        }

        private void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            Export();
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings();
        }
        #endregion

        #region Export
        private async void Export()
        {
            try
            {
                SettingsBtn.IsEnabled = false;
                AddBtn.IsEnabled = false;
                RemoveSelectedBtn.IsEnabled = false;
                RemoveAllBtn.IsEnabled = false;
                ExportBtn.IsEnabled = false;
                ExitBtn.IsEnabled = false;
                WorldCoordinates.IsEnabled = false;
                LocalCoordinates.IsEnabled = false;
                WorldCoordinates.IsEnabled = false;
                LocalCoordinates.IsEnabled = false;
                xTranslation.IsEnabled = false;
                yTranslation.IsEnabled = false;
                zTranslation.IsEnabled = false;
                Srs.IsEnabled = false;

                for (int i = 0; i < ExportList.Items.Count; i++)
                {
                    ////_dispatcherTimer = new DispatcherTimer(_secTimespan, DispatcherPriority.Send, UpdateProgress, Dispatcher);
                    ////_stopwatch = new Stopwatch();

                    ////_stopwatch.Start();
                    ////_dispatcherTimer.Start();

                    ListViewItem lwi = ExportList.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;

                    if (lwi != null)
                    {
                        GridViewRowPresenter gvrp = LayoutTools.GetDescendantByType(lwi, typeof(GridViewRowPresenter)) as GridViewRowPresenter;
                        if (gvrp != null)
                        {
                            //_currentName = drw[1].ToString();
                            _currentProgressGrid = VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(gvrp, 9), 0) as Grid;
                            _currentProgressBar = null;
                            if (_currentProgressGrid != null)
                            {
                                _currentProgressBar = _currentProgressGrid.Children[0] as ProgressBar;
                                if (_currentProgressBar != null)
                                {
                                    if (_defaultProgressBrush != null)
                                    {
                                        _currentProgressBar.Foreground = _defaultProgressBrush;
                                    }
                                    else
                                    {
                                        _defaultProgressBrush = _currentProgressBar.Foreground;
                                    }

                                    _currentProgressBar.Value = 0;
                                }
                                _progressText = _currentProgressGrid.Children[1] as TextBlock;
                                if (_progressText != null)
                                {
                                    _progressText.Visibility = Visibility.Visible;
                                }
                            }
                            _currentLeftTextBlock = VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(gvrp, 10), 0) as TextBlock;
                            if (_currentLeftTextBlock != null)
                            {
                                _currentLeftTextBlock.Text = "";
                            }
                        }
                    }
                }

                for (int i = 0; i < ExportList.Items.Count; i++)
                {
                    _export = null;
                    _dispatcherTimer = new DispatcherTimer(_secTimespan, DispatcherPriority.Send, UpdateProgress, Dispatcher);
                    _stopwatch = new Stopwatch();

                    ListViewItem lwi = ExportList.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;

                    if (lwi != null)
                    {
                        GridViewRowPresenter gvrp = LayoutTools.GetDescendantByType(lwi, typeof(GridViewRowPresenter)) as GridViewRowPresenter;
                        if (gvrp != null)
                        {
                            //_currentName = drw[1].ToString();
                            _currentElapsedTextBlock = VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(gvrp, 8), 0) as TextBlock;
                            _currentProgressGrid = VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(gvrp, 9), 0) as Grid;
                            _currentProgressBar = null;
                            if (_currentProgressGrid != null)
                            {
                                _currentProgressBar = _currentProgressGrid.Children[0] as ProgressBar;
                                _progressText = _currentProgressGrid.Children[1] as TextBlock;
                            }
                            _currentLeftTextBlock = VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(gvrp, 10), 0) as TextBlock;

                            _stopwatch.Start();
                            _dispatcherTimer.Start();

                            //_success = true;
                            ExportElement element = (ExportElement)ExportList.Items[i];

                            if (!File.Exists(element.Filename))
                            {
                                Message.ErrorMessage("The selected file doesn't exist!");
                            }
                            if (element.IsNew && !GetDBIsNew(element.Layer0, element.Layer1, element.Layer2, element.Layer3, element.Name, element.Version))
                            {
                                Message.ErrorMessage("Object already exist!");
                            }
                            else if (!element.IsNew && GetDBIsNew(element.Layer0, element.Layer1, element.Layer2, element.Layer3, element.Name, element.Version))
                            {
                                Message.ErrorMessage("Object doesn't exits!");
                            }
                            else if (!element.IsNew && !GetDBWrite(element.Layer0, element.Layer1, element.Layer2, element.Layer3, element.Name, element.Version))
                            {
                                Message.ErrorMessage("Object isn't imported in write mode!");
                            }
                            else if (xTranslation.Value == null || yTranslation.Value == null || zTranslation.Value == null)
                            {
                                Message.ErrorMessage("You must set a value for the translation (0 for no translation)!");
                            }
                            else
                            {
                                string srs = String.IsNullOrWhiteSpace(Srs.Text) ? null : Srs.Text;
                                _export = new ExportFile(element, LocalCoordinates.IsChecked == true, (double)xTranslation.Value, (double)yTranslation.Value, (double)zTranslation.Value, srs);

                                Thread thread = ThreadTools.StartNewBackgroudThread(_export.Export);

                                while (thread.IsAlive)
                                {
                                    await Task.Delay(500);
                                }
                            }

                            if (_export == null || !_export.Success)
                            {
                                if (_currentProgressBar != null)
                                {
                                    if (_defaultProgressBrush == null)
                                    {
                                        _defaultProgressBrush = _currentProgressBar.Foreground;
                                    }
                                    _currentProgressBar.Foreground = new SolidColorBrush(Colors.Red);
                                }
                                if (_currentLeftTextBlock != null)
                                {
                                    _currentLeftTextBlock.Text = "ERROR! - " + _progressText.Text;
                                }
                            }
                        }
                    }

                    _stopwatch.Stop();
                    _dispatcherTimer.Stop();
                    UpdateProgress(this, null);
                }

                Message.InformationMessage("Export process completed!");
            }
            catch (Exception ex)
            {
                Message.ErrorMessage("Unexpected error while exporting objects!\n\n" + ex.Message);
            }
            finally
            {
                try
                {
                    SettingsBtn.IsEnabled = true;
                    AddBtn.IsEnabled = true;
                    RemoveSelectedBtn.IsEnabled = true;
                    RemoveAllBtn.IsEnabled = true;
                    ExportBtn.IsEnabled = true;
	                ExitBtn.IsEnabled = true;
                    WorldCoordinates.IsEnabled = true;
                    LocalCoordinates.IsEnabled = true;
                    xTranslation.IsEnabled = true;
                    yTranslation.IsEnabled = true;
                    zTranslation.IsEnabled = true;
                    Srs.IsEnabled = true;
                }
                catch
                {
                    // ignored
                }
            }
        }

        private void UpdateProgress(object sender, EventArgs e)
        {
            try
            {
                TimeSpan elapsed = _stopwatch.Elapsed;
                _currentElapsedTextBlock.Text = $"{elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";

                if (_export != null)
                {
                    float progress;
                    lock (_export.LockProgressObject)
                    {
                        progress = _export.Progress;
                    }

                    _currentProgressBar.Value = progress;
                    if (progress > 0)
                    {
                        _left = new TimeSpan((long)(elapsed.Ticks * (100 / progress) - elapsed.Ticks));
                        _currentLeftTextBlock.Text = $"{_left.Hours:00}:{_left.Minutes:00}:{_left.Seconds:00}";
                    }
                    else
                    {
                        _currentLeftTextBlock.Text = "";
                    }
                }
                else
                {
                    _currentLeftTextBlock.Text = "";
                }
            }
            catch
            {
                // ignored
            }
        }
        #endregion

        private void OpenSettings()
        {
            SettingsWindow sw = new SettingsWindow();
            sw.ShowDialog();
        }

        private bool GetDBWrite(string layer0, string layer1, string layer2, string layer3, string name, int? version)
        {
            OdbcParameter dbLayer0 = new OdbcParameter("@Layer0", OdbcType.VarChar, 255)
                                       {
                                           Value = layer0
                                       };
            OdbcParameter dbLayer1 = new OdbcParameter("@Layer1", OdbcType.VarChar, 255)
                                       {
                                           Value = layer1
                                       };
            OdbcParameter dbLayer2 = new OdbcParameter("@Layer2", OdbcType.VarChar, 255)
                                       {
                                           Value = layer2
                                       };
            OdbcParameter dbLayer3 = new OdbcParameter("@Layer3", OdbcType.VarChar, 255)
                                         {
                                             Value = layer3
                                         };
            OdbcParameter dbName = new OdbcParameter("@Name", OdbcType.VarChar, 255)
                                       {
                                           Value = name
                                       };
            OdbcParameter dbVersion = new OdbcParameter("@Version", OdbcType.Int)
                                          {
                                              Value = version
                                          };

            bool rw = true;

            _db.NewCommand("SELECT \"Live\", \"OggettiVersion\".\"Lock\" FROM \"Oggetti\" JOIN \"OggettiVersion\" ON \"Oggetti\".\"Codice\" = \"OggettiVersion\".\"CodiceOggetto\" WHERE \"Layer0\" = ? AND \"Layer1\" = ? AND \"Layer2\" = ? AND \"Layer3\" = ? AND \"Name\" = ? AND \"Versione\" = ?");
            _db.ParametersAdd(dbLayer0);
            _db.ParametersAdd(dbLayer1);
            _db.ParametersAdd(dbLayer2);
            _db.ParametersAdd(dbLayer3);
            _db.ParametersAdd(dbName);
            _db.ParametersAdd(dbVersion);

            OdbcDataReader myReader1 = _db.SafeExecuteReader();
            if (myReader1.Read())
            {
                if (myReader1["Lock"].ToString() != AuthenticationUtility.User)
                {
                    rw = false;
                }

                //dbStatus = myReader1.GetInt32(myReader1.GetOrdinal("Live")) == 3 ? "Added from maintenance" : "Present";
            }
            else
            {
                //dbStatus = "Not present";
            }
            myReader1.Close();

            return rw;
        }

        private bool GetDBIsNew(string layer0, string layer1, string layer2, string layer3, string name, int? version)
        {
            OdbcDataReader myReader = null;

            try
            {
                OdbcParameter dbLayer0 = new OdbcParameter("@Layer0", OdbcType.VarChar, 255)
                                           {
                                               Value = layer0
                                           };
                OdbcParameter dbLayer1 = new OdbcParameter("@Layer1", OdbcType.VarChar, 255)
                                           {
                                               Value = layer1
                                           };
                OdbcParameter dbLayer2 = new OdbcParameter("@Layer2", OdbcType.VarChar, 255)
                                           {
                                               Value = layer2
                                           };
                OdbcParameter dbLayer3 = new OdbcParameter("@Layer3", OdbcType.VarChar, 255)
                                             {
                                                 Value = layer3
                                             };
                OdbcParameter dbName = new OdbcParameter("@Name", OdbcType.VarChar, 255)
                                           {
                                               Value = name
                                           };
                OdbcParameter dbVersion = new OdbcParameter("@Version", OdbcType.Int)
                                              {
                                                  Value = version
                                              };

                _db.NewCommand("SELECT \"OggettiVersion\".\"Lock\" FROM \"Oggetti\" JOIN \"OggettiVersion\" ON \"Oggetti\".\"Codice\" = \"OggettiVersion\".\"CodiceOggetto\" WHERE \"Layer0\" = ? AND \"Layer1\" = ? AND \"Layer2\" = ? AND \"Layer3\" = ? AND \"Name\" = ? AND \"Versione\" = ?");
                _db.ParametersAdd(dbLayer0);
                _db.ParametersAdd(dbLayer1);
                _db.ParametersAdd(dbLayer2);
                _db.ParametersAdd(dbLayer3);
                _db.ParametersAdd(dbName);
                _db.ParametersAdd(dbVersion);

                myReader = _db.SafeExecuteReader();

                return !myReader.HasRows;
            }
            finally
            {
                myReader?.Close();
            }
        }
    }
}
