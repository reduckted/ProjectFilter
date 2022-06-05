using System;
using System.Collections.Generic;
using System.Linq;


namespace ProjectFilter.Helpers;


internal class HierarchyData {

    private Dictionary<string, HierarchyData>? _lookup;


    public HierarchyData(Guid identifier, string name, Guid? parent, bool isShared) {
        Identifier = identifier;
        Name = name;
        Parent = parent;
        IsShared = isShared;
        DependencyNames = Enumerable.Empty<string>();
    }


    public Guid Identifier { get; }


    public string Name { get; }


    public Guid? Parent { get; }


    public HierarchyType Type { get; private set; }


    public Guid CLSID { get; private set; }


    public bool IsProject { get; private set; }


    public bool IsShared { get; }


    public IEnumerable<string> DependencyNames { get; private set; }


    public IEnumerable<HierarchyData> GetDependencyData() {
        if (_lookup is not null) {
            return DependencyNames.Select((x) => _lookup[x]).ToList();
        }

        return Enumerable.Empty<HierarchyData>();
    }


    public void SetType(HierarchyType type, Guid clsid, bool isProject) {
        Type = type;
        CLSID = clsid;
        IsProject = isProject;
    }


    public void SetDependencies(IEnumerable<string> dependencies, Dictionary<string, HierarchyData> lookup) {
        DependencyNames = dependencies.ToList();
        _lookup = lookup;
    }

}
