using qfacedotnet.Tests;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;
using Xunit;

namespace  Tests.Dependency.Phone
{
    public class PhoneTests
    {
        [Fact]
        public async Task AdapterEventConnection()
        {
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync();
                var address = dbusDaemon.Address;
                var conn1 = new Connection(address);
                var conn2 = new Connection(address);
                var phoneImpl = new PhoneImpl();
                var phoneDBusAdapter = new PhoneDBusAdapter(phoneImpl);

                await phoneDBusAdapter.RegisterObject(conn2);

                var phoneDBusProxy = new PhoneDBusProxy(conn1);
                await phoneDBusProxy.CreateProxy();

                var contact = new Tests.AddressBook.Contact { name = "FooName", number = "FooNumber" };
                var proxyReady = new TaskCompletionSource<bool>();
                phoneDBusProxy.readyChanged += args => proxyReady.SetResult(args);

                await phoneDBusProxy.CreateProxy();
                await proxyReady.Task;

                var contactCalled = new TaskCompletionSource<Tests.AddressBook.Contact>();
                phoneImpl.contactCalled += args => contactCalled.SetResult(args);
                await phoneDBusProxy.callContactAsync(contact);

                await contactCalled.Task;
            }
        }
    }
}
