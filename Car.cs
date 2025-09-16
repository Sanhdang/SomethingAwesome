using SplashKitSDK;

public class Car
{
    private Bitmap _carBitmap;
    private Window _gameWindow;
    private int _x, _y;
    private int _fuel;

    public Car(Window gameWindow, string carType)
    {
        _gameWindow = gameWindow;
        switch (carType)
        {
            case "F1":
                _carBitmap = new Bitmap("F1Car", "f1_car.png");
                break;
            case "Racing":
                _carBitmap = new Bitmap("RacingCar", "racing_car.png");
                break;
            default:
                _carBitmap = new Bitmap("F1Car", "racing_car.png"); // fallback
                break;
        }
        _x = (gameWindow.Width - _carBitmap.Width) / 2;
        _y = gameWindow.Height - _carBitmap.Height - 20;
        _fuel = 15;
    }

    public int Fuel
    {
        get { return _fuel; }
        set { _fuel = value; }
    }

    public int X => _x;
    public int Y => _y;

    public void HandleInput()
    {
        if (SplashKit.KeyDown(KeyCode.LeftKey) && _x > 0)
        {
            _x -= 7;
        }
        if (SplashKit.KeyDown(KeyCode.RightKey) && _x < _gameWindow.Width - _carBitmap.Width)
        {
            _x += 7;
        }
    }

    public void StayOnWindow(Window gameWindow)
    {
        if (_x < 180) _x = 180;
        if (_x > gameWindow.Width - _carBitmap.Width - 180)
            _x = gameWindow.Width - _carBitmap.Width - 180;
    }

    public void Draw()
    {
        SplashKit.DrawBitmap(_carBitmap, _x, _y);
    }

    public bool CollidedWithCar(Obstacle obstacle)
    {
        return _carBitmap.BitmapCollision(_x, _y, obstacle.Bitmap, obstacle.X, obstacle.Y);
    }

    public bool CollidedWithFuel(Fuel fuel)
    {
        return _carBitmap.BitmapCollision(_x, _y, fuel.Bitmap, fuel.X, fuel.Y);
    }

    public bool CollidedWithSprite(Bitmap sprite, int x, int y)
    {
        return _carBitmap.BitmapCollision(_x, _y, sprite, x, y);
    }

    public void Refuel(int amount)
    {
        Fuel += amount;
    }

    public bool Quit()
    {
        return SplashKit.KeyDown(KeyCode.EscapeKey);
    }
}
