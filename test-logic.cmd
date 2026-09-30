rem Compile and link as amd64 for testing purposes
gcc -m64 -o test-logic.exe main.c revmatch.c calibration.c

rem Run the test executable
test-logic.exe