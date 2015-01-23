using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Puenktlich
{
    /// <summary>
    ///     The puenktlich scheduler. Pünktlich means punctual in German. Nur echt mit den Umlauten!
    /// </summary>
    public class Scheduler : IDisposable
    {
        private readonly Dictionary<object, IJobRegistration<object>> _jobs =
            new Dictionary<object, IJobRegistration<object>>();

        private readonly object _jobsLock = new object();

        private ManualResetEventSlim _running = new ManualResetEventSlim();

        /// <summary>
        ///     Initializes a new instance of the <see cref="Scheduler" /> class.
        /// </summary>
        public Scheduler() : this(() => DateTimeOffset.UtcNow)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Scheduler" /> class.
        /// </summary>
        public Scheduler(Func<DateTimeOffset> clock)
        {
            Clock = clock;
        }

        /// <summary>
        ///     Gets or sets the clock. The clock is a function that should return an UTC timestamp.
        ///     This property allows easy dependency injection and defaults to a function returning
        ///     <see cref="DateTimeOffset.UtcNow" />.
        /// </summary>
        /// <value>The function returning a timestamp.</value>
        public Func<DateTimeOffset> Clock { get; set; }

        public bool IsRunning
        {
            get { return _running.IsSet; }
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_running == null) return;

            Stop();

            lock (_jobsLock)
            {
                foreach (var job in _jobs)
                {
                    job.Value.Dispose();
                }
            }

            _running.Dispose();
            _running = null;
        }

        public event EventHandler<JobExceptionEventArgs> JobException;

        public void ScheduleJob<T>(T data, Action<IExecutionContext<T>> action, params ITrigger[] triggers)
            where T : class
        {
            var job = new SyncJobRegistration<T>
            {
                Action = action,
                ExecutionContext = new ExecutionContext<T> {Data = data},
                Triggers = triggers.ToList(),
            };

            ScheduleJobImpl(job);
        }

        public void ScheduleAsyncJob<T>(T data, Func<IExecutionContext<T>, Task> action, params ITrigger[] triggers)
            where T : class
        {
            var job = new AsyncJobRegistration<T>
            {
                Action = action,
                ExecutionContext = new ExecutionContext<T> {Data = data},
                Triggers = triggers.ToList(),
            };

            ScheduleJobImpl(job);
        }

        private void ScheduleJobImpl(IJobRegistration<object> job)
        {
            job.Init(OnTick, job);

            lock (_jobsLock)
            {
                _jobs.Add(job.ExecutionContext.Data, job);
            }

            RefreshJob(job);
        }

        public void UnscheduleJob<T>(T data)
        {
            lock (_jobsLock)
            {
                UnscheduleJob(_jobs[data]);
            }
        }

        private void UnscheduleJob(IJobRegistration<object> job)
        {
            lock (_jobsLock)
            {
                _jobs.Remove(job.ExecutionContext.Data);
            }

            job.Dispose();
        }

        public JobInfo<T> GetJobInfo<T>(T data)
        {
            IJobRegistration<object> job;

            lock (_jobsLock)
            {
                job = _jobs[data];
            }

            return new JobInfo<T>((IJobRegistration<T>) job, this);
        }

        /// <summary>
        ///     Returns all jobs.
        /// </summary>
        /// <returns></returns>
        public ReadOnlyCollection<JobInfo<object>> GetAllJobs()
        {
            lock (_jobsLock)
            {
                return new ReadOnlyCollection<JobInfo<object>>(_jobs.Values.Select(j => new JobInfo<object>(j, this)).ToList());
            }
        }

        /// <summary>
        ///     Returns all jobs which have the specified type <typeparamref name="T"/> as data type.
        /// </summary>
        /// <typeparam name="T">The data type to look for</typeparam>
        public ReadOnlyCollection<JobInfo<T>> GetAllJobs<T>()
        {
            List<JobInfo<T>> jobs;

            lock (_jobsLock)
            {
                jobs = _jobs.Values
                    .OfType<IJobRegistration<T>>()
                    .Select(j => new JobInfo<T>(j, this))
                    .ToList();
            }

            return new ReadOnlyCollection<JobInfo<T>>(jobs);
        }

        internal IJobRegistration<T> GetJob<T>(T data)
        {
            lock (_jobsLock)
            {
                return (IJobRegistration<T>) _jobs[data];
            }
        }

        /// <summary>
        ///     Starts this scheduler.
        /// </summary>
        public void Start()
        {
            if (_running.IsSet) return;

            _running.Set();

            lock (_jobsLock)
            {
                foreach (var job in _jobs)
                {
                    RefreshJob(job.Value);
                }
            }
        }

        /// <summary>
        ///     Stops this scheduler.
        /// </summary>
        public void Stop()
        {
            if (!_running.IsSet) return;

            _running.Reset();

            lock (_jobsLock)
            {
                foreach (var job in _jobs)
                {
                    job.Value.Timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        internal void RefreshJob(IJobRegistration<object> job)
        {
            DateTimeOffset now = Clock();
            DateTimeOffset next = DateTimeOffset.MaxValue;

            IList<ITrigger> triggers;

            lock (job.TriggersLock)
            {
                triggers = job.Triggers.ToList();
            }

            foreach (ITrigger trigger in triggers)
            {
                DateTimeOffset upcoming = trigger.GetUpcomingOccurrences(now).FirstOrDefault();

                if (upcoming == DateTimeOffset.MinValue)
                {
                    lock (job.TriggersLock)
                    {
                        // trigger ended, we remove it.
                        job.Triggers.Remove(trigger);
                    }

                    continue;
                }

                if (upcoming < next)
                {
                    next = upcoming;
                }
            }

            job.ExecutionContext.ScheduledFireTime = next;

            if (next == DateTimeOffset.MaxValue)
            {
                // no triggers left, we keep the job though.
                return;
            }

            now = Clock();

            long dueTime = Math.Max(0, (long) (next - now).TotalMilliseconds);

            job.Timer.Change(dueTime, -1);
        }

        private void OnTick(object state)
        {
            var job = (IJobRegistration<object>) state;

            if (!_running.IsSet)
            {
                // in case somebody stops the scheduler in a job and multiple instances of this job trigger at once
            }
            else if (!job.IsPaused)
            {
                job.ExecutionContext.ActualFireTime = Clock();

                job.Execute(
                    () => HandleJobFinished(job),
                    e => HandleJobException(job, e));
            }
        }

        private void HandleJobFinished(IJobRegistration<object> job)
        {
            if (_running.IsSet)
            {
                // in case somebody stops the scheduler in a job
                RefreshJob(job);
            }
        }

        private void OnJobException(JobExceptionEventArgs args)
        {
            if (JobException != null) JobException(this, args);
        }

        private void HandleJobException(IJobRegistration<object> job, Exception exception)
        {
            var aggregateException = exception as AggregateException;

            if (aggregateException != null && aggregateException.InnerExceptions.Count == 1)
            {
                exception = aggregateException.InnerException;
            }

            var args = new JobExceptionEventArgs(job.ExecutionContext, exception);
            OnJobException(args);
        }
    }
}