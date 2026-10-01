.PHONY: patches selftest clean

ifeq ($(OS),Windows_NT)
M68K_PREFIX := m68k-elf
else
M68K_PREFIX := m68k-linux-gnu
endif

AS      := $(M68K_PREFIX)-as
CC      := $(M68K_PREFIX)-gcc
LD      := $(M68K_PREFIX)-ld
OBJDUMP := $(M68K_PREFIX)-objdump
HOST_CC := gcc

ASFLAGS := -mcpu=cpu32 -g
CFLAGS  := -mcpu=cpu32 -ffreestanding -c -g

PATCH_OBJECTS := patches_debug.o throttle.o calibration.o main.o
PATCH_OUTPUTS := patches.elf patches_disasm.txt patches_hexdump.txt patches_full.txt

patches: $(PATCH_OUTPUTS)

selftest: main.c throttle.c calibration.c globals.h calibration.h selftest.h
	$(HOST_CC) -g -o $@ main.c throttle.c calibration.c

patches_debug.o: patches.s
	$(AS) $(ASFLAGS) -o $@ $<

throttle.o: throttle.c globals.h calibration.h selftest.h
	$(CC) $(CFLAGS) -o $@ $<

calibration.o: calibration.c
	$(CC) $(CFLAGS) -o $@ $<

main.o: main.c globals.h selftest.h
	$(CC) $(CFLAGS) -o $@ $<

patches.elf: $(PATCH_OBJECTS) patches.ld
	$(LD) -T patches.ld -Map=patches.map -o $@ $(PATCH_OBJECTS)

patches_disasm.txt: patches.elf
	$(OBJDUMP) -S -d $< > $@

patches_hexdump.txt: patches.elf
	$(OBJDUMP) -s $< > $@

patches_full.txt: patches.elf
	$(OBJDUMP) -D -x -r $< > $@

clean:
	del /Q $(PATCH_OBJECTS) $(PATCH_OUTPUTS) patches.map 2>NUL || exit 0
