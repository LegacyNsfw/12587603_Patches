/* Linker script for firmware patches */

MEMORY
{
    HOOK_EOIT (rx)    : ORIGIN = 0x32778, LENGTH = 0x100
    CODE (rx)    : ORIGIN = 0x90000, LENGTH = 0x10000  
    DATA (rw)    : ORIGIN = 0xa0000, LENGTH = 0x10000
}

SECTIONS
{
    /* Hook section at original firmware location */
    .patch_hook_eoit : {
        *(.code.hook.eoit)
        KEEP(*(.code.hook.eoit*))
    } >HOOK_EOIT
    
    /* Main patch code section */    
    .patch_code : {
        *(.code.implementation)
        KEEP(*(.code.implementation*))
        *(.text)
    } >CODE
    
    /* Patch data section */
    .patch_data : {
        *(.data.tables)
        KEEP(*(.data.tables*))
        *(.data)
    } >DATA
    
    /* Discard unwanted sections */
    /DISCARD/ : {
        *(.note*)
        *(.comment*)
        *(.eh_frame*)
    }
}
