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
static inline __attribute__((always_inline))
void setTargetRpm(
    unsigned short ratio,
    unsigned short vehicle_speed)
{
    *pTargetRpm = (ratio >> 6) * (vehicle_speed >> 6);
}

// This is factored out to enable it to be called from two places.
static inline __attribute__((always_inline))
void lookupThrottleBladeAngle(
    unsigned short *pRpmToThrottleBladeAngleTable,
    unsigned short *pInputRpm,
    unsigned short *pOutputThrottleBladeAngle)
{
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
        "tblu.w   (%[table],%%d4.w),%%d4  \n\t"
        "move.w   %%d4,%[throttle_angle]  \n\t"
        : [throttle_angle] "=m"(*pOutputThrottleBladeAngle)
        : [target_rpm] "m"(*pInputRpm),
          [table] "a"(pRpmToThrottleBladeAngleTable)
        : "d4", "a2", "cc", "memory"
    );
#else
    // For testing on non-m68k platforms, implement table lookup with interpolation in code.
    unsigned short target_rpm = *pInputRpm;
    int low_index = target_rpm / 500;
    if (low_index < 0) { *pOutputThrottleBladeAngle = pRpmToThrottleBladeAngleTable[0]; return; }
    if (low_index >= 15) { *pOutputThrottleBladeAngle = pRpmToThrottleBladeAngleTable[15]; return; }
    int high_index = low_index + 1;
    int low_value = pRpmToThrottleBladeAngleTable[low_index];
    int high_value = pRpmToThrottleBladeAngleTable[high_index];

    // Linear interpolation
    int fraction = target_rpm % 500;
    *pOutputThrottleBladeAngle = low_value + ((high_value - low_value) * fraction) / 500;
#endif
}

__attribute__((section(".code.implementation")))
void throttlePatch(void)
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

        // Determine the maximum allowable throttle blade angle based on the current gear.
        *pMaxThrottle = PERCENTAGE(100);
        if (*pCurrentGear >= 0 && *pCurrentGear < 6)
        {
            unsigned short *pThrottleLimitTable = PerGearThrottleLimit[*pCurrentGear - 1];
            lookupThrottleBladeAngle(pThrottleLimitTable, pEngineSpeed, pMaxThrottle);
        }

        // If the clutch is pressed, use the clutch-specific throttle limit.
        if (*pCurrentGear == CLUTCH) 
        {
            *pMaxThrottle = ClutchThrottleLimit[*pVehicleSpeed >> 6];
        }

        // Global maximum throttle blade angle, to simplify life with LS3-style throttle bodies.
        // This way you don't need to put 97% everywhere in the tables where you really mean 100%.
        if (*pMaxThrottle > MaximumThrottleBladeAngle)
        {
            *pMaxThrottle = MaximumThrottleBladeAngle;
        }

        // Actually enforce the limit.
        if (*pDesiredThrottlePlateAngle > *pMaxThrottle)
        {
            *pDesiredThrottlePlateAngle = *pMaxThrottle;
        }
        return;
    }

    // Rev match conditions are met.
    // For now, we'll just command a fixed throttle blade angle.
    *pDesiredThrottlePlateAngle = FixedThrottleBladeAngle;

    // But we'll also calculate the target RPM for rev matching.
    unsigned short gear_index = *pPreviousGear;
    unsigned short ratio = SpeedToRpmFactorArray[gear_index - 1];
    setTargetRpm(ratio, *pVehicleSpeed);

    // Look up the desired throttle blade angle based on the target RPM.
    lookupThrottleBladeAngle(RpmToThrottleBladeAngle, pTargetRpm, pTestDesiredThrottlePlateAngle);
}

void selfTestThrottlePatch(void)
{
    char* module = "Throttle";

    // TODO, logging: find the actual speed sensor value at 7000 RPM in first gear
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
    *pDesiredThrottlePlateAngle = PERCENTAGE(25);
    throttlePatch();
    assert(1, comparePrimaryAndRedundantThrottlePositionsInvoked, module, "Normal: Rev match logic does not interfere with normal throttle logic");
    assert(0, *pTargetRpm, module, "Normal: Target RPM should not be set when rev matching not active");
    assert(PERCENTAGE(25), *pDesiredThrottlePlateAngle, module, "Normal: Desired throttle plate angle should not be modified when rev matching not active");

    // Do not engage rev match logic when clutch is pressed and accelerator pedal is NOT pressed
    comparePrimaryAndRedundantThrottlePositionsInvoked = 0;
    *pPreviousGear = 1;
    *pCurrentGear = CLUTCH;
    *pPedalPosition = MinimumThrottlePedalPosition - 1;
    *pTargetRpm = 1000;
    *pDesiredThrottlePlateAngle = PERCENTAGE(25);
    throttlePatch();
    assert(1, comparePrimaryAndRedundantThrottlePositionsInvoked, module, "Clutch: Rev match logic does not interfere with normal throttle logic");
    assert(0, *pTargetRpm, module, "Clutch: Target RPM should not be set when rev matching not active");
    assert(PERCENTAGE(25), *pDesiredThrottlePlateAngle, module, "Clutch: Desired throttle plate angle should not be modified when rev matching not active");

    // Do not engage rev match logic when accelerator pedal is pressed and clutch is not pressed
    comparePrimaryAndRedundantThrottlePositionsInvoked = 0;
    *pPreviousGear = 1;
    *pCurrentGear = 2;
    *pPedalPosition = MinimumThrottlePedalPosition + 1;
    *pTargetRpm = 1000;
    *pDesiredThrottlePlateAngle = PERCENTAGE(25);
    throttlePatch();
    assert(1, comparePrimaryAndRedundantThrottlePositionsInvoked, module, "Accelerator: Rev match logic does not interfere with normal throttle logic");
    assert(0, *pTargetRpm, module, "Accelerator: Target RPM should not be set when rev matching not active");
    assert(PERCENTAGE(25), *pDesiredThrottlePlateAngle, module, "Accelerator: Desired throttle plate angle should not be modified when rev matching not active");

    // Do not engage rev match logic from first gear
    comparePrimaryAndRedundantThrottlePositionsInvoked = 0;
    *pPreviousGear = 0;
    *pCurrentGear = CLUTCH;
    *pPedalPosition = MinimumThrottlePedalPosition + 1;
    *pTargetRpm = 1000;
    *pDesiredThrottlePlateAngle = PERCENTAGE(25);
    throttlePatch();
    assert(1, comparePrimaryAndRedundantThrottlePositionsInvoked, module, "First gear: Rev match logic does not interfere with normal throttle logic");
    assert(0, *pTargetRpm, module, "First gear: Target RPM should not be set when rev matching not active");
    assert(PERCENTAGE(25), *pDesiredThrottlePlateAngle, module, "First gear: Desired throttle plate angle should not be modified when rev matching not active");

    // Do not engage rev match logic from higher-than-6th gear
    comparePrimaryAndRedundantThrottlePositionsInvoked = 0;
    *pPreviousGear = 6;
    *pCurrentGear = CLUTCH;
    *pPedalPosition = MinimumThrottlePedalPosition + 1;
    *pTargetRpm = 1000;
    *pDesiredThrottlePlateAngle = PERCENTAGE(25);
    throttlePatch();
    assert(1, comparePrimaryAndRedundantThrottlePositionsInvoked, module, "Higher-than-6th: Rev match logic does not interfere with normal throttle logic");
    assert(0, *pTargetRpm, module, "Higher-than-6th: Target RPM should not be set when rev matching not active");
    assert(PERCENTAGE(25), *pDesiredThrottlePlateAngle, module, "Higher-than-6th: Desired throttle plate angle should not be modified when rev matching not active");

    // Engage when clutch and accelerator pedal are pressed
    comparePrimaryAndRedundantThrottlePositionsInvoked = 0;
    *pPreviousGear = 1;
    *pCurrentGear = CLUTCH;
    *pPedalPosition = MinimumThrottlePedalPosition + 1;
    *pTargetRpm = 1000;
    *pDesiredThrottlePlateAngle = PERCENTAGE(25);
    *pVehicleSpeed = 3600;
    throttlePatch();
    assert(0, comparePrimaryAndRedundantThrottlePositionsInvoked, module, "RevMatch: Rev match logic skips default code");
    assert(7000 * 5.12, *pTargetRpm, module, "RevMatch: Target RPM calculated");
    assert(FixedThrottleBladeAngle, *pDesiredThrottlePlateAngle, module, "RevMatch: Desired throttle plate angle should be fixed for now");
    assert(1638, *pTestDesiredThrottlePlateAngle, module, "RevMatch: Compute the real throttle plate angle");
    
    // Show that throttle is limited by the first-gear throttle limit table
    comparePrimaryAndRedundantThrottlePositionsInvoked = 0;
    *pPreviousGear = 0;
    *pCurrentGear = 0;
    *pPedalPosition = PERCENTAGE(100);
    *pTargetRpm = 0;
    *pDesiredThrottlePlateAngle = PERCENTAGE(100);
    *pVehicleSpeed = 3600;
    printf("Before throttlePatch: DesiredThrottlePlateAngle = %d\n", *pDesiredThrottlePlateAngle);
    throttlePatch();
    assert(1, comparePrimaryAndRedundantThrottlePositionsInvoked, module, "First gear limit: Rev match logic does not interfere with normal throttle logic");
    assert(0, *pTargetRpm, module, "First gear limit: Target RPM should not be set when rev matching not active");
    assert(PERCENTAGE(50), *pDesiredThrottlePlateAngle, module, "First gear limit: Desired throttle plate angle should not be modified when rev matching not active");
}