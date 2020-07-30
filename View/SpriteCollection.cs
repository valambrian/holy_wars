using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class SpriteCollection
{
	[SerializeField]
	private Texture2D spriteSheet;

	[SerializeField]
	private TextAsset spriteData;

    private char[] separators = { ',', ':' };

    private Vector2 pivot = new Vector2(0f, 0f);
    private float pixelsToUnits = 32.0f;

    private Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
    private Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

    public Dictionary<string, Texture2D> Textures
	{
		get {return _textures;}
	}

    public Dictionary<string, Sprite> Sprites
    {
        get { return _sprites; }
    }

    public void ParseData()
	{
		if (this.spriteData == null)
		{
			Debug.LogError("SpriteCollection: sprite data file is not set");
			return;
		}

		StringReader reader = new StringReader(this.spriteData.text);
		if ( reader == null )
		{
			Debug.LogError("SpriteCollection: sprite data file not found or not readable");
			return;
		}

		string currentLine;
		int x, y, width, height;
		while ( (currentLine = reader.ReadLine()) != null )
		{
			//Debug.Log("-->" + currentLine);
			string[] values = currentLine.Split(this.separators);
			if(values.Length == 5)
			{
				if (int.TryParse(values[1].Trim(), out x) == false)
				{
					Debug.LogError("SpriteCollection: " + values[1].Trim() + " is not a valid x coordinate");
					continue;
				}
				if (int.TryParse(values[2].Trim(), out y) == false)
				{
					Debug.LogError("SpriteCollection: " + values[2].Trim() + " is not a valid y coordinate");
					continue;
				}
				if (int.TryParse(values[3].Trim(), out width) == false)
				{
					Debug.LogError("SpriteCollection: " + values[3].Trim() + " is not a valid width");
					continue;
				}
				if (int.TryParse(values[4].Trim(), out height) == false)
				{
					Debug.LogError("SpriteCollection: " + values[4].Trim() + " is not a valid height");
					continue;
				}

				string name = values[0].Trim();

				Texture2D newTexture = new Texture2D(width, height);
				newTexture.SetPixels(this.spriteSheet.GetPixels(x, y, width, height));
				newTexture.filterMode = FilterMode.Point;
				newTexture.wrapMode = TextureWrapMode.Clamp;
				newTexture.Apply();
				_textures[name] = newTexture;

                Sprite newSprite = Sprite.Create(this.spriteSheet, new Rect(x, y, width, height), pivot, pixelsToUnits);
                this._sprites[name] = newSprite;
            }
            else
			{
				Debug.LogError("SpriteCollection: sprite data file not found or not readable");
			}
		}
	}

}
