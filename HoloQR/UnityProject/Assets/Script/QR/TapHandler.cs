using HoloToolkit.Unity.InputModule;
using UnityEngine;

public class TapHandler : MonoBehaviour, IInputClickHandler
{
    private Placeholder placeholder;
    void Awake()
    {
        placeholder = GetComponent<Placeholder>();
    }

    private void Start()
    {
        InputManager.Instance.PushFallbackInputHandler(gameObject);
    }

    #region IInputClickHandler
    public void OnInputClicked(InputClickedEventData eventData)
    {
        Debug.Log("OnInputClicked AirTap");
        placeholder.OnScan();
    }
    #endregion IInputClickHandler
}