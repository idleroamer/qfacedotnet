using qfacedotnet.Tests;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;
using Xunit;

namespace Tests.AddressBook
{
    public class AddressBookTests
    {
        [Fact]
        public async Task AdapterEventConnection()
        {
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync();
                var address = dbusDaemon.Address;
                var conn1 = new Connection(address);
                var addressBookImpl = new AddressBookImpl();
                var addressBookAdapter = new AddressBookDBusAdapter(addressBookImpl);

                var contactCreatedTask = new TaskCompletionSource<contactCreatedArgs>();
                addressBookAdapter.contactCreated += args => contactCreatedTask.SetResult(args);
                await addressBookImpl.createNewContactAsync();
                await contactCreatedTask.Task;
            }
        }

        [Fact]
        public async Task Properties()
        {
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync();
                var address = dbusDaemon.Address;
                var conn1 = new Connection(address);

                var conn2 = new Connection(address);
                var addressBookImpl = new AddressBookImpl();
                var addressBookAdapter = new AddressBookDBusAdapter(addressBookImpl);
                await addressBookAdapter.RegisterObject(conn2);
                addressBookImpl.isLoaded = true;
                var proxy = new AddressBookDBusProxy(conn1);
                var proxyReady = new TaskCompletionSource<bool>();
                proxy.readyChanged += args => proxyReady.SetResult(args);
                await proxy.CreateProxy();
                await proxyReady.Task;
                Assert.Equal(proxy.isLoaded, true);

                // check if setproperty from proxy side works
                var tcsProxy2 = new TaskCompletionSource<bool>();
                addressBookImpl.isLoadedChanged += args => tcsProxy2.SetResult(args);
                proxy.isLoaded = false;
                await tcsProxy2.Task;
                Assert.Equal(addressBookImpl.isLoaded, false);
            }
        }

        [Fact]
        public async Task Methods()
        {
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync();
                var address = dbusDaemon.Address;
                var conn1 = new Connection(address);

                var conn2 = new Connection(address);
                var addressBookImpl = new AddressBookImpl();
                var addressBookAdapter = new AddressBookDBusAdapter(addressBookImpl);
                await addressBookAdapter.RegisterObject(conn2);
                var contact = new Contact { name = "FooName", number = "FooNumber" };
                addressBookImpl.lastCreatedContact = new contactCreatedArgs { contact = contact };

                var proxy = new AddressBookDBusProxy(conn1);
                var proxyReady = new TaskCompletionSource<bool>();
                proxy.readyChanged += args => proxyReady.SetResult(args);

                await proxy.CreateProxy();
                await proxyReady.Task;
                Assert.Equal(proxy.ready, true);

                var tcsAdapter = new TaskCompletionSource<contactCreatedArgs>();
                addressBookImpl.contactCreated += args => tcsAdapter.SetResult(args);

                var tcsProxy2 = new TaskCompletionSource<contactCreatedArgs>();
                proxy.contactCreated += args => tcsProxy2.SetResult(args);
                proxy.createNewContactAsync();

                var eventAdapter = await tcsAdapter.Task;
                var eventProxy = await tcsProxy2.Task;
                Assert.Equal(eventAdapter.contact.name, eventProxy.contact.name);
                Assert.Equal(eventAdapter.contact.number, eventProxy.contact.number);
            }
        }
        [Fact]
        public async Task Signals()
        {
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync();
                var address = dbusDaemon.Address;
                var conn1 = new Connection(address);

                var conn2 = new Connection(address);
                var addressBookImpl = new AddressBookImpl();
                var addressBookAdapter = new AddressBookDBusAdapter(addressBookImpl);
                await addressBookAdapter.RegisterObject(conn2);
                var contact = new Contact();
                contact.name = "FooName";
                contact.number = "FooNumber";
                var contactCreatedArg = new contactCreatedArgs();
                contactCreatedArg.contact = contact;
                var proxy = new AddressBookDBusProxy(conn1);
                var proxyReady = new TaskCompletionSource<bool>();
                proxy.readyChanged += args => proxyReady.SetResult(args);

                await proxy.CreateProxy();
                await proxyReady.Task;
                Assert.Equal(proxy.ready, true);

                var tcs = new TaskCompletionSource<contactCreatedArgs>();
                proxy.contactCreated += args => tcs.SetResult(args);
                await addressBookImpl.mockCreateNewContactAsync(contactCreatedArg);

                var reply = await tcs.Task;
                Assert.Equal(contactCreatedArg.contact.name, reply.contact.name);
                Assert.Equal(contactCreatedArg.contact.number, reply.contact.number);
            }
        }

        [Fact]
        public async Task AdapterRemoved()
        {
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync();
                var address = dbusDaemon.Address;
                var conn1 = new Connection(address);
                var addressBookImpl = new AddressBookImpl();
                var addressBookAdapter = new AddressBookDBusAdapter(addressBookImpl);
                var conn2 = new Connection(address);
                await addressBookAdapter.RegisterObject(conn2);

                var proxy = new AddressBookDBusProxy(conn1);
                var proxyReady = new TaskCompletionSource<bool>();
                proxy.readyChanged += args => proxyReady.TrySetResult(args);
                await proxy.CreateProxy();
                await proxyReady.Task;
                Assert.Equal(proxy.ready, true);

                var proxyReady2 = new TaskCompletionSource<bool>();
                proxy.readyChanged += args => proxyReady2.SetResult(args);
                await addressBookAdapter.UnregisterObject(conn2);
                
                await proxyReady2.Task;
                Assert.Equal(proxy.ready, false);
            }
        }
    }
}