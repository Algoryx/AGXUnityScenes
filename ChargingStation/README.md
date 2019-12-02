# Charging Robot Demo Scene Instructions

## Dependencies

The scene uses Cinemachine for camera control. This package can be downloaded and installed from the Unity Packages interface.

## Scene description and instructions

* When playing the scene, a generic car model equipped with a charging socket mockup will be automatically driven to within reach of the charging robot. The objective is to position the charging cable in the car socket using the robot.
* Automatic controls can move the robot to three predefined positions, manual control of the robot can be used for final positioning of the robot.
* The camera is set to follow the car when it is driving and then switch to focusing on the robot.
* When the plug is gripped by the robot and is sufficiently close to the socket (typically 0.5 m) the camera will zoom in to help with the connection visuals. The camera is zoomed out again if the robot lets go of the grip or the plug is removed from the close enough zone.
* *Note!* The plug can only be inserted fully when in a certain rotation, with the head rolled approximately 10 degrees clockwise from auto position 3.
* *Note!* The demo scene does only limited checks to ensure that the robot does not move beyond reasonable limits, so in order for the simulation to stay stable one should not try to move the robot head too far away from its base or colliding with the car etc.

## Keyboard control of the robot

*Automatic control:* there are three predefined positions for the robot that are activated with the number keys 1, 2 and 3 on the keyboard (top left, not keypad). Pressing one of the three numbers will attempt to move the robot towards the following positions, if that position is within reach:

* Robot rest position 		1 key 	(initial state)
* Grip plug position		2 key	(above the charging plug ready to grip)
* Attach to car position	3 key	(in front of the car socket)

*Manual control* is available whenever automatic movement is not in progress. The position movements are for the head relative to the robot base. The rotation movements are relative to the gripped objects current orientation. Use the following letter keys on the keyboard:

* Forward / backward: 	W / S
* Sideways left/right: 		A / D
* Raise / lower:			E / Q
* Roll cw / ccw:	 	T / Y
* Yaw cw / ccw 		G / H
* Pitch down / up:		B / N
* Grip / release cable:		X
