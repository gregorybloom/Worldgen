
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteDatabase : MonoBehaviour
{
    public Dictionary<string, SpriteData> dictSpriteData = new Dictionary<string, SpriteData>();


    private bool loaded = false;

    void Awake()
    {
        if (loaded == false)
        {
            LoadResources();
        }
    }

    public void LoadResources()
    {
        loaded = true;
        Debug.Log("LOAD SPRITE RESOURCES");

        dictSpriteData["blockSprites"] = new SpriteData(16, 16, "WorldMesh/e75c9__Faithful-texture-pack-1");
//        dictSpriteData["ninjaGameSprites"] = new SpriteData(448 / 16, 640 / 16, "Images/superpowers-asset-packs-master/ninja-adventure/background-elements/tileset");
    }


    public class SpriteData
    {
        public string spritePath;
        public int tileCountX = 1;
        public int tileCountY = 1;
        public Texture2D spriteTexture;

        public SpriteData(int x, int y, string path)
        {
            spritePath = path; tileCountX = x; tileCountY = y;
            spriteTexture = Resources.Load(spritePath) as Texture2D;
        }
    }

}
