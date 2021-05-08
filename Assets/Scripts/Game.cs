using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Game
{
	public static GameManager Manager;
	public static Input Input = new _InputActions();

	#region Settings Values
	public static float VolumeScale = 1;
    #endregion


    // Modified controls class to enable on construction
    private class _InputActions : Input {
		public _InputActions() : base() {
			Enable();
		}
	}
}
