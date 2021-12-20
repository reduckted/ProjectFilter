using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;


namespace ProjectFilter.Services;


public interface IWaitDialogFactory {

    Task<IWaitDialog> CreateAsync(string title, ThreadedWaitDialogProgressData progress);

}
