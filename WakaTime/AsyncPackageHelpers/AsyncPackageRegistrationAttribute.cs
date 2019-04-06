//------------------------------------------------------------------------------
// <copyright file="PackageRegistrationAttribute.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using static System.String;

namespace WakaTime.AsyncPackageHelpers {
    /// <devdoc>
    ///     This attribute is defined on a package to get it to be registered.  It
    ///     is internal because packages are meant to be registered, so it is
    ///     implicit just by having a package in the assembly.
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class AsyncPackageRegistrationAttribute : RegistrationAttribute
    {
        /// <devdoc>
        ///     Select between specifying the Codebase entry or the Assembly entry in the registry.
        ///     This can be overriden during registration
        /// </devdoc>
        public RegistrationMethod RegisterUsing { get; set; } = RegistrationMethod.Default;

        /// <summary>
        /// For managed resources, there should not be a native ui dll registered.
        /// </summary>
        public bool UseManagedResourcesOnly { get; set; } = false;

        /// <summary>
        /// Package is safe to load on a background thread.
        /// </summary>
        public bool AllowsBackgroundLoading { get; set; } = false;

        /// <summary>
        /// To specify a resource dll located in a different location then the default,
        /// set this property. This can be useful if your package is installed in the GAC.
        /// If this is not set, the directory where the package is located will be use.
        /// 
        /// Note that the dll should be located at the following path:
        ///        SatellitePath\lcid\PackageDllNameUI.dll
        /// </summary>
        public string SatellitePath { get; set; } = null;

        private static string RegKeyName(RegistrationContext context)
        {
            return Format(CultureInfo.InvariantCulture, "Packages\\{0}", context.ComponentType.GUID.ToString("B"));
        }

        /// <devdoc>
        ///     Called to register this attribute with the given context.  The context
        ///     contains the location where the registration information should be placed.
        ///     it also contains such as the type being registered, and path information.
        ///
        ///     This method is called both for registration and unregistration.  The difference is
        ///     that unregistering just uses a hive that reverses the changes applied to it.
        /// </devdoc>
        /// <param name="context">
        ///     Contains the location where the registration information should be placed.
        ///     It also contains other information such as the type being registered 
        ///     and path of the assembly.
        /// </param>
        public override void Register(RegistrationContext context) {
            var t = context.ComponentType;

            Key packageKey = null;
            try
            {
                packageKey = context.CreateKey(RegKeyName(context));

                //use a friendly description if it exists.
                if (TypeDescriptor.GetAttributes(t)[typeof(DescriptionAttribute)] is DescriptionAttribute attr && !IsNullOrEmpty(attr.Description)) {
                    packageKey.SetValue(Empty, attr.Description);
                }
                else {
                    packageKey.SetValue(Empty, t.Name);
                }

                packageKey.SetValue("InprocServer32", context.InprocServerPath);
                packageKey.SetValue("Class", t.FullName);

                // If specified on the command line, let the command line option override
                if (context.RegistrationMethod != RegistrationMethod.Default)
                {
                    RegisterUsing = context.RegistrationMethod;
                }

                // Select registration method
                switch (RegisterUsing)
                {
                    case RegistrationMethod.Assembly:
                    case RegistrationMethod.Default:
                        packageKey.SetValue("Assembly", t.Assembly.FullName);
                        break;

                    case RegistrationMethod.CodeBase:
                        packageKey.SetValue("CodeBase", context.CodeBase);
                        break;
                }

                Key childKey = null;
                if (!UseManagedResourcesOnly)
                {
                    try
                    {
                        childKey = packageKey.CreateSubkey("SatelliteDll");

                        // Register the satellite dll
                        var satelliteDllPath = SatellitePath != null
                            ? context.EscapePath(SatellitePath)
                            : context.ComponentPath;
                        childKey.SetValue("Path", satelliteDllPath);
                        childKey.SetValue("DllName",
                            Format(CultureInfo.InvariantCulture, "{0}UI.dll",
                                Path.GetFileNameWithoutExtension(t.Assembly.ManifestModule.Name)));
                    }
                    finally
                    {
                        childKey?.Close();
                    }
                }

                if (AllowsBackgroundLoading)
                {
                    packageKey.SetValue("AllowsBackgroundLoad", true);
                }

                if (typeof(IVsPackageDynamicToolOwner).IsAssignableFrom(context.ComponentType) ||
                    typeof(IVsPackageDynamicToolOwnerEx).IsAssignableFrom(context.ComponentType))
                {
                    packageKey.SetValue("SupportsDynamicToolOwner", Microsoft.VisualStudio.PlatformUI.Boxes.BooleanTrue);
                }
            }
            finally
            {
                packageKey?.Close();
            }
        }

        /// <devdoc>
        ///     Unregister this package.
        /// </devdoc>
        /// <param name="context"></param>
        public override void Unregister(RegistrationContext context) => context.RemoveKey(RegKeyName(context));
    }
}

