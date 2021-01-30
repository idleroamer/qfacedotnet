using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;
using Xunit;

namespace Tests.AddressBook
{
    public class AddressBookTests {
	[Fact]
        public async Task Signals()
        {
                var conn1 = new Connection(Address.Session);
                await conn1.ConnectAsync();

                var conn2 = new Connection(Address.Session);
                var conn2Info = await conn2.ConnectAsync();
                var addressBook = new AddressBook();
                await conn2.RegisterObjectAsync(addressBook);
                var contact = new Contact();
                contact.name = "FooName";
                contact.number = "FooNumber";
                var contactCreatedArg = new contactCreatedArgs();
                contactCreatedArg.contact = contact;
                var conn2Name = conn2Info.LocalName;
                var proxy = conn1.CreateProxy<IAddressBook>(conn2Name, new ObjectPath("/Tests/AddressBook/AddressBook"));
                var tcs = new TaskCompletionSource<contactCreatedArgs>();
                await proxy.WatchcontactCreatedAsync(message => tcs.SetResult(message));
                await addressBook.mockCreateNewContactAsync(contactCreatedArg);

                var reply = await tcs.Task;
                Assert.Equal(contactCreatedArg.contact.name, reply.contact.name);
                Assert.Equal(contactCreatedArg.contact.number, reply.contact.number);

                conn1.Dispose();
                conn2.Dispose();
        }
    }
}