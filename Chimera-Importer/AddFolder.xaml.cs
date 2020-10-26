using System;
using System.Collections.Generic;
using System.IO;
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
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using FormDialogResult = System.Windows.Forms.DialogResult;

using ITinnovationsLibrary.Functions;

using Path = System.Windows.Shapes.Path;

namespace Chimera_Importer
{
    /// <summary>
    /// Interaction logic for AddFolder.xaml
    /// </summary>
    public partial class AddFolderWindow
    {
        public string Folder => FolderBox.Text;

        public string Type => TypeCombo.SelectedValue.ToString();
 
        public bool IsNew => IsNewCombo.SelectedValue.ToString() == "New";

        public AddFolderWindow()
        {
            InitializeComponent();
        }

        private void BordlessWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DialogResult == true && !ValidateData())
            {
                e.Cancel = true;
            }
        }

        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            FormDialogResult result = dialog.ShowDialog();
            if (result == FormDialogResult.OK)
            {
                FolderBox.Text = dialog.SelectedPath;
            }
        }
        private bool ValidateData()
        {
            if (!Directory.Exists(Folder))
            {
                Message.ErrorMessage("The selected folder doesn't exist!");
            }
            else
            {
                return true;
            }

            return false;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
