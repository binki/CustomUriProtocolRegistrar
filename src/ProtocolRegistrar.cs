// Copyright 2015 Nathan Phillip Brink
// Licensed under the license stored in ../LICENSE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace OhNoPub.CustomUriProtocolRegistrar
{
    public class ProtocolRegistrar
    : Disposable
    {
        readonly bool disablePrivilegeEscalation;

        Stack<IProtocolRegistrar> protocolRegistrars;

        public ProtocolRegistrar()
        : this(disablePrivilegeEscalation: false)
        {
        }

        public ProtocolRegistrar(bool disablePrivilegeEscalation)
        {
            this.disablePrivilegeEscalation = disablePrivilegeEscalation;
        }

        /// <summary>
        ///   Register a protocol handler.
        /// </summary>
        /// <returns>true on success.</returns>
        public bool Register(ProtocolRegistryScope scope, string scheme, string applicationPath)
        {
            return ForEachProtocolRegistrar(false, protocolRegistrar => protocolRegistrar.Register(scope, scheme, applicationPath));
        }

        /// <summary>
        ///   Unregister a protocol handler.
        /// </summary>
        /// <returns>true if, as far as the system can determine, the protocol is no longer registered.</returns>
        public bool Unregister(ProtocolRegistryScope scope, string scheme)
        {
            return ForEachProtocolRegistrar(true, protocolRegistrar => protocolRegistrar.Unregister(scope, scheme));
        }

        /// <summary>
        ///   Register an <see cref="IProtocolRegistrar"/>
        ///   implementation. This transfers ownership of <paramref
        ///   name="protocolRegistrar"/>’s lifetime to this
        ///   instance—we will call Dispose() for you.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Any newly-added registrar implementation will always
        ///     run before existing implementations.
        ///   </para>
        /// </remarks>
        public void AddProtocolRegistrar(IProtocolRegistrar protocolRegistrar)
        {
            GetProtocolRegistrars();
            protocolRegistrars.Push(protocolRegistrar);
        }

        delegate bool ForEachProtocolRegistrarFunc(IProtocolRegistrar protocolRegistrar);

        /// <param name="all">If true, requires all registrars to succeed. Otherwise stops at the first success.</param>
        /// <param name="func">The func to run on each registrary.</param>
        bool ForEachProtocolRegistrar(bool all, ForEachProtocolRegistrarFunc func)
        {
            foreach (var protocolRegistrar in GetProtocolRegistrars())
            {
                if (func(protocolRegistrar))
                {
                    if (!all)
                        return true;
                }
                else
                {
                    if (all)
                        return false;
                }
            }

            // If all was requested, then everything
            // succeeded. Otherwise, that means we didn’t find a
            // single successful registrar.
            return all;
        }

        IEnumerable<IProtocolRegistrar> GetProtocolRegistrars()
        {
            return protocolRegistrars = protocolRegistrars ?? new Stack<IProtocolRegistrar>(new IProtocolRegistrar[] {
                    // Windows should be the last-visited (and thus
                    // first-pushed) item. Most .net platforms will
                    // try to emulate/fake Registry support but may
                    // not proxy the URI protocol registration to the
                    // platform-specific mechanisms. That is, it’s
                    // easier to detect the non-presence of URI
                    // protocol mechanisms other than Windows’s
                    // Registry ;-).
                    new WindowsProtocolRegistrar(disablePrivilegeEscalation),
                });
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (protocolRegistrars != null)
                    {
                        foreach (var protocolRegistrar in protocolRegistrars)
                            protocolRegistrar.Dispose();
                        protocolRegistrars = null;
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        static int badArgs()
        {
            MessageBox.Show("This executable cannot be run in DOS mode.");
            return 1;
        }

        public static string ArgEncode(string value)
        {
            var result = new StringBuilder("+");
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if ("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(c) > -1)
                    result.Append(c);
                else
                    result.Append("_" + BitConverter.ToString(Encoding.UTF8.GetBytes(new[] { c, })).Replace(":", "") + "x");
            }
            return result.ToString();
        }

        static string ArgDecode(string value)
        {
            if (value.Length == 0 || value[0] != '+')
                return value;

            var result = new StringBuilder();
            for (var i = 1; i < value.Length; i++)
            {
                var c = value[i];
                if (c == '_')
                {
                    var terminator = value.IndexOf('x', i);
                    var b = new List<byte>();
                    i++;
                    while (terminator > i)
                    {
                        b.Add(byte.Parse(value.Substring(i, 2), NumberStyles.AllowHexSpecifier));
                        i += 2;
                    }
                    result.Append(Encoding.UTF8.GetChars(b.ToArray()));
                }
                else
                    result.Append(c);
            }
            return result.ToString();
        }

        public static int Main(string[] args)
        {
            if (ArgDecode(ArgEncode("a!b\\")) != "a!b\\") throw new Exception("POST fail");
            var _register = "register";
            var _unregister = "unregister";
            var optind = 0;
            if (args.Length <= optind
                || !((ICollection<string>)new [] { _register, _unregister, }).Contains(args[optind]))
                return badArgs();
            var register = args[optind++] == _register;

            var scheme = args.Length > optind ? ArgDecode(args[optind++]) : null;
            if (scheme == null)
                return badArgs();

            var applicationPath = args.Length > optind ? ArgDecode(args[optind++]) : null;
            if (applicationPath == null)
                return badArgs();

            // Assume that invocation through Main() indicates we have
            // elevated, perhaps through ElevateProtocolRegistrar()
            // which just tries to invoke us directly.
            using (var protocolRegistrar = new ProtocolRegistrar(false))
            {
                if (register)
                {
                    Console.Error.WriteLine("Attempting to register scheme “{0}:” to launch “{1}”.", scheme, applicationPath);
                    return protocolRegistrar.Register(ProtocolRegistryScope.User, scheme, applicationPath) ? 0 : 1;
                }
                else
                {
                    Console.Error.WriteLine("Attempting to unregister scheme “{0}:”.", scheme);
                    return protocolRegistrar.Unregister(ProtocolRegistryScope.User, scheme) ? 0 : 1;
                }
            }
        }
    }

    public enum ProtocolRegistryScope
    {
        /// <summary>
        ///   The registration will only affect the
        ///   currently-logged-in user.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     It is recommended that most user-obtainable
        ///     applications use this setting. If your application is
        ///     installed into the user’s profile instead of
        ///     system-wide, please use this option. Hopefully,
        ///     registering in this scope will not require the user to
        ///     obtain administration rights.
        ///   </para>
        /// </remarks>
        User = 0,

        /// <summary>
        ///   The registration will affect all users on the local
        ///   system.
        /// </summary>
        System,
    }
}
