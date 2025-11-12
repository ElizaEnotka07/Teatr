using System;
using System.Collections.Generic;
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
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;
using System.Text.RegularExpressions;

namespace Teatr
{
    public partial class AddEdit : Page
    {
        private bool isNew;
        private readonly string[] allowedDays = { "пн", "вт", "ср", "чт", "пт", "сб", "вс" };
        private readonly Regex timeRegex = new Regex(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$");

        public AddEdit(Репертуар_театра selectedRepertoire)
        {
            InitializeComponent();

            if (selectedRepertoire != null)
            {
                DataContext = selectedRepertoire;
                isNew = false;
            }
            else
            {
                DataContext = new Репертуар_театра();
                isNew = true;
            }

            LoadSpectacles();
        }

        // Обработчик ввода текста
        private void DaysTimeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            string newText = textBox.Text + e.Text;

            // Разрешаем только русские буквы, цифры, запятые, пробелы и дефис
            if (!IsValidInput(newText))
            {
                e.Handled = true;
                return;
            }
        }

        // Проверка валидности ввода
        private bool IsValidInput(string input)
        {
            // Разрешаем только русские буквы, цифры, запятые, пробелы, дефис и двоеточие
            var regex = new Regex(@"^[а-яёА-ЯЁ0-9,\s\-:]*$");
            return regex.IsMatch(input);
        }

        // Валидация формата при сохранении
        private bool ValidateDaysTimeFormat(string daysTime)
        {
            if (string.IsNullOrWhiteSpace(daysTime))
                return false;

            // Проверяем общий формат: дни через запятую + " - " + время
            var parts = daysTime.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return false;

            string daysPart = parts[0].Trim().ToLower();
            string timePart = parts[1].Trim();

            // Проверяем время
            if (!timeRegex.IsMatch(timePart))
                return false;

            // Проверяем дни
            var dayItems = daysPart.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var day in dayItems)
            {
                string cleanDay = day.Trim().ToLower();
                if (!allowedDays.Contains(cleanDay))
                    return false;
            }

            return true;
        }

        // Подсказка при получении фокуса
        private void DaysTimeTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = "Пн, Ср, Пт - 19:00";
                textBox.SelectAll();
            }
        }

        private void LoadSpectacles()
        {
            try
            {
                var context = TeatrEntities.GetContext();
                var spectaclesList = context.Спектакли.ToList();
                SpectacleComboBox.ItemsSource = spectaclesList;
                SpectacleComboBox.DisplayMemberPath = "Название";
                SpectacleComboBox.SelectedValuePath = "СпектакльID";

                var currentRepertoire = DataContext as Репертуар_театра;
                if (!isNew && currentRepertoire != null && currentRepertoire.СпектакльID > 0)
                {
                    SpectacleComboBox.SelectedValue = currentRepertoire.СпектакльID;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки спектаклей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var currentRepertoire = DataContext as Репертуар_театра;
            if (currentRepertoire == null)
            {
                MessageBox.Show("Ошибка: объект не инициализирован", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StringBuilder errors = new StringBuilder();

            // Проверка выбора спектакля
            if (SpectacleComboBox.SelectedItem == null)
                errors.AppendLine("Выберите спектакль");

            // Проверка полей репертуара
            if (currentRepertoire.Цена_билета <= 0)
                errors.AppendLine("Укажите корректную цену билета");

            if (currentRepertoire.Дата_премьеры == DateTime.MinValue)
                errors.AppendLine("Укажите дату премьеры");

            if (string.IsNullOrWhiteSpace(currentRepertoire.Период_проведения))
                errors.AppendLine("Укажите период проведения");

            // ВАЛИДАЦИЯ ФОРМАТА ДНИ И ВРЕМЯ
            if (string.IsNullOrWhiteSpace(currentRepertoire.Дни_и_время))
            {
                errors.AppendLine("Укажите дни и время");
            }
            else if (!ValidateDaysTimeFormat(currentRepertoire.Дни_и_время))
            {
                errors.AppendLine("Неверный формат дней и времени!\n" +
                    "Правильный формат: Пн, Ср, Пт - 19:00\n" +
                    "Допустимые дни: пн, вт, ср, чт, пт, сб, вс\n" +
                    "Время в формате: ЧЧ:MM (например: 19:00)");
            }

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString(), "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (var context = new TeatrEntities())
                {
                    if (isNew)
                    {
                        // РЕШЕНИЕ: Используем Database.SqlQuery для прямого SQL запроса
                        string sql = @"INSERT INTO Репертуар_театра (СпектакльID, Дата_премьеры, Период_проведения, Дни_и_время, Цена_билета) 
                                      VALUES (@p0, @p1, @p2, @p3, @p4)";

                        context.Database.ExecuteSqlCommand(sql,
                            (int)SpectacleComboBox.SelectedValue,
                            currentRepertoire.Дата_премьеры,
                            currentRepertoire.Период_проведения,
                            currentRepertoire.Дни_и_время,
                            currentRepertoire.Цена_билета);

                        MessageBox.Show("Информация сохранена успешно", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Для редактирования используем обычный подход EF
                        var existing = context.Репертуар_театра
                            .FirstOrDefault(r => r.РепертуарID == currentRepertoire.РепертуарID);

                        if (existing != null)
                        {
                            existing.СпектакльID = (int)SpectacleComboBox.SelectedValue;
                            existing.Дата_премьеры = currentRepertoire.Дата_премьеры;
                            existing.Период_проведения = currentRepertoire.Период_проведения;
                            existing.Дни_и_время = currentRepertoire.Дни_и_время;
                            existing.Цена_билета = currentRepertoire.Цена_билета;

                            context.SaveChanges();
                            MessageBox.Show("Информация сохранена успешно", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Запись не найдена в базе данных", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    Manager.MainFrame.GoBack();
                }
            }
            catch (DbEntityValidationException ex)
            {
                StringBuilder validationErrors = new StringBuilder();
                validationErrors.AppendLine("Ошибки валидации:");
                foreach (var entityValidationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in entityValidationErrors.ValidationErrors)
                    {
                        validationErrors.AppendLine($"- {validationError.PropertyName}: {validationError.ErrorMessage}");
                    }
                }
                MessageBox.Show(validationErrors.ToString(), "Ошибка валидации",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (DbUpdateException ex)
            {
                Exception innerException = ex;
                while (innerException.InnerException != null)
                {
                    innerException = innerException.InnerException;
                }

                string errorMessage = $"Ошибка базы данных: {innerException.Message}";
                MessageBox.Show(errorMessage, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Ошибка: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\nДетали: {ex.InnerException.Message}";
                }
                MessageBox.Show(errorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.GoBack();
        }
    }
}