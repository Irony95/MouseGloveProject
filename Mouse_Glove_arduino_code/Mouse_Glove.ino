/*
  this works in junction with a WPF app i created that reads and interprets the data, which allows you to move your mouse and stuff
*/


#include <Wire.h>
#include <SoftwareSerial.h>

class MPU {
private:
  int adress;
  int gyroSen, accelSen;
  float gyroLSBVal, accelLSBVal;  
  
public:
  float gForceX, gForceY, gForceZ;
  float rotX, rotY, rotZ;
  
  bool isAsleep;
  MPU(int ad, int gSen, int aSen, float gLSB, float aLSB) {
    adress = ad;
    gyroSen = gSen; gyroLSBVal = gLSB;
    accelSen = aSen; accelLSBVal = aLSB;    
  }

  void begin() {
    Wire.beginTransmission(adress);
    Wire.write(0x6B); //Accessing the register 6B - Power Management
    Wire.write(0b00000000); //Setting SLEEP register to 0.
    Wire.endTransmission();
    isAsleep = false;
  
    Wire.beginTransmission(adress);
    Wire.write(0x1B); //Accessing the register 1B - Gyroscope Configuration
    Wire.write(gyroSen);
    Wire.endTransmission();
  
    Wire.beginTransmission(adress);
    Wire.write(0x1C); //Accessing the register 1C - Acccelerometer Configuration
    Wire.write(accelSen);
    Wire.endTransmission();
  }

  void updateValues() {
    Wire.beginTransmission(adress);
    Wire.write(0x3B); //Starting register for Accel Readings
    Wire.endTransmission();
    Wire.requestFrom(adress, 6); //Request Accel Registers (3B - 40)
    while (Wire.available() < 6);
    long ax = Wire.read() << 8 | Wire.read(); //Store first two bytes into x
    long ay = Wire.read() << 8 | Wire.read(); //Store middle two bytes into y
    long az = Wire.read() << 8 | Wire.read(); //Store last two bytes into z
  
    gForceX = ax / accelLSBVal;
    gForceY = ay / accelLSBVal;
    gForceZ = az / accelLSBVal;

    Wire.beginTransmission(adress);
    Wire.write(0x43); //Starting register for Gyro Readings
    Wire.endTransmission();
    Wire.requestFrom(adress, 6); //Request Gyro Registers (43 - 48)
    while (Wire.available() < 6);
    long gx = Wire.read() << 8 | Wire.read(); //Store first two bytes into x
    long gy = Wire.read() << 8 | Wire.read(); //Store middle two bytes into y
    long gz = Wire.read() << 8 | Wire.read(); //Store last two bytes into z
  
    rotX = gx / gyroLSBVal;
    rotY = gy / gyroLSBVal;
    rotZ = gz / gyroLSBVal;
  }  

  void Sleep() {
    Wire.beginTransmission(adress);
    Wire.write(0x6B); //Accessing the register 6B - Power Management
    Wire.write(0b00000000); //Setting SLEEP register to 0.
    Wire.endTransmission();
    isAsleep = false;
  }
  void Wake() {
    Wire.beginTransmission(adress);
    Wire.write(0x6B); //Accessing the register 6B - Power Management
    Wire.write(0b01000000); //Setting SLEEP register to 1.
    Wire.endTransmission();
    isAsleep = true;  
  }
};

/*
 The sensitivity and LSB are inside the MPU6050 register map, and getting the registers to change the 
 sensitivity should be infered from 6B and 1C of the register map. Refer to this video: 
 https://youtu.be/M9lZ5Qy5S2s and https://www.invensense.com/wp-content/uploads/2015/02/MPU-6000-Register-Map1.pdf 
 for easier understanding of the details
*/
const int MPUGyroSensitivity =  0x0001000;   const float MPUGyroLSB = 65.5;
const int MPUAccelSensitivity = 0b00001000; const float MPUAccelLSB = 16384.0;
const int MPUAdress = 0b1101000;

MPU accGyroSensor(0b1101000, 0x0010000, 0b00000000, MPUGyroLSB, MPUAccelLSB);
SoftwareSerial btCon(10, 11);

const int greenLed = 12;
float degree = 0;

void setup() {
  Serial.begin(9600);
  Wire.begin();
  accGyroSensor.begin();
  btCon.begin(115200);

  pinMode(greenLed, OUTPUT);
  digitalWrite(greenLed, HIGH);
}


void loop() {   
  if (!accGyroSensor.isAsleep) {
    accGyroSensor.updateValues();
    
    String sentBufferData = String(accGyroSensor.rotX) + "|" + String(accGyroSensor.rotY) + "|" + String(accGyroSensor.rotZ) + "|" + String(accGyroSensor.gForceX) + "|" + String(accGyroSensor.gForceY) + "|" + String(accGyroSensor.gForceZ) + ";";
    for (int i = 0;i< sentBufferData.length();i++) {
      btCon.write((char)sentBufferData[i]);
    }
    Serial.println(sentBufferData);
    //SendBTBytes();    
  }
  
 //Needs working on plz rem im gunna forget

  delay(1);
}

void SendBTBytes() {
  btCon.write(accGyroSensor.rotX);
  btCon.write((int)'|');
  btCon.write(accGyroSensor.rotY);
  btCon.write((int)'|');
  btCon.write(accGyroSensor.rotZ);
  btCon.write((int)'|');
  btCon.write(accGyroSensor.gForceX);
  btCon.write((int)'|');
  btCon.write(accGyroSensor.gForceY);
  btCon.write((int)'|');
  
  btCon.write(accGyroSensor.gForceZ);
  btCon.write((int)';');
}

void printData() {
  Serial.print("Gyro (deg)");
  Serial.print(" Y=");
  degree += accGyroSensor.rotY/1000.0;
  Serial.println(degree);
}
