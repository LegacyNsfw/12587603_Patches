#include "globals.h"
#include "calibration.h"
#include "selftest.h"

void (*ComparePrimaryAndRedundantThrottlePositions)(void) = (void (*)(void)) 0x20A3A;

static inline __attribute__((always_inline)) void getTargetRpm(
    unsigned short ratio,
    unsigned short vehicle_speed)
{
    RAM_TARGET_RPM_UINT = (ratio >> 6) * (vehicle_speed >> 6);
}

__attribute__((section(".code.implementation")))
void revMatch(void)
{
    // If the clutch has not been pressed, update the "previous" gear to be the current gear.
    // This ensures that the previous gear is available even when the clutch is pressed.
    if (RAM_CURRENT_GEAR_BYTE != 8)
    {
        RAM_PREVIOUS_GEAR_BYTE = RAM_CURRENT_GEAR_BYTE;
    }

    // If rev match conditions are not met, just run the normal throttle logic.
    if (RAM_PREVIOUS_GEAR_BYTE > 5  || // we don't support downshifting from gears higher than 5 (sixth)
        RAM_PREVIOUS_GEAR_BYTE == 0 || // we can't downshift from first
        RAM_CURRENT_GEAR_BYTE != 8 || // clutch is not pressed
        RAM_PEDAL_POSITION_UINT < ROM_MINIMUM_THROTTLE_PEDAL_UINT)
    {
        // This function was invoked in place of ComparePrimaryAndRedundantThrottlePositions(),
        // so under normal conditions we'll just call that function and return.
        ComparePrimaryAndRedundantThrottlePositions();
        return;
    }

    // Rev match conditions are met.
    // For now, we'll just command a fixed throttle blade angle.
    RAM_DESIRED_THROTTLE_PLATE_ANGLE_UINT = ROM_TEMPORARY_FIXED_THROTTLE_BLADE_ANGLE_UINT;

    // But we'll also calculate the target RPM for rev matching.
    unsigned short gear_index = RAM_CURRENT_GEAR_BYTE;
    unsigned short ratio = ROM_MPH_TO_RPM_FACTOR[gear_index - 1];
    getTargetRpm(ratio, RAM_VEHICLE_SPEED_UINT);

    // TODO: look up target throttle plate angle based on the target RPM.
}

void selfTestRevMatch(void)
{
    char* module = "RevMatch";
    // TODO: use logger to find the speed sensor value at 7000 RPM in first gear
    getTargetRpm(ROM_MPH_TO_RPM_FACTOR[0], 30);
    assert(7000, RAM_TARGET_RPM_UINT, module, "Target RPM calculation for first gear at 30 MPH");
}