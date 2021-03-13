using facenet.Tests;
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
        public async Task Properties()
        {
            using (var dbusDaemon = new DBusDaemon())
            {
                await dbusDaemon.StartAsync();
                var address = dbusDaemon.Address;
                var conn1 = new Connection(address);
                await conn1.ConnectAsync();

                var conn2 = new Connection(address);
                await conn2.ConnectAsync();
                var addressBook = new AddressBook();
                await addressBook.RegisterObject(conn2);
		        addressBook.isLoaded = true;
                var proxy = new AddressBookDBusProxy(conn1);
                var proxyReady = new TaskCompletionSource<PropertyChanges>();
                proxy.OnPropertiesChanged += args => proxyReady.SetResult(args);
                await proxy.Connect(null);
                await proxyReady.Task;
                Assert.Equal(proxy.isLoaded, true);

                // check if setproperty from proxy side works
                var tcsProxy2 = new TaskCompletionSource<PropertyChanges>();
                addressBook.OnPropertiesChanged += args => tcsProxy2.SetResult(args);
                proxy.isLoaded = false;
                await tcsProxy2.Task;
                Assert.Equal(addressBook.isLoaded, false);
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
                await conn1.ConnectAsync();

                var conn2 = new Connection(address);
                await conn2.ConnectAsync();
                var addressBook = new AddressBook();
                await addressBook.RegisterObject(conn2);
                var contact = new Contact { name = "FooName", number = "FooNumber" };
                addressBook.lastCreatedContact = new contactCreatedArgs { contact = contact };
                
                var proxy = new AddressBookDBusProxy(conn1);
                var proxyReady = new TaskCompletionSource<PropertyChanges>();
                proxy.OnPropertiesChanged += args => proxyReady.SetResult(args);

                await proxy.Connect(null);
                await proxyReady.Task;
                Assert.Equal(proxy.ready, true);

                var tcsAdapter = new TaskCompletionSource<contactCreatedArgs>();
                addressBook.contactCreated += args => tcsAdapter.SetResult(args);
                
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
                await conn1.ConnectAsync();

                var conn2 = new Connection(address);
                await conn2.ConnectAsync();
                var addressBook = new AddressBook();
                await addressBook.RegisterObject(conn2);
                var contact = new Contact();
                contact.name = "FooName";
                contact.number = "FooNumber";
                var contactCreatedArg = new contactCreatedArgs();
                contactCreatedArg.contact = contact;
                var proxy = new AddressBookDBusProxy(conn1);
                var proxyReady = new TaskCompletionSource<PropertyChanges>();
                proxy.OnPropertiesChanged += args => proxyReady.SetResult(args);

                await proxy.Connect(null);
                await proxyReady.Task;
                Assert.Equal(proxy.ready, true);
                
                var tcs = new TaskCompletionSource<contactCreatedArgs>();
                proxy.contactCreated += args => tcs.SetResult(args);
                await addressBook.mockCreateNewContactAsync(contactCreatedArg);

                var reply = await tcs.Task;
                Assert.Equal(contactCreatedArg.contact.name, reply.contact.name);
                Assert.Equal(contactCreatedArg.contact.number, reply.contact.number);
            }
        }

    }
}
