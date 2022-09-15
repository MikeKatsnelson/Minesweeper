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

namespace KatsnelsonM_MP2
{
    class Tile
    {
        const int EMPTY = 0;
        const int BOMB = 1;

        bool flag = false;

        private int adjMines = 0;

        private int tileType;

        private bool reveal = false;

        private Texture2D clickedTileImg;

        private Rectangle tile;

        Texture2D mineImg;

        public Tile(Rectangle tile, int tileType, Texture2D mineImg, Texture2D clickedTileImg)
        {
            this.tileType = tileType;
            this.clickedTileImg = clickedTileImg;
            this.tile = tile;
            this.mineImg = mineImg;
        }

        public int ReadType()
        {
            return tileType; 
        }

        public void ChangeType(int tileType)
        {
            this.tileType = tileType;
        }

        public void IncMineCount()
        {
            adjMines++;
        }

        public bool IsTileEmpty()
        {
            if (adjMines == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public bool IsTileRevealed()
        {
            return reveal;
        }

        public bool CheckCollision(Point mousePos)
        {
            if (!reveal && tile.Contains(mousePos))
            {
                return true;
            }
            return false;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D[] numbersImg, Texture2D flagImg)
        {
            if (reveal)
            {
                spriteBatch.Draw(clickedTileImg, tile, Color.White);

                if (tileType == BOMB)
                {
                    spriteBatch.Draw(mineImg, tile, Color.White);
                }

                if (adjMines > 0 && tileType != BOMB)
                {
                    spriteBatch.Draw(numbersImg[adjMines - 1], tile, Color.White);
                }
            }

            if (flag)
            {
                spriteBatch.Draw(flagImg, tile, Color.White);
            }

        }

        public void RevealTile()
        {
            reveal = true;
        }

        public bool IsFlag()
        {
            return flag;
        }

        public void Flag(bool flag)
        {
            this.flag = flag;
        }
    }
}
