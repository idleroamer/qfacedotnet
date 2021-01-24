from qface.generator import FileSystem, Generator
import logging.config
import argparse
from path import Path
import qface
import subprocess
import sys
import os

parser = argparse.ArgumentParser(description='Generate high-level IPC/RPC interfaces defined in qface based on Tmds.DBus')
parser.add_argument('--src', dest='src', type=str, required=False, default='.',
                    help='where all .qface definitions are located (possibly in sub-directories), default value is current directory')
parser.add_argument('--input', dest='input', type=str, required=True, nargs='+',
                    help='qface interface relative to src path')
parser.add_argument('--output', dest='output', type=str, required=False, default='.',
                    help='path to place the generated code relative to go module base path, default value is current directory')
args = parser.parse_args()


def facenet_type(self: object) -> object:
    if self.type.is_primitive or self.type.is_void:
        if self.type.name == 'real':
            return 'double'
        return self.type
    elif self.type.is_list:
        return 'Lis<{0}>'.format(facenet_type(self.type.nested))
    elif self.type.is_map:
        return 'Dictionary<string, {0}>'.format(facenet_type(self.type.nested))
    else:
        split = self.type.name.split(".")
        if len(split) > 1:
            return ''.join(split[:-1]) + '.' + split[-1]
        else:
            return split[0]


FileSystem.strict = True
Generator.strict = True


setattr(qface.idl.domain.TypeSymbol, 'facenet_type', property(facenet_type))
setattr(qface.idl.domain.Field, 'facenet_type', property(facenet_type))
setattr(qface.idl.domain.Operation, 'facenet_type', property(facenet_type))
setattr(qface.idl.domain.Property, 'facenet_type', property(facenet_type))
setattr(qface.idl.domain.Parameter, 'facenet_type', property(facenet_type))


here = Path(__file__).dirname()
inputs = []
for i in args.input:
    inputs.append(os.path.join(args.src, i))
system = FileSystem.parse(inputs)
modulesToGenerate = [module.name for module in system.modules]
system = FileSystem.parse(args.src)
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
            generator.write('{{path}}/I' + interface.name + '.cs', 'DBusInterface.cs.template', ctx)
