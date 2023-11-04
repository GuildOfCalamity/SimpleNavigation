using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNavigation;

#region [Custom Event Testing]
public class ProcessEventArgs : EventArgs
{
    public int Id { get; set; } = Random.Shared.Next(1,100000);
    public string Result { get; set; } = string.Empty;
    public DateTime CompletionTime { get; set; } = DateTime.MinValue;
    public Exception? Error { get; set; } = null;
}

public class EventProcessModule
{
    public bool IsRunning { get; private set; } = false;

    public event EventHandler<ProcessEventArgs>? ProcessStarted;
    public event EventHandler<ProcessEventArgs>? ProcessCompleted;
    public event EventHandler<ProcessEventArgs>? ProcessCanceled;

    #region [Public Methods]
    public void StartProcess()
    {
        var data = new ProcessEventArgs();

        try
        {
            IsRunning = true;

            data.Result = "Process started.";
            // Unnecessary, but might be helpful for updating UI status
            OnProcessStarted(data);

            // Some business logic here...
            Thread.Sleep(2500);

            IsRunning = false;

            data.Result = "Process finished.";
            data.CompletionTime = DateTime.Now;
            OnProcessCompleted(data);
        }
        catch (Exception ex)
        {
            IsRunning = false;
            data.Result = "Failed.";
            data.Error = ex;
            data.CompletionTime = DateTime.Now; // Not the time it completed, but the time it failed.
            OnProcessCompleted(data);
        }
    }
    public async Task StartProcessAsync()
    {
        var data = new ProcessEventArgs();

        try
        {
            IsRunning = true;

            data.Result = "Process started.";
            // Unnecessary, but might be helpful for updating UI status
            OnProcessStarted(data);

            // Some business logic here...
            await Task.Delay(2500);

            IsRunning = false;

            data.Result = "Process finished.";
            data.CompletionTime = DateTime.Now;
            OnProcessCompleted(data);
        }
        catch (Exception ex)
        {
            IsRunning = false;
            data.Result = "Failed.";
            data.Error = ex;
            data.CompletionTime = DateTime.Now; // Not the time it completed, but the time it failed.
            OnProcessCompleted(data);
        }
    }

    public void StopProcess()
    {
        var data = new ProcessEventArgs();

        try
        {
            if (IsRunning)
            {
                IsRunning = false;

                // some logic code here...
                Thread.Sleep(2500);

                data.Result = "Process stopped.";
                data.Error = null;
                data.CompletionTime = DateTime.Now;
                OnProcessCanceled(data);
            }
            else
            {
                data.Result = "Process is already complete.";
                data.Error = null;
                data.CompletionTime = DateTime.Now;
                OnProcessCompleted(data);
            }

        }
        catch (Exception ex)
        {
            data.Result = "Failed.";
            data.Error = ex;
            data.CompletionTime = DateTime.Now;
            OnProcessCompleted(data);
        }
    }
    public async Task StopProcessAsync()
    {
        var data = new ProcessEventArgs();

        try
        {
            if (IsRunning)
            {
                IsRunning = false;

                // some logic code here...
                await Task.Delay(2500);

                data.Result = "Process stopped.";
                data.Error = null;
                data.CompletionTime = DateTime.Now;
                OnProcessCanceled(data);
            }
            else
            {
                data.Result = "Process is already complete.";
                data.Error = null;
                data.CompletionTime = DateTime.Now;
                OnProcessCompleted(data);
            }

        }
        catch (Exception ex)
        {
            data.Result = "Failed.";
            data.Error = ex;
            data.CompletionTime = DateTime.Now;
            OnProcessCompleted(data);
        }
    }
    #endregion

    #region [Virtual Events]
    protected virtual void OnProcessStarted(ProcessEventArgs e) => ProcessStarted?.Invoke(this, e);
    protected virtual void OnProcessCompleted(ProcessEventArgs e) => ProcessCompleted?.Invoke(this, e);
    protected virtual void OnProcessCanceled(ProcessEventArgs e) => ProcessCanceled?.Invoke(this, e);
    #endregion
}
#endregion
