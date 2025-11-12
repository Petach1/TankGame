using System;
using System.Collections.Generic;
using System.Drawing;
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
        DateTime lastShot1 = DateTime.MinValue;
        DateTime lastShot2 = DateTime.MinValue;

        private HashSet<Keys> keysPressed = new HashSet<Keys>();

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.ClientSize = new Size(1280, 900);

            string path1 = Path.Combine(Application.StartupPath, "Data", "tankmodel.png");
            if (!File.Exists(path1))
                MessageBox.Show("Tank1 obrázek nenalezen: " + path1);
            string path2 = Path.Combine(Application.StartupPath, "Data", "tankmodel1.png");


            player1 = new Tank(path1, 150, 150, Color.Blue);
            player2 = new Tank(path2, 1100, 700, Color.Green);

            // okrajové zdi
            walls.Add(new Rectangle(0, 0, ClientSize.Width, 10));
            walls.Add(new Rectangle(0, ClientSize.Height - 10, ClientSize.Width, 10));
            walls.Add(new Rectangle(0, 0, 10, ClientSize.Height));
            walls.Add(new Rectangle(ClientSize.Width - 10, 0, 10, ClientSize.Height));

            // náhodné zdi uvnitř
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
            

            gameTimer.Interval = 16;
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            keysPressed.Add(e.KeyCode);
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            keysPressed.Remove(e.KeyCode);
        }

        private void GameLoop(object sender, EventArgs e)
        {
            // --- Player 1 ---
            if (player1.IsAlive)
            {
                if (keysPressed.Contains(Keys.Left)) player1.Angle -= 5;
                if (keysPressed.Contains(Keys.Right)) player1.Angle += 5;
                if (keysPressed.Contains(Keys.Up)) player1.Move(5, walls);
                if (keysPressed.Contains(Keys.Down)) player1.Move(-5, walls);
                if (keysPressed.Contains(Keys.Space) && (DateTime.Now - lastShot1).TotalSeconds >= 0.5)
                {
                    bullets.Add(new Bullet(player1.BarrelX, player1.BarrelY, player1.Angle, player1));
                    lastShot1 = DateTime.Now;
                }
            }

            // --- Player 2 ---
            if (player2.IsAlive)
            {
                if (keysPressed.Contains(Keys.A)) player2.Angle -= 5;
                if (keysPressed.Contains(Keys.D)) player2.Angle += 5;
                if (keysPressed.Contains(Keys.W)) player2.Move(5, walls);
                if (keysPressed.Contains(Keys.S)) player2.Move(-5, walls);
                if (keysPressed.Contains(Keys.Q) && (DateTime.Now - lastShot2).TotalSeconds >= 0.5)
                {
                    bullets.Add(new Bullet(player2.BarrelX, player2.BarrelY, player2.Angle, player2));
                    lastShot2 = DateTime.Now;
                }
            }

            // --- Update bullets & check hits ---
            foreach (var b in bullets)
            {
                b.Move(walls);

                // Player 1 hit
                if (player1.IsAlive && b.Owner != player1 &&
                    new Rectangle((int)b.X, (int)b.Y, Bullet.Size, Bullet.Size)
                    .IntersectsWith(new Rectangle((int)player1.X, (int)player1.Y, player1.TextureWidth, player1.TextureHeight)))
                {
                    b.Destroyed = true;
                    player1.Kill();
                    b.Owner.Kills++;
                }

                // Player 2 hit
                if (player2.IsAlive && b.Owner != player2 &&
                    new Rectangle((int)b.X, (int)b.Y, Bullet.Size, Bullet.Size)
                    .IntersectsWith(new Rectangle((int)player2.X, (int)player2.Y, player2.TextureWidth, player2.TextureHeight)))
                {
                    b.Destroyed = true;
                    player2.Kill();
                    b.Owner.Kills++;
                }
            }

            bullets.RemoveAll(b => b.Destroyed);

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // draw walls
            foreach (var w in walls)
                using (Brush wallBrush = new SolidBrush(Color.FromArgb(255, 75, 0, 130))) // IndigoRed
                    g.FillRectangle(wallBrush, w);

            // draw tanks
            player1.Draw(g);
            player2.Draw(g);

            // draw bullets
            foreach (var b in bullets)
                b.Draw(g);

            // draw score
            g.DrawString($"Player1 Kills: {player1.Kills}", new Font("Arial", 16, FontStyle.Bold), Brushes.Blue, 10, 10);
            g.DrawString($"Player2 Kills: {player2.Kills}", new Font("Arial", 16, FontStyle.Bold), Brushes.Green, 10, 40);
        }
    }

    // ======================================
    // TANK
    // ======================================
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

        public Tank(string filename, float x, float y, Color color)
        {
            X = x;
            Y = y;
            StartX = x;
            StartY = y;
            TankColor = color;

            try
            {
                Texture = new Bitmap(filename);
            }
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
                if (w.IntersectsWith(tankRect))
                    return;

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

    // ======================================
    // BULLET
    // ======================================
    public class Bullet
    {
        public float X, Y;
        public float DX, DY;
        public int Bounces = 0;
        public bool Destroyed = false;
        public Tank Owner;

        public const int Size = 13;
        public const int MaxBounces = 3;

        public Bullet(float x, float y, float angle, Tank owner)
        {
            X = x;
            Y = y;
            DX = (float)Math.Cos(angle * Math.PI / 180) * 8;
            DY = (float)Math.Sin(angle * Math.PI / 180) * 8;
            Owner = owner;
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
}
