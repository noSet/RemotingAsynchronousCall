using System;
using System.Collections;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading.Tasks;
using AsyncRemoting.Client.ClientChannelSinkProvider;
using AsyncRemoting.Service;

namespace AsyncRemoting.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var hashtable = new Hashtable
            {
                ["port"] = 0
            };

            IClientChannelSinkProvider provider = new BinaryClientFormatterSinkProvider();
            provider.Next = new AsyncClientChannelSinkProvider();

            TcpChannel channel = new TcpChannel(hashtable, provider, new BinaryServerFormatterSinkProvider());
            ChannelServices.RegisterChannel(channel, false);

            APM();

            await TPL();

            Console.ReadKey();
        }

        private static void APM()
        {
            IEmployeeService employeeService = CreateProxy<IEmployeeService>();

            Func<int, string> @delegate = new Func<int, string>(employeeService.GetName);

            IAsyncResult ar = @delegate.BeginInvoke(0, default, default);

            while (!ar.IsCompleted)
            {
            }

            Console.WriteLine(@delegate.EndInvoke(ar));
        }

        private static async Task TPL()
        {
            IEmployeeService employeeService = CreateAsyncProxy<IEmployeeService>();

            Task<string> name = employeeService.GetNameAsync(1);

            Console.WriteLine(await name);
        }

        private static T CreateProxy<T>(string url = default)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                url = "tcp://localhost:8826/" + typeof(T).Name;
            }

            return (T)Activator.GetObject(typeof(T), url);
        }

        private static T CreateAsyncProxy<T>(string url = default)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                url = "tcp://localhost:8826/" + typeof(T).Name;
            }

            var employeeService = (T)Activator.GetObject(typeof(T), url);

            return (T)new AsyncRealProxy<T>(employeeService).GetTransparentProxy();
        }

        //private static object CreateAsyncProxy<T>(string url = default)
        //{
        //    var employeeService = (T)Activator.GetObject(typeof(T), url);
        //    return (T)new AsyncRealProxy<T>(employeeService).GetTransparentProxy();
        //}
    }
}
