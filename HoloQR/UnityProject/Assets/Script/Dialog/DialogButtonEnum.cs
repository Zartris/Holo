namespace HoloToolkit.UX.Dialog
{
    /**
     * Always let close be the last, since it will place best in dialogs.
     */
    public enum DialogButtonEnum 
    {
        None = 0,
        Close = 1,
        Confirm = 2,
        Cancel = 4,
        Accept = 8,
        Yes = 16,
        No = 32,
        OK = 64,
    }
}