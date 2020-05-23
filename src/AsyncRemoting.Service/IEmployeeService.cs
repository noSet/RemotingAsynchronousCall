using System.Security;
using System.Threading.Tasks;

// 必须加这个特性，不然委托跟方法绑定的时候会抛出异常
[assembly: SecurityRules(SecurityRuleSet.Level1)]

namespace AsyncRemoting.Service
{
    public interface IEmployeeService
    {
        string GetName(int userCode);

        Task<string> GetNameAsync(int userCode);
    }
}
