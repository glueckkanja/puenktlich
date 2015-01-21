using System;

namespace Puenktlich
{
    public interface IExecutionContext<out T>
    {
        DateTimeOffset ActualFireTime { get; set; }
        DateTimeOffset ScheduledFireTime { get; set; }
        T Data { get; }
    }

    public class ExecutionContext<T> : IExecutionContext<T>
    {
        /// <summary>
        ///     Gets the actual fire time when the trigger executed.
        /// </summary>
        /// <value>The actual fire time.</value>
        public DateTimeOffset ActualFireTime { get; set; }

        /// <summary>
        ///     Gets the scheduled fire time when the trigger was supposed to execute.
        /// </summary>
        /// <value>The scheduled fire time.</value>
        public DateTimeOffset ScheduledFireTime { get; set; }

        public T Data { get; set; }
    }
}