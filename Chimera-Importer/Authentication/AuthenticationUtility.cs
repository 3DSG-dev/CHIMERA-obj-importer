using System;
using System.Data.Odbc;

using Chimera_Importer.SelectDB;
using Chimera_Importer.Utility;

using ITinnovationsLibrary.Functions;

namespace Chimera_Importer.Authentication
{
    /// <summary>
    /// </summary>
    internal static class AuthenticationUtility
    {
        public static bool IsAuthenticated { get; private set; }

        public static string User => IsAuthenticated ? _user : null;

        private static string _user;
        private static string _password;

        static AuthenticationUtility()
        {
            IsAuthenticated = false;
        }

        public static bool Login()
        {
            try
            {
                Logout();

                AuthenticationWindow authenticationUI = new AuthenticationWindow();
                authenticationUI.ShowDialog();

                if (authenticationUI.DialogResult == true)
                {
                    _user = authenticationUI.Username;
                    _password = authenticationUI.PasswordMd5;

                    DBAuth();
                }
                else
                {
                    Message.WarningMessage("Authentication aborted!\n\n");
                }
            }
            catch (Exception ex)
            {
                Message.ErrorMessage("Unexpected error during authentication!\n\n" + ex.Message);

                Logout();
            }

            return IsAuthenticated;
        }

        public static bool Check(bool relog = false)
        {
            if (relog)
            {
                DBAuth(true);
            }

            return IsAuthenticated ? IsAuthenticated : Login();
        }

        public static void Logout()
        {
            IsAuthenticated = false;
            _user = null;
            _password = null;
        }

        private static void DBAuth(bool relog = false)
        {
            ////Thread threadWait = null;

            DB db = null;

            try
            {
                if (!string.IsNullOrEmpty(_user) && !string.IsNullOrEmpty(_password))
                {
                    ////WaitIndicator.Start(ref threadWait, "Wait for user authentication ...");

                    db = new DB(!relog);

                    OdbcParameter dbuser = new OdbcParameter("@user", OdbcType.VarChar, 255)
                                               {
                                                   Value = _user
                                               };
                    OdbcParameter dbpassword = new OdbcParameter("@password", OdbcType.VarChar)
                                                   {
                                                       Value = _password
                                                   };

                    db.NewCommand("SELECT \"User\" FROM \"Utenti\" WHERE \"User\" = ? AND UPPER(\"Password\") = ?");
                    db.ParametersAdd(dbuser);
                    db.ParametersAdd(dbpassword);

                    OdbcDataReader myReader1 = db.SafeExecuteReader();
                    if (myReader1.Read())
                    {
                        IsAuthenticated = true;

                        ////WaitIndicator.Stop(ref threadWait);

                        if (!relog)
                        {
                            SelectDBUtility.SelectDB();
                        }
                    }
                    else
                    {
                        ////WaitIndicator.Stop(ref threadWait);

                        Logout();

                        Message.ErrorMessage("Wrong user or password!\n\n");
                    }
                    myReader1.Close();
                }
                else
                {
                    Logout();

                    Message.ErrorMessage("Invalid login data!\n\n");
                }
            }
            catch (DB.DBConnectionErrorException ex)
            {
                ////WaitIndicator.Stop(ref threadWait);

                Logout();

                Message.ErrorMessage("Unexpected error while connecting to DB for the authentication process!\n\n" + ex.Message);
            }
            catch (DB.DBErrorException ex)
            {
                ////WaitIndicator.Stop(ref threadWait);

                Logout();

                Message.ErrorMessage("Unexpected error while executing DB query for the authentication process!\n\n" + ex.Message);
            }
            catch (Exception ex)
            {
                ////WaitIndicator.Stop(ref threadWait);

                Logout();

                Message.ErrorMessage("Unexpected error during DB authentication!\n\n" + ex.Message);
            }
            finally
            {
                try
                {
                    db?.CloseConnection();
                }
                catch (DB.DBCloseConnectionErrorException ex)
                {
                    Message.ErrorMessage(ex.Message);
                }
            }
        }
    }
}
