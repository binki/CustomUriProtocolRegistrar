// Copyright 2015 Nathan Phillip Brink
// Licensed under the license stored in ../LICENSE.

using System;
using System.Reflection;
using System.Windows.Forms;

namespace OhNoPub.CustomUriProtocolRegistrar
{
    public class Example
    {
        AssemblyName entryAssemblyName = Assembly.GetExecutingAssembly().GetName();

        public static int Main(string[] args)
        {
            return new Example().Run(args);
        }

        bool PromptYesNo(string prompt)
        {
            return MessageBox.Show(
                prompt,
                entryAssemblyName.Name,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Exclamation,
                MessageBoxDefaultButton.Button1) == DialogResult.Yes;
        }

        int Run(string[] args)
        {
            var failureCount = 0;
            if (args.Length > 0)
            {
                MessageBox.Show(args[0]);
            }
            else
            {
                var scheme = StringToLowerInvariant(GetType().Name);
                var applicationPath = new Uri(entryAssemblyName.CodeBase).LocalPath;
                using (var protocolRegistrar = new ProtocolRegistrar())
                {
                    if (PromptYesNo(string.Format("Register {0} for protocol {1}?", applicationPath, scheme)))
                        if (!protocolRegistrar.Register(ProtocolRegistryScope.User, scheme, applicationPath))
                        {
                            MessageBox.Show("Registering failed.", entryAssemblyName.Name, MessageBoxButtons.OK, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1);
                            failureCount++;
                        }
                    if (PromptYesNo(string.Format("Unregister protocol {1}?", applicationPath, scheme)))
                        if (!protocolRegistrar.Unregister(ProtocolRegistryScope.User, scheme))
                        {
                            MessageBox.Show("Unregistering failed.");
                            failureCount++;
                        }
                }
            }
            return failureCount > 0 ? 1 : 0;
        }

        public static string StringToLowerInvariant(string s)
        {
            return s.ToLower(System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
