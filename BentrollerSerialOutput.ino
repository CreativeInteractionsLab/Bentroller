#include <Keyboard.h>
//
const int bend1 = 0;
const int bend2 = 1;

int myButtons[6] = {2,3,4,5,6,7};
int myPreviousKeyStatus[6] = {0,0,0,0,0,0};
int myKeys[6] = {'w','a','s','d','k','l'};

void setup() {
  // put your setup code here, to run once:
  pinMode(2, INPUT);
  pinMode(3, INPUT);
  pinMode(4, INPUT);
  pinMode(5, INPUT);
  pinMode(6, INPUT);
  pinMode(7, INPUT);
  //pinMode(8, INPUT);.
  //pinMode(9, INPUT);
  //pinMode(10, INPUT);

  Serial.begin(9600);
}
//
void loop() {
  //put your main code here, to run repeatedly:
  String input = "";

  int myKeyStatus[6] = {0,0,0,0,0,0};
  
  for(int i=0; i<6; i++)
  {
    if(digitalRead(myButtons[i]) == LOW)
    {
      myKeyStatus[i] = 1;
    }
    else {
      myKeyStatus[i] = 0;
    }
    //input += myKeyStatus[i];
  }

  for(int j=0; j<6; j++)
  {
    if(myKeyStatus[j] == 1 && myPreviousKeyStatus[j] != 1)
    {
     //Keyboard.press(myKeys[j]);
    }
    else if(myKeyStatus[j] == 0 && myPreviousKeyStatus[j] != 0)
    {
      //Keyboard.release(myKeys[j]);
    }

    myPreviousKeyStatus[j] = myKeyStatus[j];
  }
  
  input += "&";
  input += String(analogRead(bend1));
  input += ",";
  input += String(analogRead(bend2));
  input += ",";
  input += String(myKeyStatus[0]);
  input += ",";
  input += String(myKeyStatus[1]);
  input += ",";
  input += String(myKeyStatus[2]);
  input += ",";
  input += String(myKeyStatus[3]);
  input += ",";
  input += String(myKeyStatus[4]);
  input += ",";
  input += String(myKeyStatus[5]);


  
  Serial.println(input);
  Serial.flush();
  delay(10);
}
