using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.IO;

namespace cli_life
{
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
        public char charRepresentation()
        {
            return IsAlive ? '1' : '0';
        }
        public void boolRepresentation(char ch)
        {
            IsAlive =  ch == '1' ? true : false;
        }
    }
    public class BoardSettings
    {
        public int width { get; set; }
        public int height { get; set; }
        public int cellSize { get; set; }
        public double liveDensity { get; set; }

        public BoardSettings(int width, int height, int cellSize, double liveDensity)
        {
            this.width = width;
            this.height = height;
            this.cellSize = cellSize;
            this.liveDensity = liveDensity;
        }

        public BoardSettings()
        {
            width = 0;
            height = 0;
            cellSize = 0;
            liveDensity = 0;
        }

        public BoardSettings(BoardSettings bs)
        {
            this.width = bs.width;
            this.height = bs.height;
            this.cellSize = bs.cellSize;
            this.liveDensity = bs.liveDensity;
        }
        
        public static void writeToFile(string filename, BoardSettings settings)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(filename, jsonString);
        }
    }
    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;

        public int Columns { get { return Cells.GetLength(1); } }
        public int Rows { get { return Cells.GetLength(0); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, double liveDensity = .1)
        {
            CellSize = cellSize;

            Cells = new Cell[height / cellSize, width / cellSize];
            for (int x = 0; x < Rows; x++)
                for (int y = 0; y < Columns; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        public Board(BoardSettings settings) : this(settings.width, settings.height, settings.cellSize, settings.liveDensity)
        {
        }

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }
        private void ConnectNeighbors()
        {
            for (int x = 0; x < Rows; x++)
            {
                for (int y = 0; y < Columns; y++)
                {
                    int xL = (x > 0) ? x - 1 : Rows - 1;
                    int xR = (x < Rows - 1) ? x + 1 : 0;

                    int yT = (y > 0) ? y - 1 : Columns - 1;
                    int yB = (y < Columns - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }
        public static void WriteToFile(string filename, Board board)
        {
            using (StreamWriter sw = File.CreateText(filename))
            {
                int col = board.Columns;
                int row = board.Rows;
                int cellSize = board.CellSize;

                sw.WriteLine(col.ToString());
                sw.WriteLine(row.ToString());
                sw.WriteLine(cellSize.ToString());

                for (int i = 0; i < row; i++)
                {
                    for (int j = 0; j < col; j++)
                    {
                        sw.Write(board.Cells[i, j].charRepresentation());
                    }
                    sw.Write('\n');
                }
            }
        }
        public static Board ReadFromFile(string filename)
        {
            using (StreamReader sr = File.OpenText(filename))
            {
                string currentStr;

                currentStr = sr.ReadLine();
                int c = int.Parse(currentStr);

                currentStr = sr.ReadLine();
                int r = int.Parse(currentStr);

                currentStr = sr.ReadLine();
                int cellSize = int.Parse(currentStr);

                Board board = new Board(c, r, cellSize, 0);

                for (int i = 0; i < r; i++)
                {
                    currentStr = sr.ReadLine();

                    for (int j = 0; j < c; j++)
                    {
                        char ch = currentStr[j];
                        board.Cells[i, j].boolRepresentation(ch);
                    }
                }

                return board;
            }
        }
    }
    class Program
    {
        static Board board;
        static private void Reset()
        {
            string filename = "BoardSettings.json";

            BoardSettings settings = new BoardSettings(50,20,1,0.5f);

            if (File.Exists(filename))
            {
                settings = new BoardSettings(JsonSerializer.Deserialize<BoardSettings>(File.ReadAllText(filename)));
            }
            else
            {
                BoardSettings.writeToFile(filename, settings);
            }

            board = new Board(settings);
        }
        static void Render()
        {
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)   
                {
                    var cell = board.Cells[row, col];
                    if (cell.IsAlive)
                    {
                        Console.Write('*');
                    }
                    else
                    {
                        Console.Write(' ');
                    }
                }
                Console.Write('\n');
            }
        }
        static void Main(string[] args)
        {
            Reset();

            string filename = "firstBoard.txt";

            board = Board.ReadFromFile(filename);

            while (true)
            {
                Console.Clear();
                Render();
                board.Advance();
                Thread.Sleep(100);
            }
        }
    }
}