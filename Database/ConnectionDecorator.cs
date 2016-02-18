namespace TransactionManagement.Database
{
    using System;
    using System.Data;
    public class ConnectionDecorator : IDbConnection
    {
        private readonly IDbConnection dbConnection;
        private readonly Lazy<TransactionDecorator> dbTransaction;        

        public ConnectionDecorator(IDbConnection dbConnection)
        {
            this.dbConnection = dbConnection;
            dbTransaction =
                new Lazy<TransactionDecorator>(() => new TransactionDecorator(this, dbConnection.BeginTransaction()));
        }

        public void Dispose()
        {
            if (dbTransaction.IsValueCreated)
            {
                dbTransaction.Value.EndTransaction();
            }
            dbConnection.Dispose();
        }

        public IDbTransaction BeginTransaction()
        {
            dbTransaction.Value.IncrementTransactionCount();
            return dbTransaction.Value;           
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            return BeginTransaction();
        }

        public void Close()
        {
            dbConnection.Close();
        }

        public void ChangeDatabase(string databaseName)
        {
            dbConnection.ChangeDatabase(databaseName);
        }

        public IDbCommand CreateCommand()
        {
            return dbConnection.CreateCommand();
        }

        public void Open()
        {
            dbConnection.Open();
        }

        public string ConnectionString
        {
            get { return dbConnection.ConnectionString; }
            set { dbConnection.ConnectionString = value; }
        }

        public int ConnectionTimeout
        {
            get { return dbConnection.ConnectionTimeout; }
        }

        public string Database
        {
            get { return dbConnection.Database; }
        }

        public ConnectionState State
        {
            get { return dbConnection.State; }
        }
    }
}