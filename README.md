# qfacedotnet

A minimal [dbus](https://dbus.freedesktop.org/doc/dbus-tutorial.html#whatis) framework which generates bindings for [Tmds.DBus](https://github.com/tmds/Tmds.DBus) based on interface-definition-language [qface](https://doc.qt.io/QtIVI/idl-syntax.html).

## Architecture

Based on qface interface definitions `qfacedotnet` generates glue bindings for [Tmds.DBus](https://github.com/tmds/Tmds.DBus), effectively leaving only concrete implementation of `methods` open.

There are four main components in `qfacedotnet` architecture. 
* `Interface` declares `methods` and contains `properties` and `events`(signals) defined in qface files 
* `InterfaceDBus` adapts the `Interface` to the one expected by `Tmds.DBus`
* `DBusAdapter` exports `methods`, `properties` and `signals` to dbus from `service process`
* `DBusProxy` represents the `DBusAdapter` on the `client process`

![Class hierarchy](http://www.plantuml.com/plantuml/proxy?cache=no&src=https://raw.github.com/idleroamer/qfacedotnet/master/assets/diagrams/class-hierarchy.puml)

## Initialization

`DBusAdapter` exposes an object on bus via `RegisterObject` from given connection. `DBusProxy` attempts to connect to a remote object via `ConnectProxy` and tries to fetch all `properties`, given the bus name of the `service` is known (should be achieved automatically by [Object Management](#Object-Management)).
Afterward the status of the connection to the service can be checked by the conventional [ready property](#ready-property)]. On successful connection the `DBusProxy` is able to call `DBusAdapter` methods besides it will watch the signals and invoke corresponding events.

![Initial Sequence](http://www.plantuml.com/plantuml/proxy?cache=no&src=https://raw.github.com/idleroamer/qfacedotnet/master/assets/diagrams/initial-adapter-proxy-sequence.puml)

### Ready Property

`ready` is a conventional auxiliary property to be checked to ensure that the connection to remote-object was successful and the remote-object `DBusAdapter` is actually ready to handle method calls.

## Properties

Properties are available as defined in qface interface both in `Interface` and `DBusProxy`.
`DBusProxy` fetches all `properties` (given a successful connection) on `CreateProxy` call. Properties are always in sync between `DBusProxy` and `DBusAdapter` by the mean of `PropertiesChanged` signal.

Given a property is not defined `readonly` in qface, its value might be changed by `DBusProxy`. See [Properties Checks](#Properties-Checks) on how to optionally verify the assigned value on `DBusAdapter`. 

![Property get set](http://www.plantuml.com/plantuml/proxy?cache=no&src=https://raw.github.com/idleroamer/qfacedotnet/master/assets/diagrams/property-get-set-sequence.puml)

## Methods

Remote method calls are initiated by `DBusProxy` and invoke the corresponding `DBusAdapter` function. Beside normal code path [exceptions](#Exceptions) can be handled as well.

## Signals

Signals defined in qface interface may be invoked from `DBusAdapter` by calling the corresponding function. In turn signals are received by the `DBusProxy` side and registered [Observers](#Observers) are informed.

### Exceptions

`methods` could throw exception of type `DBusException`s on unexpected states. `DBusException`s are caught by `Tmds.DBus` and are transfered into error replies over dbus. 

Equivalently on `client process` those error replies will be caught and `Tmds.DBus` will throw a `DBusException` with same error name and message.

**_NOTE:_** It is important to make sure `ErrorName` is in right format. `*.*`

```
throw new DBusException("DBus.Error.InvalidValue", "Error Message")
```

## Generation

A python script is the code-generator for `qfacedotnet`. It is possible to integrate the code-generation in your csproj configuration.

```
<Target Name="Generate" BeforeTargets="BeforeBuild;BeforeRebuild">
<Message Text="Generate files..." Importance="High" />
<Exec Command="python3 $(Pkgqfacedotnet)/content/generator/codegen.py --input <LIST_OF_INPUTS> --dependency <LIST_OF_DEPENDENCIES>  Outputs="<OUTPUT_PATH>/*.cs">
  <Output ItemName="Generated" TaskParameter="Outputs" />
</Exec>
<ItemGroup>
  <Compile Include="@(Generated)" />
  <FileWrites Include="@(Generated)" />
</ItemGroup>
</Target>
<ItemGroup>
<PackageReference Include="qfacedotnet" Version="<VERSION>" GeneratePathProperty="true" />
</ItemGroup>
```

`--input` list of all qface input files to generate bindings for.

`--dependency` argument is important for qfacedotnet to locate the [module interdependencies](#Module-Interdependency) otherwise current directory (where go generator file located) is taken. 

`--output` optional output path of generated files otherwise module name will be used as path.

### Dependencies

The qfacedotnet python dependency are defined in `requirement.txt` file. These dependencies needs to be installed once but nevertheless you can integrate this step as well into project configuration.

```
<Target Name="Generate" BeforeTargets="BeforeBuild;BeforeRebuild">
<Exec Command="pip3 install -r $(Pkgqfacedotnet)/content/generator/requirements.txt" />
<ItemGroup>
<PackageReference Include="qfacedotnet" Version="<VERSION>" GeneratePathProperty="true" />
</ItemGroup>
```

## Module Interdependency

Modules may import other modules via `import` keyword followed by the imported module name and version.

## Object Management

Object management in qfacedotnet follows the dbus specification of [org.freedesktop.DBus.ObjectManager](https://dbus.freedesktop.org/doc/dbus-specification.html#standard-interfaces-objectmanager).
The root object `/` implements the `ObjectManager` interface which can be used to query list of objects in this service.

Besides `Object Manager` monitors all related objects on bus in order to figure out to which service a `DBusProxy` needs to connect to. See also [related services](#Related-Services).
As long as prerequisite are in place this is a seamless operation.

Prerequisite
* `DBusAdapter` and `DBusProxy` have the same interface name and object path (see [dbus-definition](https://dbus.freedesktop.org/doc/dbus-faq.html#idm39))
* Bus name of `DBusAdapter` is in expected format (see also [related services](#Related-Services))
* Service of `DBusAdpater` has a valid object manager under the root object `/` 

### Related Services
A predefined name pattern of bus name makes detection of related services possible. So that all related services and their objects life-cycle can be monitored. 

**_NOTE:_**  The "qface.registry." pattern is used in case "DBUS_SERVICE_NAME_PATTERN" environment variable not defined

### Life time of objects

One may end the a service lifetime on dbus by unregistering the `DBusAdapter`.

```
server, err := dbus.SessionBus()
qfacedotnet.ObjectManager(server).UnregisterObject(DBusAdapter.ObjectPath(), nil)
```