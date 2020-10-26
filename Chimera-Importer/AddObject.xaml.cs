using System;
using System.Data.Odbc;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

using Chimera_Importer.Utility;

using ITinnovationsLibrary.Functions;

using Microsoft.Win32;

namespace Chimera_Importer
{
    /// <summary>
    ///     Interaction logic for AddObject.xaml
    /// </summary>
    public partial class AddObjectWindow
    {
        private const string Layer0Field = "Layer0";
        private const string Layer0DBField = "Layer0";
        private const string Layer1Field = "Layer1";
        private const string Layer1DBField = "Layer1";
        private const string Layer2Field = "Layer2";
        private const string Layer2DBField = "Layer2";
        private const string Layer3Field = "Layer3";
        private const string Layer3DBField = "Layer3";
        private const string NameField = "Name";
        private const string NameDBField = "Name";

        private readonly DB _db;

        public string Layer0 => Layer0Combo.Text;

        public string Layer1 => Layer1Combo.Text;

        public string Layer2 => Layer2Combo.Text;

        public string Layer3 => Layer3Combo.Text;

        public string Nome => NameCombo.Text;

        public int? Version => VersionBox.Value;

        public string Type => TypeCombo.SelectedValue.ToString();

        public string Filename => FileBox.Text;

        public bool IsNew => IsNewCombo.SelectedValue.ToString() == "New";

        public AddObjectWindow()
        {
            InitializeComponent();
        }

        public AddObjectWindow(DB db)
        {
            InitializeComponent();

            _db = db;
        }

        #region ManageCombo
        private void FillCombo(ComboBox combo, string field, string dbField)
        {
            try
            {
                _db.NewCommand("SELECT DISTINCT \"" + dbField + "\" FROM \"Oggetti\" ORDER BY \"" + dbField + "\"");

                OdbcDataReader myReader = _db.SafeExecuteReader();

                combo.Items.Clear();

                while (myReader.Read())
                {
                    ComboBoxItem cbi = new ComboBoxItem
                                           {
                                               Content = myReader[dbField].ToString(),
                                               Tag = myReader[dbField].ToString()
                                           };
                    combo.Items.Add(cbi);
                }
                myReader.Close();
            }
            catch (DB.DBConnectionErrorException ex)
            {
                Message.ErrorMessage("Unexpected error while loading " + field + " values!\n\n" + ex.Message);
            }
            catch (DB.DBErrorException ex)
            {
                Message.ErrorMessage("Unexpected error while loading " + field + " values!\n\n" + ex.Message);
            }
            catch (Exception ex)
            {
                Message.ErrorMessage("Unexpected error while loading " + field + " values!\n\n" + ex.Message);
            }
        }

        private bool ValidateField(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Match match = Regex.Match(value, @"/([^A-Za-z0-9-])+/g");

                return !match.Success;
            }

            return false;
        }

        private void ReadFieldsFromOBJ()
        {
            StreamReader reader = null;
            string line = "";

            try
            {
                reader = new StreamReader(FileBox.Text);

                while ((line = reader.ReadLine()) != null)
                {
                    string[] values = line.Split(new[] { ' ' }, 2);
                    switch (values[0])
                    {
                        case "g":
                            string[] layers = values[1].Split(new[] {"__"}, StringSplitOptions.None);
                            Layer0Combo.Text = layers[0];
                            Layer1Combo.Text = layers[1];
                            Layer2Combo.Text = layers[2];
                            Layer3Combo.Text = layers[3];
                            break;
                        case "o":
                            NameCombo.Text = values[1];
                            break;
                        case "v":
                        case "vt":
                        case "vn":
                        case "f":
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                Message.ErrorMessage("Unexpected error while parsing OBJ file!\n\n" + line + " is not supported!\n\n" + ex.Message);
            }
            finally
            {
                reader?.Close();
            }
        }
        #endregion

        #region Events
        private void BordlessWindow_Loaded(object sender, RoutedEventArgs e)
        {
            FillCombo(Layer0Combo, Layer0Field, Layer0DBField);
            FillCombo(Layer1Combo, Layer1Field, Layer1DBField);
            FillCombo(Layer2Combo, Layer2Field, Layer2DBField);
            FillCombo(Layer3Combo, Layer3Field, Layer3DBField);
            FillCombo(NameCombo, NameField, NameDBField);
        }

        private void BordlessWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DialogResult == true && !ValidateData())
            {
                e.Cancel = true;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void BrowseFileButton_Click(object sender, RoutedEventArgs e)
        {
            string filter;

            switch (Type)
            {
                case "Mesh" :
                    filter = "OBJ wavefront (*.obj)|*.obj";
                    break;
                case "PointCloud":
                    filter = "txt x y z r g b  (*.txt)|*.txt";
                    break;
                default:
                    return;
            }

            OpenFileDialog dialog = new OpenFileDialog
                                        {
                                            Filter = filter
            };
            if (dialog.ShowDialog() == true)
            {
                FileBox.Text = dialog.FileName;

                ReadFieldsFromOBJ();
            }
        }
        #endregion

        #region ValidateData
        private bool ValidateData()
        {
            string extension = Path.GetExtension(Filename);

            if (!ValidateField(Layer0Combo.Text))
            {
                Message.ErrorMessage(Layer0Field + " is invalid!\n\nField must be not null and contain only letter, number and minus.");
            }
            else if (!ValidateField(Layer1Combo.Text))
            {
                Message.ErrorMessage(Layer1Field + " is invalid!\n\nField must be not null and contain only letter, number and minus.");
            }
            else if (!ValidateField(Layer2Combo.Text))
            {
                Message.ErrorMessage(Layer2Field + " is invalid!\n\nField must be not null and contain only letter, number and minus.");
            }
            else if (!ValidateField(Layer3Combo.Text))
            {
                Message.ErrorMessage(Layer3Field + " is invalid!\n\nField must be not null and contain only letter, number and minus.");
            }
            else if (!ValidateField(NameCombo.Text))
            {
                Message.ErrorMessage(Layer3Field + " is invalid!\n\nField must be not null and contain only letter, number and minus.");
            }
            else if (Version == null || Version < 0)
            {
                Message.ErrorMessage("Version is invalid!\n\nField must be not null and contain positive number or zero.");
            }
            else if (Type == "Mesh" && (extension == null || extension.ToLower() != ".obj"))
            {
                Message.ErrorMessage("Only OBJ file are supported for Mesh objects");
            }
            else if (Type == "PointCloud" && (extension == null || extension.ToLower() != ".txt"))
            {
                Message.ErrorMessage("Only txt file (x y z r g b) are supported for point clouds");
            }
            else if (!File.Exists(Filename))
            {
                Message.ErrorMessage("The selected file doesn't exist!");
            }
            else if (IsNew && !GetDBIsNew())
            {
                Message.ErrorMessage("Object already exist!");
            }
            else if (!IsNew && GetDBIsNew())
            {
                Message.ErrorMessage("Object doesn't exits!");
            }
            else if (!IsNew && !GetDBWrite())
            {
                Message.ErrorMessage("Object isn't imported in write mode!");
            }
            else
            {
                return true;
            }

            return false;
        }

        ////private bool ImportWrite()
        ////{
        ////    OdbcParameter dbLayer0 = new OdbcParameter("@Layer0", OdbcType.VarChar, 255)
        ////                               {
        ////                                   Value = Layer0
        ////                               };
        ////    OdbcParameter dbLayer1 = new OdbcParameter("@Layer1", OdbcType.VarChar, 255)
        ////                               {
        ////                                   Value = Layer1
        ////                               };
        ////    OdbcParameter dbLayer2 = new OdbcParameter("@Layer2", OdbcType.VarChar, 255)
        ////                               {
        ////                                   Value = Layer2
        ////                               };
        ////    OdbcParameter dbLayer3 = new OdbcParameter("@Layer3", OdbcType.VarChar, 255)
        ////                                 {
        ////                                     Value = Layer3
        ////                                 };
        ////    OdbcParameter dbName = new OdbcParameter("@Name", OdbcType.VarChar, 255)
        ////                               {
        ////                                   Value = Nome
        ////                               };
        ////    OdbcParameter dbVersion = new OdbcParameter("@Version", OdbcType.Int)
        ////                                  {
        ////                                      Value = Version
        ////                                  };
        ////    OdbcParameter dbUser = new OdbcParameter("@User", OdbcType.VarChar, 255)
        ////                               {
        ////                                   Value = Authentication.AuthenticationUtility.User
        ////                               };

        ////    OdbcParameter dbImportRemoved = new OdbcParameter("@ImportRemoved", OdbcType.Bit)
        ////                                        {
        ////                                            Value = false
        ////                                        };
        ////    OdbcParameter dbWriteMode = new OdbcParameter("@WriteMode", OdbcType.Bit)
        ////                                    {
        ////                                        Value = true
        ////                                    };
        ////    OdbcParameter dbMatch = new OdbcParameter("@Match", OdbcType.Bit)
        ////                                {
        ////                                    Value = true
        ////                                };

        ////    _db.NewCommand("SELECT addimportnome(?, ?, ?, ?, ?, ?, ?, ?, ?)");
        ////    _db.ParametersAdd(dbLayer0);
        ////    _db.ParametersAdd(dbLayer1);
        ////    _db.ParametersAdd(dbLayer2);
        ////    _db.ParametersAdd(dbLayer3);
        ////    _db.ParametersAdd(dbName);
        ////    _db.ParametersAdd(dbMatch);
        ////    _db.ParametersAdd(dbWriteMode);
        ////    _db.ParametersAdd(dbUser);
        ////    _db.ParametersAdd(dbImportRemoved);

        ////    string result = _db.SafeExecuteScalar().ToString();

        ////    if (!string.IsNullOrWhiteSpace(result))
        ////    {
        ////        Message.ErrorMessage(result);
        ////    }

        ////    return GetDBWrite();
        ////}

        private bool GetDBWrite()
        {
            OdbcParameter dbLayer0 = new OdbcParameter("@Layer0", OdbcType.VarChar, 255)
                                       {
                                           Value = Layer0
                                       };
            OdbcParameter dbLayer1 = new OdbcParameter("@Layer1", OdbcType.VarChar, 255)
                                       {
                                           Value = Layer1
                                       };
            OdbcParameter dbLayer2 = new OdbcParameter("@Layer2", OdbcType.VarChar, 255)
                                       {
                                           Value = Layer2
                                       };
            OdbcParameter dbLayer3 = new OdbcParameter("@Layer3", OdbcType.VarChar, 255)
                                         {
                                             Value = Layer3
                                         };
            OdbcParameter dbName = new OdbcParameter("@Name", OdbcType.VarChar, 255)
                                       {
                                           Value = Nome
                                       };
            OdbcParameter dbVersion = new OdbcParameter("@Version", OdbcType.Int)
                                          {
                                              Value = Version
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
                if (myReader1["Lock"].ToString() != Authentication.AuthenticationUtility.User)
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

        private bool GetDBIsNew()
        {
            OdbcDataReader myReader = null;

            try
            {
                OdbcParameter dbLayer0 = new OdbcParameter("@Layer0", OdbcType.VarChar, 255)
                                           {
                                               Value = Layer0
                                           };
                OdbcParameter dbLayer1 = new OdbcParameter("@Layer1", OdbcType.VarChar, 255)
                                           {
                                               Value = Layer1
                                           };
                OdbcParameter dbLayer2 = new OdbcParameter("@Layer2", OdbcType.VarChar, 255)
                                           {
                                               Value = Layer2
                                           };
                OdbcParameter dbLayer3 = new OdbcParameter("@Layer3", OdbcType.VarChar, 255)
                                             {
                                                 Value = Layer3
                                             };
                OdbcParameter dbName = new OdbcParameter("@Name", OdbcType.VarChar, 255)
                                           {
                                               Value = Nome
                                           };
                OdbcParameter dbVersion = new OdbcParameter("@Version", OdbcType.Int)
                                              {
                                                  Value = Version
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
        #endregion
    }
}
