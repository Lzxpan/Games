using System.Drawing.Drawing2D;

namespace Games
{
    public partial class Form1 : Form
    {
        private const int BoardWidth = 10;
        private const int BoardHeight = 20;
        private const int CellSize = 30;
        private const int SidePanelWidth = 220;
        private const int TopPadding = 16;
        private const int LeftPadding = 16;
        private const int ShakeDurationFrames = 6;
        private const int ShakeAmplitude = 5;

        private readonly int[,] board = new int[BoardHeight, BoardWidth];
        private readonly Color[] pieceColors =
        {
            Color.Transparent,
            Color.Cyan,
            Color.Blue,
            Color.Orange,
            Color.Yellow,
            Color.LimeGreen,
            Color.MediumPurple,
            Color.Red
        };

        private readonly Point[][] pieceDefinitions =
        {
            new[] { new Point(0, 1), new Point(1, 1), new Point(2, 1), new Point(3, 1) }, // I
            new[] { new Point(0, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1) }, // J
            new[] { new Point(2, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1) }, // L
            new[] { new Point(1, 0), new Point(2, 0), new Point(1, 1), new Point(2, 1) }, // O
            new[] { new Point(1, 0), new Point(2, 0), new Point(0, 1), new Point(1, 1) }, // S
            new[] { new Point(1, 0), new Point(0, 1), new Point(1, 1), new Point(2, 1) }, // T
            new[] { new Point(0, 0), new Point(1, 0), new Point(1, 1), new Point(2, 1) }  // Z
        };

        private readonly Random random = new();
        private readonly System.Windows.Forms.Timer gameTimer = new();
        private readonly System.Windows.Forms.Timer shakeTimer = new();

        private FallingPiece currentPiece = null!;
        private FallingPiece nextPiece = null!;

        private int score;
        private int linesCleared;
        private int level = 1;
        private bool isGameOver;
        private int shakeFramesRemaining;
        private Point shakeOffset = Point.Empty;

        public Form1()
        {
            InitializeComponent();
            Text = "俄羅斯方塊 (Tetris)";
            ClientSize = new Size(LeftPadding * 2 + BoardWidth * CellSize + SidePanelWidth, TopPadding * 2 + BoardHeight * CellSize);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(24, 24, 28);
            ForeColor = Color.White;
            KeyPreview = true;
            DoubleBuffered = true;

            gameTimer.Tick += (_, _) => GameTick();
            shakeTimer.Interval = 16;
            shakeTimer.Tick += ShakeTimer_Tick;
            KeyDown += Form1_KeyDown;

            StartNewGame();
        }

        private void StartNewGame()
        {
            Array.Clear(board);
            score = 0;
            linesCleared = 0;
            level = 1;
            isGameOver = false;
            shakeFramesRemaining = 0;
            shakeOffset = Point.Empty;
            shakeTimer.Stop();

            nextPiece = CreateRandomPiece();
            SpawnNewPiece();
            UpdateSpeed();
            gameTimer.Start();
            Invalidate();
        }

        private FallingPiece CreateRandomPiece()
        {
            int type = random.Next(pieceDefinitions.Length) + 1;
            Point[] template = pieceDefinitions[type - 1];
            Point[] blocks = template.Select(p => new Point(p.X, p.Y)).ToArray();
            return new FallingPiece(type, blocks, new Point(3, 0));
        }

        private void SpawnNewPiece()
        {
            currentPiece = nextPiece;
            currentPiece.Position = new Point(3, 0);
            nextPiece = CreateRandomPiece();

            if (!CanPlace(currentPiece, currentPiece.Position, currentPiece.Blocks))
            {
                isGameOver = true;
                gameTimer.Stop();
            }
        }

        private void GameTick()
        {
            if (isGameOver)
            {
                return;
            }

            if (!MovePiece(0, 1))
            {
                LockPiece();
            }

            Invalidate();
        }

        private bool MovePiece(int dx, int dy)
        {
            Point target = new(currentPiece.Position.X + dx, currentPiece.Position.Y + dy);
            if (!CanPlace(currentPiece, target, currentPiece.Blocks))
            {
                return false;
            }

            currentPiece.Position = target;
            return true;
        }

        private void RotatePiece()
        {
            if (isGameOver || currentPiece.Type == 4)
            {
                return;
            }

            Point[] rotated = currentPiece.Blocks
                .Select(block => new Point(3 - block.Y, block.X))
                .ToArray();

            int[] kicks = { 0, -1, 1, -2, 2 };
            foreach (int kick in kicks)
            {
                Point testPos = new(currentPiece.Position.X + kick, currentPiece.Position.Y);
                if (CanPlace(currentPiece, testPos, rotated))
                {
                    currentPiece.Blocks = rotated;
                    currentPiece.Position = testPos;
                    Invalidate();
                    return;
                }
            }
        }

        private void HardDrop()
        {
            int dropped = 0;
            while (MovePiece(0, 1))
            {
                dropped++;
            }

            score += dropped * 2;
            LockPiece();
            Invalidate();
        }

        private void LockPiece()
        {
            StartLandingShake();

            foreach (Point block in currentPiece.Blocks)
            {
                int x = currentPiece.Position.X + block.X;
                int y = currentPiece.Position.Y + block.Y;
                if (y >= 0 && y < BoardHeight && x >= 0 && x < BoardWidth)
                {
                    board[y, x] = currentPiece.Type;
                }
            }

            int cleared = ClearLines();
            if (cleared > 0)
            {
                linesCleared += cleared;
                score += cleared switch
                {
                    1 => 100 * level,
                    2 => 300 * level,
                    3 => 500 * level,
                    4 => 800 * level,
                    _ => 0
                };

                int newLevel = Math.Max(1, linesCleared / 10 + 1);
                if (newLevel != level)
                {
                    level = newLevel;
                    UpdateSpeed();
                }
            }

            SpawnNewPiece();
        }

        private void StartLandingShake()
        {
            shakeFramesRemaining = ShakeDurationFrames;
            shakeOffset = new Point(
                random.Next(-ShakeAmplitude, ShakeAmplitude + 1),
                random.Next(-ShakeAmplitude, ShakeAmplitude + 1));

            if (!shakeTimer.Enabled)
            {
                shakeTimer.Start();
            }

            Invalidate();
        }

        private void ShakeTimer_Tick(object? sender, EventArgs e)
        {
            if (shakeFramesRemaining <= 0)
            {
                shakeOffset = Point.Empty;
                shakeTimer.Stop();
                Invalidate();
                return;
            }

            shakeFramesRemaining--;
            if (shakeFramesRemaining == 0)
            {
                shakeOffset = Point.Empty;
                shakeTimer.Stop();
            }
            else
            {
                shakeOffset = new Point(
                    random.Next(-ShakeAmplitude, ShakeAmplitude + 1),
                    random.Next(-ShakeAmplitude, ShakeAmplitude + 1));
            }

            Invalidate();
        }

        private int ClearLines()
        {
            int cleared = 0;
            for (int y = BoardHeight - 1; y >= 0; y--)
            {
                bool full = true;
                for (int x = 0; x < BoardWidth; x++)
                {
                    if (board[y, x] == 0)
                    {
                        full = false;
                        break;
                    }
                }

                if (!full)
                {
                    continue;
                }

                cleared++;
                for (int row = y; row > 0; row--)
                {
                    for (int col = 0; col < BoardWidth; col++)
                    {
                        board[row, col] = board[row - 1, col];
                    }
                }

                for (int col = 0; col < BoardWidth; col++)
                {
                    board[0, col] = 0;
                }

                y++;
            }

            return cleared;
        }

        private bool CanPlace(FallingPiece piece, Point position, Point[] blocks)
        {
            foreach (Point block in blocks)
            {
                int x = position.X + block.X;
                int y = position.Y + block.Y;

                if (x < 0 || x >= BoardWidth || y >= BoardHeight)
                {
                    return false;
                }

                if (y >= 0 && board[y, x] != 0)
                {
                    return false;
                }
            }

            return true;
        }

        private void UpdateSpeed()
        {
            gameTimer.Interval = Math.Max(80, 550 - (level - 1) * 45);
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (isGameOver)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    StartNewGame();
                }

                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.A:
                    MovePiece(-1, 0);
                    break;
                case Keys.Right:
                case Keys.D:
                    MovePiece(1, 0);
                    break;
                case Keys.Down:
                case Keys.S:
                    if (MovePiece(0, 1))
                    {
                        score += 1;
                    }

                    break;
                case Keys.Up:
                case Keys.W:
                    RotatePiece();
                    break;
                case Keys.Space:
                    RotatePiece();
                    break;
                case Keys.P:
                    TogglePause();
                    break;
            }

            Invalidate();
        }

        private void TogglePause()
        {
            if (isGameOver)
            {
                return;
            }

            gameTimer.Enabled = !gameTimer.Enabled;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (shakeOffset != Point.Empty)
            {
                g.TranslateTransform(shakeOffset.X, shakeOffset.Y);
            }

            DrawBoard(g);
            DrawCurrentPiece(g);
            DrawSidePanel(g);

            if (!gameTimer.Enabled && !isGameOver)
            {
                DrawOverlay(g, "已暫停\n按 P 繼續");
            }

            if (isGameOver)
            {
                DrawOverlay(g, "遊戲結束\n按 Enter 重新開始");
            }
        }

        private void DrawBoard(Graphics g)
        {
            Rectangle boardArea = new(LeftPadding, TopPadding, BoardWidth * CellSize, BoardHeight * CellSize);
            using SolidBrush bgBrush = new(Color.FromArgb(30, 30, 36));
            g.FillRectangle(bgBrush, boardArea);

            using Pen gridPen = new(Color.FromArgb(50, 55, 65));
            for (int x = 0; x <= BoardWidth; x++)
            {
                int px = LeftPadding + x * CellSize;
                g.DrawLine(gridPen, px, TopPadding, px, TopPadding + BoardHeight * CellSize);
            }

            for (int y = 0; y <= BoardHeight; y++)
            {
                int py = TopPadding + y * CellSize;
                g.DrawLine(gridPen, LeftPadding, py, LeftPadding + BoardWidth * CellSize, py);
            }

            for (int y = 0; y < BoardHeight; y++)
            {
                for (int x = 0; x < BoardWidth; x++)
                {
                    int value = board[y, x];
                    if (value != 0)
                    {
                        DrawCell(g, x, y, pieceColors[value]);
                    }
                }
            }
        }

        private void DrawCurrentPiece(Graphics g)
        {
            if (isGameOver)
            {
                return;
            }

            foreach (Point block in currentPiece.Blocks)
            {
                int x = currentPiece.Position.X + block.X;
                int y = currentPiece.Position.Y + block.Y;
                if (y >= 0)
                {
                    DrawCell(g, x, y, pieceColors[currentPiece.Type]);
                }
            }
        }

        private void DrawCell(Graphics g, int gridX, int gridY, Color color)
        {
            Rectangle rect = new(
                LeftPadding + gridX * CellSize + 1,
                TopPadding + gridY * CellSize + 1,
                CellSize - 2,
                CellSize - 2);

            using LinearGradientBrush brush = new(rect, ControlPaint.Light(color), color, 45f);
            g.FillRectangle(brush, rect);
            using Pen border = new(ControlPaint.Dark(color));
            g.DrawRectangle(border, rect);
        }

        private void DrawSidePanel(Graphics g)
        {
            int panelX = LeftPadding + BoardWidth * CellSize + 20;
            using Font titleFont = new("Segoe UI", 13, FontStyle.Bold);
            using Font normalFont = new("Segoe UI", 10, FontStyle.Regular);
            using SolidBrush textBrush = new(Color.WhiteSmoke);

            g.DrawString("俄羅斯方塊", titleFont, textBrush, panelX, TopPadding);
            g.DrawString($"分數: {score}", normalFont, textBrush, panelX, TopPadding + 44);
            g.DrawString($"消行: {linesCleared}", normalFont, textBrush, panelX, TopPadding + 70);
            g.DrawString($"等級: {level}", normalFont, textBrush, panelX, TopPadding + 96);

            g.DrawString("下一個方塊:", normalFont, textBrush, panelX, TopPadding + 140);
            DrawNextPiecePreview(g, panelX, TopPadding + 168);

            g.DrawString("操作說明:", normalFont, textBrush, panelX, TopPadding + 320);
            g.DrawString("←/→ 或 A/D：左右移動\n↑ / W / Space：旋轉\n↓ 或 S：加速下落\nP：暫停", normalFont, textBrush, panelX, TopPadding + 348);
        }

        private void DrawNextPiecePreview(Graphics g, int x, int y)
        {
            Rectangle previewArea = new(x, y, 4 * 24, 4 * 24);
            using Pen border = new(Color.DimGray);
            g.DrawRectangle(border, previewArea);

            foreach (Point block in nextPiece.Blocks)
            {
                Rectangle rect = new(x + block.X * 24 + 1, y + block.Y * 24 + 1, 22, 22);
                Color color = pieceColors[nextPiece.Type];
                using SolidBrush fill = new(color);
                g.FillRectangle(fill, rect);
                g.DrawRectangle(Pens.Black, rect);
            }
        }

        private void DrawOverlay(Graphics g, string text)
        {
            Rectangle area = new(LeftPadding, TopPadding, BoardWidth * CellSize, BoardHeight * CellSize);
            using SolidBrush overlay = new(Color.FromArgb(160, 0, 0, 0));
            g.FillRectangle(overlay, area);

            using Font font = new("Segoe UI", 16, FontStyle.Bold);
            TextRenderer.DrawText(g, text, font, area, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
        }

        private sealed class FallingPiece
        {
            public FallingPiece(int type, Point[] blocks, Point position)
            {
                Type = type;
                Blocks = blocks;
                Position = position;
            }

            public int Type { get; }
            public Point[] Blocks { get; set; }
            public Point Position { get; set; }
        }
    }
}