// Declarations for calibration constants defined in calibration.c

extern unsigned short const MinimumThrottlePedalPosition;
extern unsigned short const FixedThrottleBladeAngle;
extern unsigned short const SpeedToRpmFactorArray[];
extern unsigned short const MaximumThrottleBladeAngle;
extern unsigned short const RpmToThrottleBladeAngle[];
extern unsigned short const ClutchThrottleLimit[16];
extern unsigned short const PerGearThrottleLimit[6][16];

#define PERCENTAGE(x) ((const unsigned short) (x * 51.2))