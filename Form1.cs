using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace TankGame
{
    public partial class Form1 : Form
    {
        Timer gameTimer = new Timer();
        Tank player1, player2;
        List<Rectangle> walls = new List<Rectangle>();
        List<Bullet> bullets = new List<Bullet>();
        List<Explosion> explosions = new List<Explosion>();
        List<PowerUp> powerUps = new List<PowerUp>();
        Random rnd = new Random();

        DateTime lastShot1 = DateTime.MinValue;
        DateTime lastShot2 = DateTime.MinValue;
        private HashSet<Keys> keysPressed = new HashSet<Keys>();
        private DateTime lastPowerUpSpawn = DateTime.MinValue;

        private Button helpButton;
        private Panel helpPanel;
        private bool helpVisible = false;

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.ClientSize = new Size(1280, 900);
            this.KeyPreview = true;

            string path1 = Path.Combine(Application.StartupPath, "Data", "tankmodel.png");
            if (!File.Exists(path1))
                MessageBox.Show("Tank1 obrázek nenalezen: " + path1);
            string path2 = Path.Combine(Application.StartupPath, "Data", "tankmodel1.png");
            if (!File.Exists(path2))
                MessageBox.Show("Tank2 obrázek nenalezen: " + path2);

            player1 = new Tank(path1, 150, 150, Color.Green);
            player2 = new Tank(path2, 1100, 700, Color.Blue);

            // --- zdi ---
            walls.Add(new Rectangle(0, 0, ClientSize.Width, 10));
            walls.Add(new Rectangle(0, ClientSize.Height - 10, ClientSize.Width, 10));
            walls.Add(new Rectangle(0, 0, 10, ClientSize.Height));
            walls.Add(new Rectangle(ClientSize.Width - 10, 0, 10, ClientSize.Height));

            walls.Add(new Rectangle(350, 200, 100, 40));
            walls.Add(new Rectangle(600, 100, 150, 25));
            walls.Add(new Rectangle(900, 180, 130, 35));
            walls.Add(new Rectangle(200, 350, 100, 50));
            walls.Add(new Rectangle(500, 400, 120, 30));
            walls.Add(new Rectangle(750, 350, 140, 40));
            walls.Add(new Rectangle(1050, 300, 100, 50));
            walls.Add(new Rectangle(300, 550, 130, 30));
            walls.Add(new Rectangle(600, 600, 150, 40));
            walls.Add(new Rectangle(850, 500, 120, 50));
            walls.Add(new Rectangle(100, 700, 100, 35));
            walls.Add(new Rectangle(400, 750, 140, 30));
            walls.Add(new Rectangle(700, 800, 150, 40));

            // --- HELP button ---
            helpButton = new Button();
            helpButton.Text = "?";
            helpButton.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            helpButton.Size = new Size(50, 50);
            helpButton.Location = new Point(20, ClientSize.Height - 70);
            helpButton.BackColor = Color.FromArgb(200, 60, 60, 60);
            helpButton.ForeColor = Color.White;
            helpButton.FlatStyle = FlatStyle.Flat;
            helpButton.FlatAppearance.BorderSize = 0;
            helpButton.TabStop = false;

            GraphicsPath gp = new GraphicsPath();
            gp.AddEllipse(0, 0, helpButton.Width, helpButton.Height);
            helpButton.Region = new Region(gp);
            helpButton.Click += (s, e) => ToggleHelpPanel();
            Controls.Add(helpButton);

            // --- HELP panel ---
            helpPanel = new Panel();
            helpPanel.Size = new Size(400, 300);
            helpPanel.Location = new Point(20, ClientSize.Height - 380);
            helpPanel.BackColor = Color.FromArgb(180, 0, 0, 0);
            helpPanel.Visible = false;
            helpPanel.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;

            Label helpText = new Label();
            helpText.ForeColor = Color.White;
            helpText.Font = new Font("Consolas", 12);
            helpText.AutoSize = false;
            helpText.Dock = DockStyle.Fill;
            helpText.TextAlign = ContentAlignment.MiddleCenter;
            helpText.Text =
                "=== Controls ===\n\n" +
                "Player 1:\n" +
                "W / A / S / D – pohyb\n" +
                "SPACE – vystřelit\n\n" +
                "Player 2:\n" +
                "↑ / ↓ / ← / → – pohyb\n" +
                "L – vystřelit\n";

            helpPanel.Controls.Add(helpText);
            Controls.Add(helpPanel);

            // --- herní timer ---
            gameTimer.Interval = 16;
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;
        }

        private void ToggleHelpPanel()
        {
            helpVisible = !helpVisible;
            helpPanel.Visible = helpVisible;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            keysPressed.Add(e.KeyCode);
            e.SuppressKeyPress = true;
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            keysPressed.Remove(e.KeyCode);
            e.SuppressKeyPress = true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Up || keyData == Keys.Down ||
                keyData == Keys.Left || keyData == Keys.Right)
            {
                keysPressed.Add(keyData);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void GameLoop(object sender, EventArgs e)
        {
            // --- spawn powerup každých 12s ---
            if ((DateTime.Now - lastPowerUpSpawn).TotalSeconds >= 12)
            {
                SpawnPowerUp();
                lastPowerUpSpawn = DateTime.Now;
            }

            // --- Player 1 ---
            if (player1.IsAlive)
            {
                if (keysPressed.Contains(Keys.A)) player1.Angle -= 5;
                if (keysPressed.Contains(Keys.D)) player1.Angle += 5;
                if (keysPressed.Contains(Keys.W)) player1.Move(5 * player1.SpeedMultiplier, walls);
                if (keysPressed.Contains(Keys.S)) player1.Move(-5 * player1.SpeedMultiplier, walls);

                if (keysPressed.Contains(Keys.Space) && (DateTime.Now - lastShot1).TotalSeconds >= 0.5)
                {
                    if (player1.HasShotgun)
                    {
                        int bulletsCount = 3; // ← 3 střely
                        float startAngle = player1.Angle - 30;
                        float angleStep = 60f / (bulletsCount - 1);
                        for (int i = 0; i < bulletsCount; i++)
                        {
                            var b = new Bullet(player1.BarrelX, player1.BarrelY, startAngle + i * angleStep, player1);
                            b.MaxBounces = 0; // ← zničí se při nárazu
                            bullets.Add(b);
                        }
                    }
                    else
                    {
                        bullets.Add(new Bullet(player1.BarrelX, player1.BarrelY, player1.Angle, player1));
                    }
                    lastShot1 = DateTime.Now;
                }
            }

            // --- Player 2 ---
            if (player2.IsAlive)
            {
                if (keysPressed.Contains(Keys.Left)) player2.Angle -= 5;
                if (keysPressed.Contains(Keys.Right)) player2.Angle += 5;
                if (keysPressed.Contains(Keys.Up)) player2.Move(5 * player2.SpeedMultiplier, walls);
                if (keysPressed.Contains(Keys.Down)) player2.Move(-5 * player2.SpeedMultiplier, walls);

                if (keysPressed.Contains(Keys.L) && (DateTime.Now - lastShot2).TotalSeconds >= 0.5)
                {
                    if (player2.HasShotgun)
                    {
                        int bulletsCount = 3; // ← 3 střely
                        float startAngle = player2.Angle - 30;
                        float angleStep = 60f / (bulletsCount - 1);
                        for (int i = 0; i < bulletsCount; i++)
                        {
                            var b = new Bullet(player2.BarrelX, player2.BarrelY, startAngle + i * angleStep, player2);
                            b.MaxBounces = 0; // ← zničí se při nárazu
                            bullets.Add(b);
                        }
                    }
                    else
                    {
                        bullets.Add(new Bullet(player2.BarrelX, player2.BarrelY, player2.Angle, player2));
                    }
                    lastShot2 = DateTime.Now;
                }
            }

            // --- střely ---
            foreach (var b in bullets)
            {
                b.Move(walls);

                // Player 1 hit
                if (player1.IsAlive && b.Master != player1 &&
                    new Rectangle((int)b.X, (int)b.Y, Bullet.Size, Bullet.Size)
                    .IntersectsWith(new Rectangle((int)player1.X, (int)player1.Y, player1.TextureWidth, player1.TextureHeight)))
                {
                    b.Destroyed = true;
                    explosions.Add(new Explosion(player1.CenterX, player1.CenterY));
                    player1.Kill();
                    b.Master.Kills++;
                }

                // Player 2 hit
                if (player2.IsAlive && b.Master != player2 &&
                    new Rectangle((int)b.X, (int)b.Y, Bullet.Size, Bullet.Size)
                    .IntersectsWith(new Rectangle((int)player2.X, (int)player2.Y, player2.TextureWidth, player2.TextureHeight)))
                {
                    b.Destroyed = true;
                    explosions.Add(new Explosion(player2.CenterX, player2.CenterY));
                    player2.Kill();
                    b.Master.Kills++;
                }
            }

            bullets.RemoveAll(b => b.Destroyed);

            // --- update exploze ---
            foreach (var ex in explosions)
                ex.Update();
            explosions.RemoveAll(ex => ex.Finished);

            // --- powerup sběr ---
            foreach (var p in powerUps.ToArray())
            {
                if (player1.IsAlive && p.GetRect().IntersectsWith(new RectangleF(player1.X, player1.Y, player1.TextureWidth, player1.TextureHeight)))
                {
                    ActivatePowerUp(player1, p.Type);
                    powerUps.Remove(p);
                }
                else if (player2.IsAlive && p.GetRect().IntersectsWith(new RectangleF(player2.X, player2.Y, player2.TextureWidth, player2.TextureHeight)))
                {
                    ActivatePowerUp(player2, p.Type);
                    powerUps.Remove(p);
                }
            }

            // --- powerup timeout ---
            if (player1.PowerUpEndTime < DateTime.Now)
            {
                player1.SpeedMultiplier = 1f;
                player1.HasShotgun = false;
            }
            if (player2.PowerUpEndTime < DateTime.Now)
            {
                player2.SpeedMultiplier = 1f;
                player2.HasShotgun = false;
            }

            Invalidate();
        }

        private void SpawnPowerUp()
        {
            PowerUpType type = rnd.Next(2) == 0 ? PowerUpType.Speed : PowerUpType.Shotgun;

            RectangleF spawnRect;
            bool intersects;
            float x, y;

            do
            {
                x = rnd.Next(50, ClientSize.Width - 50);
                y = rnd.Next(50, ClientSize.Height - 50);
                spawnRect = new RectangleF(x, y, PowerUp.Size, PowerUp.Size);

                intersects = false;
                foreach (var wall in walls)
                    if (spawnRect.IntersectsWith(wall))
                    {
                        intersects = true;
                        break;
                    }

            } while (intersects);

            powerUps.Add(new PowerUp(x, y, type));
        }

        //TADY NASTAVIT POWERUP SPEED A DURATION
        private void ActivatePowerUp(Tank player, PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Speed:
                    player.SpeedMultiplier = 2f;
                    player.PowerUpEndTime = DateTime.Now.AddSeconds(5); //  5 sekund
                    break;
                case PowerUpType.Shotgun:
                    player.HasShotgun = true;
                    player.PowerUpEndTime = DateTime.Now.AddSeconds(5); //  5 sekund
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            foreach (var w in walls)
                using (Brush wallBrush = new SolidBrush(Color.FromArgb(255, 75, 0, 130)))
                    g.FillRectangle(wallBrush, w);

            foreach (var p in powerUps)
                p.Draw(g);

            player1.Draw(g);
            player2.Draw(g);

            foreach (var b in bullets)
                b.Draw(g);

            foreach (var ex in explosions)
                ex.Draw(g);

            g.DrawString($"Green Kills: {player1.Kills}", new Font("Consolas", 16, FontStyle.Bold), Brushes.Green, 10, 10);
            g.DrawString($"Blue Kills: {player2.Kills}", new Font("Consolas", 16, FontStyle.Bold), Brushes.Blue, 10, 40);
        }
    }

    // === EXPLOSION ===
    public class Explosion
    {
        public float X, Y;
        private int frame = 0;
        private int maxFrames = 15;
        private float maxSize = 120f;
        public bool Finished => frame >= maxFrames;

        public Explosion(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        public void Update() => frame++;

        public void Draw(Graphics g)
        {
            if (Finished) return;
            float progress = frame / (float)maxFrames;
            float size = maxSize * progress;
            int alpha = (int)(255 * (1f - progress));

            using (Brush b = new SolidBrush(Color.FromArgb(alpha, 255, 120, 0)))
                g.FillEllipse(b, X - size / 2f, Y - size / 2f, size, size);
            using (Pen p = new Pen(Color.FromArgb(alpha, 255, 220, 120), 3))
                g.DrawEllipse(p, X - size / 2f, Y - size / 2f, size, size);
        }
    }

    // === TANK ===
    public class Tank
    {
        public float X, Y;
        public float StartX, StartY;
        public float Angle;
        private Bitmap Texture;
        public int TextureWidth;
        public int TextureHeight;
        private int OrigTextureWidth;
        private int OrigTextureHeight;
        private Color TankColor;
        private int BarrelOffsetXInTexture = 297;
        private int BarrelOffsetYInTexture = 180;

        public bool IsAlive = true;
        private DateTime RespawnTime;
        public int Kills = 0;

        // powerup properties
        public float SpeedMultiplier { get; set; } = 1f;
        public bool HasShotgun { get; set; } = false;
        public DateTime PowerUpEndTime { get; set; } = DateTime.MinValue;

        public Tank(string filename, float x, float y, Color color)
        {
            this.X = x;
            this.Y = y;
            this.StartX = x;
            this.StartY = y;
            this.TankColor = color;

            try { Texture = new Bitmap(filename); }
            catch
            {
                Texture = new Bitmap(60, 36);
                using (Graphics g = Graphics.FromImage(Texture))
                    g.Clear(TankColor);
            }

            OrigTextureWidth = Texture.Width;
            OrigTextureHeight = Texture.Height;
            TextureWidth = 60;
            TextureHeight = 36;
            Texture = new Bitmap(Texture, TextureWidth, TextureHeight);

            BarrelOffsetXInTexture = BarrelOffsetXInTexture * TextureWidth / OrigTextureWidth;
            BarrelOffsetYInTexture = BarrelOffsetYInTexture * TextureHeight / OrigTextureHeight;
        }

        public float CenterX => X + TextureWidth / 2;
        public float CenterY => Y + TextureHeight / 2;
        public float BarrelX => X + BarrelOffsetXInTexture + (float)Math.Cos(Angle * Math.PI / 180) * 18;
        public float BarrelY => Y + BarrelOffsetYInTexture + (float)Math.Sin(Angle * Math.PI / 180) * 18 - 5;

        public void Move(float speed, List<Rectangle> walls)
        {
            if (!IsAlive) return;
            float newX = X + (float)Math.Cos(Angle * Math.PI / 180) * speed;
            float newY = Y + (float)Math.Sin(Angle * Math.PI / 180) * speed;
            Rectangle tankRect = new Rectangle((int)newX, (int)newY, TextureWidth, TextureHeight);
            foreach (var w in walls)
                if (w.IntersectsWith(tankRect)) return;
            X = newX;
            Y = newY;
        }

        public void Draw(Graphics g)
        {
            if (!IsAlive)
            {
                if ((DateTime.Now - RespawnTime).TotalSeconds >= 3)
                {
                    IsAlive = true;
                    X = StartX;
                    Y = StartY;
                }
                else return;
            }

            var state = g.Save();
            g.TranslateTransform(CenterX, CenterY);
            g.RotateTransform(Angle);
            g.DrawImage(Texture, -TextureWidth / 2, -TextureHeight / 2, TextureWidth, TextureHeight);
            g.Restore(state);
        }

        public void Kill()
        {
            IsAlive = false;
            RespawnTime = DateTime.Now;
        }
    }

    // === BULLET ===
    public class Bullet
    {
        public float X, Y;
        public float DX, DY;
        public int Bounces = 0;
        public bool Destroyed = false;
        public Tank Master;

        public const int Size = 13;
        public int MaxBounces = 3;

        public Bullet(float x, float y, float angle, Tank master)
        {
            this.X = x;
            this.Y = y;
            this.DX = (float)Math.Cos(angle * Math.PI / 180) * 8;
            this.DY = (float)Math.Sin(angle * Math.PI / 180) * 8;
            this.Master = master;
        }

        public void Move(List<Rectangle> walls)
        {
            if (Destroyed) return;
            X += DX;
            Y += DY;
            Rectangle rect = new Rectangle((int)X, (int)Y, Size, Size);
            foreach (var w in walls)
            {
                if (rect.IntersectsWith(w))
                {
                    if (MaxBounces == 0)
                    {
                        Destroyed = true;
                        break;
                    }

                    Rectangle overlap = Rectangle.Intersect(rect, w);
                    bool reflectX = overlap.Width < overlap.Height;
                    if (reflectX)
                    {
                        DX = -DX;
                        X += DX > 0 ? overlap.Width + 1 : -overlap.Width - 1;
                    }
                    else
                    {
                        DY = -DY;
                        Y += DY > 0 ? overlap.Height + 1 : -overlap.Height - 1;
                    }
                    Bounces++;
                    if (Bounces >= MaxBounces)
                        Destroyed = true;

                    X += DX * 0.1f;
                    Y += DY * 0.1f;
                    break;
                }
            }
        }

        public void Draw(Graphics g)
        {
            if (!Destroyed)
                g.FillEllipse(Brushes.Black, X, Y, Size, Size);
        }
    }

    // === POWERUP ===
    public enum PowerUpType { Speed, Shotgun }

    public class PowerUp
    {
        public float X, Y;
        public const int Size = 30;
        public PowerUpType Type;
        public DateTime SpawnTime;

        public PowerUp(float x, float y, PowerUpType type)
        {
            this.X = x;
            this.Y = y;
            this.Type = type;
            this.SpawnTime = DateTime.Now;
        }

        public void Draw(Graphics g)
        {
            Brush brush;
            switch (Type)
            {
                case PowerUpType.Speed:
                    brush = Brushes.Orange;
                    break;
                case PowerUpType.Shotgun:
                    brush = Brushes.Purple;
                    break;
                default:
                    brush = Brushes.White;
                    break;
            }
            g.FillEllipse(brush, X, Y, Size, Size);
        }

        public RectangleF GetRect() => new RectangleF(X, Y, Size, Size);
    }
}
