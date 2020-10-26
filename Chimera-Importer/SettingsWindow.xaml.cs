using System.IO;
using System.Windows;
using System.Windows.Forms;

using Message = ITinnovationsLibrary.Functions.Message;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Chimera_Importer
{
    /// <summary>
    ///     Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void BordlessWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TempPathBox.Text = Properties.Settings.Default.TempPath;
            MeshLabBox.Text = Properties.Settings.Default.MeshLabServer;
        }

        private void BordlessWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DialogResult == true)
            {
                string tempPath = TempPathBox.Text;
                string meshLabServer = MeshLabBox.Text;

                if (string.IsNullOrWhiteSpace(tempPath) || !Directory.Exists(tempPath))
                {
                    Message.ErrorMessage(tempPath + " is invalid!");
                }
                else if (string.IsNullOrWhiteSpace(meshLabServer) || !File.Exists(meshLabServer))
                {
                    Message.ErrorMessage(meshLabServer + " is invalid!");
                }
                else
                {
                    Properties.Settings.Default.TempPath = tempPath;
                    Properties.Settings.Default.MeshLabServer = meshLabServer;

                    Properties.Settings.Default.Save();

                    return;
                }

                e.Cancel = true;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void BrowseTempPathBtn_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog
                                             {
                                                 SelectedPath = Properties.Settings.Default.TempPath
                                             };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TempPathBox.Text = dialog.SelectedPath;
            }
        }

        private void BrowseMeshLabBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
                                        {
                                            Filter = "meshlabserver.exe (meshlabserver.exe)|meshlabserver.exe",
                                            InitialDirectory = Path.GetDirectoryName(Properties.Settings.Default.MeshLabServer),
                                            FileName = Properties.Settings.Default.MeshLabServer
                                        };

            if (dialog.ShowDialog() == true)
            {
                MeshLabBox.Text = dialog.FileName;
            }
        }
    }
}
