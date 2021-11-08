using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ZXing;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;
using Rectangle = System.Drawing.Rectangle;

namespace AYDI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //

        public string sqliteConnectionString = @"Data Source=C:\Backup\AYDIDatabase.sqlite;Version=3;";

        //

        FilterInfoCollection filterInfoCollection;
        VideoCaptureDevice videoCaptureDevice;
        DispatcherTimer timer = new DispatcherTimer();

        DispatcherTimer realTime = new DispatcherTimer();
        private string decoded;

        EuclideanColorFiltering filter = new EuclideanColorFiltering();
        Color color = Color.Black;
        GrayscaleToRGB grayscaleFilter = new GrayscaleToRGB();
        BlobCounter blobCounter = new BlobCounter();
        int range = 120;

        public MainWindow()
        {
            InitializeComponent();
            realTime.Interval = TimeSpan.FromSeconds(1);
            realTime.Tick += realTimer_Tick;
            realTime.Start();

            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        //

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            blobCounter.MinWidth = 10;
            blobCounter.MinHeight = 10;
            blobCounter.FilterBlobs = true;
            blobCounter.ObjectsOrder = ObjectsOrder.Size;

            filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo Device in filterInfoCollection)
                cboCamera.Items.Add(Device.Name);
            cboCamera.SelectedIndex = 0;
            videoCaptureDevice = new VideoCaptureDevice();
            OpenVideo();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            BarcodeReader Reader = new BarcodeReader();

            if (frameHolder.ImageSource != null)
            {
                Result result = Reader.Decode((BitmapSource)frameHolder.ImageSource);
                decoded = result?.Text;
                empID.Text = decoded;
                //timer.Stop();
            }
        }

        private void realTimer_Tick(object sender, EventArgs e)
        {
            time.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss");
        }

        private void OpenVideo()
        {
            videoCaptureDevice = new VideoCaptureDevice(filterInfoCollection[cboCamera.SelectedIndex].MonikerString);
            videoCaptureDevice.NewFrame += new NewFrameEventHandler(FinalFrame_NewFrame);
            //videoCaptureDevice.NewFrame += new NewFrameEventHandler(FinalFrame_NewFrame_DetectObject);
            videoCaptureDevice.Start();
        }

        private void FinalFrame_NewFrame_DetectObject(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap objectsImage = null;
            Bitmap mImage = null;
            System.Drawing.Bitmap image = (Bitmap)eventArgs.Frame.Clone();
            mImage = (Bitmap)image.Clone();
            //filter.CenterColor = Color.FromArgb(color.ToArgb());
            filter.Radius = (short)range;

            objectsImage = image;
            filter.ApplyInPlace(objectsImage);

            BitmapData objectsData = objectsImage.LockBits(new Rectangle(0, 0, image.Width, image.Height),
            ImageLockMode.ReadOnly, image.PixelFormat);
            UnmanagedImage grayImage = grayscaleFilter.Apply(new UnmanagedImage(objectsData));
            objectsImage.UnlockBits(objectsData);


            blobCounter.ProcessImage(grayImage);
            Rectangle[] rects = blobCounter.GetObjectsRectangles();

            if (rects.Length > 0)
            {

                foreach (Rectangle objectRect in rects)
                {
                    Graphics g = Graphics.FromImage(mImage);
                    using (Pen pen = new Pen(Color.FromArgb(160, 255, 160), 8))
                    {
                        g.DrawRectangle(pen, objectRect);
                    }

                    g.Dispose();
                }
            }

            image = mImage;
        }

        private void FinalFrame_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {

                System.Drawing.Bitmap img = (Bitmap)eventArgs.Frame.Clone();
                MemoryStream ms = new MemoryStream();
                img.Save(ms, ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.EndInit();
                bi.Freeze();
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    frameHolder.ImageSource = bi;
                }));
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (videoCaptureDevice.IsRunning == true)
                videoCaptureDevice.Stop();
            Application.Current.Shutdown();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to exit?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
            {
                if (videoCaptureDevice.IsRunning == true)
                    videoCaptureDevice.Stop();
                Application.Current.Shutdown();
            }
            else
            {
                return;
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void openAdmin_Click(object sender, RoutedEventArgs e)
        {
            Admin open = new Admin();
            open.Show();
            this.Hide();
            if (videoCaptureDevice.IsRunning == true)
                videoCaptureDevice.Stop();
        }




        /*
            using (SQLiteCommand cmd1 = new SQLiteCommand("SELECT a FROM attendance WHERE empID LIKE '%{0}%' + @id + '{0}%'", con))
            {
                cmd1.Parameters.AddWithValue("@id", empID.Text);
                using (SQLiteDataReader reader = cmd1.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        time.Text = reader.GetString(0);
                        var result = command.ExecuteScalar();
                        int i = Convert.ToInt32(result);
                        if (i != 0)
                        {
                            cmd.ExecuteNonQuery();
                            message.Text = "LOGIN SUCCESSFULLY!";
                            message.Foreground = System.Windows.Media.Brushes.Green;
                            //MessageBox.Show("Username or Text already exist!", "Information", MessageBoxButton.OK, MessageBoxImage.Information); ;
                            //Clear(this);
                        }
                        else if (empID.Text == string.Empty)
                        {
                            message.Text = "SHOW YOUR QRCODE";
                            message.Foreground = System.Windows.Media.Brushes.Black;
                            return;
                        }
                        else
                        {
                            message.Text = "YOUR CODE ARE NOT REGISTERED IN OUR DATABASE!";
                            message.Foreground = System.Windows.Media.Brushes.Red;
                            return;
                        }
                    }
                }
            }
            */



        private void empID_TextChanged(object sender, TextChangedEventArgs e)
        {
            using (SQLiteConnection con = new SQLiteConnection(sqliteConnectionString))
            {
                con.Open();
                using (SQLiteCommand cmdEmployee = new SQLiteCommand("Select * from employeeData where empID = '" + empID.Text + "'", con))
                {
                    var vEmployee = cmdEmployee.ExecuteScalar();
                    int iEmployee = Convert.ToInt32(vEmployee);

                    //debug.Text = iEmployee.ToString();

                    if (iEmployee == 0)
                    {
                        //  message.Text = "QR CODE NOT VALID!";
                        //message.Foreground = System.Windows.Media.Brushes.Red;
                        //messageDynamic.Text = "QR CODE NOT VALID!";
                        //messageDynamic.Foreground = System.Windows.Media.Brushes.Red;
                        //StartTimer();
                        return;
                    }
                    else
                    {
                        using (SQLiteCommand cmd_a = con.CreateCommand())
                        {
                            using (SQLiteCommand cmd_b = con.CreateCommand())
                            {
                                using (SQLiteCommand cmd_c = con.CreateCommand())
                                {
                                    using (SQLiteCommand cmd_d = con.CreateCommand())
                                    {
                                        using (SQLiteCommand cmdNos = new SQLiteCommand("Select ID from attendance where empID = '" + empID.Text + "'", con))
                                        {
                                            var vNos = cmdNos.ExecuteScalar();
                                            int iNos = Convert.ToInt32(vNos);


                                            cmd_a.CommandType = CommandType.Text;
                                            cmd_b.CommandType = CommandType.Text;
                                            cmd_c.CommandType = CommandType.Text;
                                            cmd_d.CommandType = CommandType.Text;

                                            cmd_a.CommandText = "insert into attendance(date,firstname,middlename,lastname,empID,a)" +
                                            " values(@date,@first,@middle,@last,@empID,@a)";


                                            using (SQLiteCommand cmdFirst = new SQLiteCommand("Select firstname from employeeData where empID = '" + empID.Text + "'", con))
                                            {
                                                using (SQLiteCommand cmdMiddle = new SQLiteCommand("Select middlename from employeeData where empID = '" + empID.Text + "'", con))
                                                {
                                                    using (SQLiteCommand cmdLast = new SQLiteCommand("Select lastname from employeeData where empID = '" + empID.Text + "'", con))
                                                    {
                                                        var vFirst = cmdFirst.ExecuteScalar();
                                                        string iFirst = Convert.ToString(vFirst);

                                                        var vMiddle = cmdMiddle.ExecuteScalar();
                                                        string iMiddle = Convert.ToString(vMiddle);

                                                        var vLast = cmdLast.ExecuteScalar();
                                                        string iLast = Convert.ToString(vLast);

                                                        cmd_a.Parameters.AddWithValue("date", DateTime.Now.ToString("MM/dd/yyyy"));
                                                        cmd_a.Parameters.AddWithValue("first", iFirst.ToString());
                                                        cmd_a.Parameters.AddWithValue("middle", iMiddle.ToString());
                                                        cmd_a.Parameters.AddWithValue("last", iLast.ToString());
                                                        cmd_a.Parameters.AddWithValue("empID", empID.Text);
                                                        cmd_a.Parameters.AddWithValue("a", DateTime.Now.ToString("hh:mm tt"));

                                                        //cmd_a.Parameters.AddWithValue("id", DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));

                                                        //
                                                        //cmd_a.Parameters.AddWithValue("date", DateTime.Now.ToString("MM/dd/yyyy"));
                                                        //cmd_a.Parameters.AddWithValue("a", DateTime.Now.ToString("hh:mm tt"));

                                                        //cmd_a.CommandText = "update attendance set a=@a where empID=" + empID.Text;

                                                        //cmd_b.CommandText = "update attendance set b=@b where empID = " + empID.Text + " and date = " + DateTime.Now.ToString("MM/dd/yyyy");
                                                        cmd_b.CommandText = "update attendance set b=@b where id= " + iNos.ToString();
                                                        cmd_c.CommandText = "update attendance set c=@c where id=" + iNos.ToString();
                                                        cmd_d.CommandText = "update attendance set d=@d where id=" + iNos.ToString();


                                                        cmd_b.Parameters.AddWithValue("b", DateTime.Now.ToString("hh:mm tt"));
                                                        cmd_c.Parameters.AddWithValue("c", DateTime.Now.ToString("hh:mm tt"));
                                                        cmd_d.Parameters.AddWithValue("d", DateTime.Now.ToString("hh:mm tt"));


                                                        TimeSpan now = DateTime.Now.TimeOfDay;

                                                        //using (SQLiteCommand cmdSearch = new SQLiteCommand("Select * from attendance where empID=@id AND date=@date", con))
                                                        using (SQLiteCommand cmdSearch = new SQLiteCommand("Select * from attendance where empID = '" + empID.Text + "'and date = '" + DateTime.Now.ToString("MM/dd/yyyy") + "'", con))
                                                        {
                                                            //cmdSearch.Parameters.AddWithValue("@id", this.empID.Text);
                                                            //cmdSearch.Parameters.AddWithValue("@date", this.date.Text);
                                                            var rr = cmdSearch.ExecuteScalar();
                                                            int iSearch = Convert.ToInt32(rr);

                                                            if (iSearch == 0)
                                                            {
                                                                cmd_a.ExecuteNonQuery();
                                                                messageDynamic.Text = "LOGIN SUCCESSFULLY!";
                                                                messageDynamic.Foreground = System.Windows.Media.Brushes.Green;
                                                                StartTimer();
                                                                return;
                                                            }
                                                            else
                                                            {
                                                                //MessageBox.Show("pogi ko");

                                                                // IN MORNING
                                                                if (now >= TimeSpan.Parse("06:00") && now <= TimeSpan.Parse("11:59"))
                                                                {
                                                                    messageDynamic.Text = "YOU'RE ALREADY LOGGED IN!";
                                                                    messageDynamic.Foreground = System.Windows.Media.Brushes.Red;
                                                                    StartTimer();
                                                                    return;
                                                                }

                                                                // OUT MORNING
                                                                else if (now >= TimeSpan.Parse("12:00") && now <= TimeSpan.Parse("12:30"))
                                                                {
                                                                    using (SQLiteCommand cmdOutMor = new SQLiteCommand("Select b from attendance where empID = '" + empID.Text + "'", con))
                                                                    {
                                                                        var vOutMor = cmdOutMor.ExecuteScalar();
                                                                        string iOutMor = Convert.ToString(vOutMor);

                                                                        if (iOutMor != string.Empty)
                                                                        {
                                                                            messageDynamic.Text = "YOU'RE ALREADY LOGOUT!";
                                                                            messageDynamic.Foreground = System.Windows.Media.Brushes.Red;
                                                                            StartTimer();
                                                                            return;
                                                                        }
                                                                        else
                                                                        {
                                                                            cmd_b.ExecuteNonQuery();
                                                                            messageDynamic.Text = "LOGOUT SUCCESSFULLY!";
                                                                            messageDynamic.Foreground = System.Windows.Media.Brushes.Blue;
                                                                            StartTimer();
                                                                            return;
                                                                        }
                                                                    }
                                                                }

                                                                // IN AFTERNOON
                                                                else if (now >= TimeSpan.Parse("12:31") && now <= TimeSpan.Parse("16:59"))
                                                                {
                                                                    using (SQLiteCommand cmdInAft = new SQLiteCommand("Select c from attendance where empID = '" + empID.Text + "'", con))
                                                                    {
                                                                        var vInAft = cmdInAft.ExecuteScalar();
                                                                        string iInAft = Convert.ToString(vInAft);

                                                                        if (iInAft != string.Empty)
                                                                        {
                                                                            messageDynamic.Text = "YOU'RE ALREADY LOGGED IN!";
                                                                            messageDynamic.Foreground = System.Windows.Media.Brushes.Red;
                                                                            StartTimer();
                                                                            return;
                                                                        }
                                                                        else
                                                                        {
                                                                            cmd_c.ExecuteNonQuery();
                                                                            messageDynamic.Text = "LOGIN SUCCESSFULLY!";
                                                                            messageDynamic.Foreground = System.Windows.Media.Brushes.Green;
                                                                            StartTimer();
                                                                            return;
                                                                        }
                                                                    }
                                                                }

                                                                // OUT AFTERNOON
                                                                else if (now >= TimeSpan.Parse("17:00") && now <= TimeSpan.Parse("23:00"))
                                                                {
                                                                    using (SQLiteCommand cmdOutAft = new SQLiteCommand("Select d from attendance where empID = '" + empID.Text + "'", con))
                                                                    {
                                                                        var vOutAft = cmdOutAft.ExecuteScalar();
                                                                        string iOutAft = Convert.ToString(vOutAft);

                                                                        if (iOutAft != string.Empty)
                                                                        {
                                                                            messageDynamic.Text = "YOU'RE ALREADY LOGOUT!";
                                                                            messageDynamic.Foreground = System.Windows.Media.Brushes.Red;
                                                                            StartTimer();
                                                                            return;
                                                                        }
                                                                        else
                                                                        {
                                                                            cmd_d.ExecuteNonQuery();
                                                                            messageDynamic.Text = "LOGOUT SUCCESSFULLY!";
                                                                            messageDynamic.Foreground = System.Windows.Media.Brushes.Blue;
                                                                            StartTimer();
                                                                            return;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        DispatcherTimer timerMessage = new DispatcherTimer();

        private void StartTimer()
        {
            timerMessage.Interval = TimeSpan.FromSeconds(1);
            timerMessage.Tick += Timer_Message;
            timerMessage.Start();
        }

        void Timer_Message(object sender, EventArgs e)
        {
            messageDynamic.Text = "SHOW YOUR AYDI!";
            messageDynamic.Foreground = System.Windows.Media.Brushes.Black;
        }

        private void save_Click(object sender, RoutedEventArgs e)
        {
            String filePath = @"C:\Backup\qr.png";

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)frameHolder.ImageSource));
            using (FileStream stream = new FileStream(filePath, FileMode.Create))
                encoder.Save(stream);
        }
    }
}



