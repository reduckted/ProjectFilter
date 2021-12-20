using Microsoft.VisualStudio.ComponentModelHost;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;


namespace ProjectFilter.Helpers;


internal class TestComponentModel : IComponentModel2 {

    private readonly List<(Type ServiceType, object Implementation)> _services = new();


    public CompositionScopeDefinition DefaultScopedCatalog => throw new NotSupportedException();


    public ComposablePartCatalog DefaultCatalog => throw new NotSupportedException();


    public ExportProvider DefaultExportProvider => throw new NotSupportedException();


    public ICompositionService DefaultCompositionService => throw new NotSupportedException();


    public ComposablePartCatalog GetCatalog(string catalogName) => throw new NotSupportedException();


    public IEnumerable<T> GetExtensions<T>() where T : class {
        return _services
            .Where((x) => x.ServiceType == typeof(T))
            .Select((x) => (T)x.Implementation)
            .ToList();
    }


    public T GetService<T>() where T : class {
        return _services
            .Where((x) => x.ServiceType == typeof(T))
            .Select((x) => (T)x.Implementation)
            .FirstOrDefault();
    }


    public void AddService<T>(T service) where T : class => _services.Add((typeof(T), service));

}
