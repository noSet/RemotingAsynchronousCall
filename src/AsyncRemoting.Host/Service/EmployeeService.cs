using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncRemoting.Service;

namespace AsyncRemoting.Host.Service
{
    public class EmployeeService : MarshalByRefObject, IEmployeeService
    {
        public string GetName(int userCode)
        {
            Thread.Sleep(3000);

            return "张伟";
        }

        public Task<string> GetNameAsync(int userCode)
        {
            throw new NotImplementedException();
        }
    }
}
