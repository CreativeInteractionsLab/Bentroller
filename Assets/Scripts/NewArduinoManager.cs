using UnityEngine;
using System.Collections;
using System.IO;
using System.IO.Ports;
using UnityEngine.UI;
using System.Collections.Generic;

public class NewArduinoManager : MonoBehaviour {
	public static string serialName = "COM";  //might change, check the Arduino IDE
	public SerialPort mySPort;// = new SerialPort(serialName, 9600);

	private bool calibrateComplete = false;
	public float bend1Av = 0f;
	public float bend2Av = 0f;
	private int currentCalibrationFrame = 0;
	private const int calibrationFrames = 10;
	private float[] restValues = {0, 0};
	//a buffer value so -/+ this from the restValues is also considered as rest, used to reduce noise
	private const float stateCushionValue = 50f;

	public bool lerpMode = true;  //if true the script will do a soothing filter using the previous bend value
	private float[] previousBendValues = new float[2];

	string[] buttonValues = new string[6];  //stores the button values read from the Arduino
	public int[] intButtonValues = new int[6];  //stores the parsed button values

	string[] bendValues = new string[2];  //stores the button values read from the Arduino
	public float[] floatBendValues = new float[2];  //stores the parsed bend values


//	public enum CustomGamepadButton
//	{
//		UP,
//		DOWN,
//		LEFT,
//		RIGHT,
//		A,
//		B,
//		START,
//		SELECT
//	}

//	private List<ControllerButton> gamepadButtons = new List<ControllerButton>();

	void Start()
	{
		//keep openning ports until it finds one
		int i = 3;
		while (true) {
			try {
				mySPort = new SerialPort (serialName + i, 9600);
				mySPort.Open ();
			} catch (IOException e) {
				i++;
			}
			break;
		}

		// initialize the controller dictionary
//		gamepadButtons.Add (new ControllerButton(CustomGamepadButton.A));
//		gamepadButtons.Add (new ControllerButton(CustomGamepadButton.B));
//		gamepadButtons.Add (new ControllerButton(CustomGamepadButton.UP));
//		gamepadButtons.Add (new ControllerButton(CustomGamepadButton.DOWN));
//		gamepadButtons.Add (new ControllerButton(CustomGamepadButton.LEFT));
//		gamepadButtons.Add (new ControllerButton(CustomGamepadButton.RIGHT));
//		gamepadButtons.Add (new ControllerButton(CustomGamepadButton.START));
//		gamepadButtons.Add (new ControllerButton(CustomGamepadButton.SELECT)); 
	}

	void Update() {

		if (!mySPort.IsOpen) {
			mySPort.Open ();
		}
		
		if (mySPort.ReadLine () != null) {//something is ready for read

			//Calibrating the bends with 'C' ?
			if (Input.GetKeyDown (KeyCode.C)) {
				if (calibrateComplete) {
					calibrateComplete = false;
					bend1Av = 0;
					bend2Av = 0;
					currentCalibrationFrame = 0;  
				}
			}

			//read the string from the Arduino, formatted like this:
			//&<bendValue1>,<bendValue2>,<button0status>,<button1status>,<button2status>,<button3status>,<button4status>,<button5status>
			string serialValue = mySPort.ReadLine ();
			string[] serialValues = serialValue.Split ('&');

			if (serialValues.Length > 1) {

				////////////////////////////
				/// split the values and put them to the appropriate string arrays
				///////////////////////////
				string[] allValues = serialValues [1].Split (',');

				//first two values are from the bend sensors
				for (int i = 0; i < 2; i++) {
						bendValues [i] = allValues [i];
				}

				//remaining six values are from the buttons
				for (int i = 2; i < 8; i++) {
						buttonValues [i - 2] = allValues [i];
				}

				////////////////////////////
				/// convert/parse the values in the appropriate string arrays and put them into arrays of the correct type
				///////////////////////////
				//button values are represented by integers 0(LOW) and 1(HIGH), check the Arduino code for sure
				for (int i = 0; i < 6; i++) {
					intButtonValues [i] = int.Parse (buttonValues [i]);
				}
			
				//bend values are presented by float ranging from 0 to 1024
				for (int j = 0; j < (bendValues.Length); j++) {
					/*if (j == (bendValues.Length - 1)) {
						bendValues [j] = bendValues [j].Substring (0, bendValues [j].Length - 1);
					}*/

					if (lerpMode) {
						float currentVal = float.Parse (bendValues [j]);
						previousBendValues [j] = Mathf.Lerp (previousBendValues [j], currentVal, 0.5f);
					} else {
						previousBendValues [j] = float.Parse (bendValues [j]);
					}
						
					//shift the range to -512 to 512
					floatBendValues [j] = previousBendValues [j] - (1024f / 2f);
				}

				if (calibrateComplete) {
					//call the utility function that displays the bend status in a human readible form.
					//CheckBendStatus (floatBendValues);
				} else {
					CalibrateController (floatBendValues);

					//DEBUG STUFF
					float calibrationProgress = (float)currentCalibrationFrame / (float)calibrationFrames;
					calibrationProgress *= 100f;
					string progressTemp = "Calibrating...";
					progressTemp += calibrationProgress;
					progressTemp += "%";

				}
			}
		
		}
	//		foreach (ControllerButton button in gamepadButtons)
	//		{
	//			button.Update();
	//		}

	//	printStuff ();
	}

	//A utility function that displays the bend status in a human readible form.
	//Uses the GetBendState function to help detemining it.
	//If there is a GUI text component (e.g., gestureText) in the application can also display it there.
	public void CheckBendStatus(float[] floatBendValues) {
		if (GetBendState (floatBendValues [0], 0) == 0 && GetBendState (floatBendValues [1], 1) == 0) {
			//rest state
			//gestureText.text = "REST";
			Debug.Log("REST");
		} else if (GetBendState (floatBendValues [0], 0) == 1 && GetBendState (floatBendValues [1], 1) == 1) {
			//bent up
			//gestureText.text = "BENT UP";
			Debug.Log("BENT UP");
		} else if (GetBendState (floatBendValues [0], 0) == -1 && GetBendState (floatBendValues [1], 1) == -1) {
			//bent down
			//gestureText.text = "BENT DOWN";
			Debug.Log("BENT DOWN");
		} else if (GetBendState (floatBendValues [0], 0) != -1 && GetBendState (floatBendValues [1], 1) != 1) {
			//twist up
			//gestureText.text = "TWIST UP";
			Debug.Log("TWIST UP");
		} else if (GetBendState (floatBendValues [0], 0) != 1 && GetBendState (floatBendValues [1], 1) != -1) {
			//twist down
			//gestureText.text = "TWIST DOWN";
			Debug.Log("TWIST DOWN");
		} else {
			//error
			//gestureText.text = "ERROR - NO STATE";
			Debug.LogError ("NO STATE - SHOULD NOT GET HERE");
		}
	}

	//A utility function that determins the state of the bend sensor based on the bendValue
	//input: the benValue for calculation, the sensor index so the correct calibration value can be applied
	//output: 0 for rest (no bend), -1 for bent up, 1 for bent down
	public int GetBendState(float bendValue, int sensor) {
		//use the statCushionValue to create a buffer window to reduce noise
		if(bendValue <= (restValues[sensor] - stateCushionValue)) {
			//bendValue is really small, so the bentroller is bent up
			return 1;
		}
		else if(bendValue >= (restValues[sensor] + stateCushionValue)) {
			//bendValue is really big, so the bentroller is bent down
			return -1;
		}
		//benValue is within the buffer window, treat it as no bend
		return 0;
	}

	//Find the resting value for the bend sensors and store them in the bend1Av and bend2Av variables
	//done by finding an average over calibrationFrames number of frames
	public void CalibrateController(float[] floatBendValues) {
		currentCalibrationFrame += 1;
		bend1Av += floatBendValues [0];
		bend2Av += floatBendValues [1];

		if(currentCalibrationFrame >= calibrationFrames)
		{
			bend1Av /= currentCalibrationFrame;
			restValues[0] = bend1Av;
			bend2Av /= currentCalibrationFrame;
			restValues[1] = bend2Av;
			calibrateComplete = true;

			Debug.Log ("<color=green>Calibration Complete</color>");
			Debug.Log ("<color=green>Sensor 1: " + restValues[0] + ", Sensor 2: " + restValues[1] + "</color>");
		}
	}

	//print out the sensor values read and processed by the script
	//for buttons the values are parsed into 1 or 0, 
	//for bend the values are parsed into float and shifted to -512 to 512
	public void printStuff() {
		//values for buttons
		for (int i = 0; i < 6; i++) {
			print("processed button"+i+" = " + intButtonValues [i]);
		}
		//values for bend sensors
		for (int i = 0; i < 2; i++) {
			print("processed bend"+i+" = " + floatBendValues [i]);
		}

	}

}
