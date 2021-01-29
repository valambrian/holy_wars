using System.Collections.Generic;
using UnityEngine;

public class SpriteCollectionManager: MonoBehaviour
{
	[SerializeField]
	private SpriteCollection[] collections;

	private static Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
	private static Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

	void Awake()
	{
		for(int i = 0; i < collections.Length; i++)
		{
			this.collections[i].ParseData();
			foreach(KeyValuePair<string, Texture2D> pair in this.collections[i].Textures)
			{
				_textures[pair.Key] = pair.Value;
			}
			foreach (KeyValuePair<string, Sprite> pair in this.collections[i].Sprites)
			{
				_sprites[pair.Key] = pair.Value;
			}
        	}
        	DontDestroyOnLoad(this);
	}


	public static Texture2D GetTextureByName(string name)
	{
		if (_textures.ContainsKey(name))
		{
			return _textures[name];
		}
		else
		{
			return null;
		}
	}


	public static Sprite GetSpriteByName(string name)
	{
        	if (_sprites.ContainsKey(name))
        	{
            		return _sprites[name];
        	}
        	else
        	{
            		return null;
        	}
    	}

}
