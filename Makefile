.PHONY: patches selftest clean

AS      := m68k-elf-as
CC      := m68k-elf-gcc
LD      := m68k-elf-ld
OBJDUMP := m68k-elf-objdump
HOST_CC := gcc

ASFLAGS := -mcpu=cpu32 -g
CFLAGS  := -mcpu=cpu32 -ffreestanding -c -g

PATCH_OBJECTS := patches_debug.o revmatch.o calibration.o main.o
PATCH_OUTPUTS := patches.elf patches_disasm.txt patches_hexdump.txt patches_full.txt

patches: $(PATCH_OUTPUTS)

selftest: main.c revmatch.c calibration.c globals.h calibration.h selftest.h
	$(HOST_CC) -g -o $@ main.c revmatch.c calibration.c

patches_debug.o: patches.s
	$(AS) $(ASFLAGS) -o $@ $<

revmatch.o: revmatch.c globals.h calibration.h selftest.h
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
