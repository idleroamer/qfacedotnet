using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Tests.AddressBook
{
    public class AddressBookImpl : IAddressBook
    {
        public Task mockCreateNewContactAsync(contactCreatedArgs contactCreatedArg)
        {
            OncontactCreated(contactCreatedArg);
            return Task.CompletedTask;
        }

        public contactCreatedArgs lastCreatedContact;
        public override Task createNewContactAsync()
        {
            return Task.Run( () => {
                OncontactCreated(lastCreatedContact);
                return Task.CompletedTask;
                                   } );
        }
        public override Task selectContactAsync(int contactId)
        {
            return Task.Run( () => { 
                    if (contactId < 0) {
                        throw new DBusException("DBus.Error.InvalidValue", "Invalid index");
                    }
                                   } );
        }
        public override Task<bool> deleteContactAsync(int contactId)
        {
            return Task.FromResult(true);
        }
        public override Task updateContactAsync(int contactId, Contact contact)
        {
            return Task.Run( () => { 

                                   } );
        }

        public Task<bool> blacklistContactAsync(Nested blackList)
        {
            return Task.FromResult(true);
        }
        public override void SetIntValues(IList<int> value)
        {
            if (!value.Contains(-1))
            {
                base.SetIntValues(value);
            }
            else {
                 throw new DBusException("DBus.Error.InvalidInput", "Invalid input");
            }
        }
    }
}