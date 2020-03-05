using UnityEngine;
using System.Collections;
using System;
public class PixelTest : MonoBehaviour {

    void Start() { 
        Texture2D texture = new Texture2D(128, 128);
        renderer.material.mainTexture = texture;
        int y = 0; 
        //创建一个简单的分形图案
        while (y < texture.height) { 
            int x = 0; while (x < texture.width) 
            { 
                Color color = (x & y)!=0 ? Color.white : Color.gray;
                texture.SetPixel(x, y, color); ++x;
            }
            ++y;
        } 
        texture.Apply();
    }
}
