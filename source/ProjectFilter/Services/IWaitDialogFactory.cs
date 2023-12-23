using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System.Threading.Tasks;


namespace ProjectFilter.Services;


[Guid("f4ce8cca-9886-3976-b64c-b1699fb7cea2")]
public interface IWaitDialogFactory {

    Task<IWaitDialog> CreateAsync(string title, ThreadedWaitDialogProgressData progress);

}
