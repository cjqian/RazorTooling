// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Design;
using Microsoft.DotNet.ProjectModel;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Internal;
using NuGet.Frameworks;

namespace Microsoft.AspNetCore.Razor.Tools.Internal
{
    public class ResolveTagHelpersDispatchCommand : ResolveTagHelpersCommandBase
    {
        private CommandOption BuildBasePathOption { get; set; }

        private CommandOption FrameworkOption { get; set; }

        private CommandOption ConfigurationOption { get; set; }

        private CommandArgument ProjectArgument { get; set; }

        protected override void Configure(CommandLineApplication config)
        {
            BuildBasePathOption = config.Option(
                "-b|--build-base-path",
                "The project's build base path directory.",
                CommandOptionType.SingleValue);
            FrameworkOption = config.Option(
                "-f|--framework",
                "The project's framework.",
                CommandOptionType.SingleValue);
            ConfigurationOption = config.Option(
                "-c|--configuration",
                "The project's build configuration.",
                CommandOptionType.SingleValue);
            ProjectArgument = config.Argument(
                "[project]",
                "Path to the project.json for the project resolving TagHelperDescriptors.",
                multipleValues: false);
        }

        protected override int OnExecute()
        {
            var projectFile = ProjectReader.GetProject(ProjectArgument.Value);
            var targetFrameworks = projectFile
                .GetTargetFrameworks()
                .Select(frameworkInformation => frameworkInformation.FrameworkName);

            NuGetFramework framework;
            if (!TryResolveFramework(targetFrameworks, out framework))
            {
                // Could not resolve framework for dispatch. Error was reported, exit early.
                return 0;
            }

#if NETCOREAPP1_0
            int exitCode;
            if (PackageOnlyResolveTagHelpersRunCommand.TryPackageOnlyTagHelperResolution(
                    AssemblyNamesArgument,
                    ProtocolOption,
                    BuildBasePathOption,
                    ConfigurationOption,
                    projectFile,
                    framework,
                    out exitCode))
            {
                return exitCode;
            }
#endif

            var dispatchArgs = new List<string>
            {
                CommandName,
            };

            dispatchArgs.AddRange(AssemblyNamesArgument.Values);

            if (ProtocolOption.HasValue())
            {
                dispatchArgs.Add("--protocol");
                dispatchArgs.Add(ProtocolOption.Value());
            }

            var toolName = typeof(Design.Program).GetTypeInfo().Assembly.GetName().Name;
            var dispatchCommand = DotnetToolDispatcher.CreateDispatchCommand(
                dispatchArgs,
                framework,
                ConfigurationOption.Value(),
                outputPath: null,
                buildBasePath: BuildBasePathOption.Value(),
                projectDirectory: projectFile.ProjectDirectory,
                toolName: toolName);

            using (var errorWriter = new StringWriter())
            {
                var commandExitCode = dispatchCommand
                    .ForwardStdErr(errorWriter)
                    .ForwardStdOut()
                    .Execute()
                    .ExitCode;

                if (commandExitCode != 0)
                {
                    ReportError(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ToolResources.FailedToExecuteRazorTooling,
                            errorWriter.ToString()));
                }

                return 0;
            }
        }

        private bool TryResolveFramework(
            IEnumerable<NuGetFramework> availableFrameworks,
            out NuGetFramework resolvedFramework)
        {
            NuGetFramework framework;
            if (FrameworkOption.HasValue())
            {
                var frameworkOptionValue = FrameworkOption.Value();
                framework = NuGetFramework.Parse(frameworkOptionValue);

                if (!availableFrameworks.Contains(framework))
                {
                    ReportError(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ToolResources.ProjectDoesNotSupportProvidedFramework,
                            ProjectArgument.Value,
                            frameworkOptionValue));

                    resolvedFramework = null;
                    return false;
                }
            }
            else
            {
                // Prioritize non-desktop frameworks since they have the option of not dispatching to resolve TagHelpers.
                framework = availableFrameworks.FirstOrDefault(f => !f.IsDesktop()) ?? availableFrameworks.First();
            }

            resolvedFramework = framework;
            return true;
        }
    }
}
