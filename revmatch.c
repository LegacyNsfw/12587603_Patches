#include "globals.h"
#include "calibration.h"
#include "selftest.h"

int comparePrimaryAndRedundantThrottlePositionsInvoked = 0;

#ifdef __m68k__
void (*ComparePrimaryAndRedundantThrottlePositions)(void) = (void (*)(void)) 0x20A3A;
#else
void ComparePrimaryAndRedundantThrottlePositions(void) { comparePrimaryAndRedundantThrottlePositionsInvoked = 1; }
#endif

static inline __attribute__((always_inline)) void getTargetRpm(
    unsigned short ratio,
    unsigned short vehicle_speed)
{
    *pTargetRpm = (ratio >> 6) * (vehicle_speed >> 6);
}

__attribute__((section(".code.implementation")))
void revMatch(void)
{
    // If the clutch has not been pressed, update the "previous" gear to be the current gear.
    // This ensures that the previous gear is available after the clutch has been is pressed.
    if (*pCurrentGear != 8)
    {
        *pPreviousGear = *pCurrentGear;
    }

    // If rev match conditions are not met, just run the normal throttle logic.
    if (*pPreviousGear > 5  || // we don't support downshifting from gears higher than 5 (sixth)
        *pPreviousGear == 0 || // we can't downshift from first
        *pCurrentGear != 8 || // clutch is not pressed
        *pPedalPosition < MinimumThrottlePedalPosition)
    {
        // This function was invoked in place of ComparePrimaryAndRedundantThrottlePositions(),
        // so under normal conditions we'll just call that function and return.
        ComparePrimaryAndRedundantThrottlePositions();
        return;
    }

    // Rev match conditions are met.
    // For now, we'll just command a fixed throttle blade angle.
    *pDesiredThrottlePlateAngle = FixedThrottleBladeAngle;

    // But we'll also calculate the target RPM for rev matching.
    unsigned short gear_index = *pCurrentGear;
    unsigned short ratio = SpeedToRpmFactorArray[gear_index - 1];
    getTargetRpm(ratio, *pVehicleSpeed);

#ifdef __m68k__
    __asm__ volatile (
        "movea.w  #0x1000,%%a2            \n\t"
        "move.w   %[target_rpm],%%d4      \n\t"
        "cmpi.w   #0x1000,%%d4            \n\t"
        "bcc      1f                      \n\t"
        "andi.l   #0xFFFF,%%d4            \n\t"
        "divu.w   #0xA,%%d4               \n\t"
        "bra      2f                      \n\t"
        "1:                               \n\t"
        "move.w   #0x1000,%%d4            \n\t"
        "2:                               \n\t"
        "tblu.w   (RpmToThrottleBladeAngle).l,%%d4\n\t"
        "move.w   %%d4,%[throttle_angle]  \n\t"
        : [throttle_angle] "=m"(*pDesiredThrottlePlateAngle)
        : [target_rpm] "m"(*pTargetRpm)
        : "d4", "a2", "cc", "memory"
    );
#endif
}

void selfTestRevMatch(void)
{
    char* module = "RevMatch";

    // TODO: use logger to find the actual speed sensor value at 7000 RPM in first gear
    getTargetRpm(SpeedToRpmFactorArray[0], 3600);
    assert(7000 * 5.12, *pTargetRpm, module, "Target RPM calculation for first gear at 30 MPH");

    // normal conditions
    comparePrimaryAndRedundantThrottlePositionsInvoked = 0;
    ComparePrimaryAndRedundantThrottlePositions();
    assert(1, comparePrimaryAndRedundantThrottlePositionsInvoked, module, "ComparePrimaryAndRedundantThrottlePositions test logic");
}