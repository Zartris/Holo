﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace HoloToolkit.UX.Dialog
{
    /// <summary>
    /// Enum describing the style (caption) of button on a Dialog.
    /// </summary>
    [Flags]
    public enum DialogButtonType
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
