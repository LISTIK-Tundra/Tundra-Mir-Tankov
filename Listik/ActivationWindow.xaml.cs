using System.Windows;

namespace Listik
{
    public partial class ActivationWindow : Window
    {
        private readonly string _deviceId;
        public bool IsLicenseActivated { get; private set; }
        public int RemainingDays { get; private set; }
        public string ActivationCode { get; private set; }
        public string FailureMessage { get; private set; }

        public ActivationWindow(string deviceId, string initialCode = null)
        {
            InitializeComponent();
            _deviceId = deviceId;
            CodeTextBox.Text = initialCode ?? string.Empty;
            CodeTextBox.SelectAll();
        }
        private void Activate_Click(object sender, RoutedEventArgs e)
        {
            var code = CodeTextBox.Text.Trim();
            var result = LicenseService.Activate(code, _deviceId, out var message, out var remainingDays);
            if (result == LicenseValidationResult.Active)
            {
                IsLicenseActivated = true;
                RemainingDays = remainingDays;
                ActivationCode = code;
                DialogResult = true;
            }
            else
            {
                if (result == LicenseValidationResult.Inactive)
                {
                    MessageBox.Show(message, "Активация", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CodeTextBox.Focus();
                    CodeTextBox.SelectAll();
                    return;
                }

                FailureMessage = message;
                DialogResult = false;
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
