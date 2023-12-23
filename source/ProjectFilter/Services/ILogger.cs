using System.Runtime.InteropServices;
using System.Threading.Tasks;


namespace ProjectFilter.Services;


[Guid("5dd5fdcb-f218-3b66-911b-afd03efd8c08")]
public interface ILogger {

    Task WriteLineAsync(string message);

}
