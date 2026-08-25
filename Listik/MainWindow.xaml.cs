using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Listik
{

    public partial class MainWindow : Window
    {
        public TundraManager wrapper;
        public TankiData data;
        private KeyHack _keyHack;
        private HotKeyManager _hotKeyManager;

        


        private bool _isConnected = false;
        private DispatcherTimer _timer;
        private DispatcherTimer _licenseTimer;
        private AppSettings _appSettings;
        private bool _settingsReady;
        private bool _licenseCheckInProgress;
        private bool _isShuttingDown;
        private bool _gameUpdateWarningShown;
        char initialKey1 = 'Q'; // Например, из Settings.Default.Key1
        char initialKey2 = 'Y'; // Например, из Settings.Default.Key2
        char initialKey3 = 'U'; // Например, из Settings.Default.Key3
        public MainWindow()
        {

            InitializeComponent();
            _appSettings = AppSettingsStore.LoadOrCreate();
            initialKey1 = GetHotKey(_appSettings.HotKey1, 'Q');
            initialKey2 = GetHotKey(_appSettings.HotKey2, 'Y');
            bool subscriptionActive;
            int remainingSubscriptionDays;
            string licenseFailureMessage = null;
            if (!string.IsNullOrWhiteSpace(_appSettings.ActivationCode))
            {
                var licenseResult = LicenseService.Activate(_appSettings.ActivationCode, _appSettings.DeviceId,
                    out licenseFailureMessage, out remainingSubscriptionDays);
                subscriptionActive = licenseResult == LicenseValidationResult.Active;
                if (!subscriptionActive && licenseResult == LicenseValidationResult.Inactive)
                {
                    MessageBox.Show(licenseFailureMessage, "Listik", MessageBoxButton.OK, MessageBoxImage.Warning);
                    var activation = new ActivationWindow(_appSettings.DeviceId, _appSettings.ActivationCode);
                    subscriptionActive = activation.ShowDialog() == true && activation.IsLicenseActivated;
                    remainingSubscriptionDays = activation.RemainingDays;
                    licenseFailureMessage = activation.FailureMessage;
                    if (subscriptionActive)
                    {
                        _appSettings.ActivationCode = activation.ActivationCode;
                        AppSettingsStore.Save(_appSettings);
                    }
                }
            }
            else
            {
                var activation = new ActivationWindow(_appSettings.DeviceId);
                subscriptionActive = activation.ShowDialog() == true && activation.IsLicenseActivated;
                remainingSubscriptionDays = activation.RemainingDays;
                licenseFailureMessage = activation.FailureMessage;
                if (subscriptionActive)
                {
                    _appSettings.ActivationCode = activation.ActivationCode;
                    AppSettingsStore.Save(_appSettings);
                }
            }

            if (!subscriptionActive)
            {
                MessageBox.Show(licenseFailureMessage ?? "Для работы функций требуется активная подписка.",
                    "Listik", MessageBoxButton.OK, MessageBoxImage.Warning);
                ShutdownApplication();
                return;
            }

            // Инициализация менеджера горячих клавиш
            _keyHack = new KeyHack(this);
            _hotKeyManager = new HotKeyManager();
            InitializeHotKeys(initialKey1, initialKey2, initialKey3);

            wrapper = new TundraManager();
            SubscriptionDaysText.Text = "Подписка: " + Math.Max(1, remainingSubscriptionDays) + " дн.";
            data= new TankiData();
            wrapper.ReadData(data);
            // VersionText.Text = wrapper.GetVersion().ToString();
            int version = wrapper.GetVersion(); // предположим, вернуло 230
            string formattedVersion = string.Join(".", version.ToString().Select(c => c.ToString()));
            VersionText.Text = formattedVersion; // "2.3.0"


            CheckBoxOption1.IsChecked = _appSettings.HookGrass;
            CheckBoxOption2.IsChecked = _appSettings.KeepTrunks;
            AutoDisableCheckBox.IsChecked = _appSettings.AutoDisable;
            TextBox1.TextChanged += SettingsControl_Changed;
            TextBox2.TextChanged += SettingsControl_Changed;
            _settingsReady = true;

            this.Closing += MainWindow_Closing;
            Loaded += MainWindow_Loaded;

            // Настройка таймера для периодического чтения
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500); // 0.5 секунды
            _timer.Tick += Timer_Tick;
            _timer.Start();

            _licenseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _licenseTimer.Tick += LicenseTimer_Tick;
            _licenseTimer.Start();
        }

        private async void LicenseTimer_Tick(object sender, EventArgs e)
        {
            if (_licenseCheckInProgress || _isShuttingDown)
                return;

            _licenseCheckInProgress = true;
            try
            {
                var licenseCheck = await Task.Run(() => LicenseService.Check(
                    _appSettings.ActivationCode, _appSettings.DeviceId));
                if (licenseCheck.Result == LicenseValidationResult.Active)
                {
                    SubscriptionDaysText.Text = "Подписка: " + Math.Max(1, licenseCheck.RemainingDays) + " дн.";
                    return;
                }

                MessageBox.Show(licenseCheck.Message, "Listik", MessageBoxButton.OK, MessageBoxImage.Warning);
                ShutdownApplication();
            }
            finally
            {
                _licenseCheckInProgress = false;
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var update = await UpdateService.GetLatestReleaseAsync();
            if (_isShuttingDown || update == null || wrapper == null)
                return;

            var localVersion = UpdateService.GetLocalVersion(wrapper.GetVersion());
            if (update.Version.CompareTo(localVersion) <= 0)
                return;

            var answer = MessageBox.Show(
                $"Доступна новая версия {update.Tag}.\nТекущая версия: {localVersion}.\n\nОткрыть страницу обновления?",
                "Доступно обновление", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (answer == MessageBoxResult.Yes)
                Process.Start(new ProcessStartInfo(update.ReleaseUrl) { UseShellExecute = true });
        }

        private void ShutdownApplication()
        {
            if (_isShuttingDown)
                return;
            _isShuttingDown = true;
            _timer?.Stop();
            _licenseTimer?.Stop();
            Application.Current.Shutdown();
        }
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private static char GetHotKey(string value, char defaultValue)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Length == 1 && char.IsLetter(value[0])
                ? char.ToUpper(value[0])
                : defaultValue;
        }
        private void SettingsControl_Changed(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }
        private void SaveSettings()
        {
            if (!_settingsReady)
                return;

            _appSettings.HotKey1 = TextBox1.Text;
            _appSettings.HotKey2 = TextBox2.Text;
            _appSettings.HookGrass = CheckBoxOption1.IsChecked == true;
            _appSettings.KeepTrunks = CheckBoxOption2.IsChecked == true;
            _appSettings.AutoDisable = AutoDisableCheckBox.IsChecked == true;
            AppSettingsStore.Save(_appSettings);
        }
        private void InitializeHotKeys(char key1, char key2, char key3)
        {
            // Создаем менеджер с начальными значениями
            _hotKeyManager = new HotKeyManager(key1, key2, key3);

            // Регистрируем поля с указанием индекса
            _hotKeyManager.RegisterTextBox(TextBox1, OnLetterEntered, 0);
            _hotKeyManager.RegisterTextBox(TextBox2, OnLetterEntered, 1);
           // _hotKeyManager.RegisterTextBox(TextBox3, OnLetterEntered, 2);

            UpdateStatusText();
        }
        private void UpdateStatusText()
        {
          //  var keys = _hotKeyManager.GetCurrentKeys();
          //  StatusText.Text = $"Текущие буквы: {keys[0]} {keys[1]} {keys[2]}";
        }
        // Обработчик нажатия кнопок "Назначить"
        private void OnLetterEntered(char letter)
        {
            // Здесь ваша логика обработки введенной буквы
            // Например, обновление статуса
            var currentLetters = _hotKeyManager.GetCurrentLetters();
            string status = "Текущие буквы: ";

            foreach (var kvp in currentLetters)
            {
                status += (kvp.Value.HasValue ? kvp.Value.ToString() : "_") + " ";
            }

            StatusText.Text = status;
        }


        //FullTundra  //(ствол + листва + кусты) 6- отображается всё/ 2- отображаются стволы/ 0- ничего не отображается
        //Trees       //все деревья кроме кустов 2- отображаются/ 0- выключены
        //Grass       //трава 2- отображается/ 0- нет

        byte SetFull = 6;
        byte SetTree = 2;
        int Grass_enable = 2;

        //обработчик переключения режимов
        public void IsPressHotkey(string key)
        {
           
            if (_isConnected)
            {
               
               // _tundraManager._ReadParam();
                var keys = _hotKeyManager.GetCurrentKeys();

                if (CheckBoxOption1.IsChecked == true) Grass_enable = 0;
                else Grass_enable = 2;

                if (key == TextBox1.Text) //кроны и кусты
                {
                    //byte full = _tundraManager._getData.FullTundra;
                    //byte Tree = _tundraManager._getData.Trees;
                    //byte Grass= _tundraManager._getData.Grass;

                    bool foliageIsEnabled = data.FullTundra == 6;
                    if (foliageIsEnabled)
                    {
                        SetTree = 2;
                        SetFull = CheckBoxOption2.IsChecked == true ? (byte)2 : (byte)0;
                    }
                    else
                    {
                        SetTree = 2;
                        SetFull = 6;
                    }

                    data.FullTundra = SetFull;
                    data.Trees = SetTree;
                    data.Grass = (byte)Grass_enable;
                    wrapper.WriteData(data, AutoDisableCheckBox.IsChecked == true);

                  /*   if (_tundraManager._getData.FullTundra != 6)
                        SetFull = 6;

                    _tundraManager._SetParam(Grass_enable, SetFull, SetTree);*/
                }
                if (key == TextBox2.Text) //выключить кроны деревьев и кусты
                {

                    bool foliageIsEnabled = data.Trees == 2;
                    if (foliageIsEnabled)
                    {
                        SetTree = 0;
                        SetFull = 6;
                    }
                    else
                    {
                        SetTree = 2;
                        SetFull = 6;
                    }

                    data.FullTundra = SetFull;
                    data.Trees = SetTree;
                    data.Grass = (byte)Grass_enable;
                    wrapper.WriteData(data, AutoDisableCheckBox.IsChecked == true);

                    /*if (_tundraManager._getData.Trees == 0)
                        SetTree = 2;
                    else
                        SetTree = 0;
                    //if (_tundraManager._getData.FullTundra != 0)
                    //    _tundraManager._SetParam(Grass_enable, 0);
                    //if (_tundraManager._getData.FullTundra == 0)
                    //    _tundraManager._SetParam(Grass_enable, 2);

                    _tundraManager._SetParam(Grass_enable, 6, SetTree);*/
                }
              //  if (key == TextBox3.Text)//выключить кроны
                {

                    //if (_tundraManager._getData.Trees == 2)
                    //    _tundraManager._SetParam(Grass_enable, 6, 0);
                    //if (_tundraManager._getData.Trees == 0)
                    //    _tundraManager._SetParam(Grass_enable, 6, 2);
                }

            }

        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            // Чтение параметров каждые 0.5 секунды
            ReadParameters();
        }
        private void AutoDisableCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            wrapper?.SetAutoDisableEnabled(AutoDisableCheckBox.IsChecked == true);
            SaveSettings();
        }

        public bool ProcessIsOpen()
        {
              if (!wrapper.IsProcessOpen() && _isConnected)
              {
                wrapper.CloseProcess();
                  _isConnected = false;
              }
              if (wrapper.IsProcessOpen() && !_isConnected)
              {
                if (!wrapper.OpenProcess())
                {
                    if (wrapper.GetGameCompatibilityStatus() == 0)
                        ShowGameUpdateRequired();
                    return false;
                }
                _isConnected = true;
              }
              if (_isConnected && wrapper.GetGameCompatibilityStatus() == 0)
              {
                  ShowGameUpdateRequired();
                  return false;
              }
              return wrapper.IsProcessOpen();
        }

        private void ShowGameUpdateRequired()
        {
            if (_gameUpdateWarningShown || _isShuttingDown)
                return;

            _gameUpdateWarningShown = true;
            MessageBox.Show("Требуется обновить программу. Версия игры изменилась.",
                "Listik", MessageBoxButton.OK, MessageBoxImage.Warning);
            ShutdownApplication();
        }
        private void ReadParameters()
        {
            try
            {
                if (ProcessIsOpen())
                {

                    wrapper.ReadData(data);
                    StatusText.Text = "Подключено";
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(66, 211, 146));
                    StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(66, 211, 146));
                    // Обновление UI с полученными значениями
                    Dispatcher.Invoke(() =>
                    {
                        FullTundraText.Text = data.FullTundraStatus();
                        TreesText.Text = data.TreesStatus();
                        GrassText.Text = data.GrassStatus();
                        IsBattleText.Text = data.IsBattleStatus();
                    });
                }
                else
                {
                    StatusText.Text = "Отключено";
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 112, 125));
                    StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(255, 112, 125));
                    Dispatcher.Invoke(() =>
                    {
                        FullTundraText.Text = "-";
                        TreesText.Text = "-";
                        GrassText.Text = "-";
                        IsBattleText.Text = "-";
                    });
                }
            }
            catch (Exception ex)
            {
                // В случае ошибки чтения, проверяем соединение

            }
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Здесь можно отменить закрытие
          /*  if (MessageBox.Show("Вы уверены, что хотите выйти?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                e.Cancel = true; // Отменяем закрытие
                return;
            }*/
            _keyHack.UnHookKeys();
            wrapper.CloseProcess();
            // Сохраняем данные перед закрытием

        }

      
    }
}
