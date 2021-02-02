using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Examples.AddressBook
{
    public class AddressBook : AddressBookDBusAdapter
    {
        public override Task createNewContactAsync()
        {
            return Task.Run(() =>
            {
                Console.WriteLine("create new contact called!");
                return Task.CompletedTask;
            });
        }
        public override Task selectContactAsync(int contactId)
        {
            return Task.Run(() =>
            {
                Console.WriteLine("select contact called! contactId" + contactId);
                return Task.CompletedTask;
            });
        }
        public override Task<bool> deleteContactAsync(int contactId)
        {
            return Task.Run(() =>
            {
                Console.WriteLine("delete contact called! contactId" + contactId);
                return Task.FromResult(true);
            });
        }
        public override Task updateContactAsync(int contactId, Contact contact)
        {
            return Task.Run(() =>
            {
                Console.WriteLine("select contact called! contactId" + contactId + "contact" + contact);
                return Task.CompletedTask;
            });
        }
    }
}