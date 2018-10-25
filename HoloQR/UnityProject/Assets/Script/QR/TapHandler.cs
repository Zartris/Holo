using HoloToolkit.Unity.InputModule;
using UnityEngine;

public class TapHandler : MonoBehaviour, IInputClickHandler
{
    private Scanner scanner;
    void Awake()
    {
        scanner = GetComponent<Scanner>();
    }

    private void Start()
    {
        InputManager.Instance.PushFallbackInputHandler(gameObject);
    }

    #region IInputClickHandler
    public void OnInputClicked(InputClickedEventData eventData)
    {
        Debug.Log("OnInputClicked AirTap");
        scanner.OnScan();
    }
    #endregion IInputClickHandler
}