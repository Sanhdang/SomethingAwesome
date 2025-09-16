# SomethingAwesome

This repository contains a **2D Racing Game** developed in **C# using SplashKitSDK**.  
The game challenges the player to control a car, dodge random obstacles, collect fuel, grab bonus items, and survive as long as possible to achieve the highest score.

---

## Gameplay Features

- **Car Selection**  
  At the start, the player can choose between two different cars: an **F1 car** or a **Racing car**.

- **Controls**  
  Use the **Left Arrow** and **Right Arrow** keys to steer the car left or right. The car is restricted to stay within the road boundaries.

- **Fuel System**
  - Fuel is constantly draining by 2 units every second.  
  - Crashing into obstacles reduces fuel further.  
  - Picking up fuel cans restores fuel and adds extra time:  
    - *Small fuel can*: +5 fuel and +3 seconds.  
    - *Large fuel can*: +10 fuel and +6 seconds.

- **Obstacles**  
  Random obstacles fall from the top of the road. Colliding with them reduces fuel.

- **Bonus Items**
  - *Star*: activates double score for 10 seconds.  
  - *Shield*: protects the car from damage for 5 seconds.  
  - *Coin*: awards 100 points (200 points if double score is active).

- **Pause Function**  
  Press **P** to pause the game. The screen freezes with a “PAUSED” message. Press **P** again to continue.

- **Game Over Conditions**
  - If the timer runs out → a **TIME OUT** screen is shown.  
  - If the fuel reaches zero → a **GAME OVER** screen is shown.  

  In both cases, the final score is displayed at the center of the screen.