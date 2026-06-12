using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Protocolo.Framework.Generic
{
    public static class TaskExtensions
    {
        public static Task ContinueWith(this Task task, Action<Task> continuationAction, TaskFactory factory)
        {
            return task.ContinueWith(continuationAction, factory.CancellationToken, factory.ContinuationOptions, factory.Scheduler);
        }

        public static Task<TResult> ContinueWith<TResult>(this Task task, Func<Task, TResult> continuationFunction, TaskFactory factory)
        {
            return task.ContinueWith(continuationFunction, factory.CancellationToken, factory.ContinuationOptions, factory.Scheduler);
        }

        public static Task ContinueWith<TResult>(this Task<TResult> task, Action<Task<TResult>> continuationAction, TaskFactory<TResult> factory)
        {
            return task.ContinueWith(continuationAction, factory.CancellationToken, factory.ContinuationOptions, factory.Scheduler);
        }

        public static Task<TNewResult> ContinueWith<TResult, TNewResult>(this Task<TResult> task, Func<Task<TResult>, TNewResult> continuationFunction, TaskFactory<TResult> factory)
        {
            return task.ContinueWith(continuationFunction, factory.CancellationToken, factory.ContinuationOptions, factory.Scheduler);
        }

        public static Task ToAsync(this Task task, AsyncCallback callback, object state)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }

            var tcs = new TaskCompletionSource<object>(state);
            task.ContinueWith(_ => { tcs.SetFromTask(task); if (callback != null) { callback(tcs.Task); } });
            return tcs.Task;
        }

        public static Task<TResult> ToAsync<TResult>(this Task<TResult> task, AsyncCallback callback, object state)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }

            var tcs = new TaskCompletionSource<TResult>(state);
            task.ContinueWith(_ => { tcs.SetFromTask(task); if (callback != null) { callback(tcs.Task); } });
            return tcs.Task;
        }

        public static Task IgnoreExceptions(this Task task)
        {
            return task.ContinueWith(t => { AggregateException ignored = t.Exception; }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        }

        public static Task<T> IgnoreExceptions<T>(this Task<T> task)
        {
            return (Task<T>)((Task)task).IgnoreExceptions();
        }

        public static Task FailFastOnException(this Task task)
        {
            task.ContinueWith(t => Environment.FailFast("A task faulted.", t.Exception), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            return task;
        }

        public static Task<T> FailFastOnException<T>(this Task<T> task)
        {
            return (Task<T>)((Task)task).FailFastOnException();
        }

        public static void PropagateExceptions(this Task task)
        {
            if (!task.IsCompleted)
            {
                throw new InvalidOperationException("The task has not completed.");
            }

            if (task.IsFaulted)
            {
                task.Wait();
            }
        }

        public static void PropagateExceptions(this Task[] tasks)
        {
            if (tasks == null)
            {
                throw new ArgumentNullException("tasks");
            }

            if (tasks.Any(t => t == null))
            {
                throw new ArgumentException("tasks");
            }

            if (tasks.Any(t => !t.IsCompleted))
            {
                throw new InvalidOperationException("A task has not completed.");
            }

            Task.WaitAll(tasks);
        }

        public static void AttachToParent(this Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }

            task.ContinueWith(t => t.Wait(), CancellationToken.None, TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public static Task WithTimeout(this Task task, TimeSpan timeout)
        {
            var result = new TaskCompletionSource<object>(task.AsyncState);
            var timer = new Timer(state => ((TaskCompletionSource<object>)state).TrySetCanceled(), result, timeout, TimeSpan.FromMilliseconds(-1));

            task.ContinueWith(t => { timer.Dispose(); result.TrySetFromTask(t); }, TaskContinuationOptions.ExecuteSynchronously);

            return result.Task;
        }

        public static Task<TResult> WithTimeout<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            var result = new TaskCompletionSource<TResult>(task.AsyncState);
            var timer = new Timer(state => ((TaskCompletionSource<TResult>)state).TrySetCanceled(), result, timeout, TimeSpan.FromMilliseconds(-1));
            task.ContinueWith(t => { timer.Dispose(); result.TrySetFromTask(t); }, TaskContinuationOptions.ExecuteSynchronously);
            return result.Task;
        }

        public static IObservable<TResult> ToObservable<TResult>(this Task<TResult> task)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }

            return new TaskObservable<TResult> { Task = task };
        }

        public static TaskStatus WaitForCompletionStatus(this Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }

            ((IAsyncResult)task).AsyncWaitHandle.WaitOne();
            return task.Status;
        }

        #region Nested type: CancelOnDispose

        private class CancelOnDispose : IDisposable
        {
            internal CancellationTokenSource Source;

            #region IDisposable Members

            void IDisposable.Dispose()
            {
                Source.Cancel();
            }

            #endregion
        }

        #endregion

        #region Nested type: TaskObservable

        private class TaskObservable<TResult> : IObservable<TResult>
        {
            internal Task<TResult> Task;

            #region IObservable<TResult> Members

            public IDisposable Subscribe(IObserver<TResult> observer)
            {

                if (observer == null)
                {
                    throw new ArgumentNullException("observer");
                }


                var cts = new CancellationTokenSource();


                Task.ContinueWith(t =>
                {
                    switch (t.Status)
                    {
                        case TaskStatus.RanToCompletion:
                            observer.OnNext(Task.Result);
                            observer.OnCompleted();
                            break;

                        case TaskStatus.Faulted:
                            observer.OnError(Task.Exception);
                            break;

                        case TaskStatus.Canceled:
                            observer.OnError(new TaskCanceledException(t));
                            break;
                    }
                }, cts.Token);


                return new CancelOnDispose { Source = cts };
            }

            #endregion
        }

        #endregion
    }
}
