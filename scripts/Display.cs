using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public partial class Display : GridContainer
{
	[ExportCategory("Properties")]
	[Export] double framerate = 30;
	[Export] Vector2I resolution;

	[ExportCategory("References")]
	[Export] Texture2D[] TexturesSheet;
	[Export] OptionButton DisplayTexture;
	[Export] Label NumDisplay;
	[Export] Label TextDisplay;

	private byte[,] displayBuffer;
	private byte[,] displayBufferBuffer;
	private string charBuffer = "";
	private int displayedNum = 0;
	private bool unsigned = true;

	private AtlasTexture[,] Textures;
	private int textureIndex = 0;
	private int resolutionIndex = 1;
	private double time = 0;

	private Vector2I pixelPos;

	private List<char> charValues = new List<char> {' ', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '.', '!', '?'};

	private List<Vector2I> resolutionList = new List<Vector2I> {
		new Vector2I(16,16),
		new Vector2I(32,32),
		new Vector2I(64,64),
		new Vector2I(128,128)
		};
	
	private List<Vector2I> spriteSizeList = new List<Vector2I> {
		new Vector2I(32,32),
		new Vector2I(16,16),
		new Vector2I(8,8),
		new Vector2I(4,4)
		};

	public void _ready()
	{
		time = Time.GetUnixTimeFromSystem();

		//Split Texture Sheet
		int length = 16;
		Textures = new AtlasTexture[TexturesSheet.Length, length];
		for(var i = 0; i < TexturesSheet.Length; i++)
		{
			var size = TexturesSheet[i].GetSize();
			var region_width = size.X / length;
			
			for(var j = 0; j < length; j++)
			{
				var atlas = new AtlasTexture();
				atlas.Atlas = TexturesSheet[i];
				atlas.Region = new Rect2(region_width * j, 0, size.X / length, size.Y);
				Textures[i, j] = atlas;
			}
		}

		DisplaySetup();
		DisplayInit();
	}

	public Texture2D GetTexture(int pixelNumber = 0)
	{
		return Textures[textureIndex, pixelNumber];
	}

	public void DisplayInit()
	{
		displayedNum = 0;
		TextDisplay.Text = "__________";
		NumDisplay.Text = "" + displayedNum;
		UpdateSprites();
	}

	public void DisplaySetup()
	{
		resolution = resolutionList[resolutionIndex];
		displayBuffer = new byte[resolution.X, resolution.Y];
		displayBufferBuffer = new byte[resolution.X, resolution.Y];
		Columns = resolution.X;

		foreach(Node i in GetChildren())
		{
			RemoveChild(i);
		}

		for (int x = 0; x < resolution.X; x++)
		{
			for (int y = 0; y < resolution.Y; y++)
			{
				TextureRect sprite = new TextureRect();
				sprite.Texture = GetTexture(textureIndex);
				sprite.CustomMinimumSize = spriteSizeList[resolutionIndex];
				sprite.Size = spriteSizeList[resolutionIndex];
				sprite.ExpandMode = (TextureRect.ExpandModeEnum)1;
				AddChild(sprite);
			}
		}
	}

	public void ChangeResolution(int index = -1)
	{
		if (index == -1) { return; }
		resolutionIndex = index;
		DisplaySetup();
		UpdateSprites();
		GetOwner<Main>().Reset();
	}

	public void ChangeTexture(int index = -1)
	{
		if (index == -1) { return; }
		textureIndex = index;
		UpdateSprites();
	}

	public void UpdateSprites(bool force = false)
	{
		if (Time.GetUnixTimeFromSystem() - time >= 1d / framerate || force)
		{
			Array<Node> children = GetChildren();
			int pixels = resolution.X * resolution.Y;
			for (int i = 0; i < pixels; i++)
			{
				TextureRect child = children[i] as TextureRect;
				child.Texture = GetTexture(displayBuffer[i % resolution.X, i / resolution.Y]);
			}
			time = Time.GetUnixTimeFromSystem();
		}
	}

	private void UpdateNumDisplay()
	{
		if (unsigned) NumDisplay.Text = "" + displayedNum;
		else NumDisplay.Text = "" + ((displayedNum & 0b01111111) + (((displayedNum & 0b10000000) != 0) ? -128 : 0));
	}

	public void PushBuffer()
	{
		System.Array.Copy(displayBufferBuffer, 0, displayBuffer, 0, resolution.X * resolution.Y);
		UpdateSprites();
	}

	public void ClearBuffer()
	{
		for (int x = 0; x < resolution.X * resolution.Y; x++)
			displayBufferBuffer[x%resolution.X, x/resolution.Y] = 0;
	}

	public void StorePort(byte port, byte data)
	{
		switch (port)
		{
			//Pixel X
			case 240:
				pixelPos.X = data;
				pixelPos.X = pixelPos.X % resolution.X;
				break;
			//Pixel Y
			case 241:
				pixelPos.Y = data;
				pixelPos.Y = resolution.Y - 1 - pixelPos.Y % resolution.Y;
				break;
			//Draw Pixel
			case 242:
				var color = (byte)(data & 0b00001111);
				if (color == 0) { color = 1; }
				displayBufferBuffer[pixelPos.X, pixelPos.Y] = color;
				break;
			//Clear Pixel
			case 243:
				displayBufferBuffer[pixelPos.X, pixelPos.Y] = 0;
				break;
			//Buffer screen
			case 245:
				PushBuffer();
				break;
			//Clear Screen Buffer
			case 246:
				ClearBuffer();
				break;
			//Write Char
			case 247:
				charBuffer += charValues[data];
				break;
			//Buffer Chars
			case 248:
				TextDisplay.Text = PadWithUnderscores(charBuffer.ToUpper());
				break;
			//Clear Chars Buffer
			case 249:
				charBuffer = "";
				break;
			//Show Number
			case 250:
				displayedNum = data;
				UpdateNumDisplay();
				break;
			//Clear Number
			case 251:
				NumDisplay.Text = "";
				break;
			//Signed Mode
			case 252:
				unsigned = false;
				UpdateNumDisplay();
				break;
			//Unsigned Mode
			case 253:
				unsigned = true;
				UpdateNumDisplay();
				break;
			default:
				break;
		}
	}

	public static string PadWithUnderscores(string inputString)
	{
		if (inputString == null)
		{
			throw new ArgumentNullException(nameof(inputString));
		}

		int targetLength = 10;
		int padLength = targetLength - inputString.Length;
		return padLength > 0 ? inputString.PadRight(targetLength, '_') : inputString;
	}

	public byte LoadPort(byte port)
	{
		switch (port)
		{
			case 244:
				return displayBufferBuffer[pixelPos.X, pixelPos.Y];
			case 254:
				Random rand = new Random();
				return (byte)rand.Next();
			case 255:
				return GetInputs();
			default:
				return 0;
		}
	}

	public byte GetInputs()
	{
		bool up = Input.IsActionPressed("up");
		bool down = Input.IsActionPressed("down");
		bool left = Input.IsActionPressed("left");
		bool right = Input.IsActionPressed("right");
		bool start = Input.IsActionPressed("start");
		bool select = Input.IsActionPressed("select");
		bool a = Input.IsActionPressed("a");
		bool b = Input.IsActionPressed("b");
		bool[] inputs = new bool[] {start, select, a, b, up, right, down, left};
		return ToByte(inputs);
	}

	public static byte ToByte(bool[] data)
	{
		if (data.Length != 8)
		{
			throw new ArgumentOutOfRangeException(nameof(data), "data length must be 8");
		}

		byte result = 0;
		for (int i = 0; i < data.Length; i++)
		{
			result |= (byte)(data[i] ? 1 << (7 - i) : 0);
		}
		return result;
	}
}
