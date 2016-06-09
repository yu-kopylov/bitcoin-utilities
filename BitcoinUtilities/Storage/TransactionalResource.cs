using System;
using System.Transactions;

namespace BitcoinUtilities.Storage
{
    /// <summary>
    /// A basic implementation of <see cref="IEnlistmentNotification"/> that support enlistment in a single concurrent transaction.
    /// </summary>
    internal class TransactionalResource : IEnlistmentNotification
    {
        private readonly Action commitAction;
        private readonly Action rollbackAction;

        private Transaction currentTransaction;

        public TransactionalResource(Action commitAction, Action rollbackAction)
        {
            this.commitAction = commitAction;
            this.rollbackAction = rollbackAction;
        }

        public void Enlist()
        {
            Transaction tx = Transaction.Current;
            if (tx == null)
            {
                //todo: describe this exception in XMLDOC and add test
                throw new InvalidOperationException("This resource should be accessed within a transaction.");
            }
            if (currentTransaction != null)
            {
                if (currentTransaction != tx)
                {
                    throw new InvalidOperationException("This resource is already enlisted in a transaction.");
                }
                return;
            }
            currentTransaction = tx;
            tx.EnlistVolatile(this, EnlistmentOptions.None);
        }

        public void Prepare(PreparingEnlistment enlistment)
        {
            enlistment.Prepared();
        }

        public void Commit(Enlistment enlistment)
        {
            currentTransaction = null;
            commitAction();
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            currentTransaction = null;
            rollbackAction();
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            currentTransaction = null;
            rollbackAction();
            enlistment.Done();
        }
    }
}