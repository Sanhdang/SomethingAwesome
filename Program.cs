using SplashKitSDK;
using System;

public class Program
{
    public static void Main()
    {
        string selectedCar = ReadUserOption();

        if (!string.IsNullOrEmpty(selectedCar))
        {
            Window gameWindow = new Window("Car Racing Game", 840, 650);
            Gameplay game = new Gameplay(gameWindow, selectedCar);

            while (!game.Quit() && !game.IsGameOver())
            {
                SplashKit.ProcessEvents();
                game.HandleInput();
                game.Update();
                game.Draw();
            }

            gameWindow.Close();
        }
    }

    public static string ReadUserOption()
    {
        int option;
        Console.WriteLine("--------------------");
        Console.WriteLine("| 1 | F1 Car       |");
        Console.WriteLine("| 2 | Racing Car   |");
        Console.WriteLine("--------------------");

        do
        {
            Console.Write("Choose your car [1-2]: ");
            try
            {
                option = Convert.ToInt32(Console.ReadLine());
            }
            catch
            {
                Console.WriteLine("Please input 1 or 2.");
                option = -1;
            }
        } while (option < 1 || option > 2);

        switch (option)
        {
            case 1:
                return "F1";       
            case 2:
                return "Racing";   
            default:
                return "F1";
        }
    }
}