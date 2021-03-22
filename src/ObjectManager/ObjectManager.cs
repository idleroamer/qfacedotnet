using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tmds.DBus;

namespace facenet
{
    public struct InterfacesAddedArgs
    {
        public ObjectPath objectPath;
        public IDictionary<string, IDictionary<string, object>> interfacesAndProperties;
    }
    public struct InterfacesRemovedArgs
    {
        public ObjectPath objectPath;
        public string[] interfaces;
    }

    /*! TODO: Can be removed if NameOwnerChanged of Tmds is accessible */
    [DBusInterface("org.freedesktop.DBus")]
    public interface IFreedesktopDBus : IDBusObject
    {
        Task<IDisposable> WatchNameOwnerChangedAsync(Action<ServiceOwnerChangedEventArgs> handler);
    }

    [DBusInterface("org.freedesktop.DBus.ObjectManager")]
    public interface IObjectManager : IDBusObject
    {
        Task<IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>>> GetManagedObjectsAsync();
        Task<IDisposable> WatchInterfacesAddedAsync(Action<InterfacesAddedArgs> handler);
        Task<IDisposable> WatchInterfacesRemovedAsync(Action<InterfacesRemovedArgs> handler);
    }

    public class ObjectManager : IObjectManager
    {
        public static ObjectPath _Path = new ObjectPath("/");

        private static Dictionary<Connection, ObjectManager> objectManagers = new Dictionary<Connection, ObjectManager>();
        private IDictionary<string, IObjectManager> serviceProxies;
        private IDictionary<ObjectPath, string> objectServices;
        private IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>> managedObjects;
        private HashSet<string> _watchedServices;

        private Connection _conn;

        public static async Task<ObjectManager> Manager(Connection conn)
        {
            if (!objectManagers.ContainsKey(conn))
            {
                objectManagers.Add(conn, new ObjectManager(conn));
                await objectManagers[conn].Setup();
            }
            return objectManagers[conn];
        }
        private ObjectManager(Connection conn)
        {
            _conn = conn;

            serviceProxies = new Dictionary<string, IObjectManager>();
            objectServices = new Dictionary<ObjectPath, string>();
            managedObjects = new Dictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>>();
            _watchedServices = new HashSet<string>();
        }

        private async Task Setup()
        {
            var servicesPattern = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("facenet.service")) ?
            Environment.GetEnvironmentVariable("facenet.service") : "qface.service";
            var freedesktopDBusProxy = _conn.CreateProxy<IFreedesktopDBus>("org.freedesktop.DBus", "/org/freedesktop/DBus");
            var connectInfo = await _conn.ConnectAsync();
            await _conn.RegisterServiceAsync(servicesPattern + ".X" + Regex.Replace(connectInfo.LocalName, "[:|.]+", ""));
            await _conn.RegisterObjectAsync(this);

            foreach (var serviceName in await _conn.ListServicesAsync())
            {
                if (serviceName.StartsWith(servicesPattern))
                {
                    WatchService(serviceName);
                }
            }

            await freedesktopDBusProxy.WatchNameOwnerChangedAsync(args =>
            {
                if (args.ServiceName.StartsWith(servicesPattern))
                {
                    if (args.NewOwner != null)
                    {
                        WatchService(args.ServiceName);
                    }
                    else
                    {
                        _watchedServices.Remove(args.ServiceName);
                        foreach (var item in objectServices.Where(kvp => kvp.Value == args.ServiceName).ToList())
                        {
                            RemoveObject(item.Key, null);
                        }
                    }
                }
            });
        }

        public ObjectPath ObjectPath { get => _Path; }
        public Task<IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>>> GetManagedObjectsAsync()
        {
            return Task.FromResult(managedObjects);
        }

        public Task<IDisposable> WatchInterfacesAddedAsync(Action<InterfacesAddedArgs> handler)
        {
            return SignalWatcher.AddAsync(this, "InterfacesAdded", handler);
        }
        public Task<IDisposable> WatchInterfacesRemovedAsync(Action<InterfacesRemovedArgs> handler)
        {
            return SignalWatcher.AddAsync(this, "InterfacesRemoved", handler);
        }

        public void RegisterObject(ObjectPath objectPath, IDictionary<string, IDictionary<string, object>> interfacesAndProperties)
        {
            managedObjects[objectPath] = interfacesAndProperties;
            InterfacesAdded?.Invoke(new InterfacesAddedArgs { objectPath = objectPath, interfacesAndProperties = interfacesAndProperties });
        }

        public void UnregisterObject(ObjectPath objectPath, string[] interfaces)
        {
            managedObjects.Remove(objectPath);
            InterfacesRemoved?.Invoke(new InterfacesRemovedArgs { objectPath = objectPath, interfaces = interfaces });
        }

        public string ObjectService(ObjectPath objectPath)
        {
            string serviceName;
            return objectServices.TryGetValue(objectPath, out serviceName) ? serviceName : "";
        }

        private async void WatchService(string service)
        {
            if (!_watchedServices.Contains(service))
            {
                serviceProxies.Add(service, _conn.CreateProxy<IObjectManager>(service, _Path));

                await serviceProxies[service].WatchInterfacesAddedAsync(args =>
                {
                    AddObject(service, args.objectPath, args.interfacesAndProperties);
                });
                await serviceProxies[service].WatchInterfacesRemovedAsync(args =>
                {
                    RemoveObject(args.objectPath, args.interfaces);
                });
                try
                {
                    var objects = await serviceProxies[service].GetManagedObjectsAsync();
                    foreach (ObjectPath objectPath in objects.Keys)
                    {
                        AddObject(service, objectPath, objects[objectPath]);
                    }
                }
                catch
                {
                    // TODO how to log the error?
                }
                _watchedServices.Add(service);
            }
        }
        private void AddObject(string service, ObjectPath objectPath, IDictionary<string, IDictionary<string, object>> interfacesAndProperties)
        {
            if (!objectServices.ContainsKey(objectPath))
            {
                objectServices.Add(objectPath, service);
                InterfacesAdded?.Invoke(new InterfacesAddedArgs
                {
                    objectPath = objectPath,
                    interfacesAndProperties = interfacesAndProperties
                });
            }
            else if (objectServices[objectPath] != service)
            {
                // log error, object already registered by other service
            }
        }

        private void RemoveObject(ObjectPath objectPath, string[] interfaces)
        {
            if (objectServices.ContainsKey(objectPath))
            {
                objectServices.Remove(objectPath);
                InterfacesRemoved?.Invoke(new InterfacesRemovedArgs { objectPath = objectPath, interfaces = interfaces });
            }
        }

        public event Action<InterfacesAddedArgs> InterfacesAdded;
        public event Action<InterfacesRemovedArgs> InterfacesRemoved;
    }
}
