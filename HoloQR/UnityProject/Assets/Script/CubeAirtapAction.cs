using HoloToolkit.Unity.InputModule;
using UnityEngine;

public class CubeAirtapAction : MonoBehaviour, IInputClickHandler
{
    public Scanner ScannerScript;

    #region IInputClickHandler
    public void OnInputClicked(InputClickedEventData eventData)
    {
        ScannerScript.OnReset();
        if (gameObject.GetComponent<MeshRenderer>().material.color != Color.red)
        {
            gameObject.GetComponent<MeshRenderer>().material.color = Color.red;
        }
        else
        {
            gameObject.GetComponent<MeshRenderer>().material.color = new Color(0, 0, 255, 255);
        }
    }
    #endregion IInputClickHandler
}