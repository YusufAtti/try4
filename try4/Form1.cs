
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace try4
{
    public partial class Form1 : Form
    {
        // İzgara boyutunu tutar
        private int gridSize = 50;
        // Rastgele sayı üretmek için kullanılan nesne
        private Random random = new Random();
        // Engellerin resmini tutar
        private Image obstacleImage;
        // Hedefe giden yolun noktalarını tutar
        private Queue<Point> path;
        // Animasyondaki mevcut adımı tutar
        private int currentStep = 0;
        // Hücre boyutunu tutar
        private int cellSize;
        // İzgara oluşturuldu mu kontrolü
        private bool isGridCreated = false;
        // Ekstra engeller oluşturuldu mu kontrolü
        private bool isExtraObstaclesCreated = false;
        // Önceki izgara boyutunu tutar
        private int prevGridSize = 0;
        // Zamanlayıcı nesnesi
        private Timer timer = new Timer();

        // Form oluşturulduğunda çalışan constructor metodu
        public Form1()
        {
           InitializeComponent();
            pictureBox1.Paint += PictureBox1_Paint;

            // Timer ayarları
            timer.Interval = 40; // Her 20 milisaniyede bir Tick olayı tetiklenecek
            timer.Tick += Timer_Tick;
        }


        // PictureBox1'in boyama olayını işleyen metot
        private void PictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (!isGridCreated)
                return;

            // Hücre boyutunu belirler
            cellSize = Math.Min(pictureBox1.Width, pictureBox1.Height) / gridSize;
            // Izgarayı çizer
            DrawGrid(e.Graphics, pictureBox1.Width, pictureBox1.Height, cellSize);


            // Sol yarısını LightSteelBlue renginde, sağ yarısını Salmon renginde şeffaf yapar
            int halfWidth = pictureBox1.Width / 2;
            Rectangle leftRect = new Rectangle(0, 0, halfWidth, pictureBox1.Height);
            Rectangle rightRect = new Rectangle(halfWidth, 0, halfWidth, pictureBox1.Height);
            Brush leftBrush = new SolidBrush(Color.FromArgb(128, Color.LightSteelBlue));
            Brush rightBrush = new SolidBrush(Color.FromArgb(128, Color.Salmon));
            e.Graphics.FillRectangle(leftBrush, leftRect);
            e.Graphics.FillRectangle(rightBrush, rightRect);

            // Engelleri ve başlangıç noktasını çizer
            if (prevGridSize != gridSize || !isExtraObstaclesCreated)
            {

                CreateCustomAri(e.Graphics);
                CreateCustomAri(e.Graphics);
                CreateGoldObstacles(e.Graphics);
                isExtraObstaclesCreated = true;
                prevGridSize = gridSize;
            }

            // Başlangıç noktasını alır
            Point startPoint = GetRandomStartPoint();
            int startX = startPoint.X * cellSize;
            int startY = startPoint.Y * cellSize;
            e.Graphics.FillRectangle(Brushes.Orange, startX, startY, cellSize, cellSize);

            // Tüm altın engellerine giden en kısa yolu bul
            path = FindShortestPathToAllGoldObstacles(startPoint);

            // Bulunan yolu takip et
            AnimatePath();
        }




        // Animasyonu başlatan metot
        private void StartAnimation()
        {
            // Timer'ı başlatır
            timer.Start();
        }


        // Timer'ın tick olayını işleyen metot
        private void Timer_Tick(object sender, EventArgs e)
        {
            // Yolu animasyonla takip eder
            if (currentStep < path.Count)
            {
                Point p = path.ElementAt(currentStep);
                int x = p.X * cellSize;
                int y = p.Y * cellSize;
                pictureBox1.CreateGraphics().FillRectangle(Brushes.Green, x, y, cellSize, cellSize);
                currentStep++;
                UpdateStepLabel(currentStep);
                AnimateCustomAriColor();
                AddCoordinatesToRichTextBox(p);
            }
            else
            {
                timer.Stop();
                MessageBox.Show("Hedefe ulaşıldı!");
            }
        }


        private void AnimateCustomAriColor()
        {
            // Engelin rengini sarıdan kırmızıya değiştiren animasyon
            Color startColor = Color.Yellow;
            Color endColor = Color.Red;
            int animationDuration = 20; // Animasyon süresi (ms)
            int steps = 3; // Animasyon adımları sayısı

            for (int i = 0; i < steps; i++)
            {
                // Her adımda yeni bir ara renk hesapla
                float ratio = (float)i / steps;
                int r = (int)(startColor.R + (endColor.R - startColor.R) * ratio);
                int g = (int)(startColor.G + (endColor.G - startColor.G) * ratio);
                int b = (int)(startColor.B + (endColor.B - startColor.B) * ratio);
                Color currentColor = Color.FromArgb(r, g, b);

                // Sadece CreateCustomAri metodunun engellerini yeni renge boyar
                foreach (Rectangle obstacleRect in obstacleRectangles)
                {
                    if (IsCustomAri(obstacleRect)) // Engelin CreateCustomAri metodundan mı olduğunu kontrol eder
                    {
                        int obstacleX = obstacleRect.X;
                        int obstacleY = obstacleRect.Y;

                        for (int j = 0; j < 8; j++)
                        {
                            for (int k = 0; k < 2; k++)
                            {
                                int currentX = obstacleX + k * cellSize;
                                int currentY = obstacleY + j * cellSize;

                                pictureBox1.CreateGraphics().FillRectangle(new SolidBrush(currentColor), currentX, currentY, cellSize, cellSize);
                            }
                        }
                    }
                }

                // Bir sonraki adıma geçmeden önce beklet
                System.Threading.Thread.Sleep(animationDuration / steps);
            }
        }

        // Verilen dikdörtgenin CreateCustomAri metodundan oluşturulup oluşturulmadığını kontrol eder
        private bool IsCustomAri(Rectangle obstacleRect)
        {
            // Engelin sol üst köşesi
            int obstacleX = obstacleRect.X;
            int obstacleY = obstacleRect.Y;

            // Engelin boyutları
            int obstacleWidth = obstacleRect.Width;
            int obstacleHeight = obstacleRect.Height;

            // CreateCustomAri metodundaki engelin boyutları
            int customAriWidth = 2 * cellSize;
            int customAriHeight = 8 * cellSize;

            // Eğer engelin boyutları CreateCustomAri metodundaki engel boyutlarına eşitse, bu engel CreateCustomAri metodundan oluşturulmuştur
            return obstacleWidth == customAriWidth && obstacleHeight == customAriHeight;
        }



        // Yolu animasyonla takip eden metot
        private void AnimatePath()
        {
            // Yolun null veya boş olup olmadığını kontrol et
            if (path == null || path.Count == 0)
            {
                MessageBox.Show("Hedefe ulaşıldı!");
                return;
            }

            // Timer'ı başlat
            StartAnimation();
        }





        private void CreateCustomAri(Graphics g)
{
    int obstacleX, obstacleY;
    int obstacleWidth = 2 * cellSize;
    int obstacleHeight = 8 * cellSize;
    bool isOverlap = false;

    do
    {
        isOverlap = false;
        obstacleX = random.Next(1, gridSize - 2) * cellSize;
        obstacleY = random.Next(1, gridSize - 9) * cellSize;
        Rectangle newObstacleRect = new Rectangle(obstacleX, obstacleY, obstacleWidth, obstacleHeight);

        // Engellerin birbirleriyle çakışıp çakışmadığını kontrol et
        foreach (Rectangle obstacleRect in obstacleRectangles)
        {
            if (newObstacleRect.IntersectsWith(obstacleRect))
            {
                isOverlap = true;
                break;
            }
        }
    } while (isOverlap);

    for (int i = 0; i < 8; i++)
    {
        for (int j = 0; j < 2; j++)
        {
            int currentX = obstacleX + j * cellSize;
            int currentY = obstacleY + i * cellSize;

            g.FillRectangle(Brushes.Yellow, currentX, currentY, cellSize, cellSize);
        }
    }

    obstacleRectangles.Add(new Rectangle(obstacleX, obstacleY, obstacleWidth, obstacleHeight));
}



        // Altın engellerini oluşturan metot
        private void CreateGoldObstacles(Graphics g)
        {
            // 5 adet 1x1 boyutunda altın renginde engel oluştur
            for (int i = 0; i < 5; i++)
            {
                int obstacleX, obstacleY;
                bool isOverlap = false;

                do
                {
                    isOverlap = false;

                    obstacleX = random.Next(1, gridSize - 1) * cellSize;
                    obstacleY = random.Next(1, gridSize - 1) * cellSize;

                    // Yeni engelin koordinatları
                    Rectangle newObstacleRect = new Rectangle(obstacleX, obstacleY, cellSize, cellSize);

                    // Tüm mevcut engellerle çakışıp çakışmadığını kontrol et
                    foreach (Rectangle obstacleRect in obstacleRectangles)
                    {
                        if (newObstacleRect.IntersectsWith(obstacleRect))
                        {
                            isOverlap = true;
                            break;
                        }
                    }
                } while (isOverlap);

                // Engeli ekle
                obstacleRectangles.Add(new Rectangle(obstacleX, obstacleY, cellSize, cellSize));
                // Engeli çiz
                DrawImageInCell(g, obstacleX, obstacleY, cellSize, "C:\\Users\\berat\\OneDrive\\Masaüstü\\Otomasyon sistem\\p1\\hazineler\\gold.jpg");
            }
        }

        private List<Rectangle> obstacleRectangles = new List<Rectangle>();



        // Başlangıç noktasını belirleyen metot
        private Point GetRandomStartPoint()
        {
            int totalCells = gridSize * gridSize;

            // Rastgele başlangıç noktasını belirler
            // Başlangıç noktası herhangi bir koşula bağlı olmadan seçilir
            int index = random.Next(totalCells);
            int row = index / gridSize;
            int col = index % gridSize;
            return new Point(col, row);
        }



        // Tüm altın engellerine olan en kısa yolu bulan metot
        private Queue<Point> FindShortestPathToAllGoldObstacles(Point start)
        {


            // Başlangıç noktasından tüm altın engellerine olan en kısa yolları birleştirir
            Queue<Point> shortestPath = new Queue<Point>();

            // Altın engellerinin hücrelerini bul
            HashSet<Point> goldObstacleCells = new HashSet<Point>();
            foreach (Rectangle obstacleRect in obstacleRectangles)
            {
                int x = obstacleRect.X / cellSize;
                int y = obstacleRect.Y / cellSize;
                goldObstacleCells.Add(new Point(x, y));
            }

            // Her bir altın engeline olan en kısa yolu bul ve birleştir
            Point currentPoint = start;
            foreach (Point goldObstacleCell in goldObstacleCells)
            {
                Queue<Point> tempPath = FindShortestPath(currentPoint, goldObstacleCell);
                while (tempPath.Count > 0)
                {
                    shortestPath.Enqueue(tempPath.Dequeue());
                }
                currentPoint = goldObstacleCell;
            }

            return shortestPath;
        }

        

       

        // Izgarayı çizen metot
        private void DrawGrid(Graphics g, int width, int height, int cellSize)
        {
            for (int x = 0; x <= width; x += cellSize)
            {
                g.DrawLine(Pens.Black, x, 0, x, height);
            }

            for (int y = 0; y <= height; y += cellSize)
            {
                g.DrawLine(Pens.Black, 0, y, width, y);
            }
        }

        

    

        // Verilen noktanın komşularını döndüren metot
        private IEnumerable<Point> GetNeighbors(Point point)
        {
            // Verilen noktanın komşularını döndürür
            List<Point> neighbors = new List<Point>();

            if (point.X > 0)
                neighbors.Add(new Point(point.X - 1, point.Y));
            if (point.X < gridSize - 1)
                neighbors.Add(new Point(point.X + 1, point.Y));
            if (point.Y > 0)
                neighbors.Add(new Point(point.X, point.Y - 1));
            if (point.Y < gridSize - 1)
                neighbors.Add(new Point(point.X, point.Y + 1));

            return neighbors;
        }


        // Başlangıç noktasından hedefe olan en kısa yolu bulan metot
        private Queue<Point> FindShortestPath(Point start, Point end)
        {
            Queue<Point> path = new Queue<Point>();
            Dictionary<Point, Point> parentMap = new Dictionary<Point, Point>();
            HashSet<Point> visited = new HashSet<Point>();
            Queue<Point> queue = new Queue<Point>();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                Point current = queue.Dequeue();

                if (current == end)
                {
                    // Hedefe ulaşıldığında, parentMap üzerinden en kısa yolu oluştur
                    while (parentMap.ContainsKey(current))
                    {
                        path.Enqueue(current);
                        current = parentMap[current];
                    }
                    path.Enqueue(start);
                    path = new Queue<Point>(path.Reverse());
                    return path;
                }

                foreach (Point neighbor in GetNeighbors(current))
                {
                    if (!visited.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        visited.Add(neighbor);
                        parentMap[neighbor] = current;
                    }
                }
            }

            return path;
        }


        // Yeni bir izgara boyutu alır ve izgarayı günceller
        private void button1_Click(object sender, EventArgs e)
        {
            if (int.TryParse(textBox1.Text, out int newSize))
            {
                if (newSize <= 52)
                {
                    MessageBox.Show("Lütfen 52'den büyük bir değer girin!");
                }
                else if (newSize % 2 != 0)
                {
                    MessageBox.Show("Lütfen çift bir sayı girin!");
                }
                else
                {
                    gridSize = newSize;
                    currentStep = 0;
                    label1.Text = "Geçilen Adım Sayısı: " + currentStep;

                    isExtraObstaclesCreated = false;
                    prevGridSize = 0;

                    pictureBox1.Invalidate();
                    path = null;
                    isGridCreated = true;
                }
            }
            else
            {
                MessageBox.Show("Geçerli bir pozitif tam sayı girin!");
            }
        }


        // Adım sayısını güncelleyen metot
        private void UpdateStepLabel(int step)
        {
            if (label1.InvokeRequired)
            {
                label1.Invoke(new Action<int>(UpdateStepLabel), step);
            }
            else
            {
                label1.Text = "Geçilen Adım Sayısı: " + step;
            }
        }


        // Koordinatları RichTextBox'a ekleyen metot
        private void AddCoordinatesToRichTextBox(Point p)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action<Point>(AddCoordinatesToRichTextBox), p);
            }
            else
            {
                richTextBox1.AppendText($"X: {p.X}, Y: {p.Y}\n");
            }
        }


        // Resmi hücre içine yerleştiren metot
        private void DrawImageInCell(Graphics g, int x, int y, int cellSize, string imagePath)
        {
            Image image = Image.FromFile(imagePath);
            Bitmap bmp = new Bitmap(image);

            for (int i = 0; i < cellSize; i++)
            {
                for (int j = 0; j < cellSize; j++)
                {
                    Color pixelColor = bmp.GetPixel(i, j);
                    if (pixelColor.R < 100 && pixelColor.G < 100 && pixelColor.B > 150)
                    {
                        bmp.SetPixel(i, j, Color.Transparent);
                    }
                }
            }

            g.DrawImage(bmp, x, y, cellSize, cellSize);
        }




        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}

