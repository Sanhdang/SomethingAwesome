using System;
using System.Collections.Generic;
using SplashKitSDK;
using SKTimer = SplashKitSDK.Timer;

public class Gameplay
{
    // ---- Settings ----
    private const int InitialTimeSeconds      = 15;
    private const int ObstacleSpawnMsDefault  = 1000;
    private const int FuelSpawnMsDefault      = 2500;
    private const int ObstacleSpawnMsMin      = 500;
    private const int FuelSpawnMsMin          = 1500;
    private const int FuelDrainEverySec       = 1;
    private const int FuelDrainAmountPerTick  = 2;
    private const int ObstacleMaxSpeed        = 30;
    private const int ObstacleSpeedStep       = 3;

    private const int CollisionFuelPenalty    = 5;
    private const int CollisionCooldownMs     = 800;

    private const int BackgroundScrollSpeed   = 4;

    // Time bonus when picking up fuel
    private const int TimeBonusSmallFuel      = 3; // seconds
    private const int TimeBonusLargeFuel      = 6; // seconds

    // ---- Core state ----
    private readonly Window _window;
    private readonly Car _player;

    private readonly List<Obstacle> _obstacles = new();
    private readonly List<Fuel> _fuels = new();

    // ---- Timers ----
    private readonly SKTimer _gameTimer;
    private readonly SKTimer _obstacleTimer;
    private readonly SKTimer _fuelTimer;
    private readonly SKTimer _fuelDrainTimer;
    private readonly SKTimer _hitTimer;

    // Bonus timers
    private readonly SKTimer _bonusTimer;
    private readonly SKTimer _doubleScoreTimer;
    private readonly SKTimer _shieldTimer;

    // ---- Presentation ----
    private readonly Bitmap _background;
    private readonly Bitmap _fuelIcon;
    private readonly Bitmap _starBitmap;
    private readonly Bitmap _shieldBitmap;
    private readonly Bitmap _coinBitmap;
    private Bitmap? _gameOverImage;

    private int _bgY1 = 0;
    private int _bgY2 = 0;

    // ---- Gameplay bookkeeping ----
    private bool _isGameOver = false;
    private string _gameOverReason = string.Empty;

    private int _score = 0;
    private int _remainingTime = InitialTimeSeconds;

    private int _obstacleSpawnInterval = ObstacleSpawnMsDefault;
    private int _fuelSpawnInterval = FuelSpawnMsDefault;

    private bool _paused = false;

    private bool _doubleScoreActive = false;
    private bool _shieldActive = false;

    // ---- Floating text ----
    private class FloatingText
    {
        public string Text;
        public int X, Y;
        public Color Color;
        public int Lifetime;

        public FloatingText(string text, int x, int y, Color color, int lifetime = 1000)
        {
            Text = text;
            X = x;
            Y = y;
            Color = color;
            Lifetime = lifetime;
        }
    }
    private readonly List<FloatingText> _floatingTexts = new();

    // ---- Bonus item ----
    private enum BonusType { Star, Shield, Coin }

    private class Bonus
    {
        public BonusType Type;
        public Bitmap Sprite;
        public int X;
        public int Y;
        public int Speed = 3;

        public Bonus(BonusType type, Bitmap sprite, Window window, Random rng)
        {
            Type = type;
            Sprite = sprite;
            int margin = 180;
            X = rng.Next(margin, Math.Max(margin, window.Width - sprite.Width - margin + 1));
            Y = -sprite.Height;
        }

        public void Update() => Y += Speed;
        public void Draw() => SplashKit.DrawBitmap(Sprite, X, Y);
        public bool Offscreen(Window w) => Y > w.Height;
    }

    private readonly List<Bonus> _bonuses = new();
    private readonly Random _rng = new();

    public Gameplay(Window gameWindow, string carType)
    {
        _window     = gameWindow;
        _background = new Bitmap("bg_main", "road.png");
        _fuelIcon   = new Bitmap("fuel_icon", "fuel_icon.jpg");

        // bonus sprites
        _starBitmap   = new Bitmap("bonus_star",   "bonus_star.png");
        _shieldBitmap = new Bitmap("bonus_shield", "bonus_shield.png");
        _coinBitmap   = new Bitmap("bonus_coin",   "bonus_coin.png");

        _player     = new Car(gameWindow, carType);

        _bgY1 = 0;
        _bgY2 = -_background.Height;

        // Timers
        _gameTimer       = SplashKit.CreateTimer("t_game");
        _obstacleTimer   = SplashKit.CreateTimer("t_obstacle");
        _fuelTimer       = SplashKit.CreateTimer("t_fuel");
        _fuelDrainTimer  = SplashKit.CreateTimer("t_fuel_drain");
        _hitTimer        = SplashKit.CreateTimer("t_hit");
        _bonusTimer      = SplashKit.CreateTimer("t_bonus_spawn");
        _doubleScoreTimer= SplashKit.CreateTimer("t_double_score");
        _shieldTimer     = SplashKit.CreateTimer("t_shield");

        SplashKit.StartTimer(_gameTimer);
        SplashKit.StartTimer(_obstacleTimer);
        SplashKit.StartTimer(_fuelTimer);
        SplashKit.StartTimer(_fuelDrainTimer);
        SplashKit.StartTimer(_hitTimer);
        SplashKit.StartTimer(_bonusTimer);
        SplashKit.StartTimer(_doubleScoreTimer);
        SplashKit.StartTimer(_shieldTimer);
    }

    public void HandleInput()
    {
        if (SplashKit.KeyTyped(KeyCode.PKey))
        {
            _paused = !_paused;
            if (_paused)
            {
                SplashKit.PauseTimer(_gameTimer);
                SplashKit.PauseTimer(_obstacleTimer);
                SplashKit.PauseTimer(_fuelTimer);
                SplashKit.PauseTimer(_fuelDrainTimer);
                SplashKit.PauseTimer(_hitTimer);
                SplashKit.PauseTimer(_bonusTimer);
                SplashKit.PauseTimer(_doubleScoreTimer);
                SplashKit.PauseTimer(_shieldTimer);
            }
            else
            {
                SplashKit.ResumeTimer(_gameTimer);
                SplashKit.ResumeTimer(_obstacleTimer);
                SplashKit.ResumeTimer(_fuelTimer);
                SplashKit.ResumeTimer(_fuelDrainTimer);
                SplashKit.ResumeTimer(_hitTimer);
                SplashKit.ResumeTimer(_bonusTimer);
                SplashKit.ResumeTimer(_doubleScoreTimer);
                SplashKit.ResumeTimer(_shieldTimer);
            }
        }

        if (!_paused) _player.HandleInput();
    }

    public void Update()
    {
        if (_paused || _isGameOver) return;

        if (!string.IsNullOrEmpty(_gameOverReason))
        {
            ShowGameOverOnce();
            _isGameOver = true;
            return;
        }

        _player.StayOnWindow(_window);

        AdjustDifficultyByTime();
        TrySpawnObstacle();
        TrySpawnFuel();

        UpdateObstacles();
        CleanupOffscreenObstacles();

        UpdateFuels();          // add fuel + time bonus
        CleanupOffscreenFuels();

        TrySpawnBonus();
        UpdateBonuses();
        CleanupOffscreenBonuses();

        ScrollBackground();

        ApplyTimeoutRule();
        ApplyFuelDrainRule();

        if (_player.Fuel <= 0)
        {
            _gameOverReason = "Game over";
        }

        _score = (int)(_gameTimer.Ticks / 1000);
    }

    public void Draw()
    {
        SplashKit.DrawBitmap(_background, 0, _bgY1);
        SplashKit.DrawBitmap(_background, 0, _bgY2);

        foreach (var b in _bonuses) b.Draw();
        _player.Draw();
        foreach (var o in _obstacles) o.Draw();
        foreach (var f in _fuels) f.Draw();

        foreach (var txt in _floatingTexts)
            SplashKit.DrawText(txt.Text, txt.Color, "Arial", 16, txt.X, txt.Y);

        DrawHud();

        if (_paused && !_isGameOver)
        {
            SplashKit.FillRectangle(Color.RGBAColor(0, 0, 0, 150), 0, 0, _window.Width, _window.Height);
            SplashKit.DrawText("PAUSED", Color.Red, "Arial", 40,
                (_window.Width / 2) - 80, _window.Height / 2 - 20);
        }

        SplashKit.RefreshScreen(60);
    }

    public bool IsGameOver() => _isGameOver;
    public bool Quit() => _player.Quit();

    // ----------------- Helpers -----------------
    private void ScrollBackground()
    {
        _bgY1 += BackgroundScrollSpeed;
        _bgY2 += BackgroundScrollSpeed;

        if (_bgY1 >= _window.Height)
            _bgY1 = _bgY2 - _background.Height;

        if (_bgY2 >= _window.Height)
            _bgY2 = _bgY1 - _background.Height;
    }

    private void AdjustDifficultyByTime()
    {
        int elapsedSec = (int)(_gameTimer.Ticks / 1000);
        if (elapsedSec > 0 && elapsedSec % 10 == 0)
        {
            int newObstacle = (int)Math.Max(ObstacleSpawnMsMin, _obstacleSpawnInterval * 0.95);
            int newFuel     = (int)Math.Max(FuelSpawnMsMin, _fuelSpawnInterval * 0.95);

            if (newObstacle != _obstacleSpawnInterval || newFuel != _fuelSpawnInterval)
            {
                _obstacleSpawnInterval = newObstacle;
                _fuelSpawnInterval = newFuel;
            }
        }
    }

    private void TrySpawnObstacle()
    {
        if (SplashKit.TimerTicks(_obstacleTimer) > _obstacleSpawnInterval)
        {
            _obstacles.Add(new Obstacle(_window));
            SplashKit.ResetTimer(_obstacleTimer);
        }
    }

    private void TrySpawnFuel()
    {
        if (SplashKit.TimerTicks(_fuelTimer) > _fuelSpawnInterval)
        {
            _fuels.Add(RandomFuel());   // now both small & large
            SplashKit.ResetTimer(_fuelTimer);
        }
    }

    private void UpdateObstacles()
    {
        foreach (var obstacle in _obstacles)
        {
            obstacle.Update();

            if (obstacle.Speed < ObstacleMaxSpeed && SplashKit.TimerTicks(_obstacleTimer) > _obstacleSpawnInterval)
                obstacle.Speed += ObstacleSpeedStep;

            if (_player.CollidedWithCar(obstacle) && !_shieldActive && SplashKit.TimerTicks(_hitTimer) >= CollisionCooldownMs)
            {
                _player.Fuel = Math.Max(0, _player.Fuel - CollisionFuelPenalty);
                _floatingTexts.Add(new FloatingText($"-{CollisionFuelPenalty} Fuel", _player.X, _player.Y - 20, Color.Red));
                SplashKit.ResetTimer(_hitTimer);
            }
        }
    }

    private void CleanupOffscreenObstacles()
        => _obstacles.RemoveAll(o => o.Y > _window.Height);

    // Fuel: add fuel + time bonus + floating texts
    private void UpdateFuels()
    {
        foreach (var fuel in _fuels)
        {
            fuel.Update();
            if (_player.CollidedWithFuel(fuel))
            {
                int added = fuel.RefuelAmount();
                _player.Refuel(added);

                // Time bonus: small vs large
                int timeBonus = (added >= 10) ? TimeBonusLargeFuel : TimeBonusSmallFuel;
                _remainingTime += timeBonus;

                _floatingTexts.Add(new FloatingText($"+{added} Fuel", _player.X, _player.Y - 20, Color.Green));
                _floatingTexts.Add(new FloatingText($"+{timeBonus}s", _player.X, _player.Y - 40, Color.Orange));

                _fuels.Remove(fuel);
                break;
            }
        }

        // Update floating texts lifetimes
        for (int i = _floatingTexts.Count - 1; i >= 0; i--)
        {
            _floatingTexts[i].Lifetime -= 16;
            _floatingTexts[i].Y -= 1;
            if (_floatingTexts[i].Lifetime <= 0)
                _floatingTexts.RemoveAt(i);
        }
    }

    private void CleanupOffscreenFuels()
        => _fuels.RemoveAll(f => f.Y > _window.Height);

    // ----- BONUS (Star/Shield/Coin) -----
    private void TrySpawnBonus()
    {
        if (SplashKit.TimerTicks(_bonusTimer) > 8000 && (int)(_gameTimer.Ticks / 1000) > 5)
        {
            int roll = _rng.Next(100);
            BonusType type = roll < 45 ? BonusType.Coin : (roll < 75 ? BonusType.Star : BonusType.Shield);

            Bitmap sprite = type switch
            {
                BonusType.Star   => _starBitmap,
                BonusType.Shield => _shieldBitmap,
                _                => _coinBitmap
            };

            _bonuses.Add(new Bonus(type, sprite, _window, _rng));
            SplashKit.ResetTimer(_bonusTimer);
        }
    }

    private void UpdateBonuses()
    {
        for (int i = 0; i < _bonuses.Count; i++)
        {
            var b = _bonuses[i];
            b.Update();

            if (_player.CollidedWithSprite(b.Sprite, b.X, b.Y))
            {
                ApplyBonus(b.Type);
                _bonuses.RemoveAt(i);
                i--;
                continue;
            }
        }

        if (_doubleScoreActive && _doubleScoreTimer.Ticks >= 10000)
            _doubleScoreActive = false;

        if (_shieldActive && _shieldTimer.Ticks >= 5000)
            _shieldActive = false;
    }

    private void CleanupOffscreenBonuses()
        => _bonuses.RemoveAll(b => b.Offscreen(_window));

    private void ApplyBonus(BonusType type)
    {
        switch (type)
        {
            case BonusType.Star:
                _doubleScoreActive = true;
                SplashKit.ResetTimer(_doubleScoreTimer);
                _floatingTexts.Add(new FloatingText("Double Score (10s)", _player.X, _player.Y - 30, Color.Yellow));
                break;

            case BonusType.Shield:
                _shieldActive = true;
                SplashKit.ResetTimer(_shieldTimer);
                _floatingTexts.Add(new FloatingText("Shield (5s)", _player.X, _player.Y - 30, Color.Cyan));
                break;

            case BonusType.Coin:
                int gain = _doubleScoreActive ? 200 : 100;
                _score += gain;
                _floatingTexts.Add(new FloatingText("+" + gain + " Points", _player.X, _player.Y - 30, Color.Orange));
                break;
        }
    }

    private void ApplyTimeoutRule()
    {
        int elapsedSec = (int)(SplashKit.TimerTicks(_gameTimer) / 1000);
        if (elapsedSec > _remainingTime)
            _gameOverReason = "Time out";
    }

    private void ApplyFuelDrainRule()
    {
        if (_fuelDrainTimer.Ticks / 1000 >= FuelDrainEverySec)
        {
            _player.Fuel = Math.Max(0, _player.Fuel - FuelDrainAmountPerTick);
            _fuelDrainTimer.Reset();
        }
    }

    // ---- Game Over screen with Final Score ----
    private void DrawCenteredText(string text, Color color, int size, int y)
    {
        int w = SplashKit.TextWidth(text, "Arial", size);
        int x = (_window.Width - w) / 2;
        SplashKit.DrawText(text, color, "Arial", size, x, y);
    }

    private void ShowGameOverOnce()
    {
        _gameOverImage = _gameOverReason switch
        {
            "Time out"  => new Bitmap("gameover_timeout", "time_out.png"),
            "Game over" => new Bitmap("gameover_generic", "game_over.png"),
            _           => null
        };

        if (_gameOverImage != null)
            SplashKit.CurrentWindow().DrawBitmap(_gameOverImage, 0, 0);
        else
            SplashKit.FillRectangle(Color.Black, 0, 0, _window.Width, _window.Height);

        string title = _gameOverReason == "Time out" ? "OUT OF TIME" : "GAME OVER";
        DrawCenteredText(title, Color.White, 42, _window.Height / 2 - 60);

        DrawCenteredText($"FINAL SCORE: {_score}", Color.Yellow, 28, _window.Height / 2);
        DrawCenteredText("Press ESC to exit", Color.White, 18, _window.Height / 2 + 40);

        _window.Refresh(60);
        SplashKit.Delay(2500);
    }

    // ---- HUD ----
    private void DrawHud()
    {
        int elapsedSec = (int)(SplashKit.TimerTicks(_gameTimer) / 1000);
        int timeLeft   = _remainingTime - elapsedSec;

        SplashKit.DrawText("SCORE: " + _score, Color.Black, "Arial", 20, _window.Width - 120, 10);
        SplashKit.DrawText("Time: " + timeLeft, Color.Black, "Arial", 20, _window.Width - 120, 30);

        // Fuel icon + bar
        SplashKit.DrawBitmap(_fuelIcon, 20, 15);

        int barWidth = 150;
        int barHeight = 20;
        int barX = 60;
        int barY = 20;

        SplashKit.FillRectangle(Color.Gray, barX, barY, barWidth, barHeight);

        double percent = Math.Min(1.0, _player.Fuel / 100.0);
        int currentWidth = (int)(barWidth * percent);

        Color fuelColor = _player.Fuel > 20 ? Color.Green : Color.Red;
        SplashKit.FillRectangle(fuelColor, barX, barY, currentWidth, barHeight);
        SplashKit.DrawRectangle(Color.Black, barX, barY, barWidth, barHeight);

        if (_doubleScoreActive)
        {
            int left = Math.Max(0, 10 - (int)(_doubleScoreTimer.Ticks / 1000));
            SplashKit.DrawText("x2 Score (" + left + "s)", Color.Yellow, "Arial", 18, 20, 50);
        }
        if (_shieldActive)
        {
            int left = Math.Max(0, 5 - (int)(_shieldTimer.Ticks / 1000));
            SplashKit.DrawText("Shield (" + left + "s)", Color.Cyan, "Arial", 18, 20, 70);
        }
    }

    // ---- Fuel factory (both small & large) ----
    public Fuel RandomFuel()
        => (SplashKit.Rnd() * 100) <= 50 ? new FuelSmall(_window) : new FuelLarge(_window);
}
