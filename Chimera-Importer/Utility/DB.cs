using System;
using System.Data.Odbc;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows;

namespace Chimera_Importer.Utility
{
    public class DB : IDisposable
    {
        private const string DB_DSN_DatabaseList = "DSN=Chimera_DatabaseList";

        private readonly string _appName = Assembly.GetExecutingAssembly().GetName().Name;

        private static string _dbDSN;

        public static bool SettedDSN => (_dbDSN != null) && (_dbDSN != DB_DSN_DatabaseList);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public string CommandText
        {
            get { return _myCommand.CommandText; }
            set { _myCommand = new OdbcCommand(value, _myConnection); }
        }

        public OdbcDataReader Reader { get; set; }

        public OdbcDataAdapter Adapter => new OdbcDataAdapter(_myCommand);

        private OdbcConnection _myConnection;

        private OdbcCommand _myCommand;

        #region Exceptions
        [Serializable]
        public class DBErrorException : Exception
        {
            public DBErrorException() : base("Unexpected error while querying the DB!")
            {
            }

            public DBErrorException(string message) : base(message)
            {
            }

            public DBErrorException(string message, Exception innerEx) : base(message, innerEx)
            {
            }

            /// <exception cref="SerializationException">
            ///     The class name is null or <see cref="P:System.Exception.HResult" /> is zero
            ///     (0).
            /// </exception>
            /// <exception cref="ArgumentNullException">The <paramref name="info" /> parameter is null. </exception>
            protected DBErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }

        [Serializable]
        public class DBConnectionErrorException : Exception
        {
            public DBConnectionErrorException() : base("Unexpected error while connecting to the DB!")
            {
            }

            public DBConnectionErrorException(string message) : base(message)
            {
            }

            public DBConnectionErrorException(string message, Exception innerEx) : base(message, innerEx)
            {
            }

            /// <exception cref="SerializationException">
            ///     The class name is null or <see cref="P:System.Exception.HResult" /> is zero
            ///     (0).
            /// </exception>
            /// <exception cref="ArgumentNullException">The <paramref name="info" /> parameter is null. </exception>
            protected DBConnectionErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }

        [Serializable]
        public class DBCloseConnectionErrorException : Exception
        {
            public DBCloseConnectionErrorException() : base("Unexpected error while closing the DB connection!")
            {
            }

            public DBCloseConnectionErrorException(string message) : base(message)
            {
            }

            public DBCloseConnectionErrorException(string message, Exception innerEx) : base(message, innerEx)
            {
            }

            /// <exception cref="SerializationException">
            ///     The class name is null or <see cref="P:System.Exception.HResult" /> is zero
            ///     (0).
            /// </exception>
            /// <exception cref="ArgumentNullException">The <paramref name="info" /> parameter is null. </exception>
            protected DBCloseConnectionErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }
        #endregion

        /// <exception cref="DBConnectionErrorException">Condition.</exception>
        public DB(bool useDatabaseList = false)
        {
            if (useDatabaseList)
            {
                _dbDSN = DB_DSN_DatabaseList;
            }

            OpenConnection();
        }

        #region Dispose & Finalize
        private bool _disposed;

        //Implement IDisposable.
        /// <exception cref="ArgumentNullException"> is null. </exception>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                    //TODO: check dispose during finalization
                    _myCommand?.Dispose();
                    _myConnection?.Dispose();
                }
                // Free your own state (unmanaged objects).
                // Set large fields to null.
                _disposed = true;
            }
        }

        // Use C# destructor syntax for finalization code.
        ~DB()
        {
            // Simply call Dispose(false).
            Dispose(false);
        }
        #endregion

        #region OdbcCommands
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public void NewCommand(string commandText)
        {
            _myCommand = new OdbcCommand(commandText, _myConnection);
        }
        #endregion

        #region OdbcParameters
        /// <exception cref="ArgumentNullException">The <paramref name="value" /> parameter is null.</exception>
        /// <exception cref="ArgumentException">
        ///     The <see cref="T:System.Data.Odbc.OdbcParameter" /> specified in the
        ///     <paramref name="value" /> parameter is already added to this or another
        ///     <see cref="T:System.Data.Odbc.OdbcParameterCollection" />.
        /// </exception>
        public OdbcParameter ParametersAdd(OdbcParameter value)
        {
            return _myCommand.Parameters.Add(value);
        }

        public OdbcParameter ParametersAdd(string parameterName, OdbcType odbcType)
        {
            return _myCommand.Parameters.Add(parameterName, odbcType);
        }

        public OdbcParameter ParametersAdd(string parameterName, OdbcType odbcType, int size)
        {
            return _myCommand.Parameters.Add(parameterName, odbcType, size);
        }

        public OdbcParameter ParametersAdd(string parameterName, OdbcType odbcType, int size, string sourceColumn)
        {
            return _myCommand.Parameters.Add(parameterName, odbcType, size, sourceColumn);
        }

        public void ParametersClear()
        {
            _myCommand.Parameters.Clear();
        }
        #endregion

        #region Connection
        /// <exception cref="DBConnectionErrorException">Condition.</exception>
        public void OpenConnection()
        {
            Exception ex1 = null;
            bool done = false;

            ////Thread threadWait = null;

            try
            {
                _myConnection = null;

                while (!done)
                {
                    ////WaitIndicator.Start(ref threadWait, "Opening the DB connection ...");

                    _myConnection = new OdbcConnection(_dbDSN);

                    for (int i = 1; i < 3; i++)
                    {
                        ex1 = null;

                        if (_myConnection.State != System.Data.ConnectionState.Open)
                        {
                            try
                            {
                                _myConnection.Open();
                            }
                            catch (Exception ex)
                            {
                                ex1 = ex;
                            }
                        }
                        if (_myConnection.State == System.Data.ConnectionState.Open)
                        {
                            return;
                        }

                        Thread.Sleep(i * 1000);
                    }

                    _myConnection = null;

                    ////WaitIndicator.Stop(ref threadWait);

                    //// ReSharper disable once PossibleNullReferenceException
                    MessageBoxResult retry = MessageBox.Show("Can't connect to the DB!\n\n" + ex1.Message + "\n\nTo execute current query it is necessary to have an opened DB connection: do you want to retry to open the DB connection?", _appName, MessageBoxButton.OKCancel, MessageBoxImage.Error);

                    if (retry != MessageBoxResult.OK)
                    {
                        done = true;
                    }
                }

                throw new DBConnectionErrorException("You choose not to retry to connect to the DB, so the current query is aborted\n\n" + ex1.Message + "\n\nDepending on the current operation, the DB can be in an inconsistent state!", ex1);
            }
            catch (DBConnectionErrorException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _myConnection = null;

                throw new DBConnectionErrorException("Fatal error while connecting to the DB!\n\n" + ex.Message + "\n\nOperation will be aborted: depending on the current operation, the DB can be in an inconsistent state!", ex);
            }
            ////finally
            ////{
            ////    WaitIndicator.Stop(ref threadWait);
            ////}
        }

        /// <exception cref="DBConnectionErrorException">Condition.</exception>
        public void CheckRetryConnection()
        {
            try
            {
                if (_myConnection == null || _myConnection.State != System.Data.ConnectionState.Open)
                {
                    OpenConnection();

                    _myCommand.Connection = _myConnection;
                }
            }
            catch (DBConnectionErrorException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _myConnection = null;

                throw new DBConnectionErrorException("Fatal error while connecting to the DB!\n\n" + ex.Message + "\n\nOperation will be aborted: depending on the current operation, the DB can be in an inconsistent state!", ex);
            }
        }

        /// <exception cref="DBCloseConnectionErrorException">Condition.</exception>
        public void CloseConnection()
        {
            try
            {
                _myConnection?.Close();
            }
            catch (Exception ex)
            {
                _myConnection = null;

                throw new DBCloseConnectionErrorException("Unexpected error while closing the DB connection!\n\n" + ex.Message, ex);
            }
        }

        //// ReSharper disable once InconsistentNaming
        /// <exception cref="DBConnectionErrorException">Condition.</exception>
        public void SelectDB(string DSN)
        {
            try
            {
                CloseConnection();

                _dbDSN = DSN;

                OpenConnection();
            }
            catch (Exception ex)
            {
                throw new DBConnectionErrorException("Fatal error while connecting to the DB!\n\n" + ex.Message + "\n\nOperation will be aborted: depending on the current operation, the DB can be in an inconsistent state!", ex);
            }
        }

        //// ReSharper disable once InconsistentNaming
        /// <exception cref="DBConnectionErrorException">Condition.</exception>
        public static void SelectDB_NoOpenConnection(string DSN)
        {
            try
            {
                _dbDSN = DSN;
            }
            catch (Exception ex)
            {
                throw new DBConnectionErrorException("Fatal error while connecting to the DB!\n\n" + ex.Message + "\n\nOperation will be aborted: depending on the current operation, the DB can be in an inconsistent state!", ex);
            }
        }
        #endregion

        #region Query
        public int SafeExecuteNonQuery()
        {
            return SafeExecuteQuery(0);
        }

        public object SafeExecuteScalar()
        {
            return SafeExecuteQuery(1);
        }

        public OdbcDataReader SafeExecuteReader()
        {
            return SafeExecuteQuery(2);
        }

        private dynamic SafeExecuteQuery(int type)
        {
            try
            {
                int result0 = 0;
                object result1 = null;
                OdbcDataReader result2 = null;

                bool done = false;

                Exception ex1 = null;

                if (_myCommand != null)
                {
                    while (!done)
                    {
                        for (int i = 0; !done && (i < 2); i++)
                        {
                            ex1 = null;

                            try
                            {
                                CheckRetryConnection();
                            }
                            catch (DBConnectionErrorException ex)
                            {
                                throw new DBConnectionErrorException("The DB connection isn't open! Can't execute the query: " + _myCommand.CommandText + "\n\n" + ex.Message + "\n\nOperation will be aborted: depending on the current operation, the DB can be in an inconsistent state!", ex);
                            }
                            catch (Exception ex)
                            {
                                throw new DBConnectionErrorException("Fatal error while checking the DB connection state! Can't execute the query: " + _myCommand.CommandText + "\n\n" + ex.Message + "\n\nOperation will be aborted: depending on the current operation, the DB can be in an inconsistent state!", ex);
                            }

                            try
                            {
                                switch (type)
                                {
                                    case 0:
                                        result0 = _myCommand.ExecuteNonQuery();
                                        break;
                                    case 1:
                                        result1 = _myCommand.ExecuteScalar();
                                        break;
                                    case 2:
                                        result2 = _myCommand.ExecuteReader();
                                        break;
                                    default:
                                        throw new DBErrorException("Can't identify the query type: unexpected query type!");
                                }

                                done = true;
                            }
                            catch (Exception ex)
                            {
                                ex1 = ex;
                            }
                        }
                        if (!done)
                        {
                            // ReSharper disable once PossibleNullReferenceException
                            if (ex1.Message.Contains("connessione") || ex1.Message.Contains("connection"))
                            {
                                MessageBoxResult retry = MessageBox.Show("Error while executing the query: " + _myCommand.CommandText + "\n\n" + ex1.Message + "\n\nCan't completed requested operation without successfully execute the query (depending on the current operation, the DB can be in an inconsistent state!): do you want retry to execute the query?", _appName, MessageBoxButton.OKCancel, MessageBoxImage.Error);

                                if (retry != MessageBoxResult.OK)
                                {
                                    //// ReSharper disable once ThrowingSystemException
                                    throw ex1;
                                }
                            }
                            else
                            {
                                //// ReSharper disable once ThrowingSystemException
                                throw ex1;
                            }
                        }
                    }

                    switch (type)
                    {
                        case 0:
                            return result0;
                        case 1:
                            return result1;
                        case 2:
                            return result2;
                        default:
                            throw new DBErrorException("Can't identify the query type: unexpected query type!");
                    }
                }
                else
                {
                    throw new DBErrorException("The query to be executed isn't set!");
                }
            }
            catch (DBErrorException)
            {
                throw;
            }
            catch (DBConnectionErrorException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (_myCommand != null)
                {
                    throw new DBErrorException("Fatal error while executing the query: " + _myCommand.CommandText + "\n\n" + ex.Message, ex);
                }
                else
                {
                    throw new DBErrorException("Fatal error while executing the query\n\n" + ex.Message, ex);
                }
            }
            finally
            {
                try
                {
                    if (_myCommand != null)
                    {
                        _myCommand.Parameters.Clear();
                        _myCommand = null;
                    }
                }
                catch (Exception ex)
                {
                    throw new DBErrorException("Can't set the parameters of DB command!\n\n" + ex.Message, ex);
                }
            }
        }
        #endregion
    }
}
