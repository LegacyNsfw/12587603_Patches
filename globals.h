#ifdef DEFINE_HOST_RAM
    #define EXTERN
#else
    #define EXTERN extern
#endif

#ifdef __m68k__
    #ifdef DEFINE_HOST_RAM
        #define UINT_AT_ADDRESS(name, addr) unsigned short *name = (unsigned short *)(addr);
        #define BYTE_AT_ADDRESS(name, addr) unsigned char *name = (unsigned char *)(addr);
    #else
        #define UINT_AT_ADDRESS(name, addr) extern unsigned short *name;
        #define BYTE_AT_ADDRESS(name, addr) extern unsigned char *name;
    #endif
#else
    #ifdef DEFINE_HOST_RAM
        #define UINT_AT_ADDRESS(name, addr) unsigned short ram_##addr; EXTERN unsigned short *name = &ram_##addr;
        #define BYTE_AT_ADDRESS(name, addr) unsigned char ram_##addr; EXTERN unsigned char *name = &ram_##addr;
    #else
        #define UINT_AT_ADDRESS(name, addr) extern unsigned short *name;
        #define BYTE_AT_ADDRESS(name, addr) extern unsigned char *name;
    #endif
#endif

///////////////////////////////////////////////////////////////////////////////
// Accelerator pedal position.
//
// Units: TODO: find out accelerator pedal position units by logging the raw value.
// Data type: 16 bit unsigned
//
// Cross-reference: 
//
UINT_AT_ADDRESS(PedalPositionPtr, 0xFF900A)

///////////////////////////////////////////////////////////////////////////////
// Current gear.
//
// 0 = first gear
// 1 = second gear
// etc, etc
// 8 = clutch pressed
BYTE_AT_ADDRESS(pCurrentGear, 0xFF95DC)

///////////////////////////////////////////////////////////////////////////////
// Previous gear.
//
// Same semantics as pCurrentGear, but will never transition to 8
// (clutch pressed), so it always identifies the previously engaged gear.
//
// TODO: Confirm that we can actually use this address to store the previous gear!
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
UINT_AT_ADDRESS(pDesiredThrottlePlateAngle, 0xFF9050)

///////////////////////////////////////////////////////////////////////////////
// Target engine RPM for rev matching.
//
// Units: RPM with what conversion factor? TODO
// Data type: 16 bit unsigned
//
// TODO: Confirm that this address is truly not used
// AF04 was previously used by automatic-transmission logic.
UINT_AT_ADDRESS(pTargetRpm, 0xFFAF04)

///////////////////////////////////////////////////////////////////////////////
// vehicle speed (actually transmission output shaft speed)
//
// Units: TODO: Determine the appropriate conversion factor for transmission output shaft speed to MPH.
// Data type: 16 bit unsigned
//
// Cross-reference: Code at 0x084190 determines current gear from RPM and MPH
UINT_AT_ADDRESS(pVehicleSpeed, 0xFFA3BE)
