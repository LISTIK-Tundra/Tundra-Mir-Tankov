using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Listik
{
    public class HotKeyManager
    {
        private readonly List<TextBox> _textBoxes;
        private readonly Dictionary<TextBox, Action<char>> _textBoxActions;


        // Стандартные стили
        private Style _normalStyle;
        private Style _selectedStyle;


        public char Key1 { get; set; }
        public char Key2 { get; set; }
        public char Key3 { get; set; }


        public HotKeyManager(char key1 = '\0', char key2 = '\0', char key3 = '\0')
        {
            _textBoxes = new List<TextBox>();
            _textBoxActions = new Dictionary<TextBox, Action<char>>();
            InitializeStyles();
            Key1 = key1;
            Key2 = key2;
          //  Key3 = key3;
        }
        private void InitializeTextBoxWithKey(TextBox textBox, int index)
        {
            char initialKey = GetKeyByIndex(index);
            if (initialKey != '\0' && IsEnglishLetter(initialKey))
            {
                textBox.Text = char.ToUpper(initialKey).ToString();

                // Проверяем на уникальность при инициализации
                if (HasDuplicateInitialKeys())
                {
                    MessageBox.Show($"Внимание: Обнаружены повторяющиеся буквы при инициализации!\n" +
                                  $"Key1: {Key1}, Key2: {Key2}, Key3: {Key3}\n" +
                                  $"Пожалуйста, проверьте начальные значения.",
                                  "Предупреждение",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                }
            }
        }
        private void UpdateKeyProperty(TextBox textBox, char letter)
        {
            int index = _textBoxes.IndexOf(textBox);
            switch (index)
            {
                case 0: Key1 = letter; break;
                case 1: Key2 = letter; break;
                case 2: Key3 = letter; break;
            }
        }
        public char[] GetCurrentKeys()
        {
            return new char[] { Key1, Key2, Key3 };
        }

        private char GetKeyByIndex(int index)
        {
            switch (index)
            {
                case 0: return Key1;
                case 1: return Key2;
                case 2: return Key3;
                default: return '\0';
            }
        }

        private bool HasDuplicateInitialKeys()
        {
            var keys = new List<char>();
            if (Key1 != '\0') keys.Add(char.ToUpper(Key1));
            if (Key2 != '\0') keys.Add(char.ToUpper(Key2));
            if (Key3 != '\0') keys.Add(char.ToUpper(Key3));

            return keys.Count != keys.Distinct().Count();
        }

        private void InitializeStyles()
        {

            // Создаем шаблон без каретки, но с отображением текста
            var template = new ControlTemplate(typeof(TextBox));
            // Создаем Border
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(TextBox.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(TextBox.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(TextBox.BorderThicknessProperty));

            // Вместо ScrollViewer используем TextBlock для отображения текста
            var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            textBlockFactory.SetValue(TextBlock.TextProperty, new TemplateBindingExtension(TextBox.TextProperty));
            textBlockFactory.SetValue(TextBlock.TextAlignmentProperty, new TemplateBindingExtension(TextBox.TextAlignmentProperty));
            textBlockFactory.SetValue(TextBlock.VerticalAlignmentProperty, new TemplateBindingExtension(TextBox.VerticalContentAlignmentProperty));
            textBlockFactory.SetValue(TextBlock.FontSizeProperty, new TemplateBindingExtension(TextBox.FontSizeProperty));
            textBlockFactory.SetValue(TextBlock.MarginProperty, new Thickness(5, 0, 0, 0));

            borderFactory.AppendChild(textBlockFactory);
            template.VisualTree = borderFactory;

            
            // Нормальный стиль
            _normalStyle = new Style(typeof(TextBox));
            _normalStyle.Setters.Add(new Setter(TextBox.TemplateProperty, template));
            _normalStyle.Setters.Add(new Setter(TextBox.BackgroundProperty, Brushes.White));
            _normalStyle.Setters.Add(new Setter(TextBox.BorderBrushProperty, Brushes.Gray));
            _normalStyle.Setters.Add(new Setter(TextBox.BorderThicknessProperty, new Thickness(3)));
            _normalStyle.Setters.Add(new Setter(TextBox.FontWeightProperty, FontWeights.Bold));
            _normalStyle.Setters.Add(new Setter(TextBox.ForegroundProperty, Brushes.Green));

           
            // Стиль для выбранного поля
            _selectedStyle = new Style(typeof(TextBox));
            _selectedStyle.Setters.Add(new Setter(TextBox.TemplateProperty, template));
            _selectedStyle.Setters.Add(new Setter(TextBox.BackgroundProperty, Brushes.LightBlue));
            _selectedStyle.Setters.Add(new Setter(TextBox.BorderBrushProperty, Brushes.DodgerBlue));
            _selectedStyle.Setters.Add(new Setter(TextBox.BorderThicknessProperty, new Thickness(3)));
            _selectedStyle.Setters.Add(new Setter(TextBox.FontWeightProperty, FontWeights.Bold));
            _selectedStyle.Setters.Add(new Setter(TextBox.ForegroundProperty, Brushes.GreenYellow));



        }

        /// <summary>
        /// Регистрирует TextBox для управления горячими клавишами
        /// </summary>
        /// <param name="textBox">Поле ввода</param>
        /// <param name="onLetterEntered">Действие при вводе буквы</param>
        public void RegisterTextBox(TextBox textBox, Action<char> onLetterEntered, int index)
        {
            if (textBox == null)
                throw new ArgumentNullException(nameof(textBox));

            if (_textBoxes.Count >= 3)
                throw new InvalidOperationException("Можно зарегистрировать не более 3 полей ввода");

            textBox.MaxLength = 1;
            textBox.TextAlignment = TextAlignment.Center;
            textBox.VerticalContentAlignment = VerticalAlignment.Center;

            // Стиль объявлен в XAML, чтобы поля горячих клавиш оставались частью
            // общей тёмной темы и не переключались на системный белый фон.
            var keyBoxStyle = textBox.TryFindResource("KeyBoxStyle") as Style;
            if (keyBoxStyle != null)
            {
                textBox.Style = keyBoxStyle;
            }

            // Подписываемся на события
            textBox.PreviewTextInput += OnPreviewTextInput;
            textBox.PreviewKeyDown += OnPreviewKeyDown;
            textBox.TextChanged += OnTextChanged;
            // Отключаем контекстное меню
            textBox.ContextMenu = null;

            InitializeTextBoxWithKey(textBox, index);

            DataObject.AddPastingHandler(textBox, OnPaste);

            // Скрываем каретку при получении фокуса
            textBox.GotFocus += (s, e) =>
            {
                var tb = s as TextBox;
                tb?.Select(tb.Text.Length, 0);
            };

            _textBoxes.Add(textBox);
            _textBoxActions[textBox] = onLetterEntered;
        }

  

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // Проверяем, что введен один символ и это английская буква
            if (e.Text.Length != 1 || !IsEnglishLetter(e.Text[0]))
            {
                e.Handled = true;
                return;
            }

            char newLetter = char.ToUpper(e.Text[0]);

            // Проверяем, не используется ли эта буква в других полях
            if (IsLetterUsedInOtherTextBoxes(textBox, newLetter))
            {
                e.Handled = true;
                ShowDuplicateLetterWarning();
                return;
            }

            // Очищаем текущее поле перед вводом новой буквы
            textBox.Clear();
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Разрешаем только клавиши с буквами, Backspace и Delete
            if (e.Key == Key.Space)
            {
                e.Handled = true;
                return;
            }

            // Разрешаем навигационные клавиши
            if (e.Key == Key.Tab || e.Key == Key.Enter ||
                e.Key == Key.Escape || e.Key == Key.Back ||
                e.Key == Key.Delete || e.Key == Key.Left ||
                e.Key == Key.Right || e.Key == Key.Home ||
                e.Key == Key.End)
            {
                return;
            }

            // Для букв проверяем, что это английские
            if (e.Key >= Key.A && e.Key <= Key.Z)
            {
                return; // Разрешаем обработку в PreviewTextInput
            }

            // Все остальное блокируем
            e.Handled = true;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            if (!string.IsNullOrEmpty(textBox.Text))
            {
                char letter = textBox.Text[0];
                if (IsEnglishLetter(letter))
                {
                    // Приводим к верхнему регистру для единообразия
                    textBox.Text = char.ToUpper(letter).ToString();
                    textBox.CaretIndex = textBox.Text.Length;

                    // Вызываем функцию обработки
                 //   _textBoxActions[textBox]?.Invoke(letter);
                }
            }
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            // Запрещаем вставку
            e.CancelCommand();
            e.Handled = true;
        }

        private bool IsEnglishLetter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        private bool IsLetterUsedInOtherTextBoxes(TextBox currentTextBox, char letter)
        {
            return _textBoxes
                .Where(tb => tb != currentTextBox)
                .Any(tb => !string.IsNullOrEmpty(tb.Text) &&
                          char.ToUpper(tb.Text[0]) == char.ToUpper(letter));
        }

        private void ShowDuplicateLetterWarning()
        {
            MessageBox.Show("Эта буква уже используется в другом поле!",
                          "Предупреждение",
                          MessageBoxButton.OK,
                          MessageBoxImage.Warning);
        }

        /// <summary>
        /// Очищает все поля
        /// </summary>
        public void ClearAll()
        {
            foreach (var textBox in _textBoxes)
            {
                textBox.Clear();
            }
        }

        /// <summary>
        /// Получает текущие назначенные буквы
        /// </summary>
        public Dictionary<TextBox, char?> GetCurrentLetters()
        {
            var result = new Dictionary<TextBox, char?>();
            foreach (var textBox in _textBoxes)
            {
                result[textBox] = string.IsNullOrEmpty(textBox.Text) ?
                    (char?)null : textBox.Text[0];
            }
            return result;
        }
    }
}
