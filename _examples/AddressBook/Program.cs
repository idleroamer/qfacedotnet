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
            Console.WriteLine("Hello facenet");
            var connection = new Connection(Address.Session);

            Task.Run(async () =>
            {
                await connection.ConnectAsync();
                await connection.RegisterServiceAsync("facenet.examples.addressbook");
                var addressBook = new AddressBook();
                var addressBookAdapter = new AddressBookDBusAdapter(addressBook);
                await addressBookAdapter.RegisterObject(connection);
                
                Console.WriteLine("Press CTRL+C to quit");
                await Task.Delay(-1);
                
            }).Wait();
        }
    }
}
