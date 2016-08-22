// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;


namespace Microsoft.AspNetCore.Razor.Design.Internal
{
    public class AssemblyDescriptorFactoryResolver
    {
        public bool LoadedAssembly { get; set; }
        public bool HasAssembly { get; set; }
        public Type DescriptorFactoryClass { get; set; }
        public MethodInfo CreateDescriptorsMethod { get; set; }
        public MethodInfo CreateDescriptorProviderMethod { get; set; }

        public void LoadAssembly()
        {
            LoadedAssembly = true;

            var assemblyName = "Microsoft.AspNetCore.Mvc.Razor";
            var namespaceName = "ViewComponentTagHelpers";
            var className = "ViewComponentTagHelperDescriptorFactory";

            try
            {
                var assembly = Assembly.Load(new AssemblyName("Microsoft.AspNetCore.Mvc.Razor"));

                // Get the method and class we need from the assembly.
                DescriptorFactoryClass = assembly.GetType($"{assemblyName}.{namespaceName}.{className}");
                GetCreateDescriptorProviderMethod();
                GetCreateDescriptorsMethod();

                // Update with success!
                HasAssembly = true;
            }
            catch
            {
                HasAssembly = false;
            }
        }

        public AssemblyDescriptorFactoryResolver()
        {
            LoadedAssembly = false;
        }

        private void GetCreateDescriptorProviderMethod()
        {
            var methodName = "CreateDescriptorProvider";
            var parameters = new Type[1] { typeof(string) };

            if (DescriptorFactoryClass == null) return;

            CreateDescriptorProviderMethod = DescriptorFactoryClass.GetMethod(
                methodName,
                parameters);
        }

        private void GetCreateDescriptorsMethod()
        {
            var methodName = "CreateDescriptors";
            var parameters = new Type[0];

            if (DescriptorFactoryClass == null) return;

            CreateDescriptorsMethod = DescriptorFactoryClass.GetMethod(
                methodName,
                parameters);
        }
    }
}