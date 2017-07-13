using UnityEngine;
using System.Collections;
using System.IO.Ports;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEditor;

public class NewArduinoManager : MonoBehaviour {
	public static string serialName = "COM4";
	public SerialPort mySPort = new SerialPort(serialName, 9600);

	public bool lerpMode = true;
	private float[] previousBendValues = new float[2];


	private bool calibrateComplete = false;
	public float bend1Av = 0f;
	public float bend2Av = 0f;
	private int currentCalibrationFrame = 0;
	private const int calibrationFrames = 10;

	public int[] buttons = new int[6];

	public string[] bendValues = new string[2];
	string[] buttonValues = new string[6];

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
		mySPort.Open();

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

	void Update()
	{
		//Calibrating the bends with 'C' ?
		if (mySPort.ReadLine () != null) {
			if (Input.GetKeyDown (KeyCode.C)) {
				if (calibrateComplete) {
					calibrateComplete = false;
					bend1Av = 0;
					bend2Av = 0;
					currentCalibrationFrame = 0;  
				}
			}

			string serialValue = mySPort.ReadLine ();
			string[] serialValues = serialValue.Split ('&');

			if (serialValues.Length > 1) {

				string[] allValues = serialValues [1].Split (',');


				for (int i = 0; i < 2; i++) {

						bendValues [i] = allValues [i];
				}

				for (int i = 2; i < 8; i++) {

						buttonValues [i - 2] = allValues [i];
				}
		
				for (int i = 0; i < 6; i++) {
					buttons [i] = int.Parse (buttonValues [i]);
				}
			
				float[] floatBendValues = new float[bendValues.Length];

				for (int j = 0; j < (bendValues.Length); j++) {
					if (j == (bendValues.Length - 1)) {
						bendValues [j] = bendValues [j].Substring (0, bendValues [j].Length - 1);
					}

					if (lerpMode) {
						float currentVal = float.Parse (bendValues [j]);
						previousBendValues [j] = Mathf.Lerp (previousBendValues [j], currentVal, 0.5f);
					} else {
						previousBendValues [j] = float.Parse (bendValues [j]);
					}
						
					float greenVal = Mathf.Abs ((1024f / 2f) - previousBendValues [j]);
					floatBendValues [j] = (1024f / 2f) - previousBendValues [j];
				}

				if (calibrateComplete) {
//					CheckBendStatus (floatBendValues);
				} else {
					CalibrateController (floatBendValues);

					//DEBUG STUFF
					float calibrationProgress = (float)currentCalibrationFrame / (float)calibrationFrames;
					calibrationProgress *= 100f;
					string progressTemp = "Calibrating...";
					progressTemp += calibrationProgress;
					progressTemp += "%";
//					Debug.Log (progressTemp);

				}
			}
		
		}
//		foreach (ControllerButton button in gamepadButtons)
//		{
//			button.Update();
//		}

		printStuff ();
	}

//	public void CheckBendStatus(float[] floatBendValues)
//	{
//		if (GetBendState (floatBendValues [0], 0) == 0 && GetBendState (floatBendValues [1], 1) == 0) {
//			//rest state
//			gestureText.text = "REST";
//		} else if (GetBendState (floatBendValues [0], 0) == 1 && GetBendState (floatBendValues [1], 1) == 1) {
//			//bent up
//			gestureText.text = "BENT UP";
//		} else if (GetBendState (floatBendValues [0], 0) == -1 && GetBendState (floatBendValues [1], 1) == -1) {
//			//bent down
//			gestureText.text = "BENT DOWN";
//		} else if (GetBendState (floatBendValues [0], 0) != -1 && GetBendState (floatBendValues [1], 1) != 1) {
//			//twist up
//			gestureText.text = "TWIST UP";
//		} else if (GetBendState (floatBendValues [0], 0) != 1 && GetBendState (floatBendValues [1], 1) != -1) {
//			//twist down
//			gestureText.text = "TWIST DOWN";
//		} else {
//			//error
//			gestureText.text = "ERROR - NO STATE";
//			Debug.LogError ("NO STATE - SHOULD NOT GET HERE");
//		}
//	}

	private float[] restValues = {0, 0};
	private const float stateCushionValue = 50f;

	public int GetBendState(float bendValue, int sensor)
	{
		/*if (bendValue >= (restValues[sensor] - stateCushionValue) && bendValue <= (restValues[sensor] + stateCushionValue))
		{
			//rest state
			return 0;
		}
		else if(bendValue < (restValues[sensor] - stateCushionValue))
		{
			//bent up
			return 1;
		}
		else if(bendValue > (restValues[sensor] + stateCushionValue))
		{
			//bent down
			return -1;
		}

		return 2;*/

		if(bendValue <= (restValues[sensor] - stateCushionValue))
		{
			//bent up
			return 1;
		}
		else if(bendValue >= (restValues[sensor] + stateCushionValue))
		{
			//bent down
			return -1;
		}

		return 0;
	}

	public void CalibrateController(float[] floatBendValues)
	{
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

	public void printStuff()
	{
		for (int i = 0; i < 6; i++) {
			//print("button= " + buttons [i]);
		}

		for (int i = 0; i < 2; i++) {
			//print("bend= " + previousBendValues [i]);
		}
		//print ("bend1Av= " + bend1Av);
		//print ("bend2Av= " + bend2Av);

		}
	

}
