using ForgeLauncher.WPF.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ForgeLauncher.WPF.Extensions
{
    public static class ServiceCollectionExtentions
    {
        public static void AddExportedTypesFromAssembly(this ServiceCollection serviceCollection, Assembly? assembly)
        {
            if (assembly == null)
                return;
            foreach (var type in assembly.GetTypes() ?? Enumerable.Empty<Type>())
            {
                var exportAttributes = type.GetCustomAttributes<ExportAttribute>();
                if (exportAttributes != null)
                {
                    var isSingleton = type.GetCustomAttribute<SharedAttribute>() != null;
                    foreach (var exportAttribute in exportAttributes)
                    {
                        var contractType = exportAttribute.ContractType ?? type;
                        if (isSingleton)
                            serviceCollection.AddSingleton(contractType, type);
                        else
                            serviceCollection.AddTransient(contractType, type);
                    }
                }
            }
        }
    }
}
