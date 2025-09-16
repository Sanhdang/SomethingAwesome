using SplashKitSDK;
using System;

public class Obstacle
{
    private readonly Window _window;
    private readonly Bitmap _sprite;

    private int _posX;
    private int _posY;
    private int _speed;

    private static readonly Random _rng = new Random();

    // Properties
    public int X => _posX;
    public int Y => _posY;
    public int Speed
    {
        get => _speed;
        set => _speed = value;
    }

    public Bitmap Sprite => _sprite;

    
    public Bitmap Bitmap => _sprite;

    public Obstacle(Window gameWindow)
    {
        _window = gameWindow;
        _sprite = PickRandomSprite();

        // Random vị trí xuất hiện, đảm bảo nằm trong "đường"
        _posX = _rng.Next(200, gameWindow.Width - _sprite.Width - 200);
        _posY = -_sprite.Height;
        _speed = 5;
    }

    /// <summary>
    /// Pick random obstacles
    /// </summary>
    private Bitmap PickRandomSprite()
    {
        int choice = _rng.Next(1, 4); // 1 đến 3
        string resourceId = $"Obstacle_{choice}";

        return choice switch
        {
            1 => new Bitmap(resourceId, "o_1.png"),
            2 => new Bitmap(resourceId, "o_2.png"),
            3 => new Bitmap(resourceId, "o_3.png"),
            _ => new Bitmap(resourceId, "o_3.png")
        };
    }

    public void Update()
    {
        _posY += _speed;
    }

    public void Draw()
    {
        SplashKit.DrawBitmap(_sprite, _posX, _posY);
    }
}
