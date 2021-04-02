using qfacedotnet.Tests;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
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
                Assert.True(proxy.isLoaded);

                // check if setproperty from proxy side works
                var tcsProxy2 = new TaskCompletionSource<bool>();
                addressBookImpl.isLoadedChanged += args => tcsProxy2.SetResult(args);
                await proxy.setIsLoaded(false);
                await tcsProxy2.Task;
                Assert.False(addressBookImpl.isLoaded);
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
                Assert.True(proxy.ready);

                var tcsAdapter = new TaskCompletionSource<contactCreatedArgs>();
                addressBookImpl.contactCreated += args => tcsAdapter.SetResult(args);

                var tcsProxy2 = new TaskCompletionSource<contactCreatedArgs>();
                proxy.contactCreated += args => tcsProxy2.SetResult(args);
                proxy.createNewContactAsync();

                var eventAdapter = await tcsAdapter.Task;
                var eventProxy = await tcsProxy2.Task;
                Assert.True(eventAdapter.contact.name.Equals(eventProxy.contact.name));
                Assert.True(eventAdapter.contact.number.Equals(eventProxy.contact.number));
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
                Assert.True(proxy.ready);

                var tcs = new TaskCompletionSource<contactCreatedArgs>();
                proxy.contactCreated += args => tcs.SetResult(args);
                await addressBookImpl.mockCreateNewContactAsync(contactCreatedArg);

                var reply = await tcs.Task;
                Assert.True(contactCreatedArg.contact.name.Equals(reply.contact.name));
                Assert.True(contactCreatedArg.contact.number.Equals(reply.contact.number));
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
                Assert.True(proxy.ready);

                var proxyReady2 = new TaskCompletionSource<bool>();
                proxy.readyChanged += args => proxyReady2.SetResult(args);
                await addressBookAdapter.UnregisterObject(conn2);
                
                await proxyReady2.Task;
                Assert.False(proxy.ready);
            }
        }
        [Fact]
        public async Task Exceptions()
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
                Assert.True(proxy.ready);

                var methodException = await Assert.ThrowsAsync<DBusException>(() => proxy.selectContactAsync(-1));
                Assert.Equal("DBus.Error.InvalidValue", methodException.ErrorName);
                Assert.Equal("Invalid index", methodException.ErrorMessage);
              
                var propException = await Assert.ThrowsAsync<DBusException>(() => proxy.setIntValues(new List<int>{-1}));
                Assert.Equal("DBus.Error.InvalidInput", propException.ErrorName);
                Assert.Equal("Invalid input", propException.ErrorMessage);
            }
        }
    }
}
