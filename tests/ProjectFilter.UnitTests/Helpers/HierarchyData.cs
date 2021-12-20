using System;


namespace ProjectFilter.Helpers;


internal class HierarchyData {

    public HierarchyData(Guid identifier, string name, Guid? parent) {
        Identifier = identifier;
        Name = name;
        Parent = parent;
    }


    public Guid Identifier { get; }


    public string Name { get; }


    public Guid? Parent { get; }


    public HierarchyType Type { get; private set; }


    public Guid CLSID { get; private set; }


    public bool IsProject { get; private set; }


    public void SetType(HierarchyType type, Guid clsid, bool isProject) {
        Type = type;
        CLSID = clsid;
        IsProject = isProject;
    }

}
