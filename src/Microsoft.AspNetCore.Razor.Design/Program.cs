// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Razor.Design
{
    public class Program
    {
        private readonly static Type ProgramType = typeof(Program);

        public static int Main(string[] args)
        {
            var app = new RazorToolingApplication(ProgramType);

            EnsureValidDispatchRecipient(ref args);

            ResolveTagHelpersCommandBase.Register<ResolveTagHelpersRunCommand>(app);

            return app.Execute(args);
        }

        private static void EnsureValidDispatchRecipient(ref string[] args)
        {
            const string DispatcherVersionArgumentName = "--dispatcher-version";

            if (!args.Contains(DispatcherVersionArgumentName, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            var dispatcherArgumentIndex = Array.FindIndex(
                args,
                (value) => string.Equals(value, DispatcherVersionArgumentName, StringComparison.OrdinalIgnoreCase));
            var dispatcherArgumentValueIndex = dispatcherArgumentIndex + 1;
            if (dispatcherArgumentValueIndex < args.Length)
            {
                var dispatcherVersion = args[dispatcherArgumentValueIndex];

                var thisAssembly = ProgramType.GetTypeInfo().Assembly;
                var version = thisAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion
                    ?? thisAssembly.GetName().Version.ToString();

                if (string.Equals(dispatcherVersion, version, StringComparison.Ordinal))
                {
                    // Remove dispatcher arguments from
                    var preDispatcherArgument = args.Take(dispatcherArgumentIndex);
                    var postDispatcherArgument = args.Skip(dispatcherArgumentIndex + 2);
                    var newProgramArguments = preDispatcherArgument.Concat(postDispatcherArgument);
                    args = newProgramArguments.ToArray();
                    return;
                }
            }

            var thisAssemblyName = typeof(Program).GetTypeInfo().Assembly.GetName().Name;

            // Could not validate the dispatcher version.
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    DesignResources.CouldNotInvokeTool,
                    thisAssemblyName,
                    "Microsoft.AspNetCore.Razor.Tools"));
        }
    }
}