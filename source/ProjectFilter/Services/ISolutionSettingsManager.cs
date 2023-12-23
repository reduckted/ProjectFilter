using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ProjectFilter.Services;


[Guid("6d648d75-7fa7-383a-a8f1-b3157b5c3f1d")]
public interface ISolutionSettingsManager {

    Task<SolutionSettings?> GetSettingsAsync();


    void SetSettings(SolutionSettings? settings);


    void Load(Stream stream);


    void Save(Stream stream);

}
