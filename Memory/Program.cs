using System;
using System.Threading;
using System.Threading.Channels;
using System.IO;
using System.Text;

class MemoryGame
{
    static void Main()
    {
        while (true) {
            Console.Clear();
            Console.WriteLine("Enter Row amount (4-6): ");
            string initYString = Console.ReadLine();
            int.TryParse(initYString, out int initY);

            Console.WriteLine("Enter Column amount (4-6): ");
            string initXString = Console.ReadLine();
            int.TryParse(initXString, out int initX);

            // Set default values if the user's are out of range
            if (initY < 4 || initY > 6 || initX < 4 || initX > 6)
            {
                Console.WriteLine("Value(s) out of range, setting default values...");

                initY = 5;
                initX = 4;

                Thread.Sleep(1000);
            }

            Console.Clear();

            // Array with the users specified X and Y values
            int[,] deck = new int[initY, initX];

            // Array with the purpose of storing which cards are allowed to be shown
            int[,] shownCards = new int[initY, initX];

            // Variable which is used in randomizing card placements
            Random random = new Random();

            byte[,] endScreen = new byte[5, 28] // 0 == Air | 1 == Text | 2 == Border
                {
                    {2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2},
                    {2, 1, 0, 1, 0, 1, 1, 1, 0, 1, 0, 1, 0, 0, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 1, 0, 1, 2},
                    {2, 0, 1, 0, 0, 1, 0, 1, 0, 1, 0, 1, 0, 0, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 1, 2},
                    {2, 0, 1, 0, 0, 1, 1, 1, 0, 1, 1, 1, 0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 0, 1, 0, 0, 1, 2},
                    {2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2}
                };

            // Card Randomization
            for (int i = 0; i < (initX * initY) / 2; i++)
            {
                int placedCards = 0;

                while (placedCards < 2) // Place each value twice
                {
                    int yRand = random.Next(deck.GetLength(0));
                    int xRand = random.Next(deck.GetLength(1));

                    if (deck[yRand, xRand] == 0) // Place only in empty spots
                    {
                        deck[yRand, xRand] = i;
                        placedCards++;
                    }
                }
            }

            byte playerX = 0, playerY = 0;
            int lastRevealedX = -1, lastRevealedY = -1;

            int totalAttempts = 0;
            int totalPaired = 0;

            long unixStart = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            bool gameEnded = false;

            // Render before user input
            Render(deck, shownCards, playerX, playerY, totalAttempts, totalPaired, unixStart, gameEnded);

            // Gameplay Logic
            while (!gameEnded)
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKey key = Console.ReadKey(intercept: true).Key;
                    switch (key)
                    {
                        case ConsoleKey.DownArrow or ConsoleKey.S:
                            if (playerY < deck.GetLength(0) - 1) playerY++;
                            break;
                        case ConsoleKey.LeftArrow or ConsoleKey.A:
                            if (playerX > 0) playerX--;
                            break;
                        case ConsoleKey.UpArrow or ConsoleKey.W:
                            if (playerY > 0) playerY--;
                            break;
                        case ConsoleKey.RightArrow or ConsoleKey.D:
                            if (playerX < deck.GetLength(1) - 1) playerX++;
                            break;
                        case ConsoleKey.Enter or ConsoleKey.Spacebar:
                            WriteFile(totalAttempts);
                            if (shownCards[playerY, playerX] == 0)
                            {
                                shownCards[playerY, playerX] = 1;

                                if (lastRevealedX == -1 && lastRevealedY == -1)
                                {
                                    // First card revealed
                                    lastRevealedX = playerX;
                                    lastRevealedY = playerY;
                                }
                                else
                                {
                                    // Second card revealed
                                    Render(deck, shownCards, playerX, playerY, totalAttempts, totalPaired, unixStart, gameEnded);

                                    if (deck[playerY, playerX] != deck[lastRevealedY, lastRevealedX])
                                    {
                                        Thread.Sleep(1000); // Show the second card before hiding both
                                        shownCards[playerY, playerX] = 0;
                                        shownCards[lastRevealedY, lastRevealedX] = 0;
                                    }
                                    else
                                    {
                                        totalPaired++;

                                        // End Screen Check
                                        if (totalPaired == (initX * initY) / 2)
                                        {
                                            gameEnded = true;

                                            long unixNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                            long unixSeconds = unixNow - unixStart; // Elapsed time in Seconds
                                            double unixMinutes = Math.Round((double)unixSeconds / 60, 0); // Elapsed time in Minutes

                                            Console.Clear();
                                            for (int y = 0; y < endScreen.GetLength(0); y++)
                                            {
                                                for (int x = 0; x < endScreen.GetLength(1); x++)
                                                {
                                                    if (endScreen[y, x] == 1)
                                                    {
                                                        Console.ForegroundColor = ConsoleColor.Green;
                                                    }
                                                    else if (endScreen[y, x] == 2)
                                                    {
                                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                                    }
                                                    else
                                                    {
                                                        Console.ForegroundColor = ConsoleColor.White;
                                                    }
                                                    Console.Write("■ ");
                                                    Thread.Sleep(1);
                                                }
                                                Console.WriteLine();
                                            }

                                            Console.ForegroundColor = ConsoleColor.White;
                                            Console.WriteLine("\nIt's supposed to say \"You Win\", congrats.");

                                            // End Screen Statistics
                                            Console.Write("\nIt Took You ");
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.Write($"{totalAttempts} Attempts");
                                            Console.ForegroundColor = ConsoleColor.White;
                                            Console.Write(" and ");
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            if (unixNow - unixStart >= 60) // Check if it's at least 60 seconds
                                            {
                                                Console.Write($"{unixMinutes} Minute(s) and {unixSeconds - unixMinutes * 60} Second(s)");
                                            }
                                            else
                                            {
                                                Console.Write($"{unixSeconds} Seconds");
                                            }
                                            Console.ForegroundColor = ConsoleColor.White;
                                            Console.WriteLine(" to Pair all the Cards.");

                                            WriteFile(totalAttempts);

                                            Console.Write("\nPress Ctrl + C to exit or any other key to continue.");
                                            Console.ReadLine();
                                        }
                                    }
                                    // Reset last revealed card
                                    lastRevealedX = -1;
                                    lastRevealedY = -1;

                                    totalAttempts++;
                                }
                            }
                            break;
                    }

                    // Render on user input
                    Render(deck, shownCards, playerX, playerY, totalAttempts, totalPaired, unixStart, gameEnded);
                }
            }
        }
    }

    static void WriteFile(int totalAttempts)
    {
        string dirP = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string dir = Path.Combine(dirP, "MemoryGame"); // Construct the directory path
        Console.WriteLine($"Directory path: {dir}");
    
        // Ensure the directory exists
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    
        string filePath = Path.Combine(dir, "saveData.txt"); // Construct the file path
    
        try
        {
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                sw.Write(totalAttempts);
            }
            Console.WriteLine($"File written successfully at: {filePath}");
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: " + e.Message);
        }
    }

static void Render(int[,] deck, int[,] shownCards, int playerX, int playerY, int totalAttempts, int totalPaired, long unixStart, bool gameEnded)
    {
        if (gameEnded == false)
        {
            // Check x and y deck values for card values and render them accordingly
            for (int y = 0; y < deck.GetLength(0); y++)
            {
                for (int x = 0; x < deck.GetLength(1); x++)
                {
                    if (shownCards[y, x] == 1 && playerX == x && playerY == y && deck[y, x] > 9)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($" {deck[y, x]}");
                        Console.ResetColor();
                    }
                    else if (shownCards[y, x] == 1 && deck[y, x] > 9)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($" {deck[y, x]}");
                        Console.ResetColor();
                    }
                    else if (shownCards[y, x] == 1 && playerX == x && playerY == y)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($" {deck[y, x]} ");
                        Console.ResetColor();
                    }
                    else if (shownCards[y, x] == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($" {deck[y, x]} ");
                        Console.ResetColor();
                    }
                    else if (playerX == x && playerY == y)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(" ? ");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.Write(" ? ");
                    }
                }
                Console.WriteLine();
            }
            long unixNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Runtime Statistics
            Console.WriteLine($"\nTotal Attempts: {totalAttempts}\n" +
                $"Total Paired: {totalPaired}" +
                $"\nTime Elapsed: {unixNow - unixStart} Seconds");

            // Use this instead of Console.Clear() due to flicker
            Console.SetCursorPosition(0, 0);
        }
    }
}