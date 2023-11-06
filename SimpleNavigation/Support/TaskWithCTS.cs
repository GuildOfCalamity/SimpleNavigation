using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNavigation;

/*  
TaskStatus   	                Informal Description
------------------------------------------------------------------------------------------------------
Created 	                    This is the starting state for tasks created through a Task constructor.  Tasks in this state will not leave the state until Start or RunSynchronously is called on the instance or until the Task is canceled.
WaitingForActivation 	        This is the starting state for tasks created through methods like ContinueWith, ContinueWhenAll, ContinueWhenAny, and FromAsync, as well as from a TaskCompletionSource<TResult>.  The task isn’t currently scheduled, and won’t be until some dependent operation has completed (some tasks may never be scheduled, such as those created by a TaskCompletionSource<TResult> that have nothing relevant to be scheduled).  For example, a task created by ContinueWith won’t be scheduled until the antecedent task (the one off of which ContinueWith was called) completes execution.
WaitingToRun 	                The task has been scheduled to a TaskScheduler and is waiting to be picked up by that scheduler and run.  This is the starting state for tasks created through TaskFactory.StartNew; by the time the Task is returned from StartNew, it will already have been scheduled, and thus the state will be at least WaitingToRun (“at least”, since by the time StartNew returns, the Task could of course have already started or even completed executing).
Running 	                    The Task is currently executing.
WaitingForChildrenToComplete    A Task isn’t considered complete until its attached children have completed.  If a Task has finished executing its code body, it will leave the Running state, and if it’s then implicitly waiting for its children to complete, it will enter this state.
RanToCompletion 	            One of the three final states.  A Task in this state has successfully completed execution, running to the end of its body without cancellation and without throwing an unhandled exception.
Canceled 	                    One of the three final states.  A Task in this state completed execution, but it did so through cancellation.  To end in the Canceled state, a Task must either have cancellation requested prior to starting execution, or it must acknowledge a cancellation request during its execution.
Faulted 	                    One of the three final states.  A Task in this state completed execution due to an unhandled exception in its body or due to one of its attached children completing in this state.

NOTE: Many of the states are transient, meaning that we’re dealing with a concurrent system, and by the time you’ve received the value of the status, the status may have changed.
*/

/// <summary>
/// A cancellable task class.
/// </summary>
public class TaskWithCTS
{
    #region [Props]
    public int? _TID { get; private set; }
    public bool _Interrupted { get; private set; }
    public Guid? _GID { get; private set; }
    public Task _Task { get; private set; }
    public DateTime? _Started { get; private set; }
    public DateTime? _Ended { get; private set; }
    public Exception _Error { get; private set; }
    public CancellationTokenSource _CancellationTokenSource { get; private set; }
    #endregion

    public TaskWithCTS(Action action, CancellationTokenSource cts)
    {
        try
        {   /*
            NOTE: TaskCreationOptions.LongRunning allows us to immeadiately over-subscribe
            when creating large amounts of TaskWithCTS objects. Typically 10 task threads
            are instantly available, but you would see a decrease in spin-up time once more
            than 10 are requested; that is why we are using the Factory's LongRunning option.
            */
            _GID = Guid.NewGuid();
            _Started = DateTime.Now;
            _CancellationTokenSource = cts;
            _Interrupted = false;
            _Task = Task.Factory.StartNew(() =>
            {   
                // NOTE: Task.Factory.StartNew doesn't understand asynchronous delegates.
                // StartNew is an extremely low-level API that should not be used normally
                // in production code. We should re-work this to use Task.Run instead.
                _TID = Task.CurrentId;
                action();
            },
            _CancellationTokenSource.Token,
            TaskCreationOptions.LongRunning,
            SynchronizationContext.Current != null ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Current)
            .ContinueWith(t => { _Ended = DateTime.Now; });

            if (_Task.Status != TaskStatus.WaitingForActivation)
                Debug.WriteLine($">>> {_TID} may have an issue. <<<");
        }
        catch (Exception ex)
        {
            _Error = ex;
        }
    }

    /// <summary>
    /// Cancels a <see cref="TaskWithCTS"/> object.
    /// </summary>
    /// <returns>true if task was able to be canceled, false otherwise</returns>
    public bool Cancel()
    {
        if (_CancellationTokenSource != null && _CancellationTokenSource.Token.CanBeCanceled)
        {
            _CancellationTokenSource.Cancel();
            _Interrupted = true;
            return true;
        }
        else
            return false;
    }

    /// <summary>
    /// Gets the state of a <see cref="TaskWithCTS"/> object.
    /// </summary>
    /// <returns>true if task matches any running or waiting state, false otherwise</returns>
    public bool IsRunning()
    {
        if (_Task != null)
        {
            if (_Task.Status == TaskStatus.Created ||
                _Task.Status == TaskStatus.WaitingToRun ||
                _Task.Status == TaskStatus.WaitingForActivation ||
                _Task.Status == TaskStatus.WaitingForChildrenToComplete ||
                _Task.Status == TaskStatus.Running)
                return true;
            else
                return false;
        }
        else
            return false;
    }

    /// <summary>
    /// Gets the state of a <see cref="TaskWithCTS"/> object.
    /// </summary>
    /// <returns>true if task matches any waiting state, false otherwise</returns>
    public bool IsWaiting()
    {
        if (_Task != null)
        {
            if (_Task.Status == TaskStatus.WaitingForActivation ||
                _Task.Status == TaskStatus.WaitingToRun ||
                _Task.Status == TaskStatus.WaitingForChildrenToComplete)
                return true;
            else
                return false;
        }
        else
            return false;
    }

    /// <summary>
    /// Gets the IsCanceled state of a <see cref="TaskWithCTS"/>.
    /// </summary>
    /// <returns>true if task was canceled, false otherwise</returns>
    public bool IsCanceled()
    {
        if (_Task != null)
        {
            if (_Task.IsCanceled || _Interrupted)
                return true;
            else
                return false;
        }
        else
            return false;
    }

    /// <summary>
    /// Gets the IsCanceled state of a <see cref="TaskWithCTS"/>.
    /// </summary>
    /// <returns>true if task was canceled, false otherwise</returns>
    public bool IsFaulted()
    {
        if (_Task != null)
        {
            if (_Task.IsFaulted || _Error != null)
                return true;
            else
            {
                Debug.WriteLine($"> {_TID}'s status is {_Task.Status}");
                return false;
            }
        }
        else
            return false;
    }
}
