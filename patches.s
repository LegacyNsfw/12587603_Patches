| Specify CPU32 instruction set
.cpu cpu32

| RAM Addresses for global variables
|
| MAF in grams per second
| This is stored in 1/128ths of a gram per second.
| So, divide this value by 128 to get the actual MAF in grams per second.
| And multiply a grams-per-second value by 128 to get the PCM's units.

| raw value x 25/32 = centigrams per second

.equ MAF_ADDRESS, 0xffffa0dc

| ROM Addresses
|
| Address of the (existing) high-MAF EOIT table
.equ DEFAULT_EOIT_TABLE, 0xE27C

|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
| Location of the EOIT tblu.w instruction in the original code
        .section .code.hook.eoit,"ax",@progbits
PATCH_HOOK_EOIT_START:
        jsr     LowMafEoitPatch
        | This nop is necessary to pave the way to the next instruction in the
        | original code.
        nop
PATCH_HOOK_EOIT_END: 
        | We need to have something here for the END label to apply to.
        nop

|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
| Base address for patch code
        .section .code.implementation
PATCH_CODE_START:

LowMafEoitPatch:
        | Use a modified EOIT table when MAF is below a certain threshold
        | The original code has a tblu.w(TABLE).l,d0 instruction with ECT in d0.
        | We'll replace that with a JSR to this code.
       
        | Move MAF value into d1
        move.w  (MAF_ADDRESS).w,%d1
        | Compare with low MAF threshold value
        cmp.w   (LOW_MAF_THRESHOLD_VALUE).l,%d1
        | If MAF is below or equal to threshold, use low MAF table
        ble     use_low_maf_table
        tblu.w  (DEFAULT_EOIT_TABLE).l,%d0
        rts
use_low_maf_table:
        tblu.w  (LOW_MAF_EOIT_TABLE).l,%d0
        rts
PATCH_CODE_END:
        nop

|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
| Base address for patch data
        .section .data.tables
PATCH_DATA_START:
LOW_MAF_THRESHOLD_VALUE: 
| Constant value for low MAF threshold (0x1900 = 50 g/s)
        .word   0x1900
LOW_MAF_EOIT_TABLE:
        .word   0x28d | -40
        .word   0x28d | -28
        .word   0x28d | -16
        .word   0x28d | -4
        .word   0x28d | 8
        .word   0x38d | 20
        .word   0x48d | 32
        .word   0x48d | 44
        .word   0x58d | 56
        .word   0x58d | 68
        .word   0x58d | 80
        .word   0x58d | 92
        .word   0x58d | 104
        .word   0x58d | 116
        .word   0x58d | 128
        .word   0x58d | 140
PATCH_DATA_END:
        .word   0x1234
