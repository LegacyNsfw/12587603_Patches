#include "globals.h"
#include "calibration.h"
#include "selftest.h"

void (*ComparePrimaryAndRedundantThrottlePositions)(void) = (void (*)(void)) 0x20A3A;

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
    // This ensures that the previous gear is available even when the clutch is pressed.
    if (*pCurrentGear != 8)
    {
        *pPreviousGear = *pCurrentGear;
    }

    // If rev match conditions are not met, just run the normal throttle logic.
    if (*pPreviousGear > 5  || // we don't support downshifting from gears higher than 5 (sixth)
        *pPreviousGear == 0 || // we can't downshift from first
        *pCurrentGear != 8 || // clutch is not pressed
        *PedalPositionPtr < MinimumThrottlePedalPosition)
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

    // TODO: look up target throttle plate angle based on the target RPM.
}

void selfTestRevMatch(void)
{
    char* module = "RevMatch";

    // TODO: use logger to find the speed sensor value at 7000 RPM in first gear
    getTargetRpm(SpeedToRpmFactorArray[0], 30);
    assert(7000, *pTargetRpm, module, "Target RPM calculation for first gear at 30 MPH");
}