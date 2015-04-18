// Copyright 2015 Nathan Phillip Brink
// Licensed under the license stored in ../LICENSE.

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Security;

namespace OhNoPub.CustomUriProtocolRegistrar
{
    /// <remarks>
    ///   <para>
    ///     http://thegrayzone.co.uk/blog/2010/08/custom-url-protocol-in-windows-ce/
    ///     linked by http://stackoverflow.com/a/4108720/429091 states that the instructions at .
    ///   </para>
    /// </remarks>
    class WindowsProtocolRegistrar
    : Disposable
    , IProtocolRegistrar
    {
        readonly bool disablePrivilegeEscalation;

        /// <param name="disablePrivilegeEscalation">Whether or not we can count on ElevateProtocolHandler happening after us… hrm… still divided against myself as to whether or not elevation should just happen right in here in the Windows backend…</param>
        public WindowsProtocolRegistrar(bool disablePrivilegeEscalation)
        {
            this.disablePrivilegeEscalation = disablePrivilegeEscalation;
        }

        bool Exists(string schema)
        {
            using (var subKey = Registry.ClassesRoot.OpenSubKey(schema))
                return subKey != null;
        }

        public bool Register(ProtocolRegistryScope scope, string scheme, string applicationPath)
        {
            try
            {
                using (var subKey = Registry.ClassesRoot.CreateSubKey(scheme))
                {
                    subKey.SetValue("URL Protocol", "", RegistryValueKind.String);
                    using (var defaultIconKey = subKey.CreateSubKey("DefaultIcon"))
                        defaultIconKey.SetValue(null, applicationPath + ", 0", RegistryValueKind.String);
                    using (var shellKey = subKey.CreateSubKey("shell"))
                        using (var openKey = shellKey.CreateSubKey("open"))
                            using (var commandKey = openKey.CreateSubKey("command"))
                                commandKey.SetValue(null, applicationPath + " \"%1\"", RegistryValueKind.String);
                    // TODO Create the CE key in HKLM… and test on CE
                    return true;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // Just let it pass for now, by failing out will get
                // to ElevateProtocolRegistrar.
                Console.Error.WriteLine(ex);
            }
            return false;
        }

        public bool Unregister(ProtocolRegistryScope scope, string scheme)
        {
            if (Exists(scheme))
            {
                try
                {
                    Registry.ClassesRoot.DeleteSubKey(scheme);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.Error.WriteLine(ex);
                }
            }

            if (!disablePrivilegeEscalation)
                // Privilege escalation will happen in the future, so
                // say we succeeded even though we didn’t and only
                // actually fail if we’re already in the escalation
                // round.
                return true;

            return !Exists(scheme);
        }
    }
}
