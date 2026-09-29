#define UINT_AT_ADDRESS(addr) (*(unsigned short *)(addr))

// TODO
#define RAM_PEDAL_POSITION_UINT UINT_AT_ADDRESS(0xFF0000)

///////////////////////////////////////////////////////////////////////////////
// Current gear.
//
// 0 = first gear
// 1 = second gear
// etc, etc
// 8 = clutch pressed
#define RAM_CURRENT_GEAR_BYTE UINT_AT_ADDRESS(0xFF95DC)

///////////////////////////////////////////////////////////////////////////////
// Previous gear.
//
// Same semantics as RAM_CURRENT_GEAR_BYTE, but will never transition to 8
// (clutch pressed), so it always identifies the previously engaged gear.
//
// TODO: Confirm that we can actually use this address!
// It was used by the automatic-transmission logic.
#define RAM_PREVIOUS_GEAR_BYTE UINT_AT_ADDRESS(0xFF95DC)

///////////////////////////////////////////////////////////////////////////////
// Desired throttle plate angle.
//
// Units: percentage * 51.2
// Data type: 16 bit unsigned
// 
// This is normally set by idle, cruise, or accelerator pedal logic, but those
// values will be overwritten in order to implement rev matching.
#define RAM_DESIRED_THROTTLE_PLATE_ANGLE_UINT UINT_AT_ADDRESS(0xFF9050)

///////////////////////////////////////////////////////////////////////////////
// Target engine RPM for rev matching.
//
// Units: RPM with what conversion factor? TODO
// Data type: 16 bit unsigned
//
// TODO: Confirm that this address is truly not used
// AF04 was previously used by automatic-transmission logic.
#define RAM_TARGET_RPM_UINT UINT_AT_ADDRESS(0xFFAF04)

///////////////////////////////////////////////////////////////////////////////
// Minimum throttle pedal position to trigger rev-match logic.
//
// Units: percentage / 51.2
// Data type: 16 bit unsigned
//
// The driver must press the throttle pedal slightly to trigger rev matching, 
// so that it never happens unexpectedly. This value specifies exactly what 
// "slightly" means.

unsigned short ROM_MINIMUM_THROTTLE_PEDAL_UINT
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
unsigned short ROM_TEMPORARY_FIXED_THROTTLE_BLADE_ANGLE_UINT
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
// compute the desired engine RPM for a given vehicle speed.
///////////////////////////////////////////////////////////////////////////////
unsigned short ROM_MPH_TO_RPM_FACTOR[]
    __attribute__((section(".data.tables"))) = {
        (unsigned short)(1000 / 1),
        (unsigned short)(2000 / 1),
        (unsigned short)(3000 / 1),
        (unsigned short)(4000 / 1),
        (unsigned short)(5000 / 1)
    };

void (*ComparePrimaryAndRedundantThrottlePositions)(void) = 0x20A3A;

void revMatch(void)
{
    if (RAM_CURRENT_GEAR_BYTE != 8) // Clutch not pressed
    {
        RAM_PREVIOUS_GEAR_BYTE = RAM_CURRENT_GEAR_BYTE;
    }

    // If rev match conditions are not met, just run the normal throttle logic.
    if (RAM_CURRENT_GEAR_BYTE > 5  || RAM_PEDAL_POSITION_UINT < ROM_MINIMUM_THROTTLE_PEDAL_UINT)
    {
        ComparePrimaryAndRedundantThrottlePositions();
        return;
    }

    // Rev match conditions are met.
    // For now, we'll just command a fixed throttle blade angle.
    RAM_DESIRED_THROTTLE_PLATE_ANGLE_UINT = ROM_TEMPORARY_FIXED_THROTTLE_BLADE_ANGLE_UINT;

}