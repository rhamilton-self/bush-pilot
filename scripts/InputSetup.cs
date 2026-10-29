using Godot;

public static class InputSetup
{
	public static void Register()
	{
		// Keyboard
		AddKey("pitch_up", Key.S);       // pull back = nose up
		AddKey("pitch_down", Key.W);
		AddKey("roll_left", Key.A);
		AddKey("roll_right", Key.D);
		AddKey("yaw_left", Key.Q);
		AddKey("yaw_right", Key.E);
		AddKey("throttle_up", Key.Shift);
		AddKey("throttle_down", Key.Ctrl);
		AddKey("flaps_up", Key.R);
		AddKey("flaps_down", Key.F);
		AddKey("brake", Key.Space);
		AddKey("toggle_camera", Key.C);
		AddKey("reset_flight", Key.Backspace);

		// Gamepad
		AddAxis("pitch_up", JoyAxis.LeftY, 1f);      // stick back = nose up
		AddAxis("pitch_down", JoyAxis.LeftY, -1f);
		AddAxis("roll_right", JoyAxis.LeftX, 1f);
		AddAxis("roll_left", JoyAxis.LeftX, -1f);
		AddAxis("yaw_right", JoyAxis.RightX, 1f);
		AddAxis("yaw_left", JoyAxis.RightX, -1f);
		AddAxis("throttle_up", JoyAxis.TriggerRight, 1f);
		AddAxis("throttle_down", JoyAxis.TriggerLeft, 1f);
		AddButton("flaps_up", JoyButton.DpadUp);
		AddButton("flaps_down", JoyButton.DpadDown);
		AddButton("brake", JoyButton.A);
		AddButton("toggle_camera", JoyButton.Y);
		AddButton("reset_flight", JoyButton.Back);
	}

	static void Ensure(string action)
	{
		if (!InputMap.HasAction(action))
			InputMap.AddAction(action, 0.15f);
	}

	static void AddKey(string action, Key key)
	{
		Ensure(action);
		InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
	}

	static void AddAxis(string action, JoyAxis axis, float value)
	{
		Ensure(action);
		InputMap.ActionAddEvent(action, new InputEventJoypadMotion { Axis = axis, AxisValue = value});
	}

	static void AddButton(string action, JoyButton button)
	{
		Ensure(action);
		InputMap.ActionAddEvent(action, new InputEventJoypadButton { ButtonIndex = button });
	}
}