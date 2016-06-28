// Copyright 2015 Nathan Phillip Brink
// Licensed under the license stored in ../LICENSE.

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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

        delegate void ExpectKeyFoundAction(RegistryKey key);
        void ExpectKey(RegistryKey parent, string name, ExpectKeyFoundAction ifFoundAction, bool create = false, bool writable = false)
        {
            Console.Error.WriteLine("Opening {0}/{1}. create={2}", parent.Name, name, create);
            using (var key = parent.OpenSubKey(name, writable) ?? (create ? parent.CreateSubKey(name) : null))
            {
                if (key == null)
                    Console.Error.WriteLine("Could not open {0}.", name);
                else
                    ifFoundAction(key);
            }
        }

        void Walk(RegistryKey parentKey, string[] names, ExpectKeyFoundAction ifFoundAction, bool create = false, int nextNameIndex = 0)
        {
            if (nextNameIndex < names.Length)
                // This function is recursive because we use
                // ExpectKey() whose using{} block controls
                // destruction.
                ExpectKey(parentKey, names[nextNameIndex], childKey => Walk(childKey, names, ifFoundAction, create, nextNameIndex + 1), create, nextNameIndex + 1 >= names.Length);
            else
                ifFoundAction(parentKey);
        }

        public bool Register(ProtocolRegistryScope scope, string scheme, string applicationPath)
        {
            Exception thrownException;
            try
            {
                Console.Error.WriteLine("A");
                // On Windows CE, the Classes key will only be
                // searched for our protocol if we create the key
                // HKLM\SOFTWARE\Microsoft\Shell\URLProtocols\<proto>
                // as described at
                // http://thegrayzone.co.uk/blog/2010/08/custom-url-protocol-in-windows-ce/
                // linked by
                // http://stackoverflow.com/a/4108720/429091. We can
                // detect this situation by TODO: BY DOING WHAT?!
                Console.Error.WriteLine("B");
                Walk(
                    Registry.ClassesRoot,
                    new [] {
                        scheme,
                    },
                    subKey => {
                        subKey.SetValue("URL Protocol", "", RegistryValueKind.String);
                        Walk(
                            subKey,
                            new [] {
                                "DefaultIcon",
                            },
                            defaultIconKey => defaultIconKey.SetValue(null, applicationPath + ", 0", RegistryValueKind.String),
                            true);
                        Walk(
                            subKey,
                            new [] {
                                "shell",
                                "open",
                                "command",
                            },
                            commandKey => commandKey.SetValue(null, applicationPath + " \"%1\"", RegistryValueKind.String),
                            true);
                    },
                    true);
                Console.Error.WriteLine("C");
                // To make the protocol show up in the Control Panel
                // GUI on Desktop Windows, it should be registered in
                // HKCU\SOFTWARE\Microsoft\Windows\Shell\Associations\UrlAssociations\<proto>. We
                // detect this situation by checking if the
                // UrlAssocations key exists. Walk() will quietly skip
                // when the key doesn’t exist.
                Walk(
                    Registry.CurrentUser,
                    new [] {
                        "SOFTWARE",
                        "Microsoft",
                        "Windows",
                        "Shell",
                        "Associations",
                        "UrlAssociations",
                    },
                    urlAssociationsKey => Walk(
                        urlAssociationsKey,
                        new [] {
                            scheme,
                        },
                        subKey => Console.Error.WriteLine("Ensured UrlAssociations/{0} exists.", scheme),
                        create: true));
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                thrownException = ex;
            }
            catch (SecurityException ex)
            {
                thrownException = ex;
            }

            // Just let it pass for now, by failing out will get
            // to ElevateProtocolRegistrar.
            Console.Error.WriteLine(thrownException);

            return Elevate(true, scheme, applicationPath);
        }

        public bool Unregister(ProtocolRegistryScope scope, string scheme)
        {
            if (Exists(scheme))
            {
                try
                {
                    Walk(
                        Registry.ClassesRoot,
                        new [] {
                            scheme,
                        },
                        subKey => Registry.ClassesRoot.DeleteSubKeyTree(scheme
                                                                        // TODO: This should not be here
                                                                        /*, false*/));
                    // TODO: Fix this? Is this some CE compatibility
                    // or something we aren’t cleaning up we should
                    // clean up?
                    /*
                    Walk(
                        Registry.ClassesRoot,
                        new [] {
                            scheme,
                        },
                        subKey => {
                        });
                    */
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.Error.WriteLine(ex);

                    return Elevate(false, scheme);
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

        protected bool Elevate(bool register, string scheme, string applicationPath = null)
        {
            var processStartInfo = new ProcessStartInfo(
                GetAssemblyPath(Assembly.GetCallingAssembly()),
                 "register " + ProtocolRegistrar.ArgEncode(scheme) + " " + ProtocolRegistrar.ArgEncode(applicationPath));
            using (var process = Process.Start(processStartInfo))
            {
                process.WaitForExit();
                System.Windows.Forms.MessageBox.Show(""+process.ExitCode);
                return process.ExitCode == 0;
            }
        }

        static string GetAssemblyPath(Assembly assembly)
        {
            return new Uri(assembly.GetName().CodeBase).LocalPath;
        }
    }
}
