using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Examples.AddressBook
{
    internal class Program
    {
        private static void Main()
        {
            Console.WriteLine("Hello qfacedotnet");
            var connection = new Connection(Address.Session);

            Task.Run(async () =>
            {
                var addressBookImpl = new AddressBookImpl();
                var addressBookAdapter = new AddressBookDBusAdapter(addressBookImpl);
                await addressBookAdapter.RegisterObject(connection);
                
                Console.WriteLine("Press CTRL+C to quit");
                await Task.Delay(-1);
                
            }).Wait();
        }
    }
}
