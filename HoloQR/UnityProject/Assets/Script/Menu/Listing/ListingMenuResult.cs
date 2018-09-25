using System;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Menu.Listing
{
    public class ListingMenuResult
    {

        public List<GameObject> Buttons { get; set; } = new List<GameObject>();

        public Dictionary<GameObject, Uri> ButtonLink { get; set; } = new Dictionary<GameObject, Uri>();

        public Dictionary<GameObject, string> ButtonNames { get; set; } = new Dictionary<GameObject, string>();

        public void createListingMenuResult(List<GameObject> buttons, List<Uri> links, List<String> names)
        {
            Buttons = buttons;
            var bLinks = new Dictionary<GameObject, Uri>();
            var bNames = new Dictionary<GameObject, string>();
            for (var index = 0; index < buttons.Count; index++)
            {
                var button = buttons[index];
                var uri = links[index];
                var name = names[index];
                bLinks[button] = uri;
                bNames[button] = name;
            }
            ButtonLink = bLinks;
            ButtonNames = bNames;
        }

    }
}