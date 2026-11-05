using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace SmoCalc
{
    public partial class MainWindow : Window
    {
        // длительность единиц времени в минутах: мин, ч, сут
        private static readonly double[] UnitMinutes = { 1.0, 60.0, 1440.0 };
        private static readonly string[] UnitNames = { "мин", "ч", "сут" };

        public MainWindow()
        {
            InitializeComponent();
            UpdateVisibility();
        }

        private void cbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateVisibility();
        }

        private void cbMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateVisibility();
        }

        // показываем только нужные поля
        private void UpdateVisibility()
        {
            if (spQueue == null || spK == null || lblService == null) return;

            int type = cbType.SelectedIndex;
            spQueue.Visibility = (type == 2) ? Visibility.Visible : Visibility.Collapsed;
            spK.Visibility = (type == 1) ? Visibility.Visible : Visibility.Collapsed;

            if (cbMode.SelectedIndex == 0)
            {
                lblService.Text = "Интенсивность обслуживания μ (заявок на канал):";
                lblServiceUnit.Text = "  в единицу:  ";
            }
            else
            {
                lblService.Text = "Среднее время обслуживания одной заявки:";
                lblServiceUnit.Text = "  в единицах:  ";
            }
        }

        private double ReadDouble(TextBox tb, string name)
        {
            string s = tb.Text.Trim().Replace(',', '.');
            double v;
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                throw new FormatException("Поле «" + name + "» должно быть числом.");
            return v;
        }

        private void btnCalc_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int type = cbType.SelectedIndex;

                int n = (int)ReadDouble(tbN, "Число каналов n");
                if (n < 1) throw new ArgumentException("Число каналов должно быть не меньше 1.");

                double lambda = ReadDouble(tbLambda, "λ");
                if (lambda <= 0) throw new ArgumentException("λ должна быть больше 0.");

                double service = ReadDouble(tbService, "Поток обслуживания");
                if (service <= 0) throw new ArgumentException("Значение потока обслуживания должно быть больше 0.");

                double lu = UnitMinutes[cbLambdaUnit.SelectedIndex];   // единица времени для λ
                double su = UnitMinutes[cbServiceUnit.SelectedIndex];  // единица времени для обслуживания
                string unit = UnitNames[cbLambdaUnit.SelectedIndex];

                // приводим μ к единице времени потока заявок
                double mu;
                if (cbMode.SelectedIndex == 0)
                    mu = service * lu / su;            // μ задана интенсивностью
                else
                    mu = 1.0 / (service * su / lu);    // задано время обслуживания: μ = 1/t

                string result;
                switch (type)
                {
                    case 0:
                        result = SmoCalculator.Loss(n, lambda, mu, unit);
                        break;
                    case 1:
                        int k = (int)ReadDouble(tbK, "k");
                        if (k < 0) throw new ArgumentException("k не может быть отрицательным.");
                        result = SmoCalculator.InfiniteQueue(n, lambda, mu, k, unit);
                        break;
                    default:
                        int m = (int)ReadDouble(tbM, "m");
                        if (m < 0) throw new ArgumentException("m не может быть отрицательным.");
                        result = SmoCalculator.LimitedQueue(n, m, lambda, mu, unit);
                        break;
                }
                tbResult.Text = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tbLambda.Clear();
            tbService.Clear();
            tbResult.Clear();
        }
    }
}