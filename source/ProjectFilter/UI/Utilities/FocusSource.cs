using System;


namespace ProjectFilter.UI.Utilities;


public class FocusSource {

    public void RequestFocus() {
        FocusRequested?.Invoke(this, EventArgs.Empty);
    }


    public event EventHandler? FocusRequested;

}
