using RoslynTutorial.Wpf.Models;
using System.Windows;

namespace RoslynTutorial.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var personModel = PersonModelFactory.Create();

            DataContext = personModel;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PersonModel personModel)
            {
                var result = personModel.Ping();
                MessageBox.Show(result.ToString("G"));
            }
        }
    }
}
