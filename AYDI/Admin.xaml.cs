using AForge.Video;
using AForge.Video.DirectShow;
using Microsoft.Win32;
using System;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;

namespace AYDI
{
    /// <summary>
    /// Interaction logic for Admin.xaml
    /// </summary>
    public partial class Admin : Window
    {
        //
        //
        public DataSet ds;
        public string strName, imageName;
        //
        public string sqliteConnectionString = @"Data Source=C:\Backup\AYDIDatabase.sqlite;Version=3;";

        //docx
        public Microsoft.Office.Interop.Word.Application wordApp = null;
        public Microsoft.Office.Interop.Word.Document wordDoc = null;

        //
        VideoCaptureDevice LocalWebCam;
        public FilterInfoCollection LocalWebCamsCollection;
        private BitmapImage latestFrame;

        //excel
        public Microsoft.Office.Interop.Excel.Application excel = null;
        public Microsoft.Office.Interop.Excel.Workbook wb = null;
        public Microsoft.Office.Interop.Excel.Worksheet ws = null;




        public Admin()
        {
            InitializeComponent();
            BindImageList();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LocalWebCamsCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            LocalWebCam = new VideoCaptureDevice(LocalWebCamsCollection[0].MonikerString);
            LocalWebCam.VideoResolution = LocalWebCam.VideoCapabilities[0];
            LocalWebCam.NewFrame += new NewFrameEventHandler(Cam_NewFrame);


            userType.Items.Add("Faculty");
            userType.Items.Add("Student");
            userDepartment.Items.Add("SHS");
            userDepartment.Items.Add("COLLEGE");

            //CreateTable();
            DisplayData();
            DisplayAtten();
            DisplayCode();
            DisplayQrCode();
        }

        //mouse cursor

        private void WaitCursor()
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait; // set the cursor to loading spinner  
        }

        //

        private void NormalCursor()
        {
            Mouse.OverrideCursor = null; // set the cursor back to normal
        }

        private void BindImageList()
        {
            using (SQLiteConnection conn = new SQLiteConnection(sqliteConnectionString))
            {
                conn.Open();

                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter("SELECT * FROM employeeData", conn))
                {
                    ds = new DataSet();
                    adapter.Fill(ds);
                    DataTable dt = ds.Tables[0];

                    //cbImages.Items.Clear();

                    //foreach (DataRow dr in dt.Rows)
                    //cbImages.Items.Add(dr["id"].ToString());

                    //cbImages.SelectedIndex = 0;
                }
            }
        }

        void Cam_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                /**/
                System.Drawing.Image img = (System.Drawing.Bitmap)eventArgs.Frame.Clone();

                MemoryStream ms = new MemoryStream();
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.EndInit();
                bi.Freeze();
                this.latestFrame = bi;

                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    uploadImage.Source = bi;
                    uploadImage.Source = new CroppedBitmap(bi, new Int32Rect(400, 0, 400, 400));
                }));

            }
            catch (Exception)
            {
                throw;
            }
        }



        private void CreateTable()
        {
            if (!File.Exists("C:\\Backup\\AYDIDatabase.sqlite"))
            {
                SQLiteConnection.CreateFile("C:\\Backup\\AYDIDatabase.sqlite");

                string sql = @"CREATE TABLE data(
                               ID INTEGER PRIMARY KEY AUTOINCREMENT ,
                                date_time               TEXT      NULL,
                                empID                  TEXT      NULL,
                                emp_name                TEXT      NULL
                               
                            );

                                CREATE TABLE attendance(
	                            ID INTEGER PRIMARY KEY AUTOINCREMENT ,
                                date                    TEXT      NULL,
                                empID                  TEXT      NULL,
                                emp_name                TEXT      NULL,
                                a                    TEXT      NULL,
                                b                   TEXT      NULL,
                                c                    TEXT      NULL,
                                d                   TEXT      NULL
                                
                            );

                                CREATE TABLE user_account(
	                               ID INTEGER PRIMARY KEY AUTOINCREMENT ,   
                                   username            TEXT NULL,
                                   password            TEXT NULL,
                                   firstname           TEXT NULL,
                                   lastname            TEXT NULL,
                                   mobile_number       TEXT NULL
                                
                            );";

                using (SQLiteConnection con = new SQLiteConnection(sqliteConnectionString))
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, con))
                    {
                        con.Open();
                        cmd.ExecuteNonQuery();
                        con.Close();
                    }
                }
            }
        }

        //

        private void DisplayCode()
        {
            if (dbEmployee.Items.Count == 0)
            {
                empId.Text = string.Empty;
            }
            else
            {
                try
                {    /**/
                    using (SQLiteDataAdapter sda = new SQLiteDataAdapter("Select NOS,empID from employeeData ORDER BY NOS DESC", sqliteConnectionString))
                    {
                        DataTable dt_code = new DataTable();
                        sda.Fill(dt_code);
                        //id_number.Text = "ID Number: " + dt.Rows[0][0].ToString();
                        empId.Text = dt_code.Rows[0]["empID"].ToString();
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }


        private void DisplayQrCode()
        {
            if (dbEmployee.Items.Count == 0)
            {
                return;
            }
            else
            {
                //display qrcode

                //var qrcode = new QRCodeWriter();
                //var qrValue = "your magic here";

                var barcodeWriter = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new EncodingOptions
                    {
                        Height = 300,
                        Width = 300,
                        Margin = 0,
                        PureBarcode = false
                    }
                };

                //string imageText = "DotBrgy";
                //string imageText = "";
                //Rectangle rectf = new Rectangle(85, 250, 0, 0);

                using (var bitmap = barcodeWriter.Write(empId.Text))
                using (var stream = new MemoryStream())
                {
                    /*
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        using (Font arialFont = new Font("Century Gothic", 20))
                        {
                            using (StringFormat sf = new StringFormat())
                            {
                                //graphics
                                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                graphics.DrawString(imageText, arialFont, Brushes.Black, rectf, sf);
                                  */
                    //bitmap
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    stream.Seek(0, SeekOrigin.Begin);
                    bi.StreamSource = stream;
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    qrImage.Source = bi;
                }
            }
        }



        private void DisplayData()
        {
            using (SQLiteConnection con = new SQLiteConnection(sqliteConnectionString))
            {
                //da = new SQLiteDataAdapter("Select * From Student order by ID desc", con);
                using (SQLiteDataAdapter sda = new SQLiteDataAdapter("Select * From employeeData order by NOS desc", con))
                {
                    using (DataSet dts = new DataSet())
                    {
                        con.Open();
                        sda.Fill(dts, "employeeData");
                        dbEmployee.ItemsSource = dts.Tables["employeeData"].DefaultView;

                        //count_total.Content = "Total Item: " + dbEmployee.Items.Count.ToString();

                        //count_total.Content = dbEmployee.Items.Count.ToString();

                        //count_complete.Content = count_item + dbEmployee.Items.Cast<DataRowView>().Count(r => r.Row.ItemArray[22].ToString() == "Claimed");

                        //count_pending.Content = count_item + dbEmployee.Items.Cast<DataRowView>().Count(r => r.Row.ItemArray[22].ToString().Trim() == "Pending");

                        //count_pending.Content = "Pending: " + count_item + dbEmployee.Items.Cast<DataRowView>().Count(r => r.Row.ItemArray[22].ToString().Trim() == "Pending");

                    }
                }
            }
        }


        private void DisplayAtten()
        {
            using (SQLiteConnection con = new SQLiteConnection(sqliteConnectionString))
            {
                //da = new SQLiteDataAdapter("Select * From Student order by ID desc", con);
                using (SQLiteDataAdapter sda = new SQLiteDataAdapter("Select * From attendance order by ID desc", con))
                {
                    using (DataSet dts = new DataSet())
                    {
                        con.Open();
                        sda.Fill(dts, "attendance");
                        dbAttendance.ItemsSource = dts.Tables["attendance"].DefaultView;

                        //count_total.Content = "Total Item: " + dbEmployee.Items.Count.ToString();

                        //count_total.Content = dbEmployee.Items.Count.ToString();

                        //count_complete.Content = count_item + dbEmployee.Items.Cast<DataRowView>().Count(r => r.Row.ItemArray[22].ToString() == "Claimed");

                        //count_pending.Content = count_item + dbEmployee.Items.Cast<DataRowView>().Count(r => r.Row.ItemArray[22].ToString().Trim() == "Pending");

                        //count_pending.Content = "Pending: " + count_item + dbEmployee.Items.Cast<DataRowView>().Count(r => r.Row.ItemArray[22].ToString().Trim() == "Pending");

                    }
                }
            }
        }


        private void buttonback_Click(object sender, RoutedEventArgs e)
        {
            MainWindow open = new MainWindow();
            open.Show();
            this.Hide();
        }

        //

        private void SaveQRCODEImage()
        {
            //path
            String filePath = @"C:\Backup\image\aydiQR.png";

            //save image
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)qrImage.Source));
            using (FileStream stream = new FileStream(filePath, FileMode.Create)) encoder.Save(stream);
        }


        //

        private void SaveResidentImage()
        {
            //path
            String filePath = @"C:\Backup\image\aydiImage.png";

            //save image
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)uploadImage.Source));
            using (FileStream stream = new FileStream(filePath, FileMode.Create)) encoder.Save(stream);
        }

        private void saveEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (uploadImage.Source == null)
            {
                MessageBox.Show("Please insert new Image!", "DotBrgy", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                using (SQLiteConnection con = new SQLiteConnection(sqliteConnectionString))
                {

                    using (SQLiteCommand cmd = con.CreateCommand())
                    {


                        //insertImageData();
                        SaveResidentImage();

                        string fileN = @"C:\Backup\image\aydiImage.png";
                        //Initialize a file stream to read the image file
                        FileStream fs = new FileStream(fileN, FileMode.Open, FileAccess.Read);

                        //Initialize a byte array with size of stream
                        byte[] imgByteArr = new byte[fs.Length];

                        //Read data from the file stream and put into the byte array
                        fs.Read(imgByteArr, 0, Convert.ToInt32(fs.Length));

                        //Close a file stream
                        fs.Close();


                        //WaitCursor();
                        con.Open();
                        cmd.CommandType = CommandType.Text;

                        //cmd.CommandText = "insert into employeeData(date_time,emp, emp_name)" +
                        //" values(@date_time,@empID,@emp_name)";
                        cmd.CommandText = "insert into employeeData(empID,firstname,middlename,lastname,image,userType,department)" +
                       " values(@id,@first,@middle,@last,@image,@type,@department)";

                        ///cmd.Parameters.AddWithValue("@date_time", DateTime.Now.ToString());

                        cmd.Parameters.AddWithValue("id", empId.Text);
                        //cmd.Parameters.AddWithValue("first", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(firstName.Text + " " + middleName.Text + " " + lastName.Text));
                        cmd.Parameters.AddWithValue("first", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(firstName.Text));
                        cmd.Parameters.AddWithValue("middle", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(middleName.Text));
                        cmd.Parameters.AddWithValue("last", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(lastName.Text));
                        cmd.Parameters.AddWithValue("image", imgByteArr);
                        cmd.Parameters.AddWithValue("type", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userType.Text));
                        cmd.Parameters.AddWithValue("department", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userDepartment.Text));
                        cmd.ExecuteNonQuery();


                        MessageBox.Show("Transaction successfully saved into Database!", "Information", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                        //MessageBox.Show("Transaction successfully saved into Database! \r" + var, "Saved", MessageBoxButton.OK, MessageBoxImage.Asterisk);

                        DisplayData();
                        //display_transaction();
                        //display_Id();
                        //DisplayCode();
                        //DisplayAddressID();
                        //DisplayQrCode();
                        //NormalCursor();
                        //sample.ScrollToTop();
                        /*output open = new output();
                        open.Show();*/
                        //hide_id();
                        Clear(this);
                    }
                }
            }
        }

        private void deleteAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dbAttendance.Items.Count == 0)
                {
                    MessageBox.Show(this, "No data found!", "DotBrgy", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    MessageBoxResult delete_all = MessageBox.Show("Are you sure you want to delete all the data? This cannot be undone!", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                    if (delete_all == MessageBoxResult.Yes)
                    {

                        using (SQLiteConnection con = new SQLiteConnection(sqliteConnectionString))
                        {
                            using (SQLiteCommand deleteTransaction = con.CreateCommand())
                            {
                                using (SQLiteCommand cleanTransaction = con.CreateCommand())
                                {
                                    using (SQLiteCommand deleteStat = con.CreateCommand())
                                    {
                                        using (SQLiteCommand cleanStat = con.CreateCommand())
                                        {
                                            using (SQLiteCommand deleteHistory = con.CreateCommand())
                                            {
                                                using (SQLiteCommand cleanHistory = con.CreateCommand())
                                                {
                                                    con.Open();
                                                    //cmd.CommandType = CommandType.Text;
                                                    //cmd.CommandText = "select distinct address from tb_address_idType";
                                                    //cmd.CommandText = "DELETE FROM sqlite_sequence WHERE name = '%transactions%'";
                                                    //cmd.CommandText = "UPDATE sqlite_sequence SET seq = 10 WHERE name = 'transactions'";
                                                    //cmd.CommandText = "truncate table transactions";
                                                    //cmd.CommandText = "delete from [transactions]";

                                                    deleteTransaction.CommandText = "delete from attendance";
                                                    deleteTransaction.ExecuteNonQuery();

                                                    cleanTransaction.CommandText = "DELETE FROM sqlite_sequence WHERE name = 'attendance'";
                                                    cleanTransaction.CommandText = "UPDATE sqlite_sequence SET seq = 0 WHERE name = 'attendance'";
                                                    cleanTransaction.ExecuteNonQuery();

                                                    /*
                                               deleteStat.CommandText = "delete from statistic";
                                               deleteStat.ExecuteNonQuery();

                                               cleanStat.CommandText = "DELETE FROM sqlite_sequence WHERE name = 'statistic'";
                                               cleanStat.CommandText = "UPDATE sqlite_sequence SET seq = 0 WHERE name = 'statistic'";
                                               cleanStat.ExecuteNonQuery();

                                               deleteHistory.CommandText = "delete from history";
                                               deleteHistory.ExecuteNonQuery();

                                               cleanHistory.CommandText = "DELETE FROM sqlite_sequence WHERE name = 'history'";
                                               cleanHistory.CommandText = "UPDATE sqlite_sequence SET seq = 0 WHERE name = 'history'";
                                               cleanHistory.ExecuteNonQuery();
                                                              */
                                                    DisplayAtten();
                                                    DisplayData();
                                                    //Clean();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (delete_all == MessageBoxResult.No)
                    {
                        return;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void printAll_Click(object sender, RoutedEventArgs e)
        {
            if (dbAttendance.Items.Count == 0)
            {
                MessageBox.Show("No record found!", "DotBrgy", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            MessageBoxResult result = MessageBox.Show("You are about to generate all data in the table, Proceed?", "DotBrgy", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.No)
            {
                return;
            }
            else
            {
                //WaitCursor();
                try
                {
                    excel = new Microsoft.Office.Interop.Excel.Application();
                    wb = excel.Workbooks.Add();
                    ws = (Microsoft.Office.Interop.Excel.Worksheet)wb.ActiveSheet;


                    for (int Idx = 0; Idx < dbAttendance.Columns.Count; Idx++)
                    {
                        ws.Range["A1"].Offset[0, Idx].Value = dbAttendance.Columns[Idx].Header;
                    }

                    for (int rowIndex = 0; rowIndex < dbAttendance.Items.Count; rowIndex++)
                    {
                        for (int columnIndex = 0; columnIndex < dbAttendance.Columns.Count; columnIndex++)
                        {
                            ws.Range["A2"].Offset[rowIndex, columnIndex].Value = (dbAttendance.Items[rowIndex] as DataRowView).Row.ItemArray[columnIndex].ToString();
                        }
                        excel.Columns.AutoFit();
                        excel.Rows.AutoFit();
                    }
                    MessageBox.Show("Thank you for your patience, Click OK to view your files!", "Transaksyon Tracer", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    excel.Visible = true;
                }
                catch (COMException ex)
                {
                    MessageBox.Show("Error accessing Excel: " + ex.ToString(), "Transaksyon Tracer", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.ToString(), "Transaksyon Tracer", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                //NormalCursor();
            }
        }

        //Clear Data

        private void Clear(DependencyObject obj)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {

                if (obj is TextBox textbox)
                    textbox.Text = string.Empty;
                if (obj is CheckBox checkbox)
                    checkbox.IsChecked = false;
                if (obj is ComboBox combobox)
                    combobox.Text = string.Empty;
                if (obj is RadioButton radiobutton)
                    radiobutton.IsChecked = false;
                if (obj is PasswordBox passwordbox)
                    passwordbox.Password = string.Empty;
                if (obj is DatePicker datepick)
                    datepick.SelectedDate = DateTime.Now;

                Clear(VisualTreeHelper.GetChild(obj, i));
            }
        }

        private void search_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (search.Text == string.Empty)
            {
                dbAttendance.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#F0F8FF");
                dbEmployee.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#F0F8FF");
            }
            else
            {
                return;
            }
        }

        private void AttendanceRadio_Click(object sender, RoutedEventArgs e)
        {
            DataView dv = dbAttendance.ItemsSource as DataView;
            //dv.RowFilter = string.Format("empName LIKE '%{0}%'", search.Text); //where n is a column name of the DataTable          
            dv.RowFilter = string.Format("firstname LIKE '%{0}%' or middlename LIKE '{0}%' or lastname LIKE '{0}%'", search.Text); //where n is a column name of the DataTable                                                                                                        
            dbAttendance.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#C7DFFC");
            dbEmployee.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#F0F8FF");
        }

        private void EmployeeRadio_Click(object sender, RoutedEventArgs e)
        {
            DataView dv = dbEmployee.ItemsSource as DataView;
            dv.RowFilter = string.Format("firstname LIKE '%{0}%' or middlename LIKE '{0}%' or lastname LIKE '{0}%'", search.Text); //where n is a column name of the DataTable                                                                                                        
            //dv.RowFilter = string.Format("empName LIKE '%{0}%' or purok LIKE '{0}%'", search.Text); //where n is a column name of the DataTable                                                                                                        
            dbEmployee.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#C7DFFC");
            dbAttendance.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#F0F8FF");
        }

        private void deleteAllEmployee_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                if (dbEmployee.Items.Count == 0)
                {
                    MessageBox.Show(this, "No data found!", "DotBrgy", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    MessageBoxResult delete_all = MessageBox.Show("Are you sure you want to delete all the data? This cannot be undone!", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                    if (delete_all == MessageBoxResult.Yes)
                    {

                        using (SQLiteConnection con = new SQLiteConnection(sqliteConnectionString))
                        {
                            using (SQLiteCommand deleteTransaction = con.CreateCommand())
                            {
                                using (SQLiteCommand cleanTransaction = con.CreateCommand())
                                {
                                    using (SQLiteCommand deleteStat = con.CreateCommand())
                                    {
                                        using (SQLiteCommand cleanStat = con.CreateCommand())
                                        {
                                            using (SQLiteCommand deleteHistory = con.CreateCommand())
                                            {
                                                using (SQLiteCommand cleanHistory = con.CreateCommand())
                                                {
                                                    con.Open();
                                                    //cmd.CommandType = CommandType.Text;
                                                    //cmd.CommandText = "select distinct address from tb_address_idType";
                                                    //cmd.CommandText = "DELETE FROM sqlite_sequence WHERE name = '%transactions%'";
                                                    //cmd.CommandText = "UPDATE sqlite_sequence SET seq = 10 WHERE name = 'transactions'";
                                                    //cmd.CommandText = "truncate table transactions";
                                                    //cmd.CommandText = "delete from [transactions]";

                                                    deleteTransaction.CommandText = "delete from employeeData";
                                                    deleteTransaction.ExecuteNonQuery();

                                                    cleanTransaction.CommandText = "DELETE FROM sqlite_sequence WHERE name = 'employeeData'";
                                                    cleanTransaction.CommandText = "UPDATE sqlite_sequence SET seq = 0 WHERE name = 'employeeData'";
                                                    cleanTransaction.ExecuteNonQuery();

                                                    /*
                                               deleteStat.CommandText = "delete from statistic";
                                               deleteStat.ExecuteNonQuery();

                                               cleanStat.CommandText = "DELETE FROM sqlite_sequence WHERE name = 'statistic'";
                                               cleanStat.CommandText = "UPDATE sqlite_sequence SET seq = 0 WHERE name = 'statistic'";
                                               cleanStat.ExecuteNonQuery();

                                               deleteHistory.CommandText = "delete from history";
                                               deleteHistory.ExecuteNonQuery();

                                               cleanHistory.CommandText = "DELETE FROM sqlite_sequence WHERE name = 'history'";
                                               cleanHistory.CommandText = "UPDATE sqlite_sequence SET seq = 0 WHERE name = 'history'";
                                               cleanHistory.ExecuteNonQuery();
                                                              */
                                                    DisplayAtten();
                                                    DisplayData();
                                                    //Clean();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (delete_all == MessageBoxResult.No)
                    {
                        return;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void dbEmployee_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {

                //MessageBox.Show(this, "Successfully Login!", "DotBrgy", MessageBoxButtons.OK, MessageBoxIcon.Information);   
                //username = textbox_username.Text;
                DataGrid gd = (DataGrid)sender;
                if (gd.SelectedItem is DataRowView row_selected)
                {
                    empId.Text = row_selected["empID"].ToString();
                    firstName.Text = row_selected["firstname"].ToString();
                    middleName.Text = row_selected["middlename"].ToString();
                    lastName.Text = row_selected["lastname"].ToString();
                    userType.Text = row_selected["userType"].ToString();
                    userDepartment.Text = row_selected["department"].ToString();
                    DisplayQrCode();

                    DataView dv = dbEmployee.ItemsSource as DataView;
                    dv.RowFilter = "Convert(empID, 'System.String') like '%" + empId.Text + "%'"; //where n is a column name of the DataTable
               
                    //user_checkboxAdmin.IsChecked = true;

                    /*
                    using (SQLiteConnection con = new SQLiteConnection(sqliteConnectionString))
                    {
                        using (SQLiteCommand cmd_username = con.CreateCommand())
                        {
                            //wait_cursor();
                            con.Open();
                            cmd_username.CommandType = CommandType.Text;
                            cmd_username.CommandText = "Select * from userAccount where username = '" + user_username.Text.Trim() + "'and password = '" + user_password.Text.Trim() + "'";
                            SQLiteDataReader sdr_admin;
                            sdr_admin = cmd_username.ExecuteReader();
                            //int count_admin = 0;
                            string userRole_admin = string.Empty;

                            while (sdr_admin.Read())
                            {
                                //count_admin = count_admin + 1;
                                userRole_admin = sdr_admin["userType"].ToString();

                            }


                            if (userRole_admin == "Administrator")
                            {
                                user_checkboxAdmin.IsChecked = true;
                            }
                            else if (userRole_admin == "Standard")
                            {
                                user_checkboxStandard.IsChecked = true;
                            }
                        }
                    }
                    */
                }
            }
            catch (System.Exception)
            {
                MessageBox.Show("Please call Technical Support!", "DotBrgy", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void empId_TextChanged(object sender, TextChangedEventArgs e)
        {

            DataTable dataTable = ds.Tables[0];

            foreach (DataRow row in dataTable.Rows)
            {
                if (row["empID"].ToString() == empId.Text)
                {
                    //Store binary data read from the database in a byte array
                    byte[] blob = (byte[])row[1];
                    MemoryStream stream = new MemoryStream();
                    stream.Write(blob, 0, blob.Length);
                    stream.Position = 0;

                    System.Drawing.Image img = System.Drawing.Image.FromStream(stream);
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();

                    MemoryStream ms = new MemoryStream();
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                    ms.Seek(0, SeekOrigin.Begin);
                    bi.StreamSource = ms;
                    bi.EndInit();
                    uploadImage.Source = bi;
                }
            }

            /*
            if (empId.Text == string.Empty)
            {
                refreshAll.Visibility = Visibility.Collapsed;
            }
            else
            {
                refreshAll.Visibility = Visibility.Visible;
            }
            */
        }

        private void refreshAll_Click(object sender, RoutedEventArgs e)
        {
            Clear(this);
            DisplayData();
            DisplayAtten();
            uploadImage.Source = null;
            dbAttendance.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#F0F8FF");
            dbEmployee.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFrom("#F0F8FF");
            //DisplayAtten();
        }

        private void startCamera_Click(object sender, RoutedEventArgs e)
        {
            LocalWebCam.Start();
            //cameras.Visibility = Visibility.Visible;
            captureCamera.Visibility = Visibility.Visible;
            stopCamera.Visibility = Visibility.Visible;
            startCamera.Visibility = Visibility.Collapsed;
        }

        private void captureCamera_Click(object sender, RoutedEventArgs e)
        {
            LocalWebCam.Stop();
            captureCamera.Visibility = Visibility.Collapsed;
            startCamera.Visibility = Visibility.Visible;
        }

        private void stopCamera_Click(object sender, RoutedEventArgs e)
        {
            if (uploadImage.Source != null)
            {
                startCamera.Visibility = Visibility.Visible;
                LocalWebCam.Stop();
                stopCamera.Visibility = Visibility.Collapsed;
                //cameras.Visibility = Visibility.Collapsed;
                uploadImage.Source = null;
                captureCamera.Visibility = Visibility.Collapsed;
            }
            else
            {
                LocalWebCam.Stop();
                uploadImage.Source = null;
                startCamera.Visibility = Visibility.Visible;
            }
        }

        private void editUser_Click(object sender, RoutedEventArgs e)
        {
            if (dbEmployee.Items.Count == 0)
            {
                MessageBox.Show(this, "No data found!", "DotBrgy", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else if (empId.Text == string.Empty)
            {
                MessageBox.Show("Select valid item!", "DotBrgy", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else
            {
                using (SQLiteConnection con = new SQLiteConnection(sqliteConnectionString))
                {
                    using (SQLiteCommand cmd = con.CreateCommand())
                    {

                        SaveResidentImage();

                        string fileN = @"C:\Backup\image\aydiImage.png";
                        //Initialize a file stream to read the image file
                        FileStream fs = new FileStream(fileN, FileMode.Open, FileAccess.Read);

                        //Initialize a byte array with size of stream
                        byte[] imgByteArr = new byte[fs.Length];

                        //Read data from the file stream and put into the byte array
                        fs.Read(imgByteArr, 0, Convert.ToInt32(fs.Length));

                        //Close a file stream
                        fs.Close();

                        //WaitCursor();
                        con.Open();
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "update employeeData set empID=@id,firstname=@first,middlename=@middle,lastname=@last,image=@image,userType=@type,department=@department where empID=" + empId.Text;

                        cmd.Parameters.AddWithValue("id", empId.Text);
                        cmd.Parameters.AddWithValue("first", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(firstName.Text));
                        cmd.Parameters.AddWithValue("middle", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(middleName.Text));
                        cmd.Parameters.AddWithValue("last", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(lastName.Text));

                        cmd.Parameters.AddWithValue("image", imgByteArr);
                        cmd.Parameters.AddWithValue("type", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userType.Text));
                        cmd.Parameters.AddWithValue("department", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userDepartment.Text));

                        cmd.ExecuteNonQuery();

                        MessageBox.Show("Record has been successfully updated!", "DotBrgy", MessageBoxButton.OK, MessageBoxImage.Question);
                        DisplayData();
                        DisplayAtten();
                        uploadImage.Source = null;
                        //display_transaction();
                        Clear(this);
                        //NormalCursor();
                    }
                }
            }
        }

        private void deleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (dbEmployee.Items.Count == 0)
            {
                MessageBox.Show(this, "No data found!", "DotBrgy", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else if (empId.Text == string.Empty)
            {
                MessageBox.Show("Select valid item!", "DotBrgy", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("Are you sure you want to delete? This cannot be undone!", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                if (result == MessageBoxResult.Yes)

                // Do this

                {

                    using (SQLiteConnection con = new SQLiteConnection(sqliteConnectionString))
                    {
                        using (SQLiteCommand cmd = con.CreateCommand())
                        {
                            con.Open();
                            cmd.CommandType = CommandType.Text;
                            //cmd.CommandText = "alter table transactions AUTO_INCREMENT = 1";
                            //cmd.CommandText = "truncate table transactions";
                            //cmd.CommandText = "delete from [transactions]";
                            cmd.CommandText = "delete from employeeData where empID=@id";
                            cmd.Parameters.AddWithValue("@id", empId.Text);
                            cmd.ExecuteNonQuery();
                            MessageBox.Show("Deleted!", "DotBrgy", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                            DisplayData();
                            Clear(this);
                        }
                    }
                }
            }
        }


        //Find and Replace Method
        private void findReplace(Microsoft.Office.Interop.Word.Application wordApp, object ToFindText, object replaceWithText)
        {
            object matchCase = true;
            object matchWholeWord = true;
            object matchWildCards = false;
            object matchSoundLike = false;
            object nmatchAllforms = false;
            object forward = true;
            object format = false;
            object matchKashida = false;
            object matchDiactitics = false;
            object matchAlefHamza = false;
            object matchControl = false;
            //object read_only = false;
            //object visible = true;
            object replace = 2;
            object wrap = 1;

            wordApp.Selection.Find.Execute(ref ToFindText,
                ref matchCase, ref matchWholeWord,
                ref matchWildCards, ref matchSoundLike,
                ref nmatchAllforms, ref forward,
                ref wrap, ref format, ref replaceWithText,
                ref replace, ref matchKashida,
                ref matchDiactitics, ref matchAlefHamza,
                ref matchControl);
        }

        private void findReplaceID(Microsoft.Office.Interop.Word.Application wordApp, Microsoft.Office.Interop.Word.Document doc, string findText, string replaceWithText)
        {
            var shapes = doc.Shapes;

            foreach (Microsoft.Office.Interop.Word.Shape shape in shapes)
            {
                if (shape.TextFrame.HasText != 0)
                {
                    var initialText = shape.TextFrame.TextRange.Text;
                    var resultingText = initialText.Replace(findText, replaceWithText);
                    if (initialText != resultingText)
                    {
                        shape.TextFrame.TextRange.Text = resultingText;
                    }
                }
            }
        }

        //Create the Doc Method
        private void CreateID(object filename, object SaveAs)
        {
            try
            {
                wordApp = new Microsoft.Office.Interop.Word.Application();
                object missing = Missing.Value;
                Microsoft.Office.Interop.Word.Document wordDoc = null;

                if (File.Exists((string)filename))
                {
                    object readOnly = false;
                    object isVisible = false;
                    wordApp.Visible = false;



                    wordDoc = wordApp.Documents.Open(ref filename, ref missing,
                                           ref readOnly, ref missing, ref missing,
                                           ref missing, ref missing, ref missing,
                                           ref missing, ref missing, ref missing,
                                           ref missing, ref missing, ref missing,
                                           ref missing, ref missing);
                    wordDoc.Activate();

                    //find and replace
                    //this.findReplaceID(wordApp, "<firstname>", firstName.Text);
                    findReplaceID(wordApp, wordDoc, "<id>", empId.Text);
                    findReplaceID(wordApp, wordDoc, "<firstname>", firstName.Text);
                    findReplaceID(wordApp, wordDoc, "<middlename>", middleName.Text.Substring(0, 1) + ".");
                    findReplaceID(wordApp, wordDoc, "<lastname>", lastName.Text);
                    findReplaceID(wordApp, wordDoc, "<type>", CultureInfo.CurrentCulture.TextInfo.ToUpper(userType.Text));
                    findReplaceID(wordApp, wordDoc, "<department>", userDepartment.Text);

                }
                else
                {
                    MessageBox.Show("File not Found!", "DotBrgy", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                //Save as
                wordDoc.SaveAs(ref SaveAs, ref missing, ref missing, ref missing,
                                ref missing, ref missing, ref missing,
                                ref missing, ref missing, ref missing,
                                ref missing, ref missing, ref missing,
                                ref missing, ref missing, ref missing);

                var shapes = wordDoc.Shapes;

                foreach (Microsoft.Office.Interop.Word.Shape shape in shapes)
                {
                    if (shape.TextFrame.HasText != 0)
                    {
                        var initialText = shape.TextFrame.TextRange.Text;
                        var resultingText = initialText.Replace("qr", "");
                        if (initialText != resultingText)
                        {
                            string image = shape.TextFrame.TextRange.Text = @"C:\Backup\image\aydiQR.png";
                            shape.Fill.UserPicture(image);
                            shape.TextFrame.TextRange.Text = resultingText;
                        }
                    }
                }

                foreach (Microsoft.Office.Interop.Word.Shape shape in shapes)
                {
                    if (shape.TextFrame.HasText != 0)
                    {
                        var initialText = shape.TextFrame.TextRange.Text;
                        var resultingText = initialText.Replace("user", "");
                        if (initialText != resultingText)
                        {
                            string image = shape.TextFrame.TextRange.Text = @"C:\Backup\image\aydiImage.png";
                            shape.Fill.UserPicture(image);
                            shape.TextFrame.TextRange.Text = resultingText;
                        }
                    }
                }

                wordDoc.Close();
                wordApp.Quit();


                MessageBoxResult result = MessageBox.Show("File successfully saved in your disk!, Click yes to Open.", "DotBrgy", MessageBoxButton.YesNo, MessageBoxImage.Asterisk);
                if (result == MessageBoxResult.Yes)
                {
                    /*
                    System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
                    myProcess.StartInfo.FileName = @"C:\Backup\print.docx";
                    myProcess.StartInfo.CreateNoWindow = true;
                    myProcess.Start();
                    */
                    System.Diagnostics.Process.Start(@"C:\Backup\aydi-print.docx");
                }
                else
                {
                    //KillWord();
                    //MessageBox.Show("File successfully saved in your disk!", "DotBrgy", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    wordApp.Quit();
                    return;
                }
            }
            catch (Exception)
            {
                //MessageBox.Show("Error: " + ex.ToString(), "DotBrgy", MessageBoxButton.OK, MessageBoxImage.Error);
                MessageBox.Show("Word cannot save this file because it is already open elsewhere.", "DotBrgy", MessageBoxButton.OK, MessageBoxImage.Error);
                wordApp.Quit();
                //KillWord();
            }
        }

        private void printID_Click(object sender, RoutedEventArgs e)
        {
            WaitCursor();
            //Tracker();
            SaveResidentImage();
            SaveQRCODEImage();
            CreateID(@"C:\Backup\id.docx", @"C:\Backup\aydi-print.docx");
            NormalCursor();
        }

        private void browseImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FileDialog fldlg = new OpenFileDialog();


                fldlg.InitialDirectory = Environment.SpecialFolder.MyPictures.ToString();
                fldlg.Filter = "Image File (*.jpg;*.bmp;*.png)|*.jpg;*.bmp;*.png";
                fldlg.ShowDialog();
                {
                    strName = fldlg.SafeFileName;
                    imageName = fldlg.FileName;
                    ImageSourceConverter isc = new ImageSourceConverter();
                    uploadImage.SetValue(Image.SourceProperty, isc.ConvertFromString(imageName));
                }
                fldlg = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }
    }
}
