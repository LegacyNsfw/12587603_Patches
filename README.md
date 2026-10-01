# 12587603_Patches

I'm modifying the code in the 12587603 operating system, to add some features that GM didn't give us.

Use at your own risk. The contents of this repo represent work in progress, not a finished product.

## Building and testing the code

### Prerequisites

You will need git's Large File Support (LFS) to handle the installers in the Resources directory. 

To build on Windows you'll need the m68k gcc tools. I used the version from [GnuToolChains.com](https://gnutoolchains.com/m68k-elf/) and I've put a copy in the Resources directory just in case.

You can also build the patches on WSL with Ubuntu - use `sudo apt install gcc-m68k-linux-gnu` to get the toolchain.

To build and run the tests on Windows, you'll need a gcc toolchain that builds Windows executables. I used [MinGW-64](https://github.com/niXman/mingw-builds-binaries/releases) (blessed by [mingw-w64.org](https://www.mingw-w64.org/downloads/#mingw-w64-builds)) and I've put a copy in the Resources directory just in case. You'll want to unzip that in c:\ and add c:\mingw32\bin to your path.

### Building And Testing

`make patches` will build the m68k code for the patches

`apply.cmd` will apply the patches to firmware image (.bin file)

`make selftest` will build a selftest binary, which runs on Intel/AMD to validate patch logic.

`selftest` actually runs the tests.

`test-on-windows.cmd` and `test-on-linux.sh` will do a clean build and the run tests.

## FirmwarePatcher

This produces a patched.bin file from a firmware image file that you specify on the command line.

# New Features (or, aspirations thereof)

## End Of Injection Time (EOIT) Enhancement

My car idles and cruises better with the fuel injection pulse delayed, but it makes more power with the stock injection timing, but the factory firmware uses the same EOIT table in all conditions.

So, there's code here that uses the stock EOIT table when airflow is high (so you can tune for power), or new EOIT table with airflow is low (so you can tune for stability). I've got the transition at 40 g/s.

In theory it would be better to interpolate between the two, but in practice I spend very little time near the boundary. And in the earliest days of this project, I wanted to try a very simple change, just to validate the patching functionality.

## Rev-Matched Downshifts

I implemented this for my Subaru years ago, and I think I can make it work for 2004 Corvettes too - but I have to admit that I'm not certain. The TAC module might be an obstacle. We'll see.

If this works, I'll add instructions for putting a 2004 PCM into 99-03 Corvettes. It only required moving one pin.

## Per-Gear Maximum Throttle

The factory firmware provides a table that limits throttle blade angle by RPM - but there is only one such table. I think it would be fun to have separate tables per gear, as a feed-forward traction control strategy, so that's coming. 

And it might eventually be possible to hijack an unused analog input, so that the amount of throttle reduction can be scaled with a knob on the dashboard... so for example 0/10 would be suitable for rain (or valet mode), 4/10 for all-season tires in the winter, 7/10 for sticky tires on a warm day, and 10/10 for unrestrained hooliganism. Maybe. I'm not promising.
