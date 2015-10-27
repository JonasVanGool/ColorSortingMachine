/* Sweep
 by BARRAGAN <http://barraganstudio.com> 
 This example code is in the public domain.

 modified 8 Nov 2013
 by Scott Fitzgerald
 http://arduino.cc/en/Tutorial/Sweep
*/ 

#include <Servo.h> 
#include <QueueList.h>
#define INTERRUPT_PIN A3

#define FEED_MOTOR_PIN A0
#define FEED_INPUT_PIN A1
#define FEED_DETECT_THRESHOLD 900

#define MAX_COUNTER  255
#define NR_SERVOS    6
#define SERVO1_Q_SIZE 1
#define SERVO2_Q_SIZE 2
#define SERVO3_Q_SIZE 3
#define SERVO4_Q_SIZE 4
#define SERVO5_Q_SIZE 5

///////FEEDSYSTEM
boolean feedStart = false, feedReady = false, feedDrop = false;

boolean hungry = false;
long hungryTime = 0;
#define TIME_HUNGRY 500

Servo servo[NR_SERVOS];
int servoPins[NR_SERVOS];
int servoPos[NR_SERVOS];

byte * inputMessage;

QueueList <byte> queueServo1;
QueueList <byte> queueServo2;
QueueList <byte> queueServo3;
QueueList <byte> queueServo4;
QueueList <byte> queueServo5;
boolean queueUpdated = false;
byte lastByte = 101;

byte messageSize = 4;


int leftPos  = 0;
int rightPos = 80;
int centerPos = 40;

int interruptPin = 2;

int motorPin1 = 6;
int motorPin2 = 5;

int servoPowerPin = 13;

int moveDelay = 300;

boolean inputFlag = false;
boolean interruptFlag = false;

int counter = 0;
void setup() 
{ 
  Serial.begin(9600);
  pinMode(FEED_MOTOR_PIN,OUTPUT);
  pinMode(FEED_INPUT_PIN,INPUT);
  
  initQueues();
  
  servoPins[0] = 13;
  servoPins[1] = 8;
  servoPins[2] = 9;
  servoPins[3] = 10;
  servoPins[4] = 11;
  servoPins[5] = 12;

  for(int i = 0; i<NR_SERVOS;i++)
    servoPos[i] = centerPos;
  
  servoWritePos(servoPos);
  
  pinMode(motorPin1,OUTPUT);
  pinMode(motorPin2,OUTPUT);
  pinMode(servoPowerPin,OUTPUT);
  
  pinMode(INTERRUPT_PIN,INPUT);
  
  analogWrite(motorPin1,LOW);
  digitalWrite(motorPin2,LOW);
  digitalWrite(servoPowerPin,LOW);
  digitalWrite(FEED_MOTOR_PIN,LOW);
} 
 
void loop() 
{ 
  if(feedStart){
    if(!feedDrop){
      if(!feedDetect()&&!feedReady){
        digitalWrite(FEED_MOTOR_PIN,HIGH);
      }else{
        feedReady = true;
        digitalWrite(FEED_MOTOR_PIN,LOW);
      }
    }else{
      if(feedDetect()){
        digitalWrite(FEED_MOTOR_PIN,HIGH);
      }else{
        feedDrop = false;
        feedReady = false;
      } 
    }
  }else{
    digitalWrite(FEED_MOTOR_PIN,LOW);
  }
  
  if(Serial.available()>0){
    handleMessage(readMessage());
  }
  
  if(!digitalRead(INTERRUPT_PIN) && !inputFlag){
    inputFlag = true;
  }
  
  if(digitalRead(INTERRUPT_PIN) && inputFlag){
    inputFlag = false;
    interruptFlag = true;
  }
 
  if(((millis()-hungryTime)>TIME_HUNGRY)&& hungry){
    hungry = false;
    feedDrop = true;
  }
  
  if(interruptFlag){
    // reset interrupt flag
    interruptFlag = false; 
    hungry = true;
    hungryTime  = millis();
    // make and send counter message
    byte outputMessage[messageSize];
    outputMessage[0] = 1;
    outputMessage[1] = counter;
    outputMessage[2] = 0;
    outputMessage[3] = lastByte;
    sendMessage(outputMessage);

    // update servo's 
    if(queueUpdated){
      servoPos[1] = queueServo1.pop();
      servoPos[2] = queueServo2.pop();
      servoPos[3] = queueServo3.pop();
      servoPos[4] = queueServo4.pop();
      servoPos[5] = queueServo5.pop();
      queueUpdated = false;
    }else{
      //error
      for(int i = 0; i<NR_SERVOS;i++)
        servoPos[i] = centerPos;
    }
    //write update servo positions 
    
    servoWritePos(servoPos);
    for(int i = 0; i<NR_SERVOS;i++)
      servoPos[i] = centerPos;
    servoWritePos(servoPos);
  }
} 
void servoWritePos(int * posInput ){
  
  
  // attach pins
  for(int j = 0; j<NR_SERVOS; j++)
    servo[j].attach(servoPins[j]);
  
  digitalWrite(servoPowerPin,HIGH);
  // wrtie posistions
  for(int k = 0; k<NR_SERVOS;k++)
    servo[k].write(posInput[k]);
   
  // give servo's time to position  
  delay(moveDelay);
  digitalWrite(servoPowerPin,LOW);
  // detach pins
  for(int l=0;l<NR_SERVOS;l++)
    servo[l].detach();
}

byte * readMessage(){
  byte message[messageSize];
    message[0] = Serial.read();
    while(Serial.available()==0){}
    message[1] = Serial.read();
    while(Serial.available()==0){}
    message[2] = Serial.read();
    while(Serial.available()==0){}
    message[3] = Serial.read();
    if(message[3]!=101)
      return NULL;
    return message;
}

void sendMessage(byte * message){
  Serial.write(message,messageSize);
}

void handleMessage(byte * message){
  switch(message[0]){
    case 0:
      break;
    case 1: addInputToOutputBuffers(message[1]);
      break;
    case 2: startConveyor(message[1]);
            feedStart = true;
      break;
    case 3: stopConveyor();
            feedStart = false;;
      break;
    default: 
      break;  
  }
}

void startConveyor(byte speed){
  analogWrite(motorPin1,speed);
  digitalWrite(motorPin2,LOW);
}

void stopConveyor(){
  digitalWrite(motorPin1,LOW);
  digitalWrite(motorPin2,LOW);
}

void addInputToOutputBuffers(byte selection){
  switch(selection){
    case 0: pushQueues(0,0);
      break;
    case 1: pushQueues(1,leftPos);
      break;
    case 2: pushQueues(1,rightPos);
      break;
    case 3: pushQueues(2,leftPos);
      break;
    case 4: pushQueues(2,rightPos);
      break;
    case 5: pushQueues(3,leftPos);
      break;
    case 6: pushQueues(3,rightPos);
      break;
    case 7: pushQueues(4,leftPos);
      break;
    case 8: pushQueues(4,rightPos);
      break;
    case 9: pushQueues(5,leftPos);
      break;
    case 10: pushQueues(5,rightPos);
      break;
    default: pushQueues(0,0);
      break;
  }
}

void initQueues(){
  for(int i=0;i<SERVO1_Q_SIZE;i++)
    queueServo1.push(centerPos);
  for(int i=0;i<SERVO2_Q_SIZE;i++)
    queueServo2.push(centerPos);
  for(int i=0;i<SERVO3_Q_SIZE;i++)
    queueServo3.push(centerPos);
  for(int i=0;i<SERVO4_Q_SIZE;i++)
    queueServo4.push(centerPos);  
  for(int i=0;i<SERVO5_Q_SIZE;i++)
    queueServo5.push(centerPos);
}

void pushQueues(byte queueNr,byte data){
  queueServo1.push((queueNr==1)? data:centerPos);
  queueServo2.push((queueNr==2)? data:centerPos);
  queueServo3.push((queueNr==3)? data:centerPos);
  queueServo4.push((queueNr==4)? data:centerPos);
  queueServo5.push((queueNr==5)? data:centerPos);
  queueUpdated = true;
}

boolean feedDetect(){
  if(analogRead(FEED_INPUT_PIN)<FEED_DETECT_THRESHOLD)
    return true;
  return false;
}
