using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using ReleaseDeck.Portal.Services;

namespace ReleaseDeck.Portal.Shared;

/// <summary>One outer boundary per user action; failures retain local work and never report a dependent success.</summary>
public abstract class DeckPage : ComponentBase, IDisposable
{
    [Inject] protected ReleaseStore Store { get; set; } = null!;
    [Inject] protected ReleaseApi Api { get; set; } = null!;
    [Inject] protected NavigationManager Navigation { get; set; } = null!;
    protected bool Busy;
    protected string Message = "";
    protected bool Failed;
    protected bool Disposed;
    int actionSequence;
    protected override void OnInitialized() => Store.Changed += Refresh;
    void Refresh() { if (!Disposed) _ = InvokeAsync(StateHasChanged); }
    /// <summary>Consumes an action error at the UI boundary and logs only its type and elapsed time when opted in.</summary>
    protected async Task Act(Func<Task> action, string? success = null, bool allowWhileBusy = false)
    {
        if (Busy && !allowWhileBusy) return;
        var sequence = ++actionSequence;
        Busy = true; Failed = false; Message = ""; var timer = Stopwatch.StartNew();
        try { await action(); if (!Disposed && sequence == actionSequence && success is not null) Message = success; }
        catch (Exception error) { if (!Disposed && sequence == actionSequence) { Failed = true; Message = error is ReleaseFailure or InvalidDataException or ArgumentException ? error.Message : "This action did not finish. Your local work is still open. Try again or download the project."; } if (Store.DiagnosticLogging) Console.Error.WriteLine("Release action failed; type=" + error.GetType().Name); }
        finally { if (sequence == actionSequence) Busy = false; if (Store.DiagnosticLogging) Console.WriteLine("Release action ended; elapsedMs=" + timer.ElapsedMilliseconds); }
    }
    /// <summary>Saves a field edit without disabling the next deliberate action while browser storage acknowledges it.</summary>
    protected async Task Changed()
    {
        var sequence = actionSequence;
        Store.MarkChanged();
        try { await Store.Persist(); }
        catch (Exception error) { if (!Disposed && sequence == actionSequence) { Failed = true; Message = "This browser could not save the edit. Keep this page open and download the project."; } if (Store.DiagnosticLogging) Console.Error.WriteLine("Local save failed; type=" + error.GetType().Name); }
    }
    public virtual void Dispose() { Disposed = true; Store.Changed -= Refresh; }
}
