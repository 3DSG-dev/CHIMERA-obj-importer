using System;
using System.Data.Odbc;
using System.Windows;
using System.Windows.Controls;

using Chimera_Importer.Authentication;
using Chimera_Importer.Utility;

using ITinnovationsLibrary.Functions;

namespace Chimera_Importer.SelectDB
{
    /// <summary>
    ///     Interaction logic for SelectDBWindow.xaml
    /// </summary>
    internal partial class SelectDBWindow
    {
        public string DSN => "DSN=" + ResponseCombo.SelectedValue;

        private DB _db;

        public SelectDBWindow()
        {
            InitializeComponent();
        }

        private void BordlessWindow_Initialized(object sender, EventArgs e)
        {
            try
            {
                if (AuthenticationUtility.Check())
                {
                    LoadDatabaseList();
                }
                else
                {
                    DialogResult = false;
                }
            }
            catch (DB.DBConnectionErrorException ex)
            {
                Message.ErrorMessage("Unexpected error while connecting to DB for the DB selection!\n\n" + ex.Message);
            }
            catch (DB.DBErrorException ex)
            {
                Message.ErrorMessage("Unexpected error while executing DB query for the DB selection!\n\n" + ex.Message);
            }
            catch (Exception ex)
            {
                Message.ErrorMessage("Unexpected error during DB selection!\n\n" + ex.Message);
            }
        }

        private void BordlessWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _db?.CloseConnection();
            }
            catch (DB.DBCloseConnectionErrorException ex)
            {
                Message.ErrorMessage(ex.Message);
            }
        }

        private void LoadDatabaseList()
        {
            _db = new DB(true);

            OdbcParameter dbUser = new OdbcParameter("@user", OdbcType.VarChar, 255)
                                       {
                                           Value = AuthenticationUtility.User
                                       };

            _db.NewCommand("SELECT * FROM \"DatabaseList\" JOIN \"AccessList\" ON \"Name\" = \"Database\" WHERE \"User\" = ? AND \"Enabled\" = true ORDER BY \"Name\"");
            _db.ParametersAdd(dbUser);

            OdbcDataReader myReader = _db.SafeExecuteReader();

            while (myReader.Read())
            {
                ComboBoxItem cbi = new ComboBoxItem
                                       {
                                           Content = myReader["DbName"],
                                           Tag = myReader["Name"]
                                       };

                ResponseCombo.Items.Add(cbi);
            }

            if (ResponseCombo.Items.Count == 1)
            {
                ResponseCombo.SelectedIndex = 0;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (ResponseCombo.SelectedItem != null)
            {
                DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
