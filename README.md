# 12587603_Patches

I'm modifying the code in the 12587603 operating system, to add some features that GM didn't give us.

Use at your own risk. The contents of this repo represent work in progress, not a finished product.

## Building and testing the code

### Prerequisites

To build on Windows you'll need the m68k gcc tools. I used the version from [GnuToolChains.com](https://gnutoolchains.com/m68k-elf/) and put a copy in the Resources directory just in case.

You can also build the patches on WSL with Ubuntu and "sudo apt install gcc-m68k-linux-gnu". The self-test script only builds under WSL right now - I gotta fix that.

### Building And Testing

`make patches` will build the m68k code for the patches

`apply.cmd` will apply the patches to firmware image (.bin file)

`make selftest` will build a selftest binary, which runs under amd64 to validate patch logic.

`selftest` actually runs the tests. (There's only one test so far. It doesn't pass. Yet.)

See the build.cmd and test.cmd commands.

## FirmwarePatcher

This produces a patched.bin file from a firmware image file that you specify on the command line.

# New Features (or, aspirations thereof)

## End Of Injection Time (EOIT) Enhancement

My car idles and cruises better with the fuel injection pulse delayed, but it makes more power with the stock injection timing, but the factory firmware uses the same EOIT table in all conditions.

So, there's code here that uses the stock EOIT table when airflow is high (so you can tune for power), or new EOIT table with airflow is low (so you can tune for stability). I've got the transition at 40 g/s.

In theory it would be better to interpolate between the two, but in practice I spend very little time near the boundary. And in the earliest days of this project, I wanted a very simple patch just to validate the patching functionality.

## Rev-Matched Downshifts

I implemented this for my Subaru years ago, and I think I can make it work here too - but I have to admit that I'm not certain. The TAC module might be an obstacle. We'll see.

## Per-Gear Maximum Throttle

The factory firmware provides a table that limits throttle blade angle by RPM - but there is only one such table. I think it would be fun to have separate tables per gear, as a feed-forward traction control strategy, so that's coming. And it might be possible to hijack an unused analog input, so that the amount of reduction can be scaled with a knob on the dashboard... so for example 0% would be suitable for rain (or valet mode), and 100% would be suitable for sticky tires on a warm day.
