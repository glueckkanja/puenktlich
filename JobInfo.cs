using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Puenktlich
{
    public class JobInfo<T>
    {
        private readonly IJobRegistration<T> _job;
        private readonly Scheduler _scheduler;
        private readonly TriggerCollection _triggerCollection;

        internal JobInfo(IJobRegistration<T> job, Scheduler scheduler)
        {
            _job = job;
            _scheduler = scheduler;
            _triggerCollection = new TriggerCollection((IJobRegistration<object>) _job, scheduler);
        }

        public bool IsPaused
        {
            get { return _job.IsPaused; }
        }

        public TriggerCollection Triggers
        {
            get { return _triggerCollection; }
        }

        public DateTimeOffset? LastActualFireTime
        {
            get
            {
                if (_job.ExecutionContext.ActualFireTime > DateTimeOffset.MinValue)
                {
                    return _job.ExecutionContext.ActualFireTime;
                }

                return null;
            }
        }

        public DateTimeOffset? NextScheduledFireTime
        {
            get
            {
                if (_job.ExecutionContext.ScheduledFireTime < DateTimeOffset.MaxValue)
                {
                    return _job.ExecutionContext.ScheduledFireTime;
                }

                return null;
            }
        }

        public T Data
        {
            get { return _job.ExecutionContext.Data; }
        }

        public void Pause()
        {
            IJobRegistration<T> job = _scheduler.GetJob(Data);

            job.IsPaused = true;

            lock (job.TimerLock)
            {
                if (job.Timer == null) throw new ObjectDisposedException("Job");

                job.Timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void Resume()
        {
            IJobRegistration<T> job = _scheduler.GetJob(Data);

            job.IsPaused = false;
            RefreshJob(job);
        }

        private void RefreshJob(IJobRegistration<T> job)
        {
            _scheduler.RefreshJob((IJobRegistration<object>) job);
        }

        public class TriggerCollection : IEnumerable<ITrigger>
        {
            private readonly IJobRegistration<object> _job;
            private readonly Scheduler _scheduler;

            internal TriggerCollection(IJobRegistration<object> job, Scheduler scheduler)
            {
                _job = job;
                _scheduler = scheduler;
            }

            public IEnumerator<ITrigger> GetEnumerator()
            {
                return GetSnapshot().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(ITrigger trigger)
            {
                IJobRegistration<object> job = _scheduler.GetJob(_job.ExecutionContext.Data);

                lock (job.TriggersLock)
                {
                    job.Triggers.Add(trigger);
                }

                _scheduler.RefreshJob(job);
            }

            public void Remove(ITrigger trigger)
            {
                IJobRegistration<object> job = _scheduler.GetJob(_job.ExecutionContext.Data);

                lock (job.TriggersLock)
                {
                    job.Triggers.Remove(trigger);
                }

                _scheduler.RefreshJob(job);
            }

            public void Clear()
            {
                IJobRegistration<object> job = _scheduler.GetJob(_job.ExecutionContext.Data);

                lock (job.TriggersLock)
                {
                    job.Triggers.Clear();
                }

                _scheduler.RefreshJob(job);
            }

            private List<ITrigger> GetSnapshot()
            {
                lock (_job.TriggersLock)
                {
                    return _job.Triggers.ToList();
                }
            }
        }
    }
}