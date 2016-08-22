// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNetCore.Razor.Design.Internal
{
    public class AssemblyTagHelperDescriptorResolver
    {
        private readonly TagHelperDescriptorFactory _descriptorFactory = new TagHelperDescriptorFactory(designTime: true);
        private readonly TagHelperTypeResolver _tagHelperTypeResolver;
        private AssemblyDescriptorFactoryResolver _assemblyResolver = new AssemblyDescriptorFactoryResolver();

        public AssemblyTagHelperDescriptorResolver()
            : this(new TagHelperTypeResolver())
        {
        }

        public AssemblyTagHelperDescriptorResolver(TagHelperTypeResolver tagHelperTypeResolver)
        {
            _tagHelperTypeResolver = tagHelperTypeResolver;
        }

        public static int DefaultProtocolVersion { get; } = 1;

        public int ProtocolVersion { get; set; } = DefaultProtocolVersion;

        public IEnumerable<TagHelperDescriptor> Resolve(string assemblyName, ErrorSink errorSink)
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            if (ProtocolVersion == 1)
            {
                var tagHelperTypes = GetTagHelperTypes(assemblyName, errorSink);
                var tagHelperDescriptors = new List<TagHelperDescriptor>();
                foreach (var tagHelperType in tagHelperTypes)
                {
                    var descriptors = _descriptorFactory.CreateDescriptors(assemblyName, tagHelperType, errorSink);
                    tagHelperDescriptors.AddRange(descriptors);
                }

                // Append view component descriptors.
                var viewComponentTagHelperDescriptors = GetViewComponentTagHelpers(assemblyName);
                tagHelperDescriptors.AddRange(viewComponentTagHelperDescriptors);

                return tagHelperDescriptors;
            }
            else
            {
                // Unknown protocol
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        DesignResources.InvalidProtocolValue,
                        typeof(TagHelperDescriptor).FullName, ProtocolVersion));
            }
        }

        /// <summary>
        /// Protected virtual for testing.
        /// </summary>
        protected virtual IEnumerable<Type> GetTagHelperTypes(string assemblyName, ErrorSink errorSink)
        {
            return _tagHelperTypeResolver.Resolve(assemblyName, SourceLocation.Zero, errorSink);
        }

        private IEnumerable<TagHelperDescriptor> GetViewComponentTagHelpers(string assemblyName)
        {
            // Our first time checking the assembly!
            if (!_assemblyResolver.LoadedAssembly)
            {
                _assemblyResolver.LoadAssembly();
            }

            if (_assemblyResolver.HasAssembly)
            {
                var descriptorProvider = _assemblyResolver.CreateDescriptorProviderMethod.Invoke(null, new object[1] { assemblyName });
                var classInstance = Activator.CreateInstance(_assemblyResolver.DescriptorFactoryClass, new object[1] { descriptorProvider });
                var descriptorsObject = _assemblyResolver.CreateDescriptorsMethod.Invoke(classInstance, new object[0]);
                var descriptors = descriptorsObject as IEnumerable<TagHelperDescriptor>;
                return descriptors;
            }

            // No assembly.
            return Enumerable.Empty<TagHelperDescriptor>();
        }

    }
}
