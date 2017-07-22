using System;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Flasher
{
    public partial class Flasher : Form
    {
        delegate void WriteBar(int val);
        WriteBar writer;
        delegate void LabelReset();
        LabelReset reset;
        LabelReset resetBar;
        //static string buff="";
        static StreamReader source;
        static SerialPort comport;
        short pagecount;
        //static CRCcalc calcProgCRC;
        string ServiceAddress = "";
        string initVector;

        
        public Flasher()
        {
            InitializeComponent();
            portNameBox.Items.AddRange(SerialPort.GetPortNames());
            //pagecount = 0;
            //calcProgCRC = new CRCcalc();
            writer = new WriteBar(progressBar1.Increment);
            reset = new LabelReset(ResetLabel);
            resetBar = new LabelReset(ResetBar);
        }

        private void openFileBtn_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                source = new StreamReader(openFileDialog1.OpenFile(), Encoding.ASCII);
                filePathLbl.Text = openFileDialog1.FileName;
            }
            //else
            //    MessageBox.Show("Could't open a file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        void comport_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string buff;
            SerialPort port = sender as SerialPort;
            port.ReadTo("$");
            buff = port.ReadLine();
            //textBox1.Invoke(writer, buff + "\r\n");
            ParseResponse(buff);
        }

        private void flashBtn_Click(object sender, EventArgs e)
        {
            //string buff;
            //calcProgCRC.FlushCRC();
            if (source == null)
                MessageBox.Show("Пожалуйста, сначала выберите файл", "Файл не выбран", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            else
            {
                try
                {
                    progressBar1.Maximum = GetNumberOfPages();
                }
                catch (NullReferenceException except)
                {
                    MessageBox.Show("Пожалуйста, сначала выберите файл", "Файл не выбран", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
                catch (Exception except)
                {
                    MessageBox.Show(except.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                try
                {
                    progressBar1.Value = 0;
                    portSetup();
                    pagecount = 0;
                    if (!comport.IsOpen)
                        comport.Open();
                    /*if ((buff = source.ReadLine()) != null)
                    {
                        comport.Write("$PFLSH" + pagecount.ToString("X4") + buff + "*" + CalculateCRC("PFLSH" + pagecount.ToString("X4") + buff) + "\n");
                        calcProgCRC.AddData(buff);
                        pagecount++;
                    }*/
                    toolStripStatusLabel1.Text = "Подключение...";
                    SendInitCommand();
                }
                catch (ArgumentException except)
                {
                    MessageBox.Show(except.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (NullReferenceException except)
                {
                    MessageBox.Show("Неправильно указан COM-порт", "Выбор порта", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception except)
                {
                    MessageBox.Show(except.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static string CalculateCRC(string msg)
        {
            byte output = 0;
            foreach (char c in msg)
            {
                output ^= Convert.ToByte(c);
            }
            return output.ToString("X2");
        }

        private void ParseResponse(string msg)
        {
            /*if (msg[0] != '$')
                MessageBox.Show("The device returned incorrect data", "Response error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else*/
            {
                switch (msg.Substring(0, 5))
                {
                    case "SFLSH":
                        /*if (msg.Substring(6, 4) == ServiceAddress)
                        {
                            if (msg.Substring(10, 2) == "OK")
                            {
                                comport.Close();
                                source.Close();
                                MessageBox.Show("Done");
                            }
                            else
                            {
                                comport.Close();
                                source.Close();
                                MessageBox.Show("Some error has occured during programming", "Programming error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else*/    
                        WriteNextPage();
                        break;
                    case "SBLFL":
                        toolStripStatusLabel1.Text = "Прошивка flash...";
                        SendInitVector();
                        break;
                    case "SEEPR":
                        if (msg.Substring(5, 4) == ServiceAddress)
                        {
                            if (msg.Substring(9, 2) == "OK")
                            {
                                comport.Close();
                                source.Close();
                                toolStripStatusLabel1.Text = "Отключено";
                                filePathLbl.Invoke(reset);
                                MessageBox.Show("Програмное обеспечение обновлено","Сообщение",MessageBoxButtons.OK,MessageBoxIcon.Information);
                                progressBar1.Invoke(resetBar);
                            }
                            else
                            {
                                comport.Close();
                                source.Close();
                                toolStripStatusLabel1.Text = "Отключено";
                                MessageBox.Show("Во время прошивки возникла ошибка", "Ошибка программирования", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                            WriteNextEEPROM();
                        break;
                    case "SSIGN":
                        WriteNextPage();
                        break;
                    default:
                        comport.Close();
                        source.Close();
                        toolStripStatusLabel1.Text = "Отключено";
                        MessageBox.Show("От устройства были получены неправильные данные", "Ошибка данных", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                }
            }
        }

        private void portSetup()
        {
            //try
            //{
                comport = new SerialPort(portNameBox.SelectedItem.ToString(), 115200, Parity.None, 8, StopBits.One);
                DetectPortSpeed();
                comport.Encoding = Encoding.ASCII;
                comport.NewLine = "\n";
                comport.DataReceived += new SerialDataReceivedEventHandler(comport_DataReceived);
            //}
            /*catch (ArgumentException except)
            {
                MessageBox.Show(except.Message, "Input error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (NullReferenceException except)
            {
                MessageBox.Show("Please select a valid port", "Port selection", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (Exception except)
            {
                MessageBox.Show(except.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }*/
        }

        private void SendInitCommand()
        {
            //comport.Write("$PBLFL*" + CalculateCRC("PBLFL") + "\n");
            byte[] initData = new byte[16];
            initData[0] = 0XAA;
            initData[1] = 12;
            initData[2] = 0;
            initData[3] = 0X15;
            Encoding.ASCII.GetBytes("$PBLFL*" + CalculateCRC("PBLFL") + "\n", 0, 10, initData, 4);
            /*for (int i = 2; i < 14; i++)
            {
                initCRC.AddData(initData[i].ToString("X2"));
            }*/
            //initData[14] = (byte)(initCRC.CRC >> 8);
            //initData[15] = (byte)(initCRC.CRC & 0X00FF);
            //string bytes = "";
            /*foreach (byte b in initData)
                bytes += b.ToString("X2") + " ";*/
            //textBox1.Invoke(writer, bytes + "\r\n");
            initData[14] = 0XD6;
            initData[15] = 0XC4;
            comport.Write(initData, 0, initData.Length);
        }

        private void WriteNextPage()
        {
            try
            {
                string buff;
                if (!((buff = source.ReadLine()).Equals("EEPROM")))
                {
                    comport.Write("$PFLSH" + pagecount.ToString("X4") + buff + "*" + CalculateCRC("PFLSH" + pagecount.ToString("X4") + buff) + "\n");
                    //calcProgCRC.AddData(buff);
                    //textBox1.Invoke(writer, calcProgCRC.CRC.ToString("X4") + "\r\n");
                    pagecount++;
                    progressBar1.Invoke(writer, 1);
                }
                else
                {
                    //comport.Write("$PFLSH" + ServiceAddress + calcProgCRC.CRC.ToString("X4") + pagecount.ToString("X4") + (new string('F', 504)) + "*" + CalculateCRC("PFLSH" + ServiceAddress + calcProgCRC.CRC.ToString("X4") + pagecount.ToString("X4") + (new string('F', 504))) + "\n");
                    //MessageBox.Show(calcProgCRC.CRC.ToString("X4"));
                    //MessageBox.Show("Done");
                    //port.Close();
                    //source.Close();
                    toolStripStatusLabel1.Text = "Прошивка EEPROM...";
                    WriteNextEEPROM();
                }
            }
            catch (NullReferenceException except)
            {
                //MessageBox.Show("No file opened\r\nPlease open a file first", "File not opened", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                comport.Close();
                toolStripStatusLabel1.Text = "Отключено";
            }
            catch (ObjectDisposedException except)
            {
                //MessageBox.Show("No file opened\r\nPlease open a file first", "File not opened", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                comport.Close();
                toolStripStatusLabel1.Text = "Отключено";
            }
            catch (Exception except)
            {
                MessageBox.Show(except.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                comport.Close();
                toolStripStatusLabel1.Text = "Отключено";
            }
        }

        private void WriteNextEEPROM()
        {
            string buff;
            if ((buff = source.ReadLine()) != null)
            {
                comport.Write("$PEEPR" + buff + "*" + CalculateCRC("PEEPR" + buff) + "\n");
            }
            else
            {
                //comport.Write("$PEEPR" + ServiceAddress + "0004" + pagecount.ToString("X4") + calcProgCRC.CRC.ToString("X4") + "*" + CalculateCRC("PEEPR" + ServiceAddress + "0004" + pagecount.ToString("X4") + calcProgCRC.CRC.ToString("X4")) + "\n");
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private int GetNumberOfPages()
        {
            source.BaseStream.Seek(-10, SeekOrigin.End);
            byte[] Numpages = new byte[4];
            source.BaseStream.Read(Numpages, 0, 4);
            source.BaseStream.Seek(0, SeekOrigin.Begin);
            return int.Parse(Encoding.ASCII.GetString(Numpages), System.Globalization.NumberStyles.HexNumber);
        }

        private void ResetLabel()
        {
            filePathLbl.Text = "Файл не выбран";
        }

        private void SendInitVector()
        {
            //string buff;
            //if (((buff = source.ReadLine()) != null) && (buff.Length == 16))
            //{
            //    comport.Write("$PSIGN" + buff + "*" + CalculateCRC("PSIGN" + buff) + "\n");
            //}
            //else
            //    MessageBox.Show(buff);
            comport.Write("$PSIGN" + initVector + "*" + CalculateCRC("PSIGN" + initVector) + "\n");
        }

        private void ResetBar()
        {
            progressBar1.Value = 0;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new AboutBox()).Show();
        }

        private void DetectPortSpeed()
        {
            if (((initVector = source.ReadLine()) != null) && (initVector.Length == 16))
            {
                GetServiceAddress();
                byte speedId = byte.Parse(initVector[0].ToString(), System.Globalization.NumberStyles.HexNumber);
                switch (speedId)
                {
                    case 1:
                        comport.BaudRate = 9600;
                        break;
                    case 2:
                        comport.BaudRate = 19200;
                        break;
                    case 0:
                        comport.BaudRate = 115200;
                        break;
                    default:
                        MessageBox.Show("Данная версия программы не поддердивает эту прошивку.\r\nПожалуйста, загрузите новую версию этой программы на www.smartlogic.com.ua", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Application.Exit();
                        break;
                }
            }
            else
            {
                comport.Close();
                source.Close();
                toolStripStatusLabel1.Text = "Отключено";
                filePathLbl.Invoke(reset);
                MessageBox.Show("Файл прошивки поврежден", "Ошибка файла", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Restart();
            }
        }

        void GetServiceAddress()
        {
            long previousPos = source.BaseStream.Position;
            source.BaseStream.Seek(-18, SeekOrigin.End);
            byte[] buf = new byte[4];
            source.BaseStream.Read(buf, 0, 4);
            ServiceAddress = source.CurrentEncoding.GetString(buf);
            source.BaseStream.Position = previousPos;
        }
    }
}
