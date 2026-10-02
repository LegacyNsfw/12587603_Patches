#include "calibration.h"

///////////////////////////////////////////////////////////////////////////////
// Minimum throttle pedal position to trigger rev-match logic.
//
// Units: percentage / 51.2
// Data type: 16 bit unsigned
//
// The driver must press the throttle pedal slightly to trigger rev matching, 
// so that it never happens unexpectedly. This value specifies exactly what 
// "slightly" means.

unsigned short const MinimumThrottlePedalPosition
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
unsigned short const FixedThrottleBladeAngle
    __attribute__((section(".data.tables")))
    = (unsigned short)(20 * 51.2);

///////////////////////////////////////////////////////////////////////////////
// Maximum throttle blade angle for non-rev-matching conditions
//
// Units: percentage / 51.2
// Data type: 16 bit unsigned
//
// Cross-reference: Table B2702 / ETC Max Throttle Position Vs. RPM
unsigned short const MaximumThrottleBladeAngle
    __attribute__((section(".data.tables")))
    = (unsigned short)(PERCENTAGE(97));

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
// RPM = 0xFFA560 (engine speed, filtered)
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
unsigned short const SpeedToRpmFactorArray[]
    __attribute__((section(".data.tables"))) = {

        RATIO(10),   // 1st gear
        RATIO(6.4),  // 2nd gear
        RATIO(4.65), // 3rd gear
        RATIO(3.4),  // 4th gear
        RATIO(2.56),  // 5th gear
        RATIO(1.78)   // 6th gear - hey wait you can't downshift into this gear
    };

///////////////////////////////////////////////////////////////////////////////
//
// Specifies the throttle blade angle that will produce the desired RPM.
// 
// Cross-reference: B2702, ETC Max Throttle Position vs. RPM
//
///////////////////////////////////////////////////////////////////////////////

unsigned short const RpmToThrottleBladeAngle[17]
    __attribute__((section(".data.tables"))) = {
        PERCENTAGE(10),   // 0
        PERCENTAGE(10),   // 500
        PERCENTAGE(10),   // 1000
        PERCENTAGE(12),   // 1500
        PERCENTAGE(13),   // 2000
        PERCENTAGE(14),   // 2500
        PERCENTAGE(15),   // 3000
        PERCENTAGE(16),   // 3500
        PERCENTAGE(17),   // 4000
        PERCENTAGE(18),   // 4500
        PERCENTAGE(20),   // 5000
        PERCENTAGE(22),   // 5500
        PERCENTAGE(24),   // 6000
        PERCENTAGE(26),   // 6500
        PERCENTAGE(28),   // 7000
        PERCENTAGE(30),   // 7500
        PERCENTAGE(32),   // 8000
    };

unsigned short const PerGearThrottleLimit[6][17]
    __attribute__((section(".data.tables"))) = {
        // First gear
        {
            PERCENTAGE(50),   // 0
            PERCENTAGE(50),   // 500
            PERCENTAGE(50),   // 1000
            PERCENTAGE(50),   // 1500
            PERCENTAGE(50),   // 2000
            PERCENTAGE(50),   // 2500
            PERCENTAGE(50),   // 3000
            PERCENTAGE(50),   // 3500
            PERCENTAGE(50),   // 4000
            PERCENTAGE(50),   // 4500
            PERCENTAGE(50),   // 5000
            PERCENTAGE(50),   // 5500
            PERCENTAGE(50),   // 6000
            PERCENTAGE(50),   // 6500
            PERCENTAGE(50),   // 7000
            PERCENTAGE(50),   // 7500
            PERCENTAGE(50),   // 8000
        },
        // Second gear
        {
            PERCENTAGE(75),   // 0
            PERCENTAGE(75),   // 500
            PERCENTAGE(75),   // 1000
            PERCENTAGE(75),   // 1500
            PERCENTAGE(75),   // 2000
            PERCENTAGE(75),   // 2500
            PERCENTAGE(75),   // 3000
            PERCENTAGE(75),   // 3500
            PERCENTAGE(75),   // 4000
            PERCENTAGE(75),   // 4500
            PERCENTAGE(75),   // 5000
            PERCENTAGE(75),   // 5500
            PERCENTAGE(75),   // 6000
            PERCENTAGE(75),   // 6500
            PERCENTAGE(75),   // 7000
            PERCENTAGE(75),   // 7500
            PERCENTAGE(75),   // 8000
        },
        // Third gear
        {
            PERCENTAGE(100),   // 0
            PERCENTAGE(100),   // 500
            PERCENTAGE(100),   // 1000
            PERCENTAGE(100),   // 1500
            PERCENTAGE(100),   // 2000
            PERCENTAGE(100),   // 2500
            PERCENTAGE(100),   // 3000
            PERCENTAGE(100),   // 3500
            PERCENTAGE(100),   // 4000
            PERCENTAGE(100),   // 4500
            PERCENTAGE(100),   // 5000
            PERCENTAGE(100),   // 5500
            PERCENTAGE(100),   // 6000
            PERCENTAGE(100),   // 6500
            PERCENTAGE(100),   // 7000
            PERCENTAGE(100),   // 7500
            PERCENTAGE(100),   // 8000
        },
        // Fourth gear
        {
            PERCENTAGE(100),   // 0
            PERCENTAGE(100),   // 500
            PERCENTAGE(100),   // 1000
            PERCENTAGE(100),   // 1500
            PERCENTAGE(100),   // 2000
            PERCENTAGE(100),   // 2500
            PERCENTAGE(100),   // 3000
            PERCENTAGE(100),   // 3500
            PERCENTAGE(100),   // 4000
            PERCENTAGE(100),   // 4500
            PERCENTAGE(100),   // 5000
            PERCENTAGE(100),   // 5500
            PERCENTAGE(100),   // 6000
            PERCENTAGE(100),   // 6500
            PERCENTAGE(100),   // 7000
            PERCENTAGE(100),   // 7500
            PERCENTAGE(100),   // 8000
        },    
        // Fifth gear
        {
            PERCENTAGE(100),   // 0
            PERCENTAGE(100),   // 500
            PERCENTAGE(100),   // 1000
            PERCENTAGE(100),   // 1500
            PERCENTAGE(100),   // 2000
            PERCENTAGE(100),   // 2500
            PERCENTAGE(100),   // 3000
            PERCENTAGE(100),   // 3500
            PERCENTAGE(100),   // 4000
            PERCENTAGE(100),   // 4500
            PERCENTAGE(100),   // 5000
            PERCENTAGE(100),   // 5500
            PERCENTAGE(100),   // 6000
            PERCENTAGE(100),   // 6500
            PERCENTAGE(100),   // 7000
            PERCENTAGE(100),   // 7500
            PERCENTAGE(100),   // 8000
        },
        // Sixth gear
        {
            PERCENTAGE(100),   // 0
            PERCENTAGE(100),   // 500
            PERCENTAGE(100),   // 1000
            PERCENTAGE(100),   // 1500
            PERCENTAGE(100),   // 2000
            PERCENTAGE(100),   // 2500
            PERCENTAGE(100),   // 3000
            PERCENTAGE(100),   // 3500
            PERCENTAGE(100),   // 4000
            PERCENTAGE(100),   // 4500
            PERCENTAGE(100),   // 5000
            PERCENTAGE(100),   // 5500
            PERCENTAGE(100),   // 6000
            PERCENTAGE(100),   // 6500
            PERCENTAGE(100),   // 7000
            PERCENTAGE(100),   // 7500
            PERCENTAGE(100),   // 8000
        },
    };
