using System;
using System.Collections.Generic;
using UnityEngine;

public class MessageBoardContent
{
    public enum Content
    {
        IMG, TEXT, NONE
    }
    // Insert content into Full space (workArea)
    public Content workAreaContent = Content.NONE;
    public string workAreaText = "";
    public string workAreaImageString = "";

    // Insert content into Full Lower area
    public Content lowerAreaContent = Content.NONE;
    public string lowerAreaText = "";
    public string lowerAreaImageString = "";
    // Insert content into Lower left area
    public Content lowerLeftAreaContent = Content.NONE;
    public string lowerLeftAreaText = "";
    public string lowerLeftAreaImageString = "";
    // Insert content into Lower right area
    public Content lowerRightAreaContent = Content.NONE;
    public string lowerRightAreaText = "";
    public string lowerRightAreaImageString = "";
    // Insert content into Full Top area
    public Content topAreaContent = Content.NONE;
    public string topAreaText = "";
    public string topAreaImageString = "";
    // Insert content into Top Left area 
    public Content topLeftAreaContent = Content.NONE;
    public string topLeftAreaText = "";
    public string topLeftAreaImageString = "";

    // Insert content into Top right area
    public Content topRightAreaContent = Content.NONE;
    public string topRightAreaText = "";
    public string topRightAreaImageString = "";

    public Texture2D LoadImageStringToTexture2D(String base64String)
    {
        //data:image/gif;base64,
        //this image is a single pixel (black)
        byte[] bytes = Convert.FromBase64String(base64String);
        Texture2D texture = new Texture2D(1,1,TextureFormat.RGBA32, false);
        texture.LoadImage(bytes);
        return texture;
    }

    public List<Content> GetListOfContents()
    {
        return new List<Content>
        {
            workAreaContent,
            lowerAreaContent,
            topAreaContent,
            lowerLeftAreaContent,
            lowerRightAreaContent,
            topLeftAreaContent,
            topRightAreaContent
        };
    }

}
