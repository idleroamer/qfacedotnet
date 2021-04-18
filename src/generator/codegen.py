from qface.generator import FileSystem, Generator
import logging.config
import argparse
from path import Path
import qface
import subprocess
import sys
import os

parser = argparse.ArgumentParser(description='Generates bindings for Tmds based on the qface IDL.')
parser.add_argument('--input', dest='input', type=str, required=True, nargs='+',
                    help='input qface interfaces, folders will be globbed looking for qface interfaces')
parser.add_argument('--output', dest='output', type=str, required=False, default='.',
                    help='relative output path of the generated code, default value is current directory')
parser.add_argument('--dependency', dest='dependency', type=str, required=False, nargs='+', default=[],
                    help='path to dependency qface interfaces, leave empty if there is no interdependency')
args = parser.parse_args()


def qfacedotnet_type(self: object) -> object:
    if self.type.is_primitive or self.type.is_void:
        if self.type.name == 'real':
            return 'double'
        return self.type
    elif self.type.is_list:
        return 'IList<{0}>'.format(qfacedotnet_type(self.type.nested))
    elif self.type.is_map:
        return 'IDictionary<string, {0}>'.format(qfacedotnet_type(self.type.nested))
    else:
        return self.type.name

def qfacedotnet_concrete_type(self: object) -> object:
    if self.type.is_list:
        return 'List<{0}>'.format(qfacedotnet_type(self.type.nested))
    elif self.type.is_map:
        return 'Dictionary<string, {0}>'.format(qfacedotnet_type(self.type.nested))
    else:
        return ""

def has_return_value(self):
    return not self.type.name == 'void'


def cap_name(self):
    return ' '.join(word[0].upper() + word[1:] for word in self.name.split())

FileSystem.strict = True
Generator.strict = True


setattr(qface.idl.domain.TypeSymbol, 'qfacedotnet_type', property(qfacedotnet_type))
setattr(qface.idl.domain.Field, 'qfacedotnet_type', property(qfacedotnet_type))
setattr(qface.idl.domain.Operation, 'qfacedotnet_type', property(qfacedotnet_type))
setattr(qface.idl.domain.Property, 'qfacedotnet_type', property(qfacedotnet_type))
setattr(qface.idl.domain.Parameter, 'qfacedotnet_type', property(qfacedotnet_type))


setattr(qface.idl.domain.Property, 'cap_name', property(cap_name))
setattr(qface.idl.domain.Property, 'qfacedotnet_concrete_type', property(qfacedotnet_concrete_type))

setattr(qface.idl.domain.Operation, 'has_return_value', property(has_return_value))

here = Path(__file__).dirname()
system = FileSystem.parse(args.input)
modulesToGenerate = [module.name for module in system.modules]
system = FileSystem.parse(args.input + args.dependency)
output = args.output
generator = Generator(search_path=Path(here / 'templates'))
generator.destination = output
ctx = {'output': output}

for module in system.modules:
    if module.name in modulesToGenerate:
        for interface in module.interfaces:
            ctx.update({'module': module})
            ctx.update({'interface': interface})
            module_path = '/'.join(module.name_parts)
            ctx.update({'path': module_path})
            generator.write('{{path}}/I' + interface.name + '.cs', 'InterfaceBase.cs.template', ctx)
            generator.write('{{path}}/I' + interface.name + 'DBus.cs', 'DBusInterface.cs.template', ctx)
            generator.write('{{path}}/' + interface.name + 'DBusAdapter.cs', 'DBusAdapter.cs.template', ctx)
            generator.write('{{path}}/' + interface.name + 'DBusProxy.cs', 'DBusProxy.cs.template', ctx)
        for struct in module.structs:
            ctx.update({'module': module})
            ctx.update({'struct': struct})
            module_path = '/'.join(module.name_parts)
            ctx.update({'path': module_path})
            generator.write('{{path}}/' + struct.name + '.cs', 'Struct.cs.template', ctx)
        for enum in module.enums:
            ctx.update({'module': module})
            ctx.update({'enum': enum})
            module_path = '/'.join(module.name_parts)
            ctx.update({'path': module_path})
            generator.write('{{path}}/' + enum.name + '.cs', 'Enum.cs.template', ctx)
