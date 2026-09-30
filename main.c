#define DEFINE_HOST_RAM
#include "globals.h"
#include "selftest.h"


#ifdef __m68k__
int printf(const char *format, ...)
{
    (void)format;
    return 0;
}
#else
#include <stdio.h>
#endif

void main(void)
{
    selfTestRevMatch();
}

void assert(unsigned short expected, unsigned short actual, char* module, char* message)
{
    if (expected != actual)
    {
        printf("Assertion failed in module %s: %s. Expected %u, got %u\n", module, message, expected, actual);
    }
}