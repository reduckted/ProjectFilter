using System;


namespace ProjectFilter.UI.Utilities;


public interface IDebouncer : IDisposable {

    void Start();


    void Cancel();


    public event EventHandler Stable;

}
