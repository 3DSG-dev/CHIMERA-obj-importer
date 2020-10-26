using System;

using Chimera_Importer.Authentication;

using ITinnovationsLibrary.Functions;

using Chimera_Importer.Utility;

namespace Chimera_Importer.SelectDB
{
    internal class SelectDBUtility
    {
        public static void SelectDB()
        {
            try
            {
                do
                {
                    SelectDBWindow selw = new SelectDBWindow();

                    if (selw.ShowDialog() == true)
                    {
                        DB.SelectDB_NoOpenConnection(selw.DSN);

                        string name = selw.DSN.Substring(4);

                        AuthenticationUtility.Check(true);
                    }
                } while (!DB.SettedDSN);
            }
            catch (Exception ex)
            {
                Message.ErrorMessage("Unexpected error during DB selection!\n\n" + ex.Message);
            }
        }
    }
}
