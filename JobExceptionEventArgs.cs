using System;

namespace Puenktlich
{
    public class JobExceptionEventArgs : EventArgs
    {
        public JobExceptionEventArgs(IExecutionContext<object> context, Exception exception)
        {
            Context = context;
            Exception = exception;
        }

        public IExecutionContext<object> Context { get; private set; }
        public Exception Exception { get; private set; }
    }
}