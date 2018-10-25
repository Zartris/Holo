using System;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Menu.Listing
{
    public class DetailMenuResult
    {

        public List<GameObject> Buttons { get; set; } = new List<GameObject>();

        public Dictionary<GameObject, string> ButtonNames { get; set; } = new Dictionary<GameObject, string>();
        public Dictionary<GameObject, MessageBoardContent> ButtonContent { get; set; } = new Dictionary<GameObject, MessageBoardContent>();

        public void createDetailMenuResult(List<GameObject> buttons, List<String> names, List<MessageBoardContent> MBContent)
        {
            Buttons = buttons;
            var bNames = new Dictionary<GameObject, string>();
            var bContent = new Dictionary<GameObject, MessageBoardContent>();
            for (var index = 0; index < buttons.Count; index++)
            {
                var button = buttons[index];
                var name = names[index];
                var content = MBContent[index];
                bNames[button] = name;
                bContent[button] = content;
            }

            ButtonContent = bContent;
            ButtonNames = bNames;
        }

    }
}