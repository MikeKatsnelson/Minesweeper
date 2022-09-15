//Author: Mike Katsnelson
//File Name: Game1.cs
//Project Name: MP2
//Creation Date: April 28, 2022
//Modified Date: May 7, 2022
//Description: This program mimics Google's aesthetic of Minesweeper


using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Animation2D;
using Helper;

namespace KatsnelsonM_MP2
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        const int INSTRUCTIONS = 0;
        const int GAME = 1;
        const int LOSS = 2;
        const int WIN = 3;

        const int EASY = 0;
        const int MEDIUM = 1;
        const int HARD = 2;

        const int SWITCH = 3;

        const int HUD_HEIGHT = 60;

        StreamReader inFile;
        StreamWriter outFile;

        Random rng = new Random();

        int difficulty = EASY;

        bool switchDiff = false;
        int[] checkY = { 21, 46, 71 };
        int[] checkX = { 10, 19, 27 };

        bool startGame = false;

        bool readOnce = true;

        Board board;

        Timer timer = new Timer(Timer.INFINITE_TIMER, true);

        //Store the board options, ranging from EASY, MEDIUM, to HARD, in the following order: 
        //# of rows, # of columns, # of Mines, tile size (width AND height)
        int[][] boardOptions = new int[][]
            {new int[] {8, 10, 10, 45},
            new int[]{14, 18, 40, 30},
            new int[] {20, 24, 99, 25}};

        private MouseState prevMouse;

        bool firstRun = true;
        bool isMuted = false;
        bool restartOnce = false;

        int [] bestTime = new int[3];
        int currentTime;

        Texture2D bgImg;
        Rectangle bgRec;

        Texture2D hudImg;
        Rectangle hudRec;

        Texture2D flagImg;
        Rectangle flagRecHud;
        SpriteFont flagText;

        Texture2D watchImg;
        Rectangle watchRec;

        Texture2D[] minesImg = new Texture2D[99];
        Rectangle[] minesRec = new Rectangle[99];

        Texture2D[] numImg = new Texture2D[8];
        Rectangle[] numRec = new Rectangle[440];

        Texture2D winImg;
        Texture2D lossImg;
        Rectangle gameOverRec;

        Texture2D lossButtonImg;
        Texture2D winButtonImg;
        Rectangle gameOverButtonRec;

        Texture2D gameOverShadowImg;
        Rectangle gameOverShadowRec;

        Texture2D[] diffImg = new Texture2D[4];
        Rectangle[] diffRec = new Rectangle[4];

        Rectangle[] switchDiffRec = new Rectangle[3];
        GameRectangle[] switchDiffRect = new GameRectangle[3];

        Texture2D checkMarkImg;
        Rectangle checkMarkRec;

        Texture2D exitImg;
        Rectangle exitRec;

        Texture2D instructionImg;
        Vector2 instructionPos;
        Animation instructionAnim;

        Texture2D[] muteImg = new Texture2D[2];
        Rectangle muteRec;

        Texture2D noTimeImg;
        Rectangle [] noTimeRec = new Rectangle[2];

        Vector2[] timePos = new Vector2[2];

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            this.IsMouseVisible = true;

            Window.IsBorderless = true;

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            ReadFile();

            switch (difficulty)
            {
                case EASY:
                    bgImg = Content.Load<Texture2D>("Images/Backgrounds/board_easy");
                    bgRec = new Rectangle(0, HUD_HEIGHT, bgImg.Width, bgImg.Height);
                    break;
                case MEDIUM:
                    bgImg = Content.Load<Texture2D>("Images/Backgrounds/board_med");
                    bgRec = new Rectangle(0, HUD_HEIGHT, bgImg.Width, bgImg.Height);
                    break;
                case HARD:
                    bgImg = Content.Load<Texture2D>("Images/Backgrounds/board_hard");
                    bgRec = new Rectangle(0, HUD_HEIGHT, bgImg.Width, bgImg.Height);
                    break;
            }

            graphics.PreferredBackBufferWidth = bgRec.Width;
            graphics.PreferredBackBufferHeight = bgRec.Height + HUD_HEIGHT;
            graphics.ApplyChanges();

            instructionImg = Content.Load<Texture2D>("Images/Sprites/Instructions");
            instructionPos = new Vector2(Convert.ToInt32((GraphicsDevice.Viewport.Width / 2) - (instructionImg.Width * 0.13)), Convert.ToInt32(0.5 * GraphicsDevice.Viewport.Height - instructionImg.Height * 0.4));
            instructionAnim = new Animation(instructionImg, 2, 1, 2, 0, Animation.NO_IDLE, Animation.ANIMATE_FOREVER, 140, instructionPos, 0.5f, false);

            if (firstRun)
            {
                instructionAnim.isAnimating = true;
            }

            hudImg = Content.Load<Texture2D>("Images/Backgrounds/HUDBar");
            hudRec = new Rectangle(0, 0, bgImg.Width, HUD_HEIGHT);

            flagImg = Content.Load<Texture2D>("Images/Sprites/flag");
            flagRecHud = new Rectangle(bgImg.Width - Convert.ToInt32(bgImg.Width * 0.7), 15, Convert.ToInt32(flagImg.Width * 0.3), Convert.ToInt32(flagImg.Height * 0.3));
            flagText = Content.Load<SpriteFont>("Images/Fonts/FlagFont");

            watchImg = Content.Load<Texture2D>("Images/Sprites/Watch");
            watchRec = new Rectangle(bgImg.Width - Convert.ToInt32(bgImg.Width * 0.5), 13, Convert.ToInt32(watchImg.Width * 0.3), Convert.ToInt32(watchImg.Height * 0.3));

            winImg = Content.Load<Texture2D>("Images/Backgrounds/GameOver_WinResults");
            lossImg = Content.Load<Texture2D>("Images/Backgrounds/GameOver_Results");
            gameOverRec = new Rectangle(Convert.ToInt32((0.5 * GraphicsDevice.Viewport.Width) - (0.5 * lossImg.Width)), Convert.ToInt32((0.5 * GraphicsDevice.Viewport.Height) - (0.5 * lossImg.Height) - 35), lossImg.Width, lossImg.Height);

            gameOverShadowImg = Content.Load<Texture2D>("Images/Backgrounds/GameOverBoardShadow");
            gameOverShadowRec = new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            lossButtonImg = Content.Load<Texture2D>("Images/Backgrounds/GameOver_TryAgain");
            winButtonImg = Content.Load<Texture2D>("Images/Backgrounds/GameOver_PlayAgain");
            gameOverButtonRec = new Rectangle(Convert.ToInt32((0.5 * GraphicsDevice.Viewport.Width) - (0.5 * lossButtonImg.Width)), Convert.ToInt32((0.5 * GraphicsDevice.Viewport.Height) - (0.5 * lossButtonImg.Height) + 125), lossButtonImg.Width, lossButtonImg.Height);

            diffImg[EASY] = Content.Load<Texture2D>("Images/Sprites/EasyButton");
            diffImg[MEDIUM] = Content.Load<Texture2D>("Images/Sprites/MedButton");
            diffImg[HARD] = Content.Load<Texture2D>("Images/Sprites/HardButton");
            diffImg[SWITCH] = Content.Load<Texture2D>("Images/Sprites/DropDown");

            for (int i = 0; i < diffRec.Length; i++)
            {
                diffRec[i] = new Rectangle(Convert.ToInt32((0.12 * GraphicsDevice.Viewport.Width) - (0.5 * diffImg[i].Width)), 15, diffImg[i].Width, diffImg[i].Height);
            }

            int a = 0;
            for (int i = 0; i < switchDiffRec.Length; i++)
            {
                switchDiffRec[i] = new Rectangle(Convert.ToInt32((0.05 * GraphicsDevice.Viewport.Width) - (0.5 * diffImg[0].Width)), 15 + a, 110, 26);
                a += 26;
            }

            checkMarkImg = Content.Load<Texture2D>("Images/Sprites/Check");
            checkMarkRec = new Rectangle(checkX[difficulty], checkY[difficulty], checkMarkImg.Width, checkMarkImg.Height);

            exitImg = Content.Load<Texture2D>("Images/Sprites/Exit");
            exitRec = new Rectangle(Convert.ToInt32(GraphicsDevice.Viewport.Width - (0.5 * exitImg.Width)), 16, Convert.ToInt32((0.25 * exitImg.Width)), Convert.ToInt32((0.25 * exitImg.Height)));

            for (int i = 0; i < numImg.Length; i++)
            {
                numImg[i] = Content.Load<Texture2D>("Images/Sprites/" + (i + 1));
            }

            muteImg[0] = Content.Load<Texture2D>("Images/Sprites/SoundOn");
            muteImg[1] = Content.Load<Texture2D>("Images/Sprites/SoundOff");

            muteRec = new Rectangle(Convert.ToInt32(GraphicsDevice.Viewport.Width - (3 * muteImg[0].Width)), 14, Convert.ToInt32(0.8 * muteImg[0].Width), Convert.ToInt32(0.8* muteImg[0].Height));

            noTimeImg = Content.Load<Texture2D>("Images/Sprites/GameOver_NoTime");
            noTimeRec[0] = new Rectangle(Convert.ToInt32(0.235 * (gameOverRec.Width - noTimeImg.Width) + 0.5 * (GraphicsDevice.Viewport.Width - gameOverRec.Width)), 
                                        Convert.ToInt32(0.38 * (gameOverRec.Height - noTimeImg.Height) + 0.5 * (GraphicsDevice.Viewport.Height - gameOverRec.Height)),
                                        noTimeImg.Width, noTimeImg.Height);

            noTimeRec[1] = new Rectangle(Convert.ToInt32(0.765 * (gameOverRec.Width - noTimeImg.Width) + 0.5 * (GraphicsDevice.Viewport.Width - gameOverRec.Width)), 
                                        Convert.ToInt32(0.38 * (gameOverRec.Height - noTimeImg.Height) + 0.5 * (GraphicsDevice.Viewport.Height - gameOverRec.Height)), 
                                        noTimeImg.Width, noTimeImg.Height);

            timePos[0] = new Vector2(Convert.ToInt32(0.3 * (gameOverRec.Width - noTimeImg.Width) + 0.5 * (GraphicsDevice.Viewport.Width - gameOverRec.Width)),
                                     Convert.ToInt32(0.33 * (gameOverRec.Height - noTimeImg.Height) + 0.5 * (GraphicsDevice.Viewport.Height - gameOverRec.Height)));

            timePos[1] = new Vector2(Convert.ToInt32(0.83 * (gameOverRec.Width - noTimeImg.Width) + 0.5 * (GraphicsDevice.Viewport.Width - gameOverRec.Width)),
                                     Convert.ToInt32(0.33 * (gameOverRec.Height - noTimeImg.Height) + 0.5 * (GraphicsDevice.Viewport.Height - gameOverRec.Height)));

            board = new Board(boardOptions[difficulty], Content);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            if (instructionAnim.isAnimating)
            {
                instructionAnim.Update(gameTime);
            }


            if (Mouse.GetState().RightButton == ButtonState.Pressed && prevMouse.RightButton != ButtonState.Pressed)
            {
                StartConditions();
            }


            if (Mouse.GetState().LeftButton == ButtonState.Pressed && prevMouse.LeftButton != ButtonState.Pressed)
            {
                if (!startGame)
                {
                    StartConditions();
                }

                if (board.GetGameState() == GAME && diffRec[difficulty].Contains(Mouse.GetState().Position) && !switchDiff)
                {
                    switchDiff = true;
                }
                else if (board.GetGameState() == GAME && diffRec[SWITCH].Contains(Mouse.GetState().Position) && switchDiff)
                {
                    for (int i = 0; i < switchDiffRec.Length; i++)
                    {
                        if (switchDiffRec[i].Contains(Mouse.GetState().Position))
                        {
                            switchDiff = false;
                            difficulty = i;
                            RestartGame();
                            checkMarkRec.Y = checkY[i];
                            break;
                        }
                    }
                }
                else if(switchDiff)
                {
                    switchDiff = false;
                }

                if (exitRec.Contains(Mouse.GetState().Position))
                {
                    Environment.Exit(0);
                }
                else if (muteRec.Contains(Mouse.GetState().Position))
                {
                    isMuted = !isMuted;
                    board.SetAudioState(isMuted);
                }
            }

            if (startGame && board.GetGameState() == GAME)
            {
                if (timer.GetTimePassed() < 999000)
                {
                    timer.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
                }
            }

            board.Update(Mouse.GetState(), startGame);


            if (board.GetGameState() == LOSS || board.GetGameState() == WIN)
            {
                if (readOnce)
                {
                    currentTime = Convert.ToInt32(Math.Truncate(timer.GetTimePassed() / 1000).ToString("000"));
                    WriteFile();
                    readOnce = false;
                }

                if (Mouse.GetState().LeftButton == ButtonState.Pressed && prevMouse.LeftButton != ButtonState.Pressed && gameOverButtonRec.Contains(Mouse.GetState().Position))
                {
                    RestartGame();
                    MediaPlayer.Stop();
                }
            }


            prevMouse = Mouse.GetState();

            board.UpdateBombs(gameTime);

            board.SetAudioState(isMuted);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            spriteBatch.Draw(bgImg, bgRec, Color.White);
            spriteBatch.Draw(hudImg, hudRec, Color.White);

            board.Draw(spriteBatch, numImg, flagImg);
            spriteBatch.DrawString(flagText, Math.Truncate(timer.GetTimePassed() / 1000).ToString("000"), new Vector2(bgImg.Width - Convert.ToInt32(bgImg.Width * 0.4), 20), Color.White);
            spriteBatch.Draw(flagImg, flagRecHud, Color.White);
            spriteBatch.DrawString(flagText, Convert.ToString(boardOptions[difficulty][2] - board.GetFlagsPlaced()), new Vector2(bgImg.Width - Convert.ToInt32(bgImg.Width * 0.61), 20), Color.White);

            if (instructionAnim.isAnimating)
            {
                instructionAnim.Draw(spriteBatch, Color.White, 0);
            }

            if (!switchDiff)
            {
                spriteBatch.Draw(diffImg[difficulty], diffRec[difficulty], Color.White);
            }
            else
            {
                spriteBatch.Draw(diffImg[SWITCH], diffRec[SWITCH], Color.White);
                spriteBatch.Draw(checkMarkImg, checkMarkRec, Color.White);
            }

            spriteBatch.Draw(watchImg, watchRec, Color.White);

            // TODO: Add your drawing code here
            switch (board.GetGameState())
            {
                case INSTRUCTIONS:
                    break;

                case GAME:
                    break;

                case LOSS:
                    spriteBatch.Draw(gameOverShadowImg, gameOverShadowRec, Color.White * 0.8f);
                    spriteBatch.Draw(lossImg, gameOverRec, Color.White);
                    spriteBatch.Draw(lossButtonImg, gameOverButtonRec, Color.White);
                    
                    if (bestTime[difficulty] == 0)
                    {
                        spriteBatch.Draw(noTimeImg, noTimeRec[0], Color.White);
                        spriteBatch.Draw(noTimeImg, noTimeRec[1], Color.White);
                    }
                    else
                    {
                        spriteBatch.Draw(noTimeImg, noTimeRec[0], Color.White);
                        spriteBatch.DrawString(flagText, Convert.ToString(bestTime[difficulty]), timePos[1], Color.White);
                    }
                    break;

                case WIN:
                    spriteBatch.Draw(gameOverShadowImg, gameOverShadowRec, Color.White * 0.8f);
                    spriteBatch.Draw(winImg, gameOverRec, Color.White);
                    spriteBatch.Draw(winButtonImg, gameOverButtonRec, Color.White);

                    if (bestTime[difficulty] == 0)
                    {
                        spriteBatch.Draw(noTimeImg, noTimeRec[0], Color.White);
                    }
                    else
                    {
                        spriteBatch.DrawString(flagText, Convert.ToString(bestTime[difficulty]), timePos[0], Color.White);
                    }

                    spriteBatch.DrawString(flagText, Convert.ToString(currentTime), timePos[1], Color.White);
                    break;
            }


            spriteBatch.Draw(exitImg, exitRec, Color.White);

            if (!isMuted)
            {
                spriteBatch.Draw(muteImg[0], muteRec, Color.White);
            }
            else
            {
                spriteBatch.Draw(muteImg[1], muteRec, Color.White);
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }

        private void RestartGame()
        {
            startGame = false;
            timer.ResetTimer(true);
            board.RestartGame(boardOptions[difficulty]);
            WriteFile();

            LoadContent();
            graphics.PreferredBackBufferWidth = bgRec.Width;
            graphics.PreferredBackBufferHeight = bgRec.Height + HUD_HEIGHT;
            graphics.ApplyChanges();

            readOnce = true;
        }

        private void StartConditions()
        {
            if (!startGame)
            {
                startGame = true;
                timer.Activate();
                instructionAnim.isAnimating = false;
                firstRun = false;
            }
        }

        private void ReadFile()
        {
            try
            {
                inFile = File.OpenText("results.txt");

                int savedDiff = Convert.ToInt32(inFile.ReadLine());

                if (!restartOnce)
                {
                    if (savedDiff >= EASY && savedDiff <= HARD)
                    {
                        difficulty = savedDiff;
                        restartOnce = true;
                        inFile.Close();
                        RestartGame();
                    }
                }

                for (int i = 0; i < bestTime.Length; i++)
                {
                    bestTime[i] = Convert.ToInt32(inFile.ReadLine());
                }

                inFile.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        private void WriteFile()
        {
            try
            {
                outFile = File.CreateText("results.txt");

                outFile.WriteLine(difficulty);
                
                if (board.GetGameState() == WIN)
                {
                    if (currentTime < bestTime[difficulty] || bestTime[difficulty] == 0)
                    {
                        bestTime[difficulty] = currentTime;
                    }
                }

                for (int i = 0; i < bestTime.Length; i++)
                {
                    outFile.WriteLine(bestTime[i]);
                }

                outFile.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }
    }
}
