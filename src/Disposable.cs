// Copyright 2015 Nathan Phillip Brink
// Licensed under the license stored in ../LICENSE.

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace OhNoPub.CustomUriProtocolRegistrar
{
    public class Disposable
    : IDisposable
    {
        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
