using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ZedGraph;
using System.Threading;
using FTD2XX_NET;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace ZedGraph_Demo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            ListBox.CheckForIllegalCrossThreadCalls = false;
        }
        AutoSizeFormClass asc = new AutoSizeFormClass();
        PointPairList list1 = new PointPairList();
        PointPairList list2 = new PointPairList();
        PointPairList list3 = new PointPairList();
        PointPairList list4 = new PointPairList();
        PointPairList list5 = new PointPairList();
        PointPairList list6 = new PointPairList();
        PointPairList list7 = new PointPairList();
        PointPairList list8 = new PointPairList();
        PointPairList list9 = new PointPairList();
        PointPairList list10 = new PointPairList();
        PointPairList list11 = new PointPairList();

        LineItem myCurve1;
        LineItem myCurve2;
        LineItem myCurve3;
        LineItem myCurve4;
        LineItem myCurve5;
        LineItem myCurve6;
        LineItem myCurve7;
        LineItem myCurve8;
        LineItem myCurve9;
        LineItem myCurve10;
        LineItem myCurve11;

        byte[] dataBytes = new byte[2048];//数据缓存
        UInt32 ftdiDeviceCount = 0;
        FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;
        private FTDI.FT_STATUS OpenFlag = FTDI.FT_STATUS.FT_IO_ERROR;
        private List<byte> list = new List<byte>();
        private FTDI myFtdiDevice = new FTDI();
        EventWaitHandle eventWait = new EventWaitHandle(false, EventResetMode.AutoReset);
        //创建一个队列
        private ConcurrentQueue<byte> Data_Buffer = new ConcurrentQueue<byte>();
        int i = 0;
        private uint numBytesWritten = 0;
        private bool isAlive = true;
        private byte Channel, Sum_Rec = 0, Amount = 0;
        private byte Sum_Cacu = 0;
        private short accel_x, accel_y, accel_z, gyro_x, gyro_y, gyro_z, magn_x, magn_y, magn_z;
        private ushort Count_ID;
        private ushort BAT_V;
        private ushort[] EMG = new ushort[2];
        private double a_x, a_y, a_z, g_x, g_y, g_z, m_x, m_y, m_z;
        private double[] Emg = new double[2];
        private double Bat_v;
        private static List<double> a_x_l = new List<double>();
        private static List<double> a_y_l = new List<double>();
        private static List<double> a_z_l = new List<double>();
        private static List<double> g_x_l = new List<double>();
        private static List<double> g_y_l = new List<double>();
        private static List<double> g_z_l = new List<double>();
        private static List<double> m_x_l = new List<double>();
        private static List<double> m_y_l = new List<double>();
        private static List<double> m_z_l = new List<double>();
        private static List<double> Emg_l = new List<double>();
        private static List<double> Emg_2 = new List<double>();
        private static List<double> X = new List<double>();
        //定义常量
        public static double G = 9.86;
        public static double M = 0.27;
        public static double PI = 3.1415926;
        public double MPU_ACCE_K = 2*G/32768.0;
        public double MPU_GYRO_K = 1000 / 32768.0 * PI / 180;
        public double MPU_MAGN_K = 49.12 / 32768.0;

        private int Tick_start = 0;
        private int Tick_Now = 0;
        private int timerDrawI = 0;
        private uint Data_Count = 0;

        bool Save_Flag = false;
        bool First_Flag = true;
        bool average_flag = true;
        bool Recolrd_Flag = true;


        StreamWriter sw;
        //用作窗体自适应
        class AutoSizeFormClass
         {
            //(1).声明结构,只记录窗体和其控件的初始位置和大小。
            public struct controlRect
            {
                public int Left;
                public int Top;
                public int Width;
                public int Height;
            }
            //(2).声明 1个对象
            //注意这里不能使用控件列表记录 List nCtrl;，因为控件的关联性，记录的始终是当前的大小。
            //      public List oldCtrl= new List();//这里将西文的大于小于号都过滤掉了，只能改为中文的，使用中要改回西文
            public List<controlRect> oldCtrl = new List<controlRect>();
            int ctrlNo = 0;//1;
                           //(3). 创建两个函数
                           //(3.1)记录窗体和其控件的初始位置和大小,
            public void controllInitializeSize(Control mForm)
            {
                controlRect cR;
                cR.Left = mForm.Left; cR.Top = mForm.Top; cR.Width = mForm.Width; cR.Height = mForm.Height;
                oldCtrl.Add(cR);//第一个为"窗体本身",只加入一次即可
                AddControl(mForm);//窗体内其余控件还可能嵌套控件(比如panel),要单独抽出,因为要递归调用
                                  //this.WindowState = (System.Windows.Forms.FormWindowState)(2);//记录完控件的初始位置和大小后，再最大化
                                  //0 - Normalize , 1 - Minimize,2- Maximize
            }
            private void AddControl(Control ctl)
            {
                foreach (Control c in ctl.Controls)
                {  //**放在这里，是先记录控件的子控件，后记录控件本身
                   //if (c.Controls.Count > 0)
                   //    AddControl(c);//窗体内其余控件还可能嵌套控件(比如panel),要单独抽出,因为要递归调用
                    controlRect objCtrl;
                    objCtrl.Left = c.Left; objCtrl.Top = c.Top; objCtrl.Width = c.Width; objCtrl.Height = c.Height;
                    oldCtrl.Add(objCtrl);
                    //**放在这里，是先记录控件本身，后记录控件的子控件
                    if (c.Controls.Count > 0)
                        AddControl(c);//窗体内其余控件还可能嵌套控件(比如panel),要单独抽出,因为要递归调用
                }
            }
            //(3.2)控件自适应大小,
            public void controlAutoSize(Control mForm)
            {
                if (ctrlNo == 0)
                { //*如果在窗体的Form1_Load中，记录控件原始的大小和位置，正常没有问题，但要加入皮肤就会出现问题，因为有些控件如dataGridView的的子控件还没有完成，个数少
                  //*要在窗体的Form1_SizeChanged中，第一次改变大小时，记录控件原始的大小和位置,这里所有控件的子控件都已经形成
                    controlRect cR;
                    //  cR.Left = mForm.Left; cR.Top = mForm.Top; cR.Width = mForm.Width; cR.Height = mForm.Height;
                    cR.Left = 0; cR.Top = 0; cR.Width = mForm.PreferredSize.Width; cR.Height = mForm.PreferredSize.Height;

                    oldCtrl.Add(cR);//第一个为"窗体本身",只加入一次即可
                    AddControl(mForm);//窗体内其余控件可能嵌套其它控件(比如panel),故单独抽出以便递归调用
                }
                float wScale = (float)mForm.Width / (float)oldCtrl[0].Width;//新旧窗体之间的比例，与最早的旧窗体
                float hScale = (float)mForm.Height / (float)oldCtrl[0].Height;//.Height;
                ctrlNo = 1;//进入=1，第0个为窗体本身,窗体内的控件,从序号1开始
                AutoScaleControl(mForm, wScale, hScale);//窗体内其余控件还可能嵌套控件(比如panel),要单独抽出,因为要递归调用
            }
            private void AutoScaleControl(Control ctl, float wScale, float hScale)
            {
                int ctrLeft0, ctrTop0, ctrWidth0, ctrHeight0;
                //int ctrlNo = 1;//第1个是窗体自身的 Left,Top,Width,Height，所以窗体控件从ctrlNo=1开始
                foreach (Control c in ctl.Controls)
                { //**放在这里，是先缩放控件的子控件，后缩放控件本身
                  //if (c.Controls.Count > 0)
                  //   AutoScaleControl(c, wScale, hScale);//窗体内其余控件还可能嵌套控件(比如panel),要单独抽出,因为要递归调用
                    ctrLeft0 = oldCtrl[ctrlNo].Left;
                    ctrTop0 = oldCtrl[ctrlNo].Top;
                    ctrWidth0 = oldCtrl[ctrlNo].Width;
                    ctrHeight0 = oldCtrl[ctrlNo].Height;
                    //c.Left = (int)((ctrLeft0 - wLeft0) * wScale) + wLeft1;//新旧控件之间的线性比例
                    //c.Top = (int)((ctrTop0 - wTop0) * h) + wTop1;
                    c.Left = (int)((ctrLeft0) * wScale);//新旧控件之间的线性比例。控件位置只相对于窗体，所以不能加 + wLeft1
                    c.Top = (int)((ctrTop0) * hScale);//
                    c.Width = (int)(ctrWidth0 * wScale);//只与最初的大小相关，所以不能与现在的宽度相乘 (int)(c.Width * w);
                    c.Height = (int)(ctrHeight0 * hScale);//
                    ctrlNo++;//累加序号
                             //**放在这里，是先缩放控件本身，后缩放控件的子控件
                    if (c.Controls.Count > 0)
                        AutoScaleControl(c, wScale, hScale);//窗体内其余控件还可能嵌套控件(比如panel),要单独抽出,因为要递归调用

                    if (ctl is DataGridView)
                    {
                        DataGridView dgv = ctl as DataGridView;
                        Cursor.Current = Cursors.WaitCursor;

                        int widths = 0;
                        for (int i = 0; i < dgv.Columns.Count; i++)
                        {
                            dgv.AutoResizeColumn(i, DataGridViewAutoSizeColumnMode.AllCells);  // 自动调整列宽  
                            widths += dgv.Columns[i].Width;   // 计算调整列后单元列的宽度和                       
                        }
                        if (widths >= ctl.Size.Width)  // 如果调整列的宽度大于设定列宽  
                            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;  // 调整列的模式 自动  
                        else
                            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;  // 如果小于 则填充  

                        Cursor.Current = Cursors.Default;
                    }
                }
            }
        }

        
        private void Form1_Load(object sender, EventArgs e)
        {
            asc.controllInitializeSize(this);
            //初始化ZedGraph控件
            CreateGraph_Gyro(zed1);
            CreateGraph_Accl(zed2);
            CreateGraph_Meg(zed3);
            CreateGraph_EMG(zed4);
            GraphPane Pitch_Angle = zed1.GraphPane;
            GraphPane Roll_Angle = zed2.GraphPane;
            GraphPane Yaw_Angle = zed3.GraphPane;
            GraphPane EMG = zed4.GraphPane;
            //曲线绑定到对应控件
            myCurve1 = Pitch_Angle.AddCurve("X", list1, Color.Red, SymbolType.None);
            myCurve2 = Pitch_Angle.AddCurve("Y", list2, Color.Blue, SymbolType.None);
            myCurve3 = Pitch_Angle.AddCurve("Z", list3, Color.Green, SymbolType.None);
            myCurve4 = Roll_Angle.AddCurve("X", list4, Color.Red, SymbolType.None);
            myCurve5 = Roll_Angle.AddCurve("Y", list5, Color.Blue, SymbolType.None);
            myCurve6 = Roll_Angle.AddCurve("Z", list6, Color.Green, SymbolType.None);
            myCurve7 = Yaw_Angle.AddCurve("X", list7, Color.Red, SymbolType.None);
            myCurve8 = Yaw_Angle.AddCurve("Y", list8, Color.Blue, SymbolType.None);
            myCurve9 = Yaw_Angle.AddCurve("Z", list9, Color.Green, SymbolType.None);
            myCurve10 = EMG.AddCurve("EMG_1", list10, Color.Red, SymbolType.None);
            myCurve11 = EMG.AddCurve("EMG_2", list11, Color.Blue, SymbolType.None);
            //定义X轴类型，执行更改
            Pitch_Angle.XAxis.Type = ZedGraph.AxisType.LinearAsOrdinal;
            Roll_Angle.XAxis.Type = ZedGraph.AxisType.LinearAsOrdinal;
            Yaw_Angle.XAxis.Type = ZedGraph.AxisType.LinearAsOrdinal;
            EMG.XAxis.Type = ZedGraph.AxisType.LinearAsOrdinal;
            Tick_start = Environment.TickCount;
            zed1.AxisChange();
            zed2.AxisChange();
            zed3.AxisChange();
            zed4.AxisChange();
            //创建新线程
            Thread datareceiver = new Thread(ThreadMethod);
            datareceiver.Name = "Serial Port Data Receiver";
            datareceiver.Priority = ThreadPriority.BelowNormal;
            datareceiver.Start();
            Thread dataProcessor = new Thread(DataMethod);
            dataProcessor.Name = "Serial Port Data Processor";
            dataProcessor.Priority = ThreadPriority.Highest;
            dataProcessor.Start();
            timer1.Interval = 10;
            timer2.Interval = 1000;
            //初始化FT232
            FT232_Init();
            //Thread drawChart = new Thread(chartMethod);
            //drawChart.Name = "Zedgraph Draw";
            //drawChart.Priority = ThreadPriority.Lowest;
            //drawChart.Start();
        }

        public void ThreadMethod() //方法内可以有参数，也可以没有参数
        {
            while (true)
            {
                //等待FT232接收事件，阻塞进程
                eventWait.WaitOne();
                listBox1.Items.Add("收到数据！");
                if (OpenFlag == FTDI.FT_STATUS.FT_OK)
                {
                    try
                    {
                        ftStatus = myFtdiDevice.Read(dataBytes, (uint)dataBytes.Length, ref numBytesWritten);
                        foreach (var item in dataBytes)
                        {
                            Data_Buffer.Enqueue(item);
                        }
                        listBox1.Items.Add("缓存长度：" + Data_Buffer.Count);
                        listBox1.TopIndex = listBox1.Items.Count - 1;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        list.Clear();
                    }
                }
            }
        }

        public void DataMethod()
        {
            Object locker = new Object(); 
            byte Item;
            while (true)
            {
                try
                {
                    if (Data_Buffer != null && Data_Buffer.Count > 32)
                    {
                        //获取第一帧
                        if (First_Flag == true)
                        {
                            for (int k = 0; k < 32; k++)
                            {
                                Data_Buffer.TryDequeue(out Item);
                                if (Item == 0xAA)
                                {
                                    Data_Buffer.TryDequeue(out Item);
                                    if (Item == 0xAA)
                                    {
                                        list.Add(0xAA);
                                        list.Add(0xAA);
                                        for (int c = 0; c < 30; c++)
                                        {
                                            Data_Buffer.TryDequeue(out Item);
                                            list.Add(Item);
                                        }
                                        First_Flag = false;
                                        break;
                                    }
                                }
                            }
                        }
                        //不是第一帧
                        else
                        {
                            for (int k = 0; k < 32; k++)
                            {
                                Data_Buffer.TryDequeue(out Item);
                                list.Add(Item);
                            }
                        }
                        i = 0;
                        if ((list[i] == 0xAA && list[++i] == 0xAA) && list.Count>=32)
                        {
                            Channel = list[++i];
                            label6.Text = "Channel:" + Channel.ToString();
                            Amount = list[++i];
                            for (int Cnt = 4; Cnt < Amount; Cnt++)
                            {
                                Sum_Cacu += list[Cnt];
                            }
                            Sum_Rec = list[Amount];
                           // 和校验通过
                            if (Sum_Cacu == Sum_Rec)
                            {
                                label5.Text = "Sum Check: Pass";
                                accel_x = (short)(list[++i] | list[++i] << 8);
                                accel_y = (short)(list[++i] | list[++i] << 8);
                                accel_z = (short)(list[++i] | list[++i] << 8);
                                gyro_x = (short)(list[++i] | list[++i] << 8);
                                gyro_y = (short)(list[++i] | list[++i] << 8);
                                gyro_z = (short)(list[++i] | list[++i] << 8);
                                magn_x = (short)(list[++i] | list[++i] << 8);
                                magn_y = (short)(list[++i] | list[++i] << 8);
                                magn_z = (short)(list[++i] | list[++i] << 8);
                                BAT_V = (ushort)(list[++i] | list[++i] << 8);
                                EMG[0] = (ushort)(list[++i] | list[++i] << 8);
                                EMG[1] = (ushort)(list[++i] | list[++i] << 8);
                                Count_ID = (ushort)(list[++i] | list[++i] << 8);
                                a_x = (double)(accel_x * MPU_ACCE_K);
                                a_y = (double)(accel_y * MPU_ACCE_K);
                                a_z = (double)(accel_z * MPU_ACCE_K);
                                g_x = (double)(gyro_x * MPU_GYRO_K);
                                g_y = (double)(gyro_y * MPU_GYRO_K);
                                g_z = (double)(gyro_z * MPU_GYRO_K);
                                m_x = (double)(magn_x * MPU_MAGN_K);
                                m_y = (double)(magn_y * MPU_MAGN_K);
                                m_z = (double)(magn_z * MPU_MAGN_K);
                                Bat_v = (double)(BAT_V / 600.0);
                                Emg[0] = (double)(EMG[0] / 1200.0);
                                Emg[1] = (double)(EMG[1] / 1200.0);
                                Data_Count++;
                                Sum_Cacu = 0;
                                if (Save_Flag == true)
                                {
                                    if (Recolrd_Flag == true)
                                    {
                                        string filename = String.Format(@"D:\Mpu_Data_{0}.txt", DateTime.Now.ToString("yyyy_MM_HH_mm_ss")); //文件名作为日期;
                                        sw = new StreamWriter(filename, false, Encoding.ASCII);
                                        sw.Write("***************************************************************************" + "\r\n"
                                                 + DateTime.Now + "\r\n" +
                                                 "***************************************************************************" + "\r\n"
                                                 + "TimeS  Acce_x  Acce_y  Acce_z  Gyro_x  Gyro_y  Gyro_z  Magn_x  Magn_y  Magn_z  EMG[0]  EMG[1]" + "\r\n"
                                                 );
                                        Recolrd_Flag = false;
                                    }

                                    sw.Write(Count_ID.ToString() + "  " + a_x.ToString("0.0000") + "  " + a_y.ToString("0.0000") + "  " + a_z.ToString("0.0000") + "  " +
                                                g_x.ToString("0.0000") + "  " + g_y.ToString("0.0000") + "  " + g_z.ToString("0.0000") + "  " +
                                                magn_x.ToString("0.0000") + "  " + magn_y.ToString("0.0000") + "  " + magn_z.ToString("0.0000") + "  " +
                                                Emg[0].ToString("0.0000") + "  " + Emg[1].ToString("0.0000") + "\r\n");
                                }
                            }
                            else
                            {
                                label5.Text = "Sum Check: Pass";
                                Sum_Cacu = 0;
                            }
                        }
                        else
                        {
                            First_Flag = true;
                        }
                        list.Clear();
                        //处理完毕一帧，清空list
                        lock (locker)
                        {
                            if (average_flag == true)
                            {
                                double x = timerDrawI;
                                a_x_l.Add(a_x);
                                a_y_l.Add(a_y);
                                a_z_l.Add(a_z);
                                g_x_l.Add(g_x);
                                g_y_l.Add(g_y);
                                g_z_l.Add(g_z);
                                m_x_l.Add(m_x);
                                m_y_l.Add(m_y);
                                m_z_l.Add(m_z);
                                Emg_l.Add(Emg[0]-0.3);
                                Emg_2.Add(Emg[1]+0.3);
                                X.Add(x);
                                timerDrawI++;
                            }
                            if (a_x_l.Count == 50 && Emg_l.Count == 50)
                            {
                                average_flag = false;
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                    list.Clear();
                }
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Object locker = new Object();
            try
            {
                lock (locker)
                {
                    if (Emg_l != null && Emg_l.Count == 50)
                    {
                        list1.Add(X.ToArray(), a_x_l.ToArray());
                        list2.Add(X.ToArray(), a_y_l.ToArray());
                        list3.Add(X.ToArray(), a_z_l.ToArray());
                        list4.Add(X.ToArray(), g_x_l.ToArray());
                        list5.Add(X.ToArray(), g_y_l.ToArray());
                        list6.Add(X.ToArray(), g_z_l.ToArray());
                        list7.Add(X.ToArray(), m_x_l.ToArray());
                        list8.Add(X.ToArray(), m_y_l.ToArray());
                        list9.Add(X.ToArray(), m_z_l.ToArray());
                        list10.Add(X.ToArray(), Emg_l.ToArray());
                        list11.Add(X.ToArray(), Emg_2.ToArray());

                        //调用ZedGraphControl.AxisChange()方法更新X和Y轴的范围
                        zed1.AxisChange();
                        zed2.AxisChange();
                        zed3.AxisChange();
                        zed4.AxisChange();
                        //调用Form.Invalidate()方法更新图表
                        zed1.Invalidate();
                        zed2.Invalidate();
                        zed3.Invalidate();
                        zed4.Invalidate();

                        a_x_l.Clear();
                        a_y_l.Clear();
                        a_z_l.Clear();
                        g_x_l.Clear();
                        g_y_l.Clear();
                        g_z_l.Clear();
                        m_x_l.Clear();
                        m_y_l.Clear();
                        m_z_l.Clear();
                        Emg_l.Clear();
                        Emg_2.Clear();
                        X.Clear();
                        average_flag = true;
                    }

                    if (list10.Count >= 1000)
                    {
                        list1.RemoveRange(0, 50);
                        list2.RemoveRange(0, 50);
                        list3.RemoveRange(0, 50);
                        list4.RemoveRange(0, 50);
                        list5.RemoveRange(0, 50);
                        list6.RemoveRange(0, 50);
                        list7.RemoveRange(0, 50);
                        list8.RemoveRange(0, 50);
                        list9.RemoveRange(0, 50);
                        list10.RemoveRange(0, 50);
                        list11.RemoveRange(0, 50);
                    }
                    label8.Text = "电池电压:" + Bat_v.ToString("0.00") + "V";
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void timer2_Tick(object sender, EventArgs e)
        {
            listBox2.Items.Add("实时通讯速率：" + Data_Count + "Hz");
            listBox2.TopIndex = listBox2.Items.Count - 1;
            Data_Count = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            timer1.Start();
            timer2.Start();
            button1.BackColor = Color.Lime;
            button2.BackColor = Color.Gainsboro;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            timer2.Stop();
            button2.BackColor = Color.Red;
            button1.BackColor = Color.Gainsboro;
        }
        bool counts = true;
        private void button3_Click(object sender, EventArgs e)
        {
            if (counts == true)
            {
                Save_Flag = true;
                button3.BackColor = Color.Red;
                counts = false;
                Recolrd_Flag = true;
            }
            else
            {
                Save_Flag = false;
                button3.BackColor = Color.Gainsboro;
                counts = true;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            myFtdiDevice.Close();
            if (sw != null)
            {
                sw.Flush();
                sw.Close();
            }
            System.Environment.Exit(0);
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            asc.controlAutoSize(this);
        }

        private void FT232_Init()
        {
            ftStatus = myFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                MessageBox.Show("Failed to get number of devices (error " + ftStatus.ToString() + ")");
            }
            if (ftdiDeviceCount == 0)
            {
                MessageBox.Show("无可用的FTDI设备 (error " + ftStatus.ToString() + ")");
            }
            // Allocate storage for device info list
            FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FTDI.FT_DEVICE_INFO_NODE[ftdiDeviceCount];

            // Populate our device list
            ftStatus = myFtdiDevice.GetDeviceList(ftdiDeviceList);
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                label1.Text = "Type:" + ftdiDeviceList[0].Type.ToString();
                label2.Text = "Location ID: " + String.Format("{0:x}", ftdiDeviceList[0].LocId);
                label3.Text = "Serial Number: " + ftdiDeviceList[0].SerialNumber.ToString();
                label4.Text = "Description: " + ftdiDeviceList[0].Description.ToString();
            }
            ftStatus = myFtdiDevice.OpenBySerialNumber(ftdiDeviceList[0].SerialNumber);
            OpenFlag = ftStatus;
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                MessageBox.Show("打开设备失败 (error " + ftStatus.ToString() + ")");
            }
            ftStatus = myFtdiDevice.SetBaudRate(921600);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                MessageBox.Show("波特率设置失败 (error " + ftStatus.ToString() + ")");
            }
            ftStatus = myFtdiDevice.SetDataCharacteristics(FTDI.FT_DATA_BITS.FT_BITS_8, FTDI.FT_STOP_BITS.FT_STOP_BITS_1, FTDI.FT_PARITY.FT_PARITY_NONE);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                MessageBox.Show("数据特征设置失败 (error " + ftStatus.ToString() + ")");
            }
            ftStatus = myFtdiDevice.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS, 0x11, 0x13);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                MessageBox.Show("流控制设置失败 (error " + ftStatus.ToString() + ")");
            }
            ftStatus = myFtdiDevice.SetTimeouts(5000, 0);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                MessageBox.Show("超时时间设置失败 (error " + ftStatus.ToString() + ")");
            }
            uint dwEventMask = FTDI.FT_EVENTS.FT_EVENT_RXCHAR;
            ftStatus = myFtdiDevice.SetEventNotification(dwEventMask, eventWait);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                MessageBox.Show("事件响应器设置失败 (error " + ftStatus.ToString() + ")");
            }
        }
        private void CreateGraph_Gyro(ZedGraphControl zgc)
        {
            // get a reference to the GraphPane
            GraphPane myPane = zgc.GraphPane;
            zgc.GraphPane.XAxis.Scale.MaxAuto = true;

            // Set the Titles
            myPane.Title.Text = "Gyro Raw Data";
            myPane.XAxis.Title.Text = "Time(s)";
            myPane.YAxis.Title.Text = "rad/s";

            myPane.XAxis.MajorGrid.IsVisible = true;//设置X虚线 
            myPane.YAxis.MajorGrid.IsVisible = true;//设置Y虚线
            myPane.Chart.Fill = new Fill(Color.White, Color.LightGray, 45.0f);
        }

        private void CreateGraph_Accl(ZedGraphControl zgc)
        {
            // get a reference to the GraphPane
            GraphPane myPane = zgc.GraphPane;
            zgc.GraphPane.XAxis.Scale.MaxAuto = true;
            // Set the Titles
            myPane.Title.Text = "Accl Raw Data";
            myPane.XAxis.Title.Text = "Time(s)";
            myPane.YAxis.Title.Text = "m/s2";
            myPane.XAxis.MajorGrid.IsVisible = true;//设置X虚线 
            myPane.YAxis.MajorGrid.IsVisible = true;//设置Y虚线
            myPane.Chart.Fill = new Fill(Color.White, Color.LightGray, 45.0f);
        }

        private void CreateGraph_Meg(ZedGraphControl zgc)
        {
            // get a reference to the GraphPane
            GraphPane myPane = zgc.GraphPane;
            zgc.GraphPane.XAxis.Scale.MaxAuto = true;
            // Set the Titles
            myPane.Title.Text = "Meg Raw Data";
            myPane.XAxis.Title.Text = "Time(s)";
            myPane.YAxis.Title.Text = "Gs";
            myPane.XAxis.MajorGrid.IsVisible = true;//设置X虚线 
            myPane.YAxis.MajorGrid.IsVisible = true;//设置Y虚线
            myPane.Chart.Fill = new Fill(Color.White, Color.LightGray, 45.0f);
        }

        private void CreateGraph_EMG(ZedGraphControl zgc)
        {
            // get a reference to the GraphPane
            GraphPane myPane = zgc.GraphPane;
            zgc.GraphPane.XAxis.Scale.MaxAuto = true;
            // Set the Titles
            myPane.Title.Text = "EMG Raw Data";
            myPane.XAxis.Title.Text = "Time(s)";
            myPane.YAxis.Title.Text = "V";
            myPane.XAxis.MajorGrid.IsVisible = true;//设置X虚线 
            myPane.YAxis.MajorGrid.IsVisible = true;//设置Y虚线
            //myPane.YAxis.Scale.Min = 1.0;
            //myPane.YAxis.Scale.Max = 2.2;
            myPane.Chart.Fill = new Fill(Color.White, Color.LightGray, 45.0f);
        }

        //勾选框事件处理函数
        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox9.Checked == true)
            {

                myCurve9.IsVisible = true;
            }
            else
            {
                myCurve9.IsVisible = false;
            }
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox8.Checked == true)
            {

                myCurve8.IsVisible = true;
            }
            else
            {
                myCurve8.IsVisible = false;
            }
        }
        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox7.Checked == true)
            {

                myCurve7.IsVisible = true;
            }
            else
            {
                myCurve7.IsVisible = false;
            }
        }
        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox6.Checked == true)
            {

                myCurve6.IsVisible = true;
            }
            else
            {
                myCurve6.IsVisible = false;
            }
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox5.Checked == true)
            {

                myCurve5.IsVisible = true;
            }
            else
            {
                myCurve5.IsVisible = false;
            }
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked == true)
            {

                myCurve4.IsVisible = true;
            }
            else
            {
                myCurve4.IsVisible = false;
            }
        }
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked == true)
            {

                myCurve3.IsVisible = true;
            }
            else
            {
                myCurve3.IsVisible = false;
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked == true)
            {

                myCurve2.IsVisible = true;
            }
            else
            {
                myCurve2.IsVisible = false;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {

                myCurve1.IsVisible = true;
            }
            else
            {
                myCurve1.IsVisible = false;
            }
        }
    }

    public static class DataProcess
    {
        public static double DataAverage(int index, double[] data)
        {
            double sum = 0, average = 0; 
            foreach (var item in data)
            {
                sum += item;
            }
            average = sum / index;
            return average;
        }
    }
}


