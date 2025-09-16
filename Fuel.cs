using SplashKitSDK;
using System;

public abstract class Fuel
{
    private readonly Window _window;
    private readonly Bitmap _sprite;

    private int _posX;
    private int _posY;

    private static readonly Random _rng = new Random();

   
    public Bitmap Sprite => _sprite;
    public int X => _posX;
    public int Y => _posY;

    
    public Bitmap Bitmap => _sprite;

    protected Fuel(Window gameWindow, string imageFile, string resourceId)
    {
        _window = gameWindow;
        _sprite = new Bitmap(resourceId, imageFile);

        
        _posX = _rng.Next(200, gameWindow.Width - _sprite.Width - 200);
        _posY = -_sprite.Height;
    }

    public void Update()
    {
        _posY += 5; 
    }

    public void Draw()
    {
        SplashKit.DrawBitmap(_sprite, _posX, _posY);
    }

    public bool IsOffScreen()
    {
        return _posY > _window.Height;
    }

    
    public abstract int RefuelAmount();
}

// ---------- Small fuel ----------
public class FuelSmall : Fuel
{
    public FuelSmall(Window gameWindow) 
        : base(gameWindow, "fuel_small.png", "Fuel_Small") { }

    public override int RefuelAmount() => 5;
}

// ---------- Large fuel ----------
public class FuelLarge : Fuel
{
    public FuelLarge(Window gameWindow) 
        : base(gameWindow, "fuel_large.png", "Fuel_Large") { }

    public override int RefuelAmount() => 10;
}
