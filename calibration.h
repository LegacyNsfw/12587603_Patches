// Declarations for calibration constants defined in calibration.c

extern unsigned short MinimumThrottlePedalPosition;
extern unsigned short FixedThrottleBladeAngle;
extern unsigned short SpeedToRpmFactorArray[];
extern unsigned short MaximumThrottleBladeAngle;
extern unsigned short ClutchThrottleLimit[16];
extern unsigned short PerGearThrottleLimit[6][16];

#define PERCENTAGE(x) ((unsigned short) (x * 51.2))