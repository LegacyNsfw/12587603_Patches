#define DEFINE_RAM_ADDRESSES
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
    printf("\r\n");
    printf("\r\n");
    selfTestThrottlePatch();
    printf("\r\n");
}

void assert(unsigned short expected, unsigned short actual, char* module, char* message)
{
    if (expected == actual)
    {
        printf("[PASS] %s: %s.\r\n", module, message);
    }
    else
    {
        printf("[FAIL] %s: %s. Expected %u, got %u\r\n", module, message, expected, actual);
    }
}