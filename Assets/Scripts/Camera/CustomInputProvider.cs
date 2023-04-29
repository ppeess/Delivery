using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class CustomInputProvider : CinemachineInputProvider {
    [SerializeField]
    private bool yAxisEnabled = true;
    public PlayerInput playerInput;

    private void Start()
    {
        SetYAxis(yAxisEnabled);
    }

    public void SetYAxis(bool enable)
    {
        yAxisEnabled = enable;
    }

    public override float GetAxisValue(int axis)
    {
        if (playerInput.actions.Contains(XYAxis))
        {
            if (!yAxisEnabled && axis == 1)
                return 0;
            return base.GetAxisValue(axis);
        }
        return 0;
    }
}