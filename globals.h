#ifdef DEFINE_RAM_ADDRESSES
    #ifdef __m68k__
        #define UINT_AT_ADDRESS(name, addr) unsigned short *name = (unsigned short *)(addr);
        #define BYTE_AT_ADDRESS(name, addr) unsigned char *name = (unsigned char *)(addr);
    #else
        #define UINT_AT_ADDRESS(name, addr) unsigned short ram_##addr; unsigned short *name = &ram_##addr;
        #define BYTE_AT_ADDRESS(name, addr) unsigned char ram_##addr; unsigned char *name = &ram_##addr;
    #endif
#else
        #define UINT_AT_ADDRESS(name, addr) extern unsigned short *name;
        #define BYTE_AT_ADDRESS(name, addr) extern unsigned char *name;
#endif

///////////////////////////////////////////////////////////////////////////////
// Accelerator pedal position.
//
// Units: TODO, static: find out accelerator pedal position units by logging the raw value at 0xFF900A.
// Data type: 16 bit unsigned
//
// Cross-reference: PID 131F
//
UINT_AT_ADDRESS(pPedalPosition, 0xFF900A)

///////////////////////////////////////////////////////////////////////////////
// Current gear.
//
// 0 = first gear
// 1 = second gear
// etc, etc
// 8 = clutch pressed
//
BYTE_AT_ADDRESS(pCurrentGear, 0xFFA3B8)
#define CLUTCH 8

///////////////////////////////////////////////////////////////////////////////
// Previous gear.
//
// Same semantics as pCurrentGear, but will never transition to 8
// (clutch pressed), so it always identifies the previously engaged gear.
//
// TODO, logging: Confirm that we can actually use this address to store the previous gear!
// It was used by the automatic-transmission logic.
BYTE_AT_ADDRESS(pPreviousGear, 0xFF95DC)

///////////////////////////////////////////////////////////////////////////////
// Desired throttle plate angle.
//
// Units: percentage * 51.2
// Data type: 16 bit unsigned
// 
// This is normally set by idle, cruise, or accelerator pedal logic, but those
// values will be overwritten in order to implement rev matching.
//
// Uncomment to actually overwrite the throttle plate angle:
// UINT_AT_ADDRESS(pDesiredThrottlePlateAngle, 0xFF9050)
//
// TODO, logging:Confirm that this address is not used by manual-transmission logic.
UINT_AT_ADDRESS(pDesiredThrottlePlateAngle, 0xFFAF02)

///////////////////////////////////////////////////////////////////////////////
// Target engine RPM for rev matching.
//
// Units: RPM * 5.12
// Data type: 16 bit unsigned
//
// TODO, logging: Confirm that this address is truly not used by manual-transmission logic.
// AF04 was previously used by automatic-transmission logic.
UINT_AT_ADDRESS(pTargetRpm, 0xFFAF04)

///////////////////////////////////////////////////////////////////////////////
// vehicle speed (actually transmission output shaft speed)
//
// Units: TODO, logging: Determine the appropriate conversion factor for transmission output shaft speed to MPH.
// Data type: 16 bit unsigned
//
// Cross-reference: Code at 0x084190 determines current gear from RPM and MPH
UINT_AT_ADDRESS(pVehicleSpeed, 0xFFA3BE)

///////////////////////////////////////////////////////////////////////////////
// Engine speed
//
// Units: RPM * 5.12
// Data type: 16 bit unsigned
// Cross-reference: Table C2907 Min RPM for MAF test, code at 202D8
//
// Raw engine speed is at FFA562 - this version is low-pass-filtered.
UINT_AT_ADDRESS(pEngineSpeed, 0xFFA560)
