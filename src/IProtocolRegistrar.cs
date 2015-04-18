// Copyright 2015 Nathan Phillip Brink
// Licensed under the license stored in ../LICENSE.

using System;
using System.Windows.Forms;

namespace OhNoPub.CustomUriProtocolRegistrar
{
    public interface IProtocolRegistrar
    : IDisposable
    {
        /// <returns>true if the scheme is registered as described by the caller (both when it already existed and when the registration just completed successfully, no-ops should return false).</returns>
        bool Register(ProtocolRegistryScope scope, string scheme, string applicationPath);

        /// <returns>false if the scheme is still registered in the given scope, true otherwise (no-ops should return true).</returns>
        bool Unregister(ProtocolRegistryScope scope, string scheme);
    }
}
