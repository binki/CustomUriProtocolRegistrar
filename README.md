Copyright 2015 Nathan Phillip Brink

Licensed under the license stored in [`LICENSE`](LICENSE).

# Usage

    using (var protocolRegistrar = new OhnoPub.CustomUriProtocolRegistrar.ProtocolRegistrar())
    {
        // If you application provides any registrars of its own for platforms
        // which CustomUriProtocolRegistrar does not know how to support yet,
        // register them here.
        //protocolRegistrar.AddProtocolRegistrar(new CustomProtocolRegistrar());

        // Assert that your URI protocol handler is registered.
        protocolRegistrar.Register(
            OhNoPub.CustomUriProtocolRegistrar.ProtocolRegistryScope.User,
            GetType().Name.ToLowerInvariant(),
            Assembly.GetEntryAssembly().CodeBase);
    }

Make sure that your application, when forming URI strings, safely
encodes the string so that its meaning does not change if it passes
through `decodeURIComponent()` an arbitrary number of
times. [Windows’s IE may decode the URI once before passing it to the
application](https://msdn.microsoft.com/en-us/library/aa767914%28VS.85%29.aspx). The
safest method would be to blanketly base64-encode all data on the
right hand side of URI. This makes the URI inscrutable but secures it
against most platform-specific pitfalls.

Now when the URI is accessed, and if the user permits the
configuration change to be made (if necessary), the application will
be launched with the *full URI including scheme* as its sole
argument. (Though, again, note that, on Windows, the URI may be parsed
into multiple parameters if it decodes to a value that includes
whitespace).

# Build

We’re going to try to target the compact framework. To build, you will
need to [obtain the CF2.0SP2 reference
assemblies](http://www.microsoft.com/en-us/download/details.aspx?id=17981)
and either install them to a default location or set the NETCF35DIR
environment variable to wherever you put ’em.

Because buildsystems, I rely on GNU Make. If you use Windows, obtain
by running
[mingw-get-setup](http://www.mingw.org/wiki/Getting_Started) (or you
can install the “MinGW Installation Manager Setup Tool” via
[npackd](https://npackd.appspot.com/)). Make sure to install the
`mingw-developer-toolkit` package via mingw-get. Then [set
`PATH`](http://rapidee.com/) to include the `bin` and `msys/1.0/bin`
directories.

Now either launch `Developer Command Prompt for VS2015` or temporarily
put `csc` in your `PATH` (a common place to find csc nowadays is
`%PROGRAMFILES(X86)%/MSBuild/14.0/Bin`). Your shell should now be all
ready for compilation now! Just run `make`.
