using System;
using System.IO.Ports;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

namespace Mouse_Glove
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {     
        //for serial port connections and communication
        private string[] availablePorts;
        private string[] availableBaudRates = { "Choose Baud", "9600", "115200" };

        private SerialPort btDevice = new SerialPort();

        //use to make the programme simulate a click
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, int dx, int dy, uint cButtons, uint dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;



        //incomplete data from the arduino
        private volatile string recievedData = "";

        //the current pitch, roll
        double roll = 0;
        double pitch = 0;

        //settings 
        double minMouseSpeed = 4;
        double maxMouseSpeed = 25;

        double minRoll = 10;
        double minPitch = 10;

        double maxRoll = 70;
        double maxPitch = 60;

        double clickAccuaction = 499;

        bool usePunchClick = false;

        public MainWindow()
        {
            InitializeComponent();

            //make sure all the settings are the same as the ones above
            UpdateAvailablePorts();
            portsComboBox.SelectedIndex = 0;

            baudRateComboBox.ItemsSource = availableBaudRates;
            baudRateComboBox.SelectedIndex = 0;

            minRollTextBox.Text = minRoll.ToString();
            minPitchTextBox.Text = minPitch.ToString();

            maxRollTextBox.Text = maxRoll.ToString();
            maxPitchTextBox.Text = maxPitch.ToString();

            minSpeedTextBox.Text = minMouseSpeed.ToString();
            maxSpeedTextBox.Text = maxMouseSpeed.ToString();

            clickAccuactionTextBox.Text = clickAccuaction.ToString();

            activationCheckBox.IsChecked = usePunchClick;

        }
        
        /// <summary>
        /// what to do when the information has been recieved from the arduino
        /// </summary>
        /// <param name="_data"></param>
        private void MouseManipulation(string _data)
        {

            //convert the data that we recieved from the arduino and split it to 2 vectors
            Vector3D gyroData;
            Vector3D accelData;
            try
            {
                string[] AllData = _data.Split('|');
                gyroData = new Vector3D(double.Parse(AllData[0]), double.Parse(AllData[1]), double.Parse(AllData[2]));
                accelData = new Vector3D(double.Parse(AllData[3]), double.Parse(AllData[4]), double.Parse(AllData[5]));
            }
            catch (Exception)
            {
                gyroData = new Vector3D(0, 0, 0);
                accelData = new Vector3D(0, 0, 0);
                Console.WriteLine("invalid input, input values are not in the correct format");
                DisconnectSetup();
            }

            //calculate roll and pitch
            roll = Math.Atan2(-accelData.X, Math.Sqrt(accelData.Y * accelData.Y + accelData.Z * accelData.Z)) * (180 / Math.PI);
            pitch = Math.Atan2(accelData.Y, accelData.Z) * (180 / Math.PI);

            //find out the value of the clicking accuaction, based on either flicking wrist left and right or punching
            double accuaction = 0;
            if (usePunchClick)
            {
                accuaction = Math.Abs(accelData.Y);
            }
            else
            {
                accuaction = gyroData.Z;
            }

            //update displayed info
            Dispatcher.BeginInvoke(new Action(() => {
                displayRollTextBlock.Text = Math.Round(roll, 1).ToString();
                displayPitchTextBlock.Text = Math.Round(pitch, 1).ToString();

                displayAccuactionTextBlock.Text = Math.Round(accuaction, 1).ToString();
            }));

            //direction calculation
            Vector direction = new Vector(0, 0);
            if (roll > minRoll)
            {
                direction.X = 1;
            }
            else if (roll < -minRoll)
            {
                direction.X = -1;
            }
            if (pitch < -minPitch)
            {
                direction.Y = 1;
            }
            else if (pitch > minPitch)
            {
                direction.Y = -1;
            }

            //speed calculation
            double speedX = Math.Round(Remap(Math.Abs(roll), minRoll, maxRoll, minMouseSpeed, maxMouseSpeed));
            double speedY= Math.Round(Remap(Math.Abs(pitch), minPitch, maxPitch, minMouseSpeed, maxMouseSpeed));
            //capping off, making sure the speed dosent exceed max speed
            if (Math.Abs(roll) > maxRoll)
            {
                speedX = maxMouseSpeed;
            }
            if (Math.Abs(pitch) > maxPitch)
            {
                speedY = maxMouseSpeed;
            }            

            System.Windows.Forms.Cursor.Position = new System.Drawing.Point(System.Windows.Forms.Cursor.Position.X + (int)Math.Round(speedX * direction.X), System.Windows.Forms.Cursor.Position.Y + (int)Math.Round(speedY * direction.Y));            
            //left Click
            if (accuaction > clickAccuaction)
            {
                int X = System.Windows.Forms.Cursor.Position.X;
                int Y = System.Windows.Forms.Cursor.Position.Y;
                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
            }
            //right click
            else if (accuaction < -clickAccuaction) 
            {
                int X = System.Windows.Forms.Cursor.Position.X;
                int Y = System.Windows.Forms.Cursor.Position.Y;
                mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, X, Y, 0, 0);
            }
        }

        double Remap(double value, double fromSource, double toSource, double fromTarget, double toTarget)
        {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }

        private void UpdateAvailablePorts()
        {
            string[] ports = SerialPort.GetPortNames();
            availablePorts = new string[ports.Length + 1];
            availablePorts[0] = "Choose Port";
            for (int i = 1; i < ports.Length + 1; i++) {
                availablePorts[i] = ports[i - 1];
            }
            portsComboBox.ItemsSource = availablePorts;

        }

        private void ConnectSetup()
        {
            Console.WriteLine("Serial Port Open");
            connectButton.Content = "Disconnect";
            informationTextBlock.Text = "connection successfully established";
        }

        private void DisconnectSetup()
        {
            try
            {
                btDevice.Close();
                Console.WriteLine("serial Port Closed");
                connectButton.Content = "Connect";

                informationTextBlock.Text = "Please Connect To Glove";
                recievedData = "";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }            
        }

        private bool InitBluetoothConnection()
        {
            try
            {
                System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                //set serial port settings
                btDevice.BaudRate = Convert.ToInt32(availableBaudRates[baudRateComboBox.SelectedIndex]);
                btDevice.PortName = availablePorts[portsComboBox.SelectedIndex];
                Console.WriteLine(Convert.ToInt32(availableBaudRates[baudRateComboBox.SelectedIndex]));

                btDevice.Parity = Parity.None;
                btDevice.StopBits = StopBits.One;
                btDevice.DataBits = 8;
                btDevice.Handshake = Handshake.None;
                btDevice.RtsEnable = true;

                btDevice.DataReceived += new SerialDataReceivedEventHandler(BtRecieveData);
                btDevice.Open();

                System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("An error has occured: " + ex.Message);
                return false;
            }
        }


        /*
        The arduino sends over a string or data every few miliseconds, in the form of 
            gx|gy|gz|ax|ay|az;
        where the gyroscope, acceleration values are seperated by the char | and each reading is differenciated with the ; symbol
        */
        private void BtRecieveData(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPort sp = (SerialPort)sender;
                string dataRecieveBuffer = sp.ReadExisting();
                for (int i = 0; i < dataRecieveBuffer.Length; i++)
                {
                    // the char ; indicates the ending of one line of data
                    if (dataRecieveBuffer[i] == ';')
                    {
                        MouseManipulation(recievedData);
                        recievedData = "";
                    }
                    //else we just add it to the recieved data that we have
                    else
                    {
                        recievedData += dataRecieveBuffer[i];
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                DisconnectSetup();
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            // make sure all settings are done and that the bluetooth is not already connected
            if (!btDevice.IsOpen && portsComboBox.SelectedIndex != 0 && baudRateComboBox.SelectedIndex != 0) {
                //if the code was able to connect to bluetooth
                if (InitBluetoothConnection()) {
                    ConnectSetup();
                }
            }
            //disconnect from bluetooth
            else if (btDevice.IsOpen) {
                 DisconnectSetup();
            }
        }
        
        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                minRoll = double.Parse(minRollTextBox.Text);
                minPitch = double.Parse(minPitchTextBox.Text);

                maxRoll = double.Parse(maxRollTextBox.Text);
                maxPitch = double.Parse(maxPitchTextBox.Text);

                minMouseSpeed = double.Parse(minSpeedTextBox.Text);
                maxMouseSpeed = double.Parse(maxSpeedTextBox.Text);

                clickAccuaction = double.Parse(clickAccuactionTextBox.Text);

                usePunchClick = (bool)activationCheckBox.IsChecked;

                Console.WriteLine(usePunchClick);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }

        }
    }
}
