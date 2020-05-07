using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using AsyncRemoting.Host.Service;
using AsyncRemoting.Service;

namespace AsyncRemoting.Host
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // 使用Tcp信道，默认使用BinaryFormatter
            TcpChannel channel = new TcpChannel(8826);
            ChannelServices.RegisterChannel(channel, false);

            RemotingConfiguration.RegisterWellKnownServiceType(typeof(EmployeeService), nameof(IEmployeeService), WellKnownObjectMode.Singleton);

            EmployeeService employeeService = new EmployeeService();
            RemotingServices.SetObjectUriForMarshal(employeeService, nameof(IEmployeeService));
            ObjRef employeeServiceObjRef = RemotingServices.Marshal(employeeService, nameof(IEmployeeService));

            // 这里没明白为什么URI是个乱码
            Console.WriteLine($@"{nameof(IEmployeeService)}.URI: ""{employeeServiceObjRef.URI}""");
            Console.WriteLine("started ...");
            Console.WriteLine("Press Enter to end publication.");
            Console.ReadKey();

            Console.WriteLine("stop");
            RemotingServices.Disconnect(employeeService);
        }
    }
}
