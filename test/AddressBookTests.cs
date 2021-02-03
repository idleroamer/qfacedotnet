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
                var conn2Info = await conn2.ConnectAsync();
                var addressBook = new AddressBook();
                await conn2.RegisterObjectAsync(addressBook);
                var conn2Name = conn2Info.LocalName;
                addressBook.isLoaded = true;
                var proxy = new AddressBookDBusProxy(conn1, conn2Name);
                Assert.Equal(proxy.isLoaded, true);
                var tcs = new TaskCompletionSource<PropertyChanges>();
                addressBook.OnPropertiesChanged += args => tcs.SetResult(args);
                proxy.isLoaded = false;
                var reply = await tcs.Task;
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
                var conn2Info = await conn2.ConnectAsync();
                var addressBook = new AddressBook();
                await conn2.RegisterObjectAsync(addressBook);
                var contact = new Contact{name = "FooName", number = "FooNumber"};
                addressBook.lastCreatedContact = new contactCreatedArgs{contact = contact};
                var conn2Name = conn2Info.LocalName;
                var proxy = new AddressBookDBusProxy(conn1, conn2Name);
                var tcsAdapter = new TaskCompletionSource<contactCreatedArgs>();
                var tcsProxy = new TaskCompletionSource<contactCreatedArgs>();
                addressBook.contactCreated += args => tcsAdapter.SetResult(args);
                proxy.contactCreated += args => tcsProxy.SetResult(args);
                proxy.createNewContactAsync();

                var eventAdapter = await tcsAdapter.Task;
                var eventProxy = await tcsAdapter.Task;
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
                var conn2Info = await conn2.ConnectAsync();
                var addressBook = new AddressBook();
                await conn2.RegisterObjectAsync(addressBook);
                var contact = new Contact();
                contact.name = "FooName";
                contact.number = "FooNumber";
                var contactCreatedArg = new contactCreatedArgs();
                contactCreatedArg.contact = contact;
                var conn2Name = conn2Info.LocalName;
                var proxy = new AddressBookDBusProxy(conn1, conn2Name);
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
