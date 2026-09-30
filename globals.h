#ifdef __m68k__
#define UINT_AT_ADDRESS(addr) (*(unsigned short *)(addr))
#else
#define UINT_AT_ADDRESS(addr) host_ram_##addr
#endif

///////////////////////////////////////////////////////////////////////////////
// Accelerator pedal position.
//
// Units: TODO: find out accelerator pedal position units by logging the raw value.
// Data type: 16 bit unsigned
//
// Cross-reference: 
//
#define RAM_PEDAL_POSITION_UINT UINT_AT_ADDRESS(0xFF900A)

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
// TODO: Confirm that we can actually use this address to store the previous gear!
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
// vehicle speed (actually transmission output shaft speed)
//
// Units: TODO: Determine the appropriate conversion factor for transmission output shaft speed to MPH.
// Data type: 16 bit unsigned
//
// Cross-reference: Code at 0x084190 determines current gear from RPM and MPH
#define RAM_VEHICLE_SPEED_UINT UINT_AT_ADDRESS(0xFFA3BE)

#ifndef __m68k__
#if defined(DEFINE_HOST_RAM)
#define EXTERN
#else
#define EXTERN extern
#endif

EXTERN unsigned short UINT_AT_ADDRESS(0xFF900A);
EXTERN unsigned short UINT_AT_ADDRESS(0xFF95DC);
EXTERN unsigned short UINT_AT_ADDRESS(0xFF9050);
EXTERN unsigned short UINT_AT_ADDRESS(0xFFAF04);
EXTERN unsigned short UINT_AT_ADDRESS(0xFFA3BE);
#endif
    