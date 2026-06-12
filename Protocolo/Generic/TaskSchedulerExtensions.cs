using System;
using System.Threading;
using System.Threading.Tasks;

namespace Protocolo.Framework.Generic
{
    public static class TaskSchedulerExtensions
    {
        public static SynchronizationContext ToSynchronizationContext(this TaskScheduler scheduler)
        {
            return new TaskSchedulerSynchronizationContext(scheduler);
        }

        #region Nested type: TaskSchedulerSynchronizationContext

        private sealed class TaskSchedulerSynchronizationContext : SynchronizationContext
        {
            private readonly TaskScheduler m_scheduler;

            internal TaskSchedulerSynchronizationContext(TaskScheduler scheduler)
            {
                if (scheduler == null)
                {
                    throw new ArgumentNullException("scheduler");
                }

                m_scheduler = scheduler;
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                Task.Factory.StartNew(() => d(state), CancellationToken.None, TaskCreationOptions.None, m_scheduler);
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                var t = new Task(() => d(state));
                t.RunSynchronously(m_scheduler);
                t.Wait();
            }
        }

        #endregion
    }
}
