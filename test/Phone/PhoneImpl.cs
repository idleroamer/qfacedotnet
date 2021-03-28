using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Tests.Dependency.Phone
{
    public class PhoneImpl : IPhone
    {
        public override Task callContactAsync(Tests.AddressBook.Contact contact)
        {
            contactCalled?.Invoke(contact);
            return Task.CompletedTask;
        } 

        public event Action<Tests.AddressBook.Contact> contactCalled;
    }
}