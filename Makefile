# Copyright 2015 Nathan Phillip Brink
# Licensed under the license stored in LICENSE.
.POSIX:
.PHONY: all clean

MY_CSC = $${CSC-csc}
MY_NETCF35DIR = $${NETCF35DIR-c:/Program Files (x86)/Microsoft.NET/SDK/CompactFramework/v2.0/WindowsCE}

all: CustomUriProtocolRegistrar.exe

SRC = \
	src/Disposable.cs \
	src/ElevateProtocolRegistrar.cs \
	src/IProtocolRegistrar.cs \
	src/ProtocolRegistrar.cs \
	src/WindowsProtocolRegistrar.cs

# Yeah, using xargs here is kind of wrong, but I can’t think of a better
# way to pass the arguments after prefixing something to them.
CustomUriProtocolRegistrar.exe: $(SRC)
	SRCDIR=$${PWD}; \
	MSSRCDIR=$$(cmd //c echo $${PWD});\
	pushd "$(MY_NETCF35DIR)" \
	&& for SRCFILE in $(SRC); do echo "$${SRCDIR}/$${SRCFILE}"; done \
	| xargs $(MY_CSC) -nostdlib -r:mscorlib.dll -out:"$${MSSRCDIR}/$(@)" 
