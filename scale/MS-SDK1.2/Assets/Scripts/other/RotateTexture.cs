using UnityEngine;
using System.Collections;

public class RotateTexture : MonoBehaviour {
    public Texture2D originTexture;
    private Texture2D rotatedTexture;

    void Start()
    {
        rotatedTexture = new Texture2D(originTexture.width, originTexture.height);
        rotatedTexture = rotateTexture(originTexture, 40);
    }

    void OnGUI()
    {
        GUILayout.Box(originTexture);
        GUI.Box(new Rect(originTexture.width, 0, originTexture.width, originTexture.height), rotatedTexture);
    }
    Texture2D rotateTexture(Texture2D tex, float angle)
    {
        Debug.Log("rotating");
        Texture2D rotImage = new Texture2D(tex.width, tex.height);
        int x, y;
        float x1, y1, x2, y2;


        int w = tex.width;
        int h = tex.height;
        float x0 = rot_x(angle, -w / 2.0f, -h / 2.0f) + w / 2.0f;
        float y0 = rot_y(angle, -w / 2.0f, -h / 2.0f) + h / 2.0f;


        float dx_x = rot_x(angle, 1.0f, 0.0f);
        float dx_y = rot_y(angle, 1.0f, 0.0f);
        float dy_x = rot_x(angle, 0.0f, 1.0f);
        float dy_y = rot_y(angle, 0.0f, 1.0f);

        x1 = x0;
        y1 = y0;

        for (x = 0; x < tex.width; x++)
        {
            x2 = x1;
            y2 = y1;
            for (y = 0; y < tex.height; y++)
            {

                //rotImage.SetPixel (x1, y1, Color.clear);           
                x2 += dx_x;//rot_x(angle, x1, y1); 
                y2 += dx_y;//rot_y(angle, x1, y1); 
                rotImage.SetPixel((int)Mathf.Floor(x), (int)Mathf.Floor(y), getPixel(tex, x2, y2));

            }

            x1 += dy_x;
            y1 += dy_y;
        }

        rotImage.Apply();
        return rotImage;

    }

    private Color getPixel(Texture2D tex, float x, float y)
    {
        Color pix;
        int x1 = (int)Mathf.Floor(x);
        int y1 = (int)Mathf.Floor(y);
        if (x1 > tex.width || x1 < 0 ||  y1 > tex.height || y1 < 0)
        {

            pix = Color.clear;
        }
        else
        {
            pix = tex.GetPixel(x1, y1);

        }
        return pix;
    }

    private float rot_x(float angle, float x, float y)
    {
        float cos = Mathf.Cos(angle / 180.0f * Mathf.PI);
        float sin = Mathf.Sin(angle / 180.0f * Mathf.PI);
        return (x * cos + y * (-sin));

    }

    private float rot_y(float angle, float x, float y)
    {
        float cos = Mathf.Cos(angle / 180.0f * Mathf.PI);
        float sin = Mathf.Sin(angle / 180.0f * Mathf.PI);
        return (x * sin + y * cos);

    }
}
