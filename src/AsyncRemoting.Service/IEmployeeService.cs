using System.Threading.Tasks;

namespace AsyncRemoting.Service
{
    public interface IEmployeeService
    {
        string GetName(int userCode);

        Task<string> GetNameAsync(int userCode);
    }
}
