using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Animation2D;
using Microsoft.Xna.Framework.Content;

namespace KatsnelsonM_MP2
{
    class Board
    {
        private const int GAME = 1;
        private const int LOSS = 2;
        private const int WIN = 3;
        private const int ANIMATING = 4;

        private const int EMPTY = 0;
        private const int BOMB = 1;

        const int MINE_COUNT = 0;
        private const int EMPTY_TILE = 1;

        private const int HUD_HEIGHT = 60;

        private Random rng = new Random();

        private int gameState = GAME;

        private Tile[,] board;

        private List <Point> minePos;

        private int numRows;
        private int numColumns;
        private int numMines;
        private int tileSize;

        private Texture2D [] minesImg = new Texture2D [8];

        private Texture2D clickedTileLight;
        private Texture2D clickedTileDark;
        private bool isTileDark = false;

        private int flagsPlaced = 0;

        private List<Tile> queue = new List<Tile>();

        private MouseState prevMouse;

        private Texture2D explosionImg;
        private Vector2[] explosionPos;
        private Animation [] explosionAnim;

        private bool isMuted = false;

        private Song lossMusic;
        private Song winMusic;
        private SoundEffect largeClear;
        private SoundEffect explosion;
        private SoundEffect smallClear;
        private SoundEffect clearFlag;
        private SoundEffect placeFlag;

        private GameTime gameTime = new GameTime();

        public Board(int[] dimensions, ContentManager content)
        {
            numRows = dimensions[0];
            numColumns = dimensions[1];
            numMines = dimensions[2];
            tileSize = dimensions[3];

            LoadContent(content);

            ResetBoard();
            SetUpExplosion();
        }

        private void ResetBoard()
        {
            board = new Tile[numRows, numColumns];
            Point rectPos = new Point(0, HUD_HEIGHT);

            minePos = new List <Point>(numMines);
            
            for (int row = 0; row < numRows; row++)
            {
                for (int column = 0; column < numColumns; column++)
                {
                    if (isTileDark)
                    {
                        board[row, column] = new Tile(new Rectangle(rectPos.X, rectPos.Y, tileSize, tileSize), EMPTY, minesImg[rng.Next(0, minesImg.Length)], clickedTileDark);
                    }
                    else
                    {
                        board[row, column] = new Tile(new Rectangle(rectPos.X, rectPos.Y, tileSize, tileSize), EMPTY, minesImg[rng.Next(0, minesImg.Length)], clickedTileLight);
                    }

                    isTileDark = !isTileDark;
                    rectPos.X += tileSize;
                }
                isTileDark = !isTileDark;

                rectPos.Y += tileSize;
                rectPos.X = 0;
            }

            for (int i = 0; i < numMines; i++)
            {
                minePos.Add(new Point(rng.Next(0, numRows), rng.Next(0, numColumns)));
 
                if (board[minePos[i].X, minePos[i].Y].ReadType() != BOMB)
                {
                    board[minePos[i].X, minePos[i].Y].ChangeType(BOMB);

                    CheckAdjacentTiles(minePos[i].X, minePos[i].Y, MINE_COUNT, minePos);
                }
                else
                {
                    i--;
                    minePos.RemoveAt(minePos.Count - 1);
                }

                if (minePos.Count >= numMines)
                {
                    break;
                }
            }
        }

        public void Update(MouseState mouse, bool startGame)
        {
            if (gameState == GAME)
            {
                if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton != ButtonState.Pressed && startGame)
                {
                    for (int row = 0; row < numRows; row++)
                    {
                        for (int col = 0; col < numColumns; col++)
                        {
                            if (board[row, col].CheckCollision(mouse.Position) && !board[row, col].IsFlag())
                            {
                                board[row, col].RevealTile();

                                if (board[row, col].IsTileEmpty())
                                {
                                    List<Point> queue = new List<Point>();
                                    queue.Add(new Point(row, col));
                                    EmptyTileTest(queue, new List<Point>());
                                }
                                else
                                {
                                    smallClear.CreateInstance().Play();
                                }

                                if (board[row, col].ReadType() == BOMB)
                                {
                                    for (int i = 0; i < minePos.Count; i++)
                                    {
                                        if (!board[minePos[i].X, minePos[i].Y].IsFlag())
                                        {
                                            board[minePos[i].X, minePos[i].Y].RevealTile();
                                            explosionAnim[i].isAnimating = true;
                                            explosionAnim[i].Update(gameTime);
                                            SoundEffectInstance explosion = this.explosion.CreateInstance();
                                            explosion.Volume = 0.03f;
                                            explosion.Play();
                                        }
                                    }
                                    gameState = ANIMATING;
                                }
                            }
                        }
                    }

                    if (AllTilesRevealedTest())
                    {
                        gameState = WIN;
                        MediaPlayer.Play(winMusic);
                        MediaPlayer.IsRepeating = true;

                    SetAudioState(isMuted);
                    }
                }

                if (mouse.RightButton == ButtonState.Pressed && prevMouse.RightButton != ButtonState.Pressed)
                {
                    for (int row = 0; row < numRows; row++)
                    {
                        for (int column = 0; column < numColumns; column++)
                        {
                            if (board[row, column].CheckCollision(mouse.Position) && !board[row, column].IsTileRevealed())
                            {
                                if (!board[row, column].IsFlag())
                                {
                                    board[row, column].Flag(true);
                                    flagsPlaced++;
                                    placeFlag.CreateInstance().Play();
                                }
                                else if (board[row, column].IsFlag())
                                {
                                    board[row, column].Flag(false);
                                    flagsPlaced--;
                                    clearFlag.CreateInstance().Play();
                                }
                            }
                        }
                    }
                }
            }

            else if (gameState == ANIMATING)
            {
                if (!explosionAnim[0].isAnimating)
                {
                    gameState = LOSS;
                    MediaPlayer.Play(lossMusic);
                    MediaPlayer.IsRepeating = true;

                    SetAudioState(isMuted);
                }
            }

            prevMouse = mouse;
        }

        private int EmptyTileTest(List <Point> queue, List <Point> visited)
        {
            board[queue[0].X, queue[0].Y].RevealTile();
            board[queue[0].X, queue[0].Y].Flag(false);

             if (!visited.Contains(queue[0]) && board[queue[0].X, queue[0].Y].IsTileEmpty() && !board[queue[0].X, queue[0].Y].IsFlag())
             {
                CheckAdjacentTiles(queue[0].X, queue[0].Y, EMPTY_TILE, queue);
             }

            if (!visited.Contains(queue[0]))
            {
                visited.Add(queue[0]);
            }

            queue.RemoveAt(0);

            if (queue.Count == 0)
            {
                largeClear.CreateInstance().Play();
                return 0;
            }

            return EmptyTileTest(queue, visited);
        }

        public void UpdateBombs(GameTime gameTime)
        {
            foreach (Animation bomb in explosionAnim)
            {
                bomb.Update(gameTime);
            }
        }

        private void CheckAdjacentTiles(int row, int column, int command, List <Point> queue)
        {
            if (row != 0 && column != 0)
            {
                IncMineCount(row - 1, column - 1, command);
                RevealAdjacentMines(row - 1, column - 1, command, queue);
            }

            if (row != 0)
            {
                IncMineCount(row - 1, column, command);
                RevealAdjacentMines(row - 1, column, command, queue);
            }

            if (row != 0 && column != (numColumns - 1))
            {
                IncMineCount(row - 1, column + 1, command);
                RevealAdjacentMines(row - 1, column + 1, command, queue);
            }

            if (column != 0)
            {
                IncMineCount(row, column - 1, command);
                RevealAdjacentMines(row, column - 1, command, queue);
            }

            if (row != (numRows - 1) && column != 0)
            {
                IncMineCount(row + 1, column - 1, command);
                RevealAdjacentMines(row + 1, column - 1, command, queue);
            }

            if (row != (numRows - 1))
            {
                IncMineCount(row + 1, column, command);
                RevealAdjacentMines(row + 1, column, command, queue);
            }

            if (row != (numRows - 1) && column != (numColumns - 1))
            {
                IncMineCount(row + 1, column + 1, command);
                RevealAdjacentMines(row + 1, column + 1, command, queue);
            }

            if (column != (numColumns - 1))
            {
                IncMineCount(row, column + 1, command);
                RevealAdjacentMines(row, column + 1, command, queue);
            }
        }

        private void IncMineCount(int row, int column, int command)
        {
            if (command == MINE_COUNT)
            {
                board[row, column].IncMineCount();
            }
        }

        private void RevealAdjacentMines(int row, int column, int command, List <Point> queue)
        {
            if (command == EMPTY_TILE)
            {
                queue.Add(new Point(row, column));
            }
        }

        private bool AllTilesRevealedTest()
        {
            for (int row = 0; row < numRows; row++)
            {
                for (int column = 0; column < numColumns; column++)
                {
                    if (!board[row, column].IsTileRevealed() && board[row, column].ReadType() != BOMB)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public int GetFlagsPlaced()
        {
            return flagsPlaced;
        }

        public int GetGameState()
        {
            return gameState;
        }

        private void SetUpExplosion()
        {
            explosionPos = new Vector2[numMines];
            explosionAnim = new Animation[numMines];

            for (int i = 0; i < explosionPos.Length; i++)
            {
                explosionPos[i] = new Vector2(minePos[i].Y * tileSize, minePos[i].X * tileSize + HUD_HEIGHT );
                explosionAnim[i] = new Animation(explosionImg, 5, 5, 23, 0, Animation.NO_IDLE, Animation.ANIMATE_ONCE, 5, explosionPos[i], (float)((tileSize) / (explosionImg.Height / 5f)), false);
            }
        }

        public void SetAudioState(bool isMuted)
        {
            this.isMuted = isMuted;
            if (isMuted)
            {
                MediaPlayer.Pause();
                SoundEffect.MasterVolume = 0f;
            }
            else
            {
                if (gameState == WIN || gameState == LOSS)
                {
                    MediaPlayer.Resume();
                }
                
                SoundEffect.MasterVolume = 1f;
            }
        }

        public void RestartGame(int[] dimensions)
        {
            this.numRows = dimensions[0];
            this.numColumns = dimensions[1];
            this.numMines = dimensions[2];
            this.tileSize = dimensions[3];

            ResetBoard();

            gameState = GAME;
            flagsPlaced = 0;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D[] numbersImg, Texture2D flagImg)
        {
            for (int row = 0; row < numRows; row++)
            {
                for (int column = 0; column < numColumns; column++)
                {
                    board[row, column].Draw(spriteBatch, numbersImg, flagImg);
                }
            }

            foreach(Animation exp in explosionAnim)
            {
                 exp.Draw(spriteBatch, Color.White, 0);
            }
        }

        private void LoadContent(ContentManager content)
        {
            lossMusic = content.Load<Song>("Sounds/Music/Lose");
            winMusic = content.Load<Song>("Sounds/Music/Win");

            for (int i = 0; i < minesImg.Length; i++)
            {
                minesImg[i] = content.Load<Texture2D>("Images/Sprites/Mine" + rng.Next(1, 9));
            }

            clickedTileDark = content.Load<Texture2D>("Images/Sprites/Clear_Dark");
            clickedTileLight = content.Load<Texture2D>("Images/Sprites/Clear_Light");

            explosionImg = content.Load<Texture2D>("Images/Sprites/explode");

            largeClear = content.Load<SoundEffect>("Sounds/Sound Effects/LargeClear");
            smallClear = content.Load<SoundEffect>("Sounds/Sound Effects/SmallClear");
            explosion = content.Load<SoundEffect>("Sounds/Sound Effects/mixkit-underground-explosion-impact-echo-1686");
            placeFlag = content.Load<SoundEffect>("Sounds/Sound Effects/PlaceFlag");
            clearFlag = content.Load<SoundEffect>("Sounds/Sound Effects/ClearFlag");
        }
    }
}
