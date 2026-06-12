using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Protocolo.Framework.Generic
{
    public static class TaskFactoryExtensions
    {
        public static TaskFactory<TResult> ToGeneric<TResult>(this TaskFactory factory)
        {
            return new TaskFactory<TResult>(factory.CancellationToken, factory.CreationOptions, factory.ContinuationOptions, factory.Scheduler);
        }

        public static TaskFactory ToNonGeneric<TResult>(this TaskFactory<TResult> factory)
        {
            return new TaskFactory(factory.CancellationToken, factory.CreationOptions, factory.ContinuationOptions, factory.Scheduler);
        }

        public static TaskScheduler GetTargetScheduler(this TaskFactory factory)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return factory.Scheduler ?? TaskScheduler.Current;
        }

        public static TaskScheduler GetTargetScheduler<TResult>(this TaskFactory<TResult> factory)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return factory.Scheduler ?? TaskScheduler.Current;
        }

        private static TaskContinuationOptions ContinuationOptionsFromCreationOptions(
    TaskCreationOptions creationOptions)
        {
            return (TaskContinuationOptions)
                   ((creationOptions & TaskCreationOptions.AttachedToParent) |
                    (creationOptions & TaskCreationOptions.PreferFairness) |
                    (creationOptions & TaskCreationOptions.LongRunning));
        }

        public static Task<IList<Task>> TrackedSequence(this TaskFactory factory, params Func<Task>[] functions)
        {
            var tcs = new TaskCompletionSource<IList<Task>>();
            factory.Iterate(TrackedSequenceInternal(functions, tcs));
            return tcs.Task;
        }

        private static IEnumerable<Task> TrackedSequenceInternal(
    IEnumerable<Func<Task>> functions, TaskCompletionSource<IList<Task>> tcs)
        {


            var tasks = new List<Task>();


            foreach (var func in functions)
            {


                Task nextTask = null;
                try
                {
                    nextTask = func();
                }
                catch (Exception exc)
                {
                    tcs.TrySetException(exc);
                }
                if (nextTask == null) yield break;



                tasks.Add(nextTask);
                yield return nextTask;
                if (nextTask.IsFaulted) break;
            }


            tcs.TrySetResult(tasks);
        }

        public static Task Iterate(
    this TaskFactory factory,
    IEnumerable<object> source, object state)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return Iterate(factory, source, state, factory.CancellationToken, factory.CreationOptions, factory.GetTargetScheduler());
        }

        public static Task Iterate(
    this TaskFactory factory,
    IEnumerable<object> source, object state,
    CancellationToken cancellationToken)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return Iterate(factory, source, state, cancellationToken, factory.CreationOptions, factory.GetTargetScheduler());
        }

        public static Task Iterate(
    this TaskFactory factory,
    IEnumerable<object> source, object state,
    TaskCreationOptions creationOptions)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return Iterate(factory, source, state, factory.CancellationToken, creationOptions, factory.GetTargetScheduler());
        }

        public static Task Iterate(
    this TaskFactory factory,
    IEnumerable<object> source, object state,
    TaskScheduler scheduler)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return Iterate(factory, source, state, factory.CancellationToken, factory.CreationOptions, scheduler);
        }

        public static Task Iterate(
    this TaskFactory factory,
    IEnumerable<object> source, object state,
    CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {

            if (factory == null) throw new ArgumentNullException("factory");
            if (source == null) throw new ArgumentNullException("asyncIterator");
            if (scheduler == null) throw new ArgumentNullException("scheduler");


            IEnumerator<object> enumerator = source.GetEnumerator();
            if (enumerator == null)
                throw new InvalidOperationException("Invalid enumerable - GetEnumerator returned null");



            var trs = new TaskCompletionSource<object>(state, creationOptions);
            trs.Task.ContinueWith(_ => enumerator.Dispose(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            Action<Task> recursiveBody = null;
            Action<Task> body = recursiveBody;
            recursiveBody = antecedent =>
            {
                try
                {




                    if (enumerator.MoveNext())
                    {
                        object nextItem = enumerator.Current;


                        if (nextItem is Task)
                        {
                            var nextTask = (Task)nextItem;

                            nextTask.IgnoreExceptions();
                            nextTask.ContinueWith(body).IgnoreExceptions();
                        }


                        else if (nextItem is TaskScheduler)
                        {
                            if (body != null)
                                Task.Factory.StartNew(() => body(null), CancellationToken.None, TaskCreationOptions.None, (TaskScheduler)nextItem).IgnoreExceptions();
                        }

                        else
                            trs.TrySetException(new InvalidOperationException("Task or TaskScheduler object expected in Iterate"));
                    }


                    else trs.TrySetResult(null);
                }


                catch (Exception exc)
                {
                    var oce = exc as OperationCanceledException;
                    if (oce != null && oce.CancellationToken == cancellationToken)
                    {
                        trs.TrySetCanceled();
                    }
                    else trs.TrySetException(exc);
                }
            };


            factory.StartNew(() => recursiveBody(null), CancellationToken.None, TaskCreationOptions.None, scheduler).IgnoreExceptions();


            return trs.Task;
        }

        public static Task Iterate(this TaskFactory factory, IEnumerable<object> source)
        {
            if (factory == null)
                throw new ArgumentNullException("factory");

            return Iterate(factory, source, null, factory.CancellationToken, factory.CreationOptions, factory.GetTargetScheduler());
        }

        public static Task Iterate(
            this TaskFactory factory,
            IEnumerable<object> source,
            CancellationToken cancellationToken)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return Iterate(factory, source, null, cancellationToken, factory.CreationOptions, factory.GetTargetScheduler());
        }

        public static Task Iterate(this TaskFactory factory, IEnumerable<object> source, TaskCreationOptions creationOptions)
        {
            if (factory == null)
                throw new ArgumentNullException("factory");

            return Iterate(factory, source, null, factory.CancellationToken, creationOptions, factory.GetTargetScheduler());
        }

        public static Task Iterate(
    this TaskFactory factory,
    IEnumerable<object> source,
    TaskScheduler scheduler)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return Iterate(factory, source, null, factory.CancellationToken, factory.CreationOptions, scheduler);
        }

        public static Task Iterate(
    this TaskFactory factory,
    IEnumerable<object> source,
    CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            return Iterate(factory, source, null, cancellationToken, creationOptions, scheduler);
        }

        public static Task FromAsync(WaitHandle waitHandle)
        {
            var tcs = new TaskCompletionSource<object>();
            RegisteredWaitHandle rwh = ThreadPool.RegisterWaitForSingleObject(waitHandle, delegate { tcs.TrySetResult(null); }, null, -1, true);
            Task<object> t = tcs.Task;
            t.ContinueWith(_ => rwh.Unregister(null), TaskContinuationOptions.ExecuteSynchronously);
            return t;
        }

        public static Task FromException(this TaskFactory factory, Exception exception)
        {
            var tcs = new TaskCompletionSource<object>(factory.CreationOptions);
            tcs.SetException(exception);
            return tcs.Task;
        }

        public static Task<TResult> FromException<TResult>(this TaskFactory factory, Exception exception)
        {
            var tcs = new TaskCompletionSource<TResult>(factory.CreationOptions);
            tcs.SetException(exception);
            return tcs.Task;
        }

        public static Task<TResult> FromResult<TResult>(this TaskFactory factory, TResult result)
        {
            var tcs = new TaskCompletionSource<TResult>(factory.CreationOptions);
            tcs.SetResult(result);
            return tcs.Task;
        }

        public static Task<TResult> FromException<TResult>(this TaskFactory<TResult> factory, Exception exception)
        {
            var tcs = new TaskCompletionSource<TResult>(factory.CreationOptions);
            tcs.SetException(exception);
            return tcs.Task;
        }

        public static Task<TResult> FromResult<TResult>(this TaskFactory<TResult> factory, TResult result)
        {
            var tcs = new TaskCompletionSource<TResult>(factory.CreationOptions);
            tcs.SetResult(result);
            return tcs.Task;
        }

        public static Task StartNewDelayed(
    this TaskFactory factory, int millisecondsDelay)
        {
            return StartNewDelayed(factory, millisecondsDelay, CancellationToken.None);
        }

        public static Task StartNewDelayed(this TaskFactory factory, int millisecondsDelay,
                                   CancellationToken cancellationToken)
        {

            if (factory == null) throw new ArgumentNullException("factory");
            if (millisecondsDelay < 0) throw new ArgumentOutOfRangeException("millisecondsDelay");


            var tcs = new TaskCompletionSource<object>(factory.CreationOptions);
            CancellationTokenRegistration[] ctr = { default(CancellationTokenRegistration) };



            var timer = new Timer(self => { ctr[0].Dispose(); ((Timer)self).Dispose(); tcs.TrySetResult(null); });


            if (cancellationToken.CanBeCanceled)
            {


                ctr[0] = cancellationToken.Register(() => { timer.Dispose(); tcs.TrySetCanceled(); });
            }


            timer.Change(millisecondsDelay, Timeout.Infinite);
            return tcs.Task;
        }

        public static Task StartNewDelayed(
    this TaskFactory factory,
    int millisecondsDelay, Action action)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return StartNewDelayed(factory, millisecondsDelay, action, factory.CancellationToken, factory.CreationOptions, factory.GetTargetScheduler());
        }

        public static Task StartNewDelayed(
    this TaskFactory factory,
    int millisecondsDelay, Action action,
    TaskCreationOptions creationOptions)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return StartNewDelayed(factory, millisecondsDelay, action, factory.CancellationToken, creationOptions, factory.GetTargetScheduler());
        }

        public static Task StartNewDelayed(
    this TaskFactory factory,
    int millisecondsDelay, Action action,
    CancellationToken cancellationToken)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return StartNewDelayed(factory, millisecondsDelay, action, cancellationToken, factory.CreationOptions, factory.GetTargetScheduler());
        }

        public static Task StartNewDelayed(
    this TaskFactory factory,
    int millisecondsDelay, Action action,
    CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            if (millisecondsDelay < 0) throw new ArgumentOutOfRangeException("millisecondsDelay");
            if (action == null) throw new ArgumentNullException("action");
            if (scheduler == null) throw new ArgumentNullException("scheduler");

            return factory.StartNewDelayed(millisecondsDelay, cancellationToken).ContinueWith(_ => action(), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, scheduler);
        }

        public static Task StartNewDelayed(
    this TaskFactory factory,
    int millisecondsDelay, Action<object> action, object state)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return StartNewDelayed(factory, millisecondsDelay, action, state, factory.CancellationToken, factory.CreationOptions, factory.GetTargetScheduler());
        }

        public static Task StartNewDelayed(
    this TaskFactory factory,
    int millisecondsDelay, Action<object> action, object state,
    TaskCreationOptions creationOptions)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return StartNewDelayed(factory, millisecondsDelay, action, state, factory.CancellationToken, creationOptions, factory.GetTargetScheduler());
        }

        public static Task StartNewDelayed(
    this TaskFactory factory,
    int millisecondsDelay, Action<object> action, object state,
    CancellationToken cancellationToken)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return StartNewDelayed(factory, millisecondsDelay, action, state, cancellationToken, factory.CreationOptions, factory.GetTargetScheduler());
        }

        public static Task StartNewDelayed(
    this TaskFactory factory,
    int millisecondsDelay, Action<object> action, object state,
    CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            if (millisecondsDelay < 0) throw new ArgumentOutOfRangeException("millisecondsDelay");
            if (action == null) throw new ArgumentNullException("action");
            if (scheduler == null) throw new ArgumentNullException("scheduler");


            var result = new TaskCompletionSource<object>(state);


            factory
                .StartNewDelayed(millisecondsDelay, cancellationToken)
                .ContinueWith(t =>
                {
                    if (t.IsCanceled) result.TrySetCanceled();
                    else
                    {
                        try
                        {
                            action(state);
                            result.TrySetResult(null);
                        }
                        catch (Exception exc)
                        {
                            result.TrySetException(exc);
                        }
                    }
                }, scheduler);


            return result.Task;
        }

        public static Task<TResult> StartNewDelayed<TResult>(
    this TaskFactory<TResult> factory,
    int millisecondsDelay, Func<TResult> function)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return StartNewDelayed(factory, millisecondsDelay, function, factory.CancellationToken, factory.CreationOptions, factory.GetTargetScheduler());
        }

        public static Task<TResult> StartNewDelayed<TResult>(
    this TaskFactory<TResult> factory,
    int millisecondsDelay, Func<TResult> function,
    TaskCreationOptions creationOptions)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return StartNewDelayed(factory, millisecondsDelay, function, factory.CancellationToken, creationOptions, factory.GetTargetScheduler());
        }

        public static Task<TResult> StartNewDelayed<TResult>(
    this TaskFactory<TResult> factory,
    int millisecondsDelay, Func<TResult> function,
    CancellationToken cancellationToken)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return StartNewDelayed(factory, millisecondsDelay, function, cancellationToken, factory.CreationOptions, factory.GetTargetScheduler());
        }

        public static Task<TResult> StartNewDelayed<TResult>(
    this TaskFactory<TResult> factory,
    int millisecondsDelay, Func<TResult> function,
    CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            if (millisecondsDelay < 0) throw new ArgumentOutOfRangeException("millisecondsDelay");
            if (function == null) throw new ArgumentNullException("function");
            if (scheduler == null) throw new ArgumentNullException("scheduler");


            var tcs = new TaskCompletionSource<object>();
            var timer = new Timer(obj => ((TaskCompletionSource<object>)obj).SetResult(null), tcs, millisecondsDelay, Timeout.Infinite);


            return tcs.Task.ContinueWith(_ => { timer.Dispose(); return function(); }, cancellationToken, ContinuationOptionsFromCreationOptions(creationOptions), scheduler);
        }

        public static Task<TResult> StartNewDelayed<TResult>(
    this TaskFactory<TResult> factory,
    int millisecondsDelay, Func<object, TResult> function, object state)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return StartNewDelayed(factory, millisecondsDelay, function, state, factory.CancellationToken, factory.CreationOptions, factory.GetTargetScheduler());
        }

        public static Task<TResult> StartNewDelayed<TResult>(
    this TaskFactory<TResult> factory,
    int millisecondsDelay, Func<object, TResult> function, object state,
    CancellationToken cancellationToken)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return StartNewDelayed(factory, millisecondsDelay, function, state, cancellationToken, factory.CreationOptions, factory.GetTargetScheduler());
        }

        public static Task<TResult> StartNewDelayed<TResult>(
    this TaskFactory<TResult> factory,
    int millisecondsDelay, Func<object, TResult> function, object state,
    TaskCreationOptions creationOptions)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return StartNewDelayed(factory, millisecondsDelay, function, state, factory.CancellationToken, creationOptions, factory.GetTargetScheduler());
        }

        public static Task<TResult> StartNewDelayed<TResult>(
    this TaskFactory<TResult> factory,
    int millisecondsDelay, Func<object, TResult> function, object state,
    CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            if (millisecondsDelay < 0) throw new ArgumentOutOfRangeException("millisecondsDelay");
            if (function == null) throw new ArgumentNullException("action");
            if (scheduler == null) throw new ArgumentNullException("scheduler");


            var result = new TaskCompletionSource<TResult>(state);
            Timer[] timer = { null };


            var functionTask = new Task<TResult>(function, state, creationOptions);


            functionTask.ContinueWith(t =>
            {
                result.SetFromTask(t);
                if (timer[0] != null) timer[0].Dispose();
            }, cancellationToken,
                                      ContinuationOptionsFromCreationOptions(creationOptions) |
                                      TaskContinuationOptions.ExecuteSynchronously, scheduler);


            timer[0] = new Timer(obj => ((Task)obj).Start(scheduler), functionTask, millisecondsDelay, Timeout.Infinite);

            return result.Task;
        }

        public static Task Create(
    this TaskFactory factory, Action action)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return new Task(action, factory.CancellationToken, factory.CreationOptions);
        }

        public static Task Create(
    this TaskFactory factory, Action action, TaskCreationOptions creationOptions)
        {
            return new Task(action, factory.CancellationToken, creationOptions);
        }

        public static Task Create(
    this TaskFactory factory, Action<Object> action, object state)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return new Task(action, state, factory.CancellationToken, factory.CreationOptions);
        }

        public static Task Create(
    this TaskFactory factory, Action<Object> action, object state, TaskCreationOptions creationOptions)
        {
            return new Task(action, state, factory.CancellationToken, creationOptions);
        }

        public static Task<TResult> Create<TResult>(
    this TaskFactory factory, Func<TResult> function)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return new Task<TResult>(function, factory.CancellationToken, factory.CreationOptions);
        }

        public static Task<TResult> Create<TResult>(
    this TaskFactory factory, Func<TResult> function, TaskCreationOptions creationOptions)
        {
            return new Task<TResult>(function, factory.CancellationToken, creationOptions);
        }

        public static Task<TResult> Create<TResult>(
    this TaskFactory factory, Func<Object, TResult> function, object state)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return new Task<TResult>(function, state, factory.CancellationToken, factory.CreationOptions);
        }

        public static Task<TResult> Create<TResult>(
    this TaskFactory factory, Func<Object, TResult> function, object state, TaskCreationOptions creationOptions)
        {
            return new Task<TResult>(function, state, factory.CancellationToken, creationOptions);
        }

        public static Task<TResult> Create<TResult>(
    this TaskFactory<TResult> factory, Func<TResult> function)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return new Task<TResult>(function, factory.CancellationToken, factory.CreationOptions);
        }

        public static Task<TResult> Create<TResult>(
    this TaskFactory<TResult> factory, Func<TResult> function, TaskCreationOptions creationOptions)
        {
            return new Task<TResult>(function, factory.CancellationToken, creationOptions);
        }

        public static Task<TResult> Create<TResult>(
    this TaskFactory<TResult> factory, Func<Object, TResult> function, object state)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            return new Task<TResult>(function, state, factory.CancellationToken, factory.CreationOptions);
        }

        public static Task<TResult> Create<TResult>(
    this TaskFactory<TResult> factory, Func<Object, TResult> function, object state,
    TaskCreationOptions creationOptions)
        {
            return new Task<TResult>(function, state, factory.CancellationToken, creationOptions);
        }

        public static Task<Task[]> WhenAll(
    this TaskFactory factory, params Task[] tasks)
        {
            return factory.ContinueWhenAll(tasks, completedTasks => completedTasks);
        }

        public static Task<Task<TAntecedentResult>[]> WhenAll<TAntecedentResult>(
    this TaskFactory factory, params Task<TAntecedentResult>[] tasks)
        {
            return factory.ContinueWhenAll(tasks, completedTasks => completedTasks);
        }

        public static Task<Task> WhenAny(
    this TaskFactory factory, params Task[] tasks)
        {
            return factory.ContinueWhenAny(tasks, completedTask => completedTask);
        }

        public static Task<Task<TAntecedentResult>> WhenAny<TAntecedentResult>(
    this TaskFactory factory, params Task<TAntecedentResult>[] tasks)
        {
            return factory.ContinueWhenAny(tasks, completedTask => completedTask);
        }
    }
}
