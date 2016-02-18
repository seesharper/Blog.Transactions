namespace TransactionManagement.Database
{
    using System.Data;
    public class TransactionDecorator : IDbTransaction
    {
        private readonly IDbTransaction dbTransaction;
        private int transactionCount;
        private int commitCount;
        public TransactionDecorator(IDbConnection dbConnection, IDbTransaction dbTransaction)
        {
            Connection = dbConnection;
            this.dbTransaction = dbTransaction;
        }

        public void IncrementTransactionCount()
        {
            transactionCount++;
        }

        public void EndTransaction()
        {
            if (commitCount == transactionCount)
            {
                dbTransaction.Commit();
            }
            else
            {
                dbTransaction.Rollback();
            }
            dbTransaction.Dispose();
        }

        public void Dispose() { }
        
        public virtual void Commit()
        {
            commitCount++;
        }

        public void Rollback() { }
        
        public IDbConnection Connection { get; }

        public IsolationLevel IsolationLevel => dbTransaction.IsolationLevel;
    }
}