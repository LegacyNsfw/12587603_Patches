///////////////////////////////////////////////////////////////////////////////
// Minimum throttle pedal position to trigger rev-match logic.
//
// Units: percentage / 51.2
// Data type: 16 bit unsigned
//
// The driver must press the throttle pedal slightly to trigger rev matching, 
// so that it never happens unexpectedly. This value specifies exactly what 
// "slightly" means.

unsigned short MinimumThrottlePedalPosition
    __attribute__((section(".data.tables")))
    = (unsigned short)(5 * 51.2);

///////////////////////////////////////////////////////////////////////////////
// For temporary use, while validating the rest of the rev-match logic.
//
// Units: percentage / 51.2
// Data type: 16 bit unsigned
//
// Cross-reference: Table B2702 / ETC Max Throttle Position Vs. RPM
//
// We'll just command a fixed throttle blade angle until the rest of the logic
// is validated. Then we'll replace this with a properly calculated value.
unsigned short FixedThrottleBladeAngle
    __attribute__((section(".data.tables")))
    = (unsigned short)(20 * 51.2);

///////////////////////////////////////////////////////////////////////////////
// Conversion factors from vehicle speed (MPH) to engine RPM.
//
// Data type: 16 bit unsigned
// Units: RPM per MPH * 2^12
//
// Cross-reference: Gear ratio thresholds table, 0x019668
//                  Code at 0x084190
//
// To detect the current gear, the factory code does this:
// RPM = 0xFFA560
// MPH = 0xFFa3BE (actually transmission output speed)
// ratio = RPM << 12 / MPH
//
// Then it compares the resulting ratio against the values in the gear ratio
// thresholds table.
//
// For rev matching, reversing that match and using a gear-specific ratio will
// compute the desired engine RPM for a given vehicle speed:
//
// RPM = (gear_specific_ratio * vehicle speed) >> 12
///////////////////////////////////////////////////////////////////////////////
#define RATIO(x) ((unsigned short)(x * 4096))
unsigned short SpeedToRpmFactorArray[]
    __attribute__((section(".data.tables"))) = {

        (unsigned short)RATIO(10),   // 1st gear
        (unsigned short)RATIO(6.4),  // 2nd gear
        (unsigned short)RATIO(5.65), // 3rd gear
        (unsigned short)RATIO(3.4),  // 4th gear
        (unsigned short)RATIO(2.4),  // 5th gear
        (unsigned short)RATIO(1.8)   // 6th gear
    };
