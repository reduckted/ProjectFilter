using System.Threading.Tasks;


namespace ProjectFilter.Services;


public interface ILogger {

    Task WriteLineAsync(string message);

}
