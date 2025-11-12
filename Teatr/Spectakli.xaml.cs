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

namespace Teatr
{
    /// <summary>
    /// Логика взаимодействия для Spectakli.xaml
    /// </summary>
    public partial class Spectakli : Page
    {
        int CountRecords;//Количество записей в таблице
        int CountPage;//Общее количество страниц
        int CurrentPage = 0;//Текущая страница

        List<Репертуар_театра> CurrentPageList = new List<Репертуар_театра>();
        List<Репертуар_театра> TableList = new List<Репертуар_театра>(); // Инициализируем пустым списком
        public Spectakli()
        {
            InitializeComponent();
            var currentProducts = TeatrEntities.GetContext().Репертуар_театра.ToList();
            Spisok.ItemsSource = currentProducts;
            ComboType.SelectedIndex = 0;
            Update();
        }

        private void Spisok_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Update();
        }

        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            Update();
        }

        private void RButtonUp_Checked(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private void RButtonDown_Checked(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private void Update()
        {
            var currentSpectacli = TeatrEntities.GetContext().Репертуар_театра.ToList();
        

            if (ComboType.SelectedIndex == 1)
            {
                currentSpectacli = currentSpectacli.Where(p => p.Спектакли.Жанр.Contains("Драма")).ToList();
            }
            if (ComboType.SelectedIndex == 2)
            {
                currentSpectacli = currentSpectacli.Where(p => p.Спектакли.Жанр.Contains("Комедия")).ToList();
            }
            if (ComboType.SelectedIndex == 3)
            {
                currentSpectacli = currentSpectacli.Where(p => p.Спектакли.Жанр.Contains("Фэнтези")).ToList();
            }
            if (ComboType.SelectedIndex == 4)
            {
                currentSpectacli = currentSpectacli.Where(p => p.Спектакли.Жанр.Contains("Трагедия")).ToList();
            }
            if (ComboType.SelectedIndex == 5)
            {
                currentSpectacli = currentSpectacli.Where(p => p.Спектакли.Жанр.Contains("Балет")).ToList();
            }
            if (ComboType.SelectedIndex == 6)
            {
                currentSpectacli = currentSpectacli.Where(p => p.Спектакли.Жанр.Contains("Эпопея")).ToList();
            }
            string searchText = TBoxSearch.Text.ToLower();

            currentSpectacli = currentSpectacli.Where(p =>
                            (string.IsNullOrEmpty(searchText) ||
                             p.Спектакли.Жанр.ToLower().Contains(searchText) ||
                             p.Спектакли.Автор.ToLower().Contains(searchText) ||
                             p.Спектакли.Тип.ToLower().Contains(searchText) ||                             
                             p.Спектакли.Название.ToLower().Contains(searchText))
                        ).ToList();


            if (RButtonDown.IsChecked.Value)
            {
                currentSpectacli = currentSpectacli.OrderByDescending(p => p.Цена_билета).ToList();
            }
            if (RButtonUp.IsChecked.Value)
            {
                currentSpectacli = currentSpectacli.OrderBy(p => p.Цена_билета).ToList();

            }

            // Сохраняем отфильтрованные данные в TableList для постраничного вывода
            TableList = currentSpectacli.ToList();
            
            // Обновляем постраничный вывод
            ChangePage(0, 0);
        }

        private void ComboType_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            Update();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new AddEdit((sender as Button).DataContext as Репертуар_театра));
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
           
                // Получаем выбранный элемент
                var selectedItem = (sender as Button).DataContext as Репертуар_театра;

                // Проверяем цену билета
                if (selectedItem != null && selectedItem.Цена_билета <= 1500)
                {
                    MessageBox.Show("Нельзя удалить запись с ценой билета меньше или равной 1500", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Если цена больше 1500, продолжаем удаление
                var currentSpectakl = (sender as Button).DataContext as Репертуар_театра;

                if (currentSpectakl != null)
                {
                    // Проверяем наличие связанных записей (опционально)
                    if (MessageBox.Show($"Вы уверены, что хотите удалить спектакль '{currentSpectakl.Спектакли.Название}'?",
                        "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        try
                        {
                            TeatrEntities.GetContext().Репертуар_театра.Remove(currentSpectakl);
                            TeatrEntities.GetContext().SaveChanges();
                            MessageBox.Show("Запись успешно удалена!", "Успех",
                                           MessageBoxButton.OK, MessageBoxImage.Information);

                            // Обновляем данные
                            Update();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                                           MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
        }

        

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new AddEdit(null));
        }

        private void ChangePage(int direction, int? selectedPage)
        {
            CurrentPageList.Clear();
            CountRecords = TableList.Count;

            if (CountRecords == 0)
            {
                // Если записей нет, сбрасываем интерфейс
                TBCount.Text = "0";
                TBAllRecords.Text = " из 0";
                PageListBox.Items.Clear();
                Spisok.ItemsSource = CurrentPageList;
                return;
            }

            if (CountRecords % 10 > 0)
            {
                CountPage = CountRecords / 10 + 1;
            }
            else
            {
                CountPage = CountRecords / 10;
            }

            Boolean Ifupdate = true;

            int min;

            if (selectedPage.HasValue)
            {
                if (selectedPage >= 0 && selectedPage < CountPage)
                {
                    CurrentPage = (int)selectedPage;
                    min = CurrentPage * 10 + 10 < CountRecords ? CurrentPage * 10 + 10 : CountRecords;
                    for (int i = CurrentPage * 10; i < min; i++)
                    {
                        CurrentPageList.Add(TableList[i]);
                    }
                }
            }
            else
            {
                switch (direction)
                {
                    case 1:
                        if (CurrentPage > 0)
                        {
                            CurrentPage--;
                            min = CurrentPage * 10 + 10 < CountRecords ? CurrentPage * 10 + 10 : CountRecords;
                            for (int i = CurrentPage * 10; i < min; i++)
                            {
                                CurrentPageList.Add(TableList[i]);
                            }
                        }
                        else
                        {
                            Ifupdate = false;
                        }
                        break;

                    case 2:
                        if (CurrentPage < CountPage - 1)
                        {
                            CurrentPage++;
                            min = CurrentPage * 10 + 10 < CountRecords ? CurrentPage * 10 + 10 : CountRecords;
                            for (int i = CurrentPage * 10; i < min; i++)
                            {
                                CurrentPageList.Add(TableList[i]);
                            }
                        }
                        else
                        {
                            Ifupdate = false;
                        }
                        break;
                }
            }
            if (Ifupdate)
            {
                PageListBox.Items.Clear();

                for (int i = 1; i <= CountPage; i++)
                {
                    PageListBox.Items.Add(i);
                }
                PageListBox.SelectedIndex = CurrentPage;

                min = CurrentPage * 10 + 10 < CountRecords ? CurrentPage * 10 + 10 : CountRecords;
                TBCount.Text = min.ToString();
                TBAllRecords.Text = " из " + CountRecords.ToString();

                Spisok.ItemsSource = CurrentPageList;
                Spisok.Items.Refresh();
            }
        }

        private void LeftDirButton_Click(object sender, RoutedEventArgs e)
        {
            ChangePage(1, null);
        }

        private void PageListBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (PageListBox.SelectedItem != null)
            {
                ChangePage(0, Convert.ToInt32(PageListBox.SelectedItem.ToString()) - 1);
            }
        }

        private void RightDirButton_Click(object sender, RoutedEventArgs e)
        {
            ChangePage(2, null);
        }
    }
}
