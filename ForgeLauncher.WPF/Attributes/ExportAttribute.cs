using System;

namespace ForgeLauncher.WPF.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class ExportAttribute : Attribute
{
    public Type ContractType { get; }

    public ExportAttribute()
        : this(null!)
    {
    }

    public ExportAttribute(Type contractType)
    {
        ContractType = contractType;
    }
}
