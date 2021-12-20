using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using System;
using Task = System.Threading.Tasks.Task;


namespace ProjectFilter.Services;


public class Logger : ILogger {

    private OutputWindowPane? _pane;


    public async Task WriteLineAsync(string message) {
        try {
            if (_pane is null) {
                _pane = await OutputWindowPane.CreateAsync(Vsix.Name);
            }

            await _pane.WriteLineAsync($"{DateTime.Now:HH:mm:ss} - {message}");

        } catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex)) {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

}
