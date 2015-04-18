// Copyright 2015 Nathan Phillip Brink
// Licensed under the license stored in ../LICENSE.

using System;
using System.Collections.Generic;

namespace OhNoPub.CustomUriProtocolRegistrar
{
    /// <remarks>
    ///   <para>
    ///     http://thegrayzone.co.uk/blog/2010/08/custom-url-protocol-in-windows-ce/
    ///     linked by http://stackoverflow.com/a/4108720/429091 states that the instructions at .
    ///   </para>
    /// </remarks>
    class ElevateProtocolRegistrar
    : Disposable
    , IProtocolRegistrar
    {
        readonly bool disablePrivilegeEscalation;

        public ElevateProtocolRegistrar(bool disablePrivilegeEscalation)
        {
            this.disablePrivilegeEscalation = disablePrivilegeEscalation;
        }

        public bool Register(ProtocolRegistryScope scope, string scheme, string applicationPath)
        {
            if (disablePrivilegeEscalation)
                return false;

            throw new NotImplementedException();
        }

        public bool Unregister(ProtocolRegistryScope scope, string scheme)
        {
            if (disablePrivilegeEscalation)
                return true;

            throw new NotImplementedException();
        }
    }
}
