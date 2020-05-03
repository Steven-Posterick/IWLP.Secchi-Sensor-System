using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using IWLP.SecchiSensorSystem.Data;
using IWLP.SecchiSensorSystem.Utils;
using LiveCharts;
using LiveCharts.Configurations;
using Ookii.Dialogs.Wpf;

namespace IWLP.SecchiSensorSystem.Windows
{
    /// <summary>
    /// Interaction logic for ApplicationWindow.xaml
    /// </summary>
    public partial class ApplicationWindow : INotifyPropertyChanged
    {
        public ApplicationWindow()
        {
            InitializeComponent();

            WriteToConsole("Starting Application.", true);
            
            _desktopLocation = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            FolderTextBox.Text = _desktopLocation;
            
            _tempUnit = TemperatureUnit.FARENHEIT;
            _lastTempUnit = TemperatureUnit.CELCIUS;
            
            ButtonStatus = "Connect";
            Connected = "Disconnected";

            SetupPlot();
            
            DataContext = this;
        }
        
        
        private void SetupPlot()
        {
            // Create line plot data.
            var mapper = Mappers.Xy<TemperatureModel>()
                .X(model => model.DateTime.Ticks)
                .Y(model => model.Temperature.GetTemperature(_tempUnit));

            Charting.For<TemperatureModel>(mapper);

            ChartValues = new ChartValues<TemperatureModel>();
            
            TimeRange = 30;
            
            //Formats the x axis
            DateTimeFormatter = value => new DateTime((long) value).ToString("mm:ss");
            DegreeFormatter = value => $"{value:0.0}";
            
            //AxisStep forces the distance between each separator in the X axis
            AxisStep = TimeSpan.FromSeconds(4).Ticks;
            
            //AxisUnit forces lets the axis know that we are plotting seconds
            //this is not always necessary, but it can prevent wrong labeling
            AxisUnit = TimeSpan.TicksPerSecond;
            
            SetPlotTitle();
            
            SetAxisLimits(DateTime.Now);

        }

        private readonly string _desktopLocation;

        private SerialPort _serialPort;
        private CsvWriter _csvWriter;
        
        private TemperatureUnit _lastTempUnit;
        private TemperatureUnit _tempUnit;
        
        private double _axisMax;
        private double _axisMin;
        private double _lastTemperature;
        
        private long _axisStep;
        
        private string _yTitle;
        
        private int _timeRange;

        private bool _connected;
        private string _buttonStatus;
        private string _stringConnected;
        private bool _bluetoothDebug;


        public ChartValues<TemperatureModel> ChartValues { get; set; }
        public Func<double, string> DateTimeFormatter { get; set; }

        public Func<double, string> DegreeFormatter { get; set; }

        public long AxisStep 
        {
            get { return _axisStep; }

            set
            {
                _axisStep = value;
                OnPropertyChanged("AxisStep");
            } 
        }
        public double AxisUnit { get; set; }

        public double AxisMax
        {
            get { return _axisMax; }
            set
            {
                _axisMax = value;
                OnPropertyChanged("AxisMax");
            }
        }

        public string Connected
        {
            get { return _stringConnected; }
            set
            {
                _stringConnected = value;
                OnPropertyChanged("Connected");
            }
        }
        public string ButtonStatus
        {
            get { return _buttonStatus;}
            set
            {
                _buttonStatus = value;
                OnPropertyChanged("ButtonStatus");
            }
        }

        public double AxisMin
        {
            get { return _axisMin; }
            set
            {
                _axisMin = value;
                OnPropertyChanged("AxisMin");
            }
        }

        public int TimeRange
        {
            set
            {
                _timeRange = value;
                OnPropertyChanged("TimeRange");
            }

            get => _timeRange;
        }
        public string YTitle
        {
            set
            {
                _yTitle = value;
                OnPropertyChanged("YTitle");
            }
            get => _yTitle;
        }

        public double LastTemperature
        {
            set
            {
                _lastTemperature = value;
                OnPropertyChanged("LastTemperature");
            }
            get { return _lastTemperature; }
        }

        private void WriteToConsole(string message, bool newLine)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, (Action) delegate 
            { 
                OutputBlock.Inlines.Add(message + (newLine ? Environment.NewLine : "")); 
            });
            
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();
            
            if (ports.Length > 0)
            {
                // Clear the entries, then reload them. 
                PortComboBox.Items.Clear();
                
                foreach (var port in ports)
                {
                    PortComboBox.Items.Add(port);
                }

                // Set selected index to first position
                PortComboBox.SelectedIndex = 0;
            } else
            {
                MessageBox.Show(this, "Could not find any communication ports.", "No ports");
            }
        }

        private void SetAxisLimits(DateTime now)
        {
            AxisMax = now.Ticks + TimeSpan.FromSeconds(0.5).Ticks; // lets force the axis to be 1 second ahead
            AxisMin = now.Ticks - TimeSpan.FromSeconds(_timeRange).Ticks; // and 8 seconds behind
        }
        private void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            // Setup folder dialog
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Please select a folder";
            dialog.UseDescriptionForTitle = true;

            // Unsupported version
            if (!VistaFolderBrowserDialog.IsVistaFolderDialogSupported)
            {
                MessageBox.Show(this, "Because you are not using Windows Vista or later, the regular folder browser dialog will be used. Please use Windows Vista to see the new dialog.", "Sample folder browser dialog");
            }
            
            // Set the text box if folder is selected. 
            var success = dialog.ShowDialog(this);
            if (success.HasValue && success.Value)
            {
                FolderTextBox.Text = dialog.SelectedPath;
                WriteToConsole("The selected folder was: " + dialog.SelectedPath, true);

                MessageBox.Show(this, "The selected folder was: " + dialog.SelectedPath,
                    "Sample folder browser dialog");
            }
            else
            {
                // Reset to desktop. 
                FolderTextBox.Text = _desktopLocation;
                MessageBox.Show(this, "Failed to read the folder.","Failed");
            }
        }

        private void PlotButton_Click(object sender, RoutedEventArgs e)
        {
            if (_connected)
            {
                _serialPort.Close();
                _connected = !_connected;
                ButtonStatus = "Connect";
                Connected = "Disconnected";
                return;
            }
            
            _tempUnit = FarenheitRadioButton.IsChecked != null && FarenheitRadioButton.IsChecked.Value ? TemperatureUnit.FARENHEIT : TemperatureUnit.CELCIUS;

            if (PortComboBox.SelectedIndex != -1)
            {
                // Get communication serial port. 
                string port = PortComboBox.SelectedItem.ToString();
                
                // Get and set the csv location/file name. 
                string excelLocation = FolderTextBox.Text;
                string fileName = DateTime.Now.ToString("yyyy-M-dd_HH-mm-ss") + "_secchi_data.csv";
                
                // Create a serial port with the baud rate of 9600. 
                _serialPort = new SerialPort(port, 9600);
                _serialPort.Open();
                _serialPort.DataReceived += SerialDataReceivedEventHandler;


                // Create the csv writer and file. 
                _csvWriter = new CsvWriter(excelLocation + System.IO.Path.DirectorySeparatorChar.ToString() + fileName);
                _csvWriter.WriteArrayToFile(new []{"time","distance","temp"}, true);
                
                _connected = !_connected;
                ButtonStatus = "Disconnect";
                Connected = "Connected";

                if (_bluetoothDebug)
                {
                    WriteToConsole("Connected to port: " + port, true);
                }
            }
            else
            {
                MessageBox.Show(this, "No port is selected.", "Port error");
            }

        }

        private void SerialDataReceivedEventHandler(object sender, SerialDataReceivedEventArgs e)
        {
            var serialPort = (SerialPort) sender;

            if (serialPort != null)
            {
                string received = _serialPort.ReadLine();

                // Verify that the start of the line contains "^"
                // Prevents issues with invalid data being received (typically when starting).
                if (!received.StartsWith("^"))
                {
                    if (_bluetoothDebug)
                    { 
                        WriteToConsole($"Invalid data: {received}", true);
                    }
                    return;
                }

                received = received.Replace("^", "");

                if (_bluetoothDebug)
                {
                    WriteToConsole(received, false);
                }
                // Data is already formatted as comma delimited, so send directly to csv. 
                string[] data = received.Split(",");
                
                // Early return
                if (data.Length <= 1)
                    return;
                
                _csvWriter.WriteToFile(received, true);

                double temp = Double.Parse(data[1]);

                Action action = delegate {
                    AddValue(temp);
                };

                // Now process data to send to graph. 
                if (Application.Current.Dispatcher != null)
                    Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, action);
            }
        }

        private void AddValue(double temp)
        {
            DateTime now = DateTime.Now;
            ChartValues.Add(new TemperatureModel(temp, now));

            // Add new chart point
            var step = (temp - _lastTemperature) / 4;

            Task.Factory.StartNew(() =>
            {
                for (var i = 0; i < 4; i++)
                {
                    Thread.Sleep(100);
                    LastTemperature += step;
                }

                LastTemperature = temp;
            });

            // Reset the axis limits
            SetAxisLimits(now);

            if (ChartValues.Count > 150) ChartValues.RemoveAt(0);

            // Reset title if necessary. 
            if (_lastTempUnit != _tempUnit)
            {
                YTitle = "Temperature (" + (_tempUnit == TemperatureUnit.FARENHEIT ? " F" : " C") + ")";
            }

            _lastTempUnit = _tempUnit;
        }

        private void Thumb_OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            TimeRange = (int) TimeSlider.Value;
            AxisStep = TimeSpan.FromSeconds(TimeRange / 8.0).Ticks;
            SetAxisLimits(DateTime.Now);        
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private void SetPlotTitle()
        {
            YTitle = "Temperature (" + (_tempUnit == TemperatureUnit.FARENHEIT ? " F" : " C") + ")";
        }
        
        private void OnCelciusClick(object sender, RoutedEventArgs e)
        {
            _tempUnit = TemperatureUnit.CELCIUS;
            SetPlotTitle();
        }

        private void OnFarenheitClick(object sender, RoutedEventArgs e)
        {
            _tempUnit = TemperatureUnit.FARENHEIT;
            SetPlotTitle();
        }

        private void BluetoothCheck_Click(object sender, RoutedEventArgs e)
        {
            if (BluetoothCheck.IsChecked != null) _bluetoothDebug = BluetoothCheck.IsChecked.Value;
        }
    }
}