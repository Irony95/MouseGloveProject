project finished on 8/11/19
last updated on 9/11/19

excutable file located in : mouse glove windows application
c# wpf file located in    : mouse glove windows code
arduino code located in   : Mouse Glove arduino code


uses an arduino uno with a mpu6050 accelerometer and hm-05 bluetooth module.

the arduino reads the data sends it to my computer in the format

gx|gy|gz|ax|ay|az;

where gx, gy, gz are gyroscope for the x, y and z respectively and ax, ay, az is acceleration for x, y and z.
the computer then does some math to calculate the roll and pitch to determine the speed of the 
cursor. the left/right click is done with acceleration of yaw.

the baud rate it usually runs on is 115200
in order to check the serial port the bluetooth is connected to, just check under extra bluetooth options

