using Microsoft;
using System;
using System.Windows.Threading;


namespace ProjectFilter.UI.Utilities;


public sealed class Debouncer : IDebouncer {

    private readonly DispatcherTimer _timer;


    public static IDebouncer Create(TimeSpan delay) {
        return new Debouncer(delay);
    }


    public Debouncer(TimeSpan delay) {
        // Use a `DispatcherTimer` constructor without a callback
        // so that the timer doesn't start immediately. We only
        // want to start the timer when the search text changes.
        _timer = new DispatcherTimer(DispatcherPriority.Normal) {
            Interval = delay
        };

        _timer.Tick += OnTick;
    }


    public void Start() {
        _timer.Stop();
        _timer.Start();
    }


    public void Cancel() {
        _timer.Stop();
    }


    private void OnTick(object sender, EventArgs e) {
        _timer.Stop();
        Stable.Raise(this, EventArgs.Empty);
    }


    public event EventHandler? Stable;


    public void Dispose() {
        _timer.Stop();
        _timer.Tick -= OnTick;
    }

}
