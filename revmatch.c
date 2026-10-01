#include "globals.h"
#include "calibration.h"
#include "selftest.h"

int comparePrimaryAndRedundantThrottlePositionsInvoked = 0;

#ifdef __m68k__
void (*ComparePrimaryAndRedundantThrottlePositions)(void) = (void (*)(void)) 0x20A3A;
#else
void ComparePrimaryAndRedundantThrottlePositions(void) { comparePrimaryAndRedundantThrottlePositionsInvoked = 1; }
#endif

// This is factored out to support unit testing.
static inline __attribute__((always_inline)) void setTargetRpm(
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
        *pCurrentGear != CLUTCH || // clutch is not pressed
        *pPedalPosition < MinimumThrottlePedalPosition)
    {
        // This function was invoked in place of ComparePrimaryAndRedundantThrottlePositions(),
        // so under normal conditions we'll just call that function and return.
        ComparePrimaryAndRedundantThrottlePositions();
        *pTargetRpm = 0;
        return;
    }

    // Rev match conditions are met.
    // For now, we'll just command a fixed throttle blade angle.
    *pDesiredThrottlePlateAngle = FixedThrottleBladeAngle;

    // But we'll also calculate the target RPM for rev matching.
    unsigned short gear_index = *pPreviousGear;
    unsigned short ratio = SpeedToRpmFactorArray[gear_index - 1];
    setTargetRpm(ratio, *pVehicleSpeed);

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
    setTargetRpm(SpeedToRpmFactorArray[0], 3600);
    assert(7000 * 5.12, *pTargetRpm, module, "Target RPM calculation for first gear at 30 MPH");

    // Prove that the test logic itself actually works
    comparePrimaryAndRedundantThrottlePositionsInvoked = 0;
    ComparePrimaryAndRedundantThrottlePositions();
    assert(1, comparePrimaryAndRedundantThrottlePositionsInvoked, module, "ComparePrimaryAndRedundantThrottlePositions test logic");

    // Prove that rev match logic doesn't interfere with normal throttle logic.
    comparePrimaryAndRedundantThrottlePositionsInvoked = 0;
    *pPreviousGear = 1;
    *pCurrentGear = 2;
    *pPedalPosition = MinimumThrottlePedalPosition - 1;
    *pTargetRpm = 1000;
    *pDesiredThrottlePlateAngle = 1234;
    revMatch();
    assert(1, comparePrimaryAndRedundantThrottlePositionsInvoked, module, "Normal: Rev match logic does not interfere with normal throttle logic");
    assert(0, *pTargetRpm, module, "Normal: Target RPM should not be set when rev matching not active");
    assert(1234, *pDesiredThrottlePlateAngle, module, "Normal: Desired throttle plate angle should not be modified when rev matching not active");

    // Do not engage rev match logic when clutch is pressed and accelerator pedal is NOT pressed
    comparePrimaryAndRedundantThrottlePositionsInvoked = 0;
    *pPreviousGear = 1;
    *pCurrentGear = CLUTCH;
    *pPedalPosition = MinimumThrottlePedalPosition - 1;
    *pTargetRpm = 1000;
    *pDesiredThrottlePlateAngle = 1234;
    revMatch();
    assert(1, comparePrimaryAndRedundantThrottlePositionsInvoked, module, "Clutch: Rev match logic does not interfere with normal throttle logic");
    assert(0, *pTargetRpm, module, "Clutch: Target RPM should not be set when rev matching not active");
    assert(1234, *pDesiredThrottlePlateAngle, module, "Clutch: Desired throttle plate angle should not be modified when rev matching not active");

    // Do not engage rev match logic when accelerator pedal is pressed and clutch is not pressed
    comparePrimaryAndRedundantThrottlePositionsInvoked = 0;
    *pPreviousGear = 1;
    *pCurrentGear = 2;
    *pPedalPosition = MinimumThrottlePedalPosition + 1;
    *pTargetRpm = 1000;
    *pDesiredThrottlePlateAngle = 1234;
    revMatch();
    assert(1, comparePrimaryAndRedundantThrottlePositionsInvoked, module, "Accelerator: Rev match logic does not interfere with normal throttle logic");
    assert(0, *pTargetRpm, module, "Accelerator: Target RPM should not be set when rev matching not active");
    assert(1234, *pDesiredThrottlePlateAngle, module, "Accelerator: Desired throttle plate angle should not be modified when rev matching not active");

    // Do not engage rev match logic from first gear
    comparePrimaryAndRedundantThrottlePositionsInvoked = 0;
    *pPreviousGear = 0;
    *pCurrentGear = CLUTCH;
    *pPedalPosition = MinimumThrottlePedalPosition + 1;
    *pTargetRpm = 1000;
    *pDesiredThrottlePlateAngle = 1234;
    revMatch();
    assert(1, comparePrimaryAndRedundantThrottlePositionsInvoked, module, "First gear: Rev match logic does not interfere with normal throttle logic");
    assert(0, *pTargetRpm, module, "First gear: Target RPM should not be set when rev matching not active");
    assert(1234, *pDesiredThrottlePlateAngle, module, "First gear: Desired throttle plate angle should not be modified when rev matching not active");

    // Do not engage rev match logic from higher-than-6th gear
    comparePrimaryAndRedundantThrottlePositionsInvoked = 0;
    *pPreviousGear = 6;
    *pCurrentGear = CLUTCH;
    *pPedalPosition = MinimumThrottlePedalPosition + 1;
    *pTargetRpm = 1000;
    *pDesiredThrottlePlateAngle = 1234;
    revMatch();
    assert(1, comparePrimaryAndRedundantThrottlePositionsInvoked, module, "Higher-than-6th: Rev match logic does not interfere with normal throttle logic");
    assert(0, *pTargetRpm, module, "Higher-than-6th: Target RPM should not be set when rev matching not active");
    assert(1234, *pDesiredThrottlePlateAngle, module, "Higher-than-6th: Desired throttle plate angle should not be modified when rev matching not active");

    // Engage when clutch and accelerator pedal are pressed
    comparePrimaryAndRedundantThrottlePositionsInvoked = 0;
    *pPreviousGear = 1;
    *pCurrentGear = CLUTCH;
    *pPedalPosition = MinimumThrottlePedalPosition + 1;
    *pTargetRpm = 1000;
    *pDesiredThrottlePlateAngle = 1234;
    *pVehicleSpeed = 3600;
    revMatch();
    assert(0, comparePrimaryAndRedundantThrottlePositionsInvoked, module, "RevMatch: Rev match logic skips default code");
    assert(7000 * 5.12, *pTargetRpm, module, "RevMatch: Target RPM calculated");
    assert(FixedThrottleBladeAngle, *pDesiredThrottlePlateAngle, module, "RevMatch: Desired throttle plate angle should be modified when rev matching is active");
}