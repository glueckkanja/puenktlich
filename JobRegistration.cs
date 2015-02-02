using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Puenktlich
{
    internal interface IJobRegistration<out T> : IDisposable
    {
        object TimerLock { get; }
        object TriggersLock { get; }

        IList<ITrigger> Triggers { get; set; }
        IExecutionContext<T> ExecutionContext { get; }
        Timer Timer { get; }
        bool IsRunning { get; set; }
        bool IsPaused { get; set; }
        void Init(TimerCallback onTick, object state);
        void Execute(Action onComplete, Action<Exception> onError);
    }

    internal abstract class JobRegistration<T> : IJobRegistration<T>
    {
        protected JobRegistration()
        {
            TimerLock = new object();
            TriggersLock = new object();
        }

        public object TimerLock { get; private set; }
        public object TriggersLock { get; private set; }

        public IList<ITrigger> Triggers { get; set; }

        public IExecutionContext<T> ExecutionContext { get; internal set; }
        public Timer Timer { get; private set; }
        public bool IsRunning { get; set; }
        public bool IsPaused { get; set; }

        public void Dispose()
        {
            lock (TimerLock)
            {
                Timer.Dispose();
                Timer = null;
            }
        }

        public void Init(TimerCallback onTick, object state)
        {
            lock (TimerLock)
            {
                Timer = new Timer(onTick, state, Timeout.Infinite, Timeout.Infinite);
            }
        }

        public abstract void Execute(Action onComplete, Action<Exception> onError);
    }

    internal sealed class SyncJobRegistration<T> : JobRegistration<T>
    {
        public Action<IExecutionContext<T>> Action { get; internal set; }

        public override void Execute(Action onComplete, Action<Exception> onError)
        {
            try
            {
                Action(ExecutionContext);
            }
            catch (Exception e)
            {
                onError(e);
            }

            onComplete();
        }
    }

    internal sealed class AsyncJobRegistration<T> : JobRegistration<T>
    {
        public Func<IExecutionContext<T>, Task> Action { get; internal set; }

        public override void Execute(Action onComplete, Action<Exception> onError)
        {
            Action(ExecutionContext).ContinueWith(x =>
            {
                if (x.IsFaulted)
                {
                    onError(x.Exception);
                }

                onComplete();
            });
        }
    }
}