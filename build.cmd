m68k-elf-as -mcpu=cpu32 -g -o patches_debug.o patches.s
m68k-elf-ld -T patches.ld -Map=patches.map -o patches.elf patches_debug.o
m68k-elf-objdump -S -d patches.elf > patches_disasm.txt
m68k-elf-objdump -s patches.elf > patches_hexdump.txt
m68k-elf-objdump -D -x -r patches.elf > patches_full.txt
